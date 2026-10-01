using Moderator.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Moderator.Windows
{
    public enum WindowDisplayState
    {
        Open,
        Minimized,
        Closed
    }

    public sealed class WindowController : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        private RectTransform rect;
        private RectTransform bounds;
        private RectTransform dragHandle;
        private WindowManager manager;
        private Vector2 pointerOffset;
        private bool dragging;
        private bool maximized;
        private Vector2 restoredSize;
        private Vector2 restoredPosition;
        public string ApplicationId { get; private set; }
        public WindowDisplayState DisplayState { get; private set; } = WindowDisplayState.Open;
        public int LastInteractionOrder { get; private set; }

        public void Initialize(RectTransform windowBounds, RectTransform header, Button minimizeButton, Button maximizeButton,
            Button closeButton, WindowManager windowManager, string applicationId)
        {
            rect = (RectTransform)transform;
            bounds = windowBounds;
            dragHandle = header;
            manager = windowManager;
            ApplicationId = applicationId;
            minimizeButton.onClick.AddListener(Minimize);
            maximizeButton.onClick.AddListener(ToggleMaximize);
            closeButton.onClick.AddListener(Close);
            manager.Register(this);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            DisplayState = WindowDisplayState.Open;
            manager.Register(this);
            manager.Touch(this);
            ClampInside();
        }

        public void Close()
        {
            DisplayState = WindowDisplayState.Closed;
            gameObject.SetActive(false);
            manager.NotifyWindowStateChanged(ApplicationId);
        }

        public void Minimize()
        {
            DisplayState = WindowDisplayState.Minimized;
            manager.Touch(this);
            gameObject.SetActive(false);
            manager.NotifyWindowStateChanged(ApplicationId);
        }

        public void ToggleMaximize()
        {
            if (!maximized)
            {
                restoredSize = rect.sizeDelta;
                restoredPosition = rect.anchoredPosition;
                rect.sizeDelta = new Vector2(Mathf.Max(480, bounds.rect.width - 18), Mathf.Max(360, bounds.rect.height - 54));
                rect.anchoredPosition = new Vector2(0, 21);
            }
            else
            {
                rect.sizeDelta = restoredSize;
                rect.anchoredPosition = restoredPosition;
            }
            maximized = !maximized;
            manager.BringToFront(this);
            ClampInside();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            manager.Touch(this);
            manager.BringToFront(this);
        }

        public void SetInteractionOrder(int order) => LastInteractionOrder = order;

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = RectTransformUtility.RectangleContainsScreenPoint(dragHandle, eventData.position, eventData.pressEventCamera);
            if (!dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, eventData.position, eventData.pressEventCamera, out var point);
            pointerOffset = rect.anchoredPosition - point;
            manager.BringToFront(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, eventData.position, eventData.pressEventCamera, out var point))
            {
                rect.anchoredPosition = point + pointerOffset;
                ClampInside();
            }
        }

        private void ClampInside()
        {
            if (rect == null || bounds == null) return;
            var area = bounds.rect;
            var half = rect.rect.size * .5f;
            rect.anchoredPosition = new Vector2(
                Mathf.Clamp(rect.anchoredPosition.x, area.xMin + half.x, area.xMax - half.x),
                Mathf.Clamp(rect.anchoredPosition.y, area.yMin + half.y, area.yMax - half.y));
        }

        public static WindowController Create(string name, string title, RectTransform desktop, WindowManager manager,
            Vector2 size, Vector2 position, out RectTransform content)
        {
            var browserStyle = name == "BrowserWindow";
            var root = UIFactory.FixedRect(name, desktop, size, position, UIFactory.Charcoal);
            UIFactory.InkOutline(root.gameObject, 4f);

            var shell = UIFactory.SlicedTexturedRect("WindowShell", root,
                browserStyle ? "Sprites/UI/BrowserFrameHand" : "Sprites/UI/SocialFrameHand",
                browserStyle ? new Vector4(120, 120, 120, 120) : new Vector4(130, 130, 130, 130),
                Vector2.zero, Vector2.one, new Vector2(-18, -18), new Vector2(18, 18), Color.white);
            shell.SetAsFirstSibling();

            var header = browserStyle
                ? UIFactory.Rect("TitleBar", root, new Vector2(0, 1), Vector2.one, new Vector2(4, -48), new Vector2(-4, -4), UIFactory.Hex("08356A"))
                : UIFactory.TexturedRect("TitleBar", root, "Sprites/UI/SocialTitleBarHand", new Vector2(0, 1), Vector2.one,
                    new Vector2(4, -48), new Vector2(-4, -4), Color.white);
            UIFactory.InkOutline(header.gameObject, 2f);
            var tab = browserStyle
                ? UIFactory.Rect("ActiveBrowserTab", header, Vector2.zero, new Vector2(0, 1), new Vector2(2, 2), new Vector2(250, -2), UIFactory.Parchment)
                : header;
            if (browserStyle) UIFactory.InkOutline(tab.gameObject, 2f);
            UIFactory.Artwork("AppMark", tab, UIFactory.LoadSprite(browserStyle ? "Sprites/UI/BrowserLogo" : "Sprites/UI/SocialLogo"),
                Vector2.zero, new Vector2(0, 1), new Vector2(7, 5), new Vector2(43, -5));
            var titleText = UIFactory.Text("Title", header, title, 17, browserStyle ? UIFactory.Charcoal : UIFactory.Paper,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(titleText.rectTransform, Vector2.zero, browserStyle ? new Vector2(0, 1) : Vector2.one,
                new Vector2(52, 0), browserStyle ? new Vector2(205, 0) : new Vector2(-150, 0));
            if (browserStyle)
            {
                var tabClose = UIFactory.Text("TabClose", header, "×", 21, UIFactory.Charcoal, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Layout(tabClose.rectTransform, Vector2.zero, new Vector2(0, 1), new Vector2(214, 0), new Vector2(246, 0));
            }

            var controlInset = browserStyle ? 0f : 28f;
            var minimize = UIFactory.SpriteButton("MinimizeDecor", header, "Sprites/UI/WindowMinimize", null, Color.clear, 0);
            UIFactory.Layout((RectTransform)minimize.transform, new Vector2(1, 0), Vector2.one, new Vector2(-137 - controlInset, 3), new Vector2(-95 - controlInset, -3));
            var maximize = UIFactory.SpriteButton("MaximizeDecor", header, "Sprites/UI/WindowMaximize", null, Color.clear, 0);
            UIFactory.Layout((RectTransform)maximize.transform, new Vector2(1, 0), Vector2.one, new Vector2(-93 - controlInset, 3), new Vector2(-51 - controlInset, -3));
            var close = UIFactory.SpriteButton("Close", header, "Sprites/UI/WindowClose", null, Color.clear, 0);
            UIFactory.Layout((RectTransform)close.transform, new Vector2(1, 0), Vector2.one, new Vector2(-49 - controlInset, 3), new Vector2(-7 - controlInset, -3));

            content = UIFactory.Rect("Content", root, Vector2.zero, Vector2.one, new Vector2(7, 7), new Vector2(-7, -55), UIFactory.Ink);
            shell.SetAsLastSibling();
            if (browserStyle) header.SetAsLastSibling();
            var controller = root.gameObject.AddComponent<WindowController>();
            controller.Initialize(desktop, header, minimize, maximize, close, manager, browserStyle ? "browser" : "social");
            return controller;
        }
    }
}
