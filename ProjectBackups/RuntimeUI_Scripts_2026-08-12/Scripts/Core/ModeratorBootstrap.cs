using System;
using Moderator.Desktop;
using Moderator.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Moderator.Core
{
    public sealed class ModeratorBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = Color.black;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.orthographic = true;
            }
            EnsureEventSystem();
            BuildInterface();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null) go.AddComponent(inputModuleType);
            else go.AddComponent<StandaloneInputModule>();
        }

        private static void BuildInterface()
        {
            var canvasObject = new GameObject("ModeratorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            var root = UIFactory.Rect("IllustratedWorkstation", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
            var background = UIFactory.Artwork("WorkstationArt", root, UIFactory.LoadSprite("Art/ModeratorWorkstationV2"), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
            background.type = Image.Type.Simple;

            var ambience = root.gameObject.AddComponent<AmbientBackgroundAnimator>();
            ambience.Build(root);

            // The generated monitor contains a deliberately empty display in this exact normalized region.
            var desktop = UIFactory.Rect("DesktopArea", root, new Vector2(.146f, .211f), new Vector2(.859f, .894f), Vector2.zero, Vector2.zero, UIFactory.Ink);
            desktop.gameObject.AddComponent<RectMask2D>();
            var outline = desktop.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, .9f);
            outline.effectDistance = new Vector2(3, -3);
            desktop.gameObject.AddComponent<DesktopController>().Build(desktop);
        }
    }
}
