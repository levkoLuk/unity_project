using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.UI
{
    public sealed class ManualLayoutPersistence : MonoBehaviour
    {
        [Serializable]
        private sealed class LayoutFile { public List<LayoutEntry> entries = new(); }

        [Serializable]
        private sealed class LayoutEntry
        {
            public string path;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector3 localScale;
            public Vector3 localEulerAngles;
        }

        private readonly Dictionary<string, LayoutEntry> saved = new();
        private int knownTransformCount;
        private int applyFramesRemaining;

        public static string LayoutPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/ModeratorManualLayoutStableV3.json"));

        private IEnumerator Start()
        {
            LoadFile();
            yield return new WaitForEndOfFrame();
            ScheduleApply();
        }

        private void LateUpdate()
        {
            var currentCount = GetComponentsInChildren<RectTransform>(true).Length;
            if (currentCount != knownTransformCount)
            {
                knownTransformCount = currentCount;
                ScheduleApply();
            }
            if (applyFramesRemaining <= 0) return;
            Canvas.ForceUpdateCanvases();
            ApplySavedLayout();
            applyFramesRemaining--;
        }

        public void SaveCurrentLayout()
        {
            var file = new LayoutFile();
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == transform || rect.name.StartsWith("Disposed_") || IsCodeControlled(rect)) continue;
                file.entries.Add(new LayoutEntry
                {
                    path = BuildPath(rect),
                    anchorMin = rect.anchorMin,
                    anchorMax = rect.anchorMax,
                    pivot = rect.pivot,
                    anchoredPosition = rect.anchoredPosition,
                    sizeDelta = rect.sizeDelta,
                    localScale = rect.localScale,
                    localEulerAngles = rect.localEulerAngles
                });
            }
            File.WriteAllText(LayoutPath, JsonUtility.ToJson(file, true));
            LoadFile();
            ScheduleApply();
            Debug.Log("[Manual UI] Layout saved: " + LayoutPath);
        }

        public void ApplySavedLayout()
        {
            if (saved.Count == 0) return;
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (IsCodeControlled(rect)) continue;
                if (!saved.TryGetValue(BuildPath(rect), out var entry) &&
                    !saved.TryGetValue(BuildLegacyPath(rect), out entry)) continue;
                rect.anchorMin = entry.anchorMin;
                rect.anchorMax = entry.anchorMax;
                rect.pivot = entry.pivot;
                rect.anchoredPosition = entry.anchoredPosition;
                rect.sizeDelta = entry.sizeDelta;
                rect.localScale = entry.localScale;
                rect.localEulerAngles = entry.localEulerAngles;
            }
        }

        public static void ClearSavedLayout()
        {
            if (File.Exists(LayoutPath)) File.Delete(LayoutPath);
            Debug.Log("[Manual UI] Saved layout cleared.");
        }

        public static void RequestReapply(Component rebuiltArea)
        {
            var persistence = rebuiltArea != null ? rebuiltArea.GetComponentInParent<ManualLayoutPersistence>() : null;
            if (persistence != null) persistence.ScheduleApply();
        }

        private void LoadFile()
        {
            saved.Clear();
            if (!File.Exists(LayoutPath)) return;
            var file = JsonUtility.FromJson<LayoutFile>(File.ReadAllText(LayoutPath));
            if (file?.entries == null) return;
            foreach (var entry in file.entries) saved[entry.path] = entry;
        }

        private void ScheduleApply() => applyFramesRemaining = Mathf.Max(applyFramesRemaining, 3);

        private static bool IsCodeControlled(RectTransform rect)
        {
            return rect.name == "BrowserWindow" || rect.name == "SocialWindow" || rect.name == "ProfileWindow" ||
                   rect.name == "MinimizeDecor" || rect.name == "MaximizeDecor" || rect.name == "Close";
        }

        private string BuildPath(Transform target)
        {
            var parts = new List<string>();
            while (target != null && target != transform)
            {
                var occurrence = 0;
                for (var index = 0; index < target.GetSiblingIndex(); index++)
                    if (target.parent.GetChild(index).name == target.name) occurrence++;
                parts.Add(target.name + "[" + occurrence + "]");
                target = target.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private string BuildLegacyPath(Transform target)
        {
            var parts = new List<string>();
            while (target != null && target != transform)
            {
                parts.Add(target.name + "[" + target.GetSiblingIndex() + "]");
                target = target.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
