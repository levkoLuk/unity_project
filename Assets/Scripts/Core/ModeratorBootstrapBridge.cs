using UnityEngine;

/// <summary>
/// Lightweight bridge to open/close the in-scene "Moderator Prototype" desktop.
/// Purpose: avoid invasive edits to existing ModeratorBootstrap while providing a safe API.
/// </summary>
public static class ModeratorBootstrapBridge
{
    private static GameObject _desktopInstance;

    /// <summary>
    /// Ensure we have a cached desktop instance (searches by name once).
    /// </summary>
    private static GameObject FindOrCacheDesktop()
    {
        if (_desktopInstance != null) return _desktopInstance;
        // try common approaches
        var go = GameObject.Find("Moderator Prototype");
        if (go != null)
        {
            _desktopInstance = go;
            return _desktopInstance;
        }

        // Search by likely component (class name may vary) - try to find a Canvas with "Moderator" in name
        var allCanvases = GameObject.FindObjectsOfType<Canvas>();
        foreach (var c in allCanvases)
        {
            if (c.gameObject.name.ToLower().Contains("moderator"))
            {
                _desktopInstance = c.gameObject;
                return _desktopInstance;
            }
        }

        return null;
    }

    /// <summary>
    /// Open (activate) the desktop. If it was already active, re-use it.
    /// If not found, returns false.
    /// Safe: does not Instantiate or change scene structure.
    /// </summary>
    public static bool OpenDesktop()
    {
        var desktop = FindOrCacheDesktop();
        if (desktop == null)
        {
            Debug.LogWarning("ModeratorBootstrapBridge: OpenDesktop — 'Moderator Prototype' not found.");
            return false;
        }
        desktop.SetActive(true);
        Debug.Log("ModeratorBootstrapBridge: OpenDesktop re-used existing desktop");
        return true;
    }

    /// <summary>
    /// Hide (deactivate) desktop if present.
    /// </summary>
    public static bool HideDesktop()
    {
        var desktop = FindOrCacheDesktop();
        if (desktop == null)
        {
            Debug.LogWarning("ModeratorBootstrapBridge: HideDesktop — 'Moderator Prototype' not found.");
            return false;
        }
        desktop.SetActive(false);
        return true;
    }
}
