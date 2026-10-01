// Assets/Editor/LLM/LLMToolWindow.cs
#if UNITY_EDITOR
using Project.LLM;
using UnityEditor;
using UnityEngine;
using System.IO;

public class LLMToolWindow : EditorWindow
{
    LocalLLMClient client;
    LLMConfig config;
    string prompt = "Напиши git-style unified diff патч для..."; // default
    string responseText = "";
    string rawJson = "";
    Vector2 scroll;

    [MenuItem("Tools/LLM Tool")]
    public static void ShowWindow()
    {
        var w = GetWindow<LLMToolWindow>("LLM Tool");
        w.minSize = new Vector2(600, 400);
    }

    void OnEnable()
    {
        // Try to find an existing config asset
        var guids = AssetDatabase.FindAssets("t:LLMConfig");
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            config = AssetDatabase.LoadAssetAtPath<LLMConfig>(path);
        }

        // Try to find LocalLLMClient in scene (for edits during Play Mode)
        client = FindObjectOfType<LocalLLMClient>();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("LLM Tool (Local / Self-hosted)", EditorStyles.boldLabel);
        config = (LLMConfig)EditorGUILayout.ObjectField("LLM Config", config, typeof(LLMConfig), false);

        if (config == null)
        {
            EditorGUILayout.HelpBox("Create or assign an LLMConfig ScriptableObject. Right-click in Project -> Create -> LLM -> Config", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prompt", EditorStyles.label);
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(140));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Send Prompt"))
        {
            SendPrompt();
        }
        if (GUILayout.Button("Clear"))
        {
            prompt = "";
            responseText = "";
            rawJson = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generated text (parsed):", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(140));
        EditorGUILayout.TextArea(responseText);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Raw JSON response:", EditorStyles.boldLabel);
        var rawScroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(120));
        EditorGUILayout.TextArea(rawJson);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save as .diff") && !string.IsNullOrEmpty(responseText))
        {
            SaveDiffDialog(responseText);
        }
        if (GUILayout.Button("Copy to Clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = responseText;
        }
        EditorGUILayout.EndHorizontal();
    }

    void SendPrompt()
    {
        if (config == null)
        {
            EditorUtility.DisplayDialog("LLM Tool", "Assign an LLMConfig asset first.", "OK");
            return;
        }

        // Ensure a LocalLLMClient exists in scene during Play Mode. In Edit mode we still can instantiate a temp GameObject.
        if (client == null)
        {
            var go = new GameObject("LLMClient_EditorRuntime");
            client = go.AddComponent<LocalLLMClient>();
            client.config = config;
            // set hide flags so it doesn't persist badly
            go.hideFlags = HideFlags.DontSave;
        }

        responseText = "Waiting...";
        rawJson = "";

        client.Ask(prompt, (generated, raw) =>
        {
            responseText = generated;
            rawJson = raw;
            Repaint();
        }, (err) =>
        {
            responseText = "";
            rawJson = err;
            Repaint();
        });
    }

    void SaveDiffDialog(string content)
    {
        string path = EditorUtility.SaveFilePanel("Save patch as .diff", Application.dataPath, "patch.diff", "diff");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            File.WriteAllText(path, content);
            EditorUtility.RevealInFinder(path);
            EditorUtility.DisplayDialog("Saved", "Saved patch:\n" + path, "OK");
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Save Failed", ex.Message, "OK");
        }
    }
}
#endif