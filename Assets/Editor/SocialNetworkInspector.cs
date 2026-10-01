#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility: scan loaded assemblies for SocialNetwork related types,
/// and print their fields/properties/methods useful for Stage3 (queue, posts, author, delete/block).
/// Use: Menu -> Tools -> Social -> Inspect SocialNetwork Types
/// </summary>
public static class SocialNetworkInspector
{
    [MenuItem("Tools/Social/Inspect SocialNetwork Types")]
    public static void Inspect()
    {
        Debug.Log("=== SocialNetworkInspector: scanning assemblies for candidate types ===");
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                                .Where(a => !a.FullName.StartsWith("Unity") && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("Microsoft"))
                                .ToArray();

        foreach (var asm in assemblies.OrderBy(a => a.GetName().Name))
        {
            Type[] types = null;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

            foreach (var t in types)
            {
                if (t == null) continue;
                var name = t.Name.ToLowerInvariant();
                // heuristic: likely candidates
                if (name.Contains("social") || name.Contains("post") || name.Contains("profile") || name.Contains("user") || name.Contains("facenet") || name.Contains("network"))
                {
                    PrintTypeSummary(t);
                }
            }
        }

        Debug.Log("=== SocialNetworkInspector: scan complete ===");
    }

    static void PrintTypeSummary(Type t)
    {
        try
        {
            Debug.LogFormat("Type: {0} (assembly: {1})", t.FullName, t.Assembly.GetName().Name);

            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                          .Where(f => !f.IsPrivate || f.IsPublic || f.IsFamily).ToArray();
            foreach (var f in fields)
            {
                Debug.LogFormat("  Field: {0} : {1} (attrs: {2})", f.Name, f.FieldType.Name, f.IsPublic ? "public" : "non-public");
            }

            var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                         .Where(p => p.GetIndexParameters().Length == 0).ToArray();
            foreach (var p in props)
            {
                Debug.LogFormat("  Prop: {0} : {1} (get:{2} set:{3})", p.Name, p.PropertyType.Name, p.CanRead, p.CanWrite);
            }

            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                           .Where(m => !m.IsSpecialName && m.GetParameters().Length <= 2).ToArray();
            foreach (var m in methods.Where(m => m.Name.ToLower().Contains("open") || m.Name.ToLower().Contains("show") ||
                                                m.Name.ToLower().Contains("delete") || m.Name.ToLower().Contains("block") ||
                                                m.Name.ToLower().Contains("post") || m.Name.ToLower().Contains("profile")))
            {
                var ps = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
                Debug.LogFormat("  Method: {0}({1}) : {2}", m.Name, ps, m.ReturnType.Name);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("SocialNetworkInspector: failed to inspect type " + t.Name + " : " + ex.Message);
        }
    }
}
#endif
