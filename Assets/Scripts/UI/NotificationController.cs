using UnityEngine;
using UnityEngine.UI;

namespace Moderator.UI
{
    public sealed class NotificationController : MonoBehaviour
    {
        private CanvasGroup group;
        private Text label;
        private RectTransform toast;
        private float hideAt;

        public void Initialize(RectTransform desktop)
        {
            var rect = UIFactory.Rect("Toast", desktop, new Vector2(1, 1), Vector2.one, new Vector2(-330, -82), new Vector2(-22, -24), UIFactory.Raised);
            toast = rect;
            group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            UIFactory.Rect("Accent", rect, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(5, 0), UIFactory.Blue);
            label = UIFactory.Text("Message", rect, string.Empty, 15, UIFactory.Paper, TextAnchor.MiddleLeft);
            UIFactory.Layout(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 4), new Vector2(-10, -4));
        }

        public void Show(string message)
        {
            label.text = message;
            group.alpha = 1;
            hideAt = Time.unscaledTime + 2.1f;
            toast.SetAsLastSibling();
        }

        private void Update()
        {
            if (group == null || group.alpha <= 0) return;
            if (Time.unscaledTime > hideAt) group.alpha = Mathf.MoveTowards(group.alpha, 0, Time.unscaledDeltaTime * 3f);
        }
    }
}
