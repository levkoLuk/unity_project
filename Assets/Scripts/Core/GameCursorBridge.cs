using UnityEngine;
using System;

/// <summary>
/// Small helper to set cursor mode for UI vs World.
/// Tries to forward to an existing GameCursor instance (if it defines such methods),
/// otherwise falls back to directly manipulating Cursor.lockState and Cursor.visible.
/// </summary>
public static class GameCursorBridge
{
    /// <summary>
    /// Set cursor visible and unlocked for UI interactions.
    /// </summary>
    public static void SetCursorForUI()
    {
        try
        {
            // Best-effort: look for a component named GameCursor and call SetCursorForUI
            var all = GameObject.FindObjectsOfType<MonoBehaviour>();
            foreach (var m in all)
            {
                var tname = m.GetType().Name;
                if (string.Equals(tname, "GameCursor", StringComparison.OrdinalIgnoreCase))
                {
                    var mi = m.GetType().GetMethod("SetCursorForUI");
                    if (mi != null)
                    {
                        mi.Invoke(m, null);
                        return;
                    }
                }
            }
        }
        catch (Exception)
        {
            // swallow and fallback
        }

        // Fallback:
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("GameCursorBridge: SetCursorForUI (fallback)");
    }

    /// <summary>
    /// Lock and hide cursor for world control.
    /// </summary>
    public static void SetCursorForWorld()
    {
        try
        {
            var all = GameObject.FindObjectsOfType<MonoBehaviour>();
            foreach (var m in all)
            {
                var tname = m.GetType().Name;
                if (string.Equals(tname, "GameCursor", StringComparison.OrdinalIgnoreCase))
                {
                    var mi = m.GetType().GetMethod("SetCursorForWorld");
                    if (mi != null)
                    {
                        mi.Invoke(m, null);
                        return;
                    }
                }
            }
        }
        catch (Exception)
        {
            // swallow and fallback
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("GameCursorBridge: SetCursorForWorld (fallback)");
    }
}
