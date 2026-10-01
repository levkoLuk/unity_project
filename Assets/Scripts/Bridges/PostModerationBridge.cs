using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-invasive bridge: find existing Delete/Block UI and moderation methods and
/// call QuestManager.CompleteQuest("quest_01_why_alex") when a real moderation action occurs.
/// Designed for Stage-2 vertical slice (best-effort wiring).
/// Uses reflection to avoid compile-time dependency on QuestManager type.
/// </summary>
[DefaultExecutionOrder(500)]
public class PostModerationBridge : MonoBehaviour
{
    // Quest id to complete for the first investigation
    public string firstQuestId = "quest_01_why_alex";

    // cached reflection objects for QuestManager
    static object _questManagerInstance;
    static MethodInfo _questCompleteMethod;
    static bool _questReflectionInitialized = false;
    static readonly object _qrLock = new object();

    // scan once at Start
    void Start()
    {
        TryInitQuestReflection(); // attempt to init early
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
                    // If method is public and takes no parameters, add a proxy invoker for easy wiring.
                    if (mi.GetParameters().Length == 0 && mi.IsPublic)
                    {
                        var go = m.gameObject;
                        var proxy = go.AddComponent<ModerationProxyInvoker>();
                        proxy.SetTarget(m, mi, firstQuestId);
                        boundMethods++;
                        break;
                    }
                    else
                    {
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

    /// <summary>
    /// Initialize reflection references to QuestManager and its CompleteQuest(string) method.
    /// Safe no-op if QuestManager type not found.
    /// </summary>
    static void TryInitQuestReflection()
    {
        if (_questReflectionInitialized) return;
        lock (_qrLock)
        {
            if (_questReflectionInitialized) return;
            try
            {
                // look across all loaded assemblies for a type named "QuestManager"
                var type = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a =>
                            {
                                try { return a.GetTypes(); } catch { return new Type[0]; }
                            })
                            .FirstOrDefault(t => string.Equals(t.Name, "QuestManager", StringComparison.Ordinal));
                if (type == null)
                {
                    _questReflectionInitialized = true; // avoid repeated costly searches
                    Debug.Log("PostModerationBridge: QuestManager type not found via reflection.");
                    return;
                }

                // try find static Instance property or field
                var pi = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null)
                {
                    _questManagerInstance = pi.GetValue(null);
                }
                else
                {
                    var fi = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi != null)
                        _questManagerInstance = fi.GetValue(null);
                }

                // try to find CompleteQuest(string) method
                _questCompleteMethod = type.GetMethod("CompleteQuest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
                if (_questCompleteMethod == null)
                {
                    // fallback: search any method named CompleteQuest with 1 parameter
                    _questCompleteMethod = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                               .FirstOrDefault(m => m.Name == "CompleteQuest" && m.GetParameters().Length == 1);
                }

                _questReflectionInitialized = true;
                Debug.Log($"PostModerationBridge: QuestManager reflection init: type={(type!=null?type.FullName:"null")}, instance={_questManagerInstance!=null}, method={_questCompleteMethod!=null}");
            }
            catch (Exception ex)
            {
                _questReflectionInitialized = true;
                Debug.LogWarning("PostModerationBridge: reflection init failed: " + ex);
            }
        }
    }

    /// <summary>
    /// Try to complete quest via reflection. No-ops if QuestManager not available.
    /// </summary>
    public static void TryCompleteQuest(string questId)
    {
        TryInitQuestReflection();
        if (_questCompleteMethod == null)
        {
            // couldn't find method (maybe QuestManager missing); log minimally
            // Debug.Log("PostModerationBridge: cannot complete quest — method not found.");
            return;
        }

        // if instance null, try to get it again (some assemblies may initialize later)
        if (_questManagerInstance == null)
        {
            // if property exists on type, re-fetch
            var type = _questCompleteMethod.DeclaringType;
            var pi = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null) _questManagerInstance = pi.GetValue(null);
            else
            {
                var fi = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null) _questManagerInstance = fi.GetValue(null);
            }
        }

        try
        {
            if (_questManagerInstance != null)
            {
                _questCompleteMethod.Invoke(_questManagerInstance, new object[] { questId });
                Debug.Log($"PostModerationBridge: Completed quest (via reflection) {questId}");
            }
            else
            {
                // maybe CompleteQuest is static
                if (_questCompleteMethod.IsStatic)
                {
                    _questCompleteMethod.Invoke(null, new object[] { questId });
                    Debug.Log($"PostModerationBridge: Completed quest (static method) {questId}");
                }
                else
                {
                    // no instance available; give up silently
                    // Debug.Log("PostModerationBridge: QuestManager instance not available yet.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PostModerationBridge: CompleteQuest invoke failed: " + ex);
        }
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
        try
        {
            // Try to complete via reflection (safe if QuestManager not compiled here)
            PostModerationBridge.TryCompleteQuest(questId);
            Debug.Log($"ModerationButtonProxy: attempted to complete quest {questId} after button {target.gameObject.name} clicked.");
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
            PostModerationBridge.TryCompleteQuest(questId);
            Debug.Log($"ModerationProxyInvoker: attempted to complete quest {questId} after invoking {targetMB?.GetType().Name}.{targetMethod?.Name}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("ModerationProxyInvoker: completing quest failed: " + ex);
        }
    }
}
