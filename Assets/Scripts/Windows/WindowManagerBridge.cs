using System;
using UnityEngine;

/// <summary>
/// Helper that tries to close the top-most UI window via an existing WindowManager instance.
/// It uses reflection to safely call CloseTopWindow if available and returns true when a window was closed.
/// </summary>
public static class WindowManagerBridge
{
    /// <summary>
    /// Attempt to close top window. Returns true if a window was closed.
    /// Safe: returns false if no WindowManager or method not found.
    /// </summary>
    public static bool CloseTopWindow()
    {
        try
        {
            // Try find a MonoBehaviour named WindowManager
            var all = GameObject.FindObjectsOfType<MonoBehaviour>();
            foreach (var m in all)
            {
                var t = m.GetType();
                if (string.Equals(t.Name, "WindowManager", StringComparison.OrdinalIgnoreCase))
                {
                    // Try method CloseTopWindow()
                    var mi = t.GetMethod("CloseTopWindow");
                    if (mi != null)
                    {
                        var res = mi.Invoke(m, null);
                        if (res is bool b) return b;
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("WindowManagerBridge.CloseTopWindow failed: " + ex.Message);
        }
        return false;
    }
}
