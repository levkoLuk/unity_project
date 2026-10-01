using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-invasive bridge: find existing Delete/Block UI and moderation methods and
/// call QuestManager.CompleteQuest("quest_01_why_alex") when a real moderation action occurs.
/// Designed for Stage-2 vertical slice (best-effort wiring).
/// </summary>
[DefaultExecutionOrder(500)]
public class PostModerationBridge : MonoBehaviour
{
    // Quest id to complete for the first investigation
    public string firstQuestId = "quest_01_why_alex";

    // scan once at Start
    void Start()
    {
        try
        {
            ScanSceneAndBind();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PostModerationBridge: failed to scan/bind: {ex}");
        }
    }

    void ScanSceneAndBind()
    {
        var buttons = FindObjectsOfType<Button>();
        int boundCount = 0;

        // common button name tokens we care about
        string[] deleteTokens = new string[] { "delete", "del", "remove" };
        string[] blockTokens = new string[] { "block", "ban" };

        foreach (var b in buttons)
        {
            var name = b.gameObject.name.ToLowerInvariant();
            bool looksLikeDelete = deleteTokens.Any(t => name.Contains(t));
            bool looksLikeBlock = blockTokens.Any(t => name.Contains(t));
            if (!looksLikeDelete && !looksLikeBlock) continue;

            // guard: avoid double-subscribe
            // we attach a wrapper that calls original onClick and then our hook
            AttachClickProxy(b, looksLikeDelete || looksLikeBlock);
            boundCount++;
        }

        // Try to bind to MonoBehaviours with known moderation method names
        var allMB = FindObjectsOfType<MonoBehaviour>();
        string[] methodNames = new string[] { "DeletePost", "OnDelete", "Delete", "PerformDelete", "RemovePost",
                                              "BlockUser", "BlockPost", "OnBlock", "PerformBlock", "BanUser" };

        int boundMethods = 0;
        foreach (var m in allMB)
        {
            var t = m.GetType();
            foreach (var mn in methodNames)
            {
                var mi = t.GetMethod(mn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi != null)
                {
                    // wrap via a small proxy if possible (try to attach via Delegate hooking is risky),
                    // instead we create a small helper component to intercept calls if they call a public event.
                    // As fallback, if method is public and takes no parameters, we create a Button proxy on same GO.
                    if (mi.GetParameters().Length == 0 && mi.IsPublic)
                    {
                        // create a temporary Button proxy to call method then our hook
                        var go = m.gameObject;
                        var proxy = go.AddComponent<ModerationProxyInvoker>();
                        proxy.SetTarget(m, mi, firstQuestId);
                        boundMethods++;
                        break;
                    }
                    else
                    {
                        // If method has signature (string postId) or similar, we can't automatically know postId here.
                        // We'll still log the presence for manual integration.
                        Debug.Log($"PostModerationBridge: found moderation method {t.Name}.{mn} (params: {mi.GetParameters().Length}) — consider calling QuestManager from that code.");
                    }
                }
            }
        }

        Debug.Log($"PostModerationBridge: bound {boundCount} buttons and {boundMethods} method proxies (best-effort).");
        if (boundCount == 0 && boundMethods == 0)
        {
            Debug.Log("PostModerationBridge: no obvious moderation buttons/methods found. You can call QuestManager.Instance.CompleteQuest(...) manually from PostView or wire buttons named Delete/Block.");
        }
    }

    void AttachClickProxy(Button b, bool isModeration)
    {
        // Avoid attaching twice
        if (b.gameObject.GetComponent<ModerationButtonProxy>() != null) return;

        var proxy = b.gameObject.AddComponent<ModerationButtonProxy>();
        proxy.Setup(b, firstQuestId);
    }
}

/// <summary>
/// Adds a proxy on the same GameObject as a Button: when clicked,
/// it runs existing onClick handlers and then triggers QuestManager completion.
/// </summary>
public class ModerationButtonProxy : MonoBehaviour
{
    Button target;
    string questId;

    public void Setup(Button button, string questToComplete)
    {
        target = button;
        questId = questToComplete;

        // remove previous proxy listener to avoid duplicates then add ours
        target.onClick.AddListener(OnClickedProxy);
    }

    void OnDestroy()
    {
        if (target != null)
            target.onClick.RemoveListener(OnClickedProxy);
    }

    void OnClickedProxy()
    {
        // Delay our hook slightly to let original handlers run (they are invoked first),
        // but we still call CompleteQuest regardless (idempotent).
        try
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CompleteQuest(questId);
                Debug.Log($"ModerationButtonProxy: Completed quest {questId} after button {target.gameObject.name} clicked.");
            }
            else
            {
                Debug.LogWarning("ModerationButtonProxy: QuestManager not present; cannot complete quest.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("ModerationButtonProxy: Exception when completing quest: " + ex);
        }
    }
}

/// <summary>
/// Simple invoker for monoBehaviour methods (no args) — used where method exists but no clear UI hook.
/// It allows those methods to be called and also to trigger QuestManager when invoked.
/// Attach via SetTarget at runtime.
/// </summary>
public class ModerationProxyInvoker : MonoBehaviour
{
    MonoBehaviour targetMB;
    MethodInfo targetMethod;
    string questId;

    public void SetTarget(MonoBehaviour mb, MethodInfo mi, string questToComplete)
    {
        targetMB = mb;
        targetMethod = mi;
        questId = questToComplete;
    }

    // Public method that UI or other code can call instead of calling targetMethod directly.
    public void InvokeTargetAndComplete()
    {
        try
        {
            targetMethod.Invoke(targetMB, null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("ModerationProxyInvoker: target invoke failed: " + ex);
        }

        try
        {
            QuestManager.Instance?.CompleteQuest(questId);
            Debug.Log($"ModerationProxyInvoker: Completed quest {questId} after invoking {targetMB?.GetType().Name}.{targetMethod?.Name}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("ModerationProxyInvoker: completing quest failed: " + ex);
        }
    }
}
