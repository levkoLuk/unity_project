using Moderator.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Moderator.Core
{
    public sealed class GameCursor : MonoBehaviour
    {
        private RectTransform cursorRect;

        public void Build(RectTransform root)
        {
            Cursor.visible = false;
            var image = UIFactory.Artwork("GameCursor", root, UIFactory.LoadSprite("Art/Story/cursor"),
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, true);
            image.raycastTarget = false;
            cursorRect = image.rectTransform;
            cursorRect.anchorMin = cursorRect.anchorMax = Vector2.zero;
            cursorRect.pivot = new Vector2(.31f, .94f);
            cursorRect.sizeDelta = new Vector2(50, 37);
            cursorRect.SetAsLastSibling();
        }

        private void LateUpdate()
        {
            if (cursorRect == null || Mouse.current == null) return;
            cursorRect.position = Mouse.current.position.ReadValue();
            cursorRect.SetAsLastSibling();
        }

        private void OnDisable() => Cursor.visible = true;
        private void OnDestroy() => Cursor.visible = true;
    }
}
