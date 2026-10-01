using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime integration for PostView: find PostView instances (by type name)
/// and attach a listener to Delete/Block buttons that calls
/// PostModerationBridge.TryCompleteQuest("quest_01_why_alex") after user action.
/// Non-invasive: does not modify original PostView source.
/// </summary>
[DefaultExecutionOrder(600)]
public class PostViewIntegration : MonoBehaviour
{
    // tokens to search for button names
    static readonly string[] deleteTokens = new[] { "delete", "del", "remove", "trash", "deletebutton" };
    static readonly string[] blockTokens  = new[] { "block", "ban", "blockbutton", "banbutton" };

    // quest id
    public string questId = "quest_01_why_alex";

    // optional: do one-time scan at Start
    void Start()
    {
        try
        {
            ScanAndAttach();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PostViewIntegration: ScanAndAttach failed: {ex}");
        }
    }

    void ScanAndAttach()
    {
        // Find all objects with a component whose type name contains "PostView"
        var allMB = GameObject.FindObjectsOfType<MonoBehaviour>();
        var postViews = new List<MonoBehaviour>();
        foreach (var mb in allMB)
        {
            if (mb == null) continue;
            var t = mb.GetType();
            if (t.Name.IndexOf("PostView", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                postViews.Add(mb);
            }
        }

        if (postViews.Count == 0)
        {
            Debug.Log("PostViewIntegration: no PostView components found in scene.");
            return;
        }

        int attached = 0;
        foreach (var pv in postViews)
        {
            // search for Button components on the same GameObject and its children
            var buttons = pv.gameObject.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b == null || b.gameObject == null) continue;
                var name = b.gameObject.name.ToLowerInvariant();

                bool isDelete = deleteTokens.Any(tok => name.Contains(tok));
                bool isBlock  = blockTokens.Any(tok => name.Contains(tok));

                if (!isDelete && !isBlock)
                {
                    // also try to inspect target methods already wired to button (if any), since names might be generic
                    // if the button's GameObject has a component with a method name containing "Delete" or "Block", prefer that
                    var comps = b.gameObject.GetComponents<MonoBehaviour>();
                    foreach (var c in comps)
                    {
                        if (c == null) continue;
                        var mt = c.GetType();
                        var mnames = mt.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                       .Select(m => m.Name.ToLowerInvariant());
                        if (mnames.Any(n => n.Contains("delete"))) isDelete = true;
                        if (mnames.Any(n => n.Contains("block") || n.Contains("ban"))) isBlock = true;
                    }
                }

                if (isDelete || isBlock)
                {
                    // avoid double attach
                    var marker = b.gameObject.GetComponent<PostViewIntegrationMarker>();
                    if (marker == null)
                    {
                        b.onClick.AddListener(() => OnModerationClicked(b));
                        b.gameObject.AddComponent<PostViewIntegrationMarker>();
                        attached++;
                    }
                }
            }

            // Additionally, some PostView implementations don't use Buttons but call methods directly.
            // Try to find methods on the PostView that look like Delete/Block and create a small invoker on the same GameObject
            var tinfo = pv.GetType();
            var methodCandidates = tinfo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(m => (m.Name.IndexOf("delete", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     m.Name.IndexOf("block", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     m.Name.IndexOf("ban", StringComparison.OrdinalIgnoreCase) >= 0) &&
                                                     m.GetParameters().Length == 0).ToArray();
            foreach (var mi in methodCandidates)
            {
                // attach invoker to the same GO
                var inv = pv.gameObject.AddComponent<PostViewMethodInvoker>();
                inv.Configure(pv, mi, questId);
                attached++;
            }
        }

        Debug.Log($"PostViewIntegration: found {postViews.Count} PostView(s) and attached listeners for {attached} moderation entrypoints.");
    }

    void OnModerationClicked(Button b)
    {
        try
        {
            // Use PostModerationBridge reflection helper to complete quest safely
            PostModerationBridge.TryCompleteQuest(questId);
            Debug.Log($"PostViewIntegration: invoked TryCompleteQuest after button click ({b.gameObject.name})");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PostViewIntegration: OnModerationClicked failed: " + ex);
        }
    }
}

/// <summary>
/// Marker component to avoid double-attachment to same GO.
/// </summary>
public class PostViewIntegrationMarker : MonoBehaviour { }

/// <summary>
/// Invoker that exposes a public method InvokeAndComplete which calls target method and completes quest.
/// It's safe to call from UI wiring if needed.
/// </summary>
public class PostViewMethodInvoker : MonoBehaviour
{
    MonoBehaviour target;
    MethodInfo method;
    string questId;

    public void Configure(MonoBehaviour targetMB, MethodInfo mi, string qid)
    {
        target = targetMB;
        method = mi;
        questId = qid;
    }

    // public method that can be wired to UI or called from other code via SendMessage/etc.
    public void InvokeAndComplete()
    {
        try
        {
            method.Invoke(target, null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PostViewMethodInvoker: target invoke failed: " + ex);
        }

        try
        {
            PostModerationBridge.TryCompleteQuest(questId);
            Debug.Log($"PostViewMethodInvoker: attempted to complete quest {questId} after invoking {target?.GetType().Name}.{method?.Name}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PostViewMethodInvoker: completing quest failed: " + ex);
        }
    }
}
