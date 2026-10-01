#if UNITY_EDITOR
using Moderator.UI;
using UnityEditor;
using UnityEngine;

namespace Moderator.EditorTools
{
    [InitializeOnLoad]
    public static class ModeratorManualLayoutMenu
    {
        static ModeratorManualLayoutMenu()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;
            var persistence = Object.FindAnyObjectByType<ManualLayoutPersistence>();
            if (persistence != null) persistence.SaveCurrentLayout();
        }

        [MenuItem("Moderator UI/Save Current Play Mode Layout", false, 1)]
        private static void SaveLayout()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Moderator UI", "Сначала включи Play Mode, измени элементы и затем вызови эту команду.", "OK");
                return;
            }
            var persistence = Object.FindAnyObjectByType<ManualLayoutPersistence>();
            if (persistence == null) return;
            persistence.SaveCurrentLayout();
            EditorUtility.DisplayDialog("Moderator UI", "Визуальная разметка сохранена. При выходе из Play Mode она также сохраняется автоматически.", "OK");
        }

        [MenuItem("Moderator UI/Apply Saved Layout Now", false, 2)]
        private static void ApplyLayoutNow()
        {
            var persistence = Object.FindAnyObjectByType<ManualLayoutPersistence>();
            if (persistence != null) persistence.ApplySavedLayout();
        }

        [MenuItem("Moderator UI/Reset Saved Layout", false, 20)]
        private static void ResetLayout()
        {
            if (!EditorUtility.DisplayDialog("Moderator UI", "Удалить всю сохранённую ручную разметку?", "Удалить", "Отмена")) return;
            ManualLayoutPersistence.ClearSavedLayout();
        }
    }
}
#endif
