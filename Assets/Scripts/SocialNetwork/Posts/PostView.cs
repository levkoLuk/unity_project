using Moderator.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moderator.SocialNetwork.Posts
{
    public sealed class PostView : MonoBehaviour
    {
        private PostData data;
        private UserData owner;
        private UnityAction<string> notify;
        private UnityAction<PostData> saved;
        private UnityAction<PostData> deleted;
        private Text counters;
        private Text moderationStatus;
        private Text deleteLabel;
        private Button deleteButton;
        private Image cardBackground;
        private bool deleteConfirmation;

        public void Build(RectTransform root, PostData post, UserData postOwner, UnityAction openProfile,
            UnityAction<string> notification, UnityAction<string> search, UnityAction<PostData> onSaved = null, UnityAction<PostData> onDeleted = null)
        {
            data = post;
            owner = postOwner;
            notify = notification;
            saved = onSaved;
            deleted = onDeleted;
            var card = UIFactory.TexturedRect("PostCard", root, "Art/UIPaperTexture", Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5), Color.white);
            cardBackground = card.GetComponent<Image>();
            UIFactory.Shadow(card.gameObject, new Color(0, 0, 0, .35f), new Vector2(4, -5));
            var cardOutline = card.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = UIFactory.Hex("1B1511");
            cardOutline.effectDistance = new Vector2(2, -2);
            var avatar = UIFactory.Button("Avatar", card, owner.avatar == null ? Initial(owner) : string.Empty, 25, UIFactory.Blue, Color.white, openProfile);
            UIFactory.Layout((RectTransform)avatar.transform, Vector2.up, Vector2.up, new Vector2(18, -86), new Vector2(82, -22));
            if (owner.avatar != null) UIFactory.Artwork("Portrait", avatar.transform, owner.avatar, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            var identity = UIFactory.Button("Identity", card, post.displayName + (owner.isVerified ? "  <color=#2477B8>✓</color>" : "") + "  <color=#6B5B49>" + post.username + "</color>", 16, Color.clear, UIFactory.Charcoal, openProfile);
            UIFactory.Layout((RectTransform)identity.transform, new Vector2(0, 1), Vector2.one, new Vector2(94, -62), new Vector2(-130, -22));
            identity.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            var time = UIFactory.Text("Time", card, post.timestamp, 13, UIFactory.Hex("6B5B49"), TextAnchor.MiddleRight);
            UIFactory.Layout(time.rectTransform, Vector2.one, Vector2.one, new Vector2(-130, -62), new Vector2(-18, -22));
            var more = UIFactory.Text("More", card, "•••", 19, UIFactory.Charcoal, TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Layout(more.rectTransform, Vector2.one, Vector2.one, new Vector2(-82, -64), new Vector2(-18, -24));
            var bodyHost = UIFactory.Rect("Body", card, new Vector2(0, 1), Vector2.one, new Vector2(20, -124), new Vector2(-20, -78));
            LinkedPostText.Build(bodyHost, post, 17, search, true);

            var image = UIFactory.Rect("PostImage", card, new Vector2(0, .25f), Vector2.one, new Vector2(20, 4), new Vector2(-20, -134), UIFactory.Hex("0D1115"));
            if (post.image != null)
            {
                var photo = UIFactory.Rect("Photo", image, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5), Color.white);
                var photoImage = photo.GetComponent<Image>();
                photoImage.sprite = post.image;
                photoImage.preserveAspect = true;
            }

            var actions = UIFactory.TexturedRect("ModeratorActions", card, "Art/UIInkTexture", Vector2.zero, new Vector2(1, .245f), new Vector2(20, 14), new Vector2(-20, -4), Color.white);
            var actionTitle = UIFactory.Text("ActionTitle", actions, "ДЕЙСТВИЯ МОДЕРАТОРА", 11, UIFactory.Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Layout(actionTitle.rectTransform, new Vector2(0, .62f), new Vector2(.35f, 1), new Vector2(2, 0), Vector2.zero);
            var delete = UIFactory.PaintedButton("DeletePost", actions, string.Empty, "Sprites/UI/ButtonRedHand", 12, UIFactory.Charcoal, DeletePost);
            UIFactory.Layout((RectTransform)delete.transform, Vector2.zero, new Vector2(.38f, .58f), Vector2.zero, Vector2.zero);
            deleteLabel = delete.GetComponentInChildren<Text>();
            deleteButton = delete;
            counters = UIFactory.Text("Counters", actions, string.Empty, 13, UIFactory.Muted, TextAnchor.MiddleRight);
            UIFactory.Layout(counters.rectTransform, new Vector2(.42f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            moderationStatus = UIFactory.Text("ModerationStatus", actions, string.Empty, 11, UIFactory.Muted, TextAnchor.UpperRight);
            UIFactory.Layout(moderationStatus.rectTransform, new Vector2(.35f, .62f), Vector2.one, Vector2.zero, Vector2.zero);

            Refresh();
        }

        private void DeletePost()
        {
            if (data.deleted) return;
            if (!deleteConfirmation)
            {
                deleteConfirmation = true;
                notify("Нажмите «ПОДТВЕРДИТЬ УДАЛЕНИЕ»");
            }
            else
            {
                data.deleted = true;
                deleted?.Invoke(data);
                deleteConfirmation = false;
                notify("Пост удалён. Действие внесено в историю модерации");
            }
            Refresh();
        }

        private void Refresh()
        {
            deleteLabel.text = data.deleted ? "УДАЛЕНО" : deleteConfirmation ? "ПОДТВЕРДИТЬ УДАЛЕНИЕ" : "УДАЛИТЬ ПОСТ";
            counters.text = "♥ " + data.likes + "   ● " + data.comments + "   ↗ " + data.shares;
            moderationStatus.text = data.saved ? "СОХРАНЕНО В ДЕЛО" : string.Empty;
            deleteButton.interactable = !data.deleted;
            if (cardBackground != null) cardBackground.color = data.deleted ? UIFactory.Hex("B94C50") : Color.white;
        }

        private static string Initial(UserData user) => string.IsNullOrEmpty(user.displayName) ? "?" : user.displayName.Substring(0, 1).ToUpperInvariant();
    }
}
