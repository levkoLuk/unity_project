#if UNITY_EDITOR
using Moderator.Core;
using UnityEditor;

namespace Moderator.EditorTools
{
    public static class StarterInvestigationSmokeMenu
    {
        [MenuItem("Moderator UI/Run Starter Investigation Smoke Test")]
        public static void Run()
        {
            StarterInvestigationSmokeProbe.RunOnStart = true;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }
    }
}
#endif
