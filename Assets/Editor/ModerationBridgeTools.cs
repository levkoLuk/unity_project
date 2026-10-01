#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;

public static class ModerationBridgeTools
{
    [MenuItem("Tools/Moderation/Find moderation buttons in scene")]
    public static void FindButtons()
    {
        var buttons = Object.FindObjectsOfType<Button>();
        int i = 0;
        foreach (var b in buttons)
        {
            var n = b.gameObject.name.ToLowerInvariant();
            if (n.Contains("delete") || n.Contains("block") || n.Contains("ban") || n.Contains("remove"))
            {
                Debug.Log($"Found candidate button: {b.gameObject.name} (path: {GetPath(b.transform)})");
                i++;
            }
        }
        if (i == 0) Debug.Log("No obvious moderation buttons found in scene.");
        else Debug.Log($"Found {i} candidate moderation buttons.");
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
