#if UNITY_EDITOR
using Moderator.UI;
using UnityEditor;
using UnityEngine;

namespace Moderator.EditorTools
{
    public static class ModeratorManualLayoutMenu
    {
        [MenuItem("Moderator UI/Save Current Play Mode Layout", false, 1)]
        private static void SaveLayout()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Moderator UI", "Сначала включи Play Mode, передвинь элементы и затем вызови эту команду.", "OK");
                return;
            }
            var persistence = Object.FindAnyObjectByType<ManualLayoutPersistence>();
            if (persistence == null) return;
            persistence.SaveCurrentLayout();
            EditorUtility.DisplayDialog("Moderator UI", "Расположение сохранено и восстановится при следующем запуске.", "OK");
        }

        [MenuItem("Moderator UI/Reset Saved Layout", false, 20)]
        private static void ResetLayout()
        {
            if (!EditorUtility.DisplayDialog("Moderator UI", "Удалить сохранённую ручную раскладку?", "Удалить", "Отмена")) return;
            ManualLayoutPersistence.ClearSavedLayout();
        }
    }
}
#endif
