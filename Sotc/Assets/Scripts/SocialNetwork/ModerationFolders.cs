using System.Collections.Generic;
using Moderator.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Moderator.SocialNetwork
{
    public sealed class ModerationFolders : MonoBehaviour
    {
        private sealed class Folder
        {
            public string Name;
            public readonly List<PostData> Posts = new();
        }

        private readonly List<Folder> folders = new();
        private RectTransform sidebar;
        private RectTransform contentHost;
        private NotificationController notifications;
        private UnityAction<RectTransform, PostData> renderPost;

        public void Initialize(RectTransform targetSidebar, RectTransform targetContent, NotificationController notificationController,
            UnityAction<RectTransform, PostData> postRenderer)
        {
            sidebar = targetSidebar;
            contentHost = targetContent;
            notifications = notificationController;
            renderPost = postRenderer;
            RefreshSidebar();
        }

        public void RegisterPost(RectTransform card, PostData post)
        {
            var source = card.gameObject.AddComponent<PostDragSource>();
            source.Initialize(post);
        }

        private void RefreshSidebar()
        {
            Clear(sidebar);
            var title = UIFactory.Text("FoldersTitle", sidebar, "ПАПКИ ДЕЛ", 14, UIFactory.Paper, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(.08f, .88f), new Vector2(.7f, .98f), Vector2.zero, Vector2.zero);
            var add = UIFactory.StretchedPaintedButton("AddFolder", sidebar, "+", "Sprites/UI/ButtonPaper", 22, UIFactory.Charcoal, ShowCreatePrompt);
            UIFactory.Layout((RectTransform)add.transform, new Vector2(.72f, .89f), new Vector2(.94f, .97f), Vector2.zero, Vector2.zero);
            for (var index = 0; index < folders.Count; index++)
            {
                var folder = folders[index];
                var y = .86f - index * .12f;
                var button = UIFactory.StretchedPaintedButton("ModerationFolder_" + index, sidebar,
                    folder.Name + "  (" + folder.Posts.Count + ")", "Sprites/UI/ButtonPaper", 11, UIFactory.Charcoal, () => ShowFolder(folder));
                UIFactory.Layout((RectTransform)button.transform, new Vector2(.06f, y - .09f), new Vector2(.94f, y), Vector2.zero, Vector2.zero);
                button.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
                button.gameObject.AddComponent<FolderDropTarget>().Initialize(folder, this);
            }
            var hint = UIFactory.Text("FoldersHint", sidebar, "Перетащите сюда\nкарточку поста", 10, UIFactory.Muted, TextAnchor.LowerCenter);
            UIFactory.Layout(hint.rectTransform, new Vector2(.06f, .02f), new Vector2(.94f, .16f), Vector2.zero, Vector2.zero);
        }

        private void ShowCreatePrompt()
        {
            var prompt = UIFactory.TexturedRect("FolderNamePrompt", contentHost, "Art/UIPaperTexture", new Vector2(.17f, .36f), new Vector2(.83f, .67f), Vector2.zero, Vector2.zero, Color.white);
            prompt.SetAsLastSibling(); UIFactory.InkOutline(prompt.gameObject, 3f);
            var label = UIFactory.Text("PromptTitle", prompt, "НАЗВАНИЕ НОВОЙ ПАПКИ", 16, UIFactory.Charcoal, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(label.rectTransform, new Vector2(.06f, .7f), new Vector2(.94f, .94f), Vector2.zero, Vector2.zero);
            var inputRect = UIFactory.Rect("FolderNameInput", prompt, new Vector2(.08f, .38f), new Vector2(.92f, .67f), Vector2.zero, Vector2.zero, UIFactory.Hex("F4E7C9"));
            UIFactory.InkOutline(inputRect.gameObject, 1.5f);
            var input = inputRect.gameObject.AddComponent<InputField>();
            var value = UIFactory.Text("InputText", inputRect, "", 15, UIFactory.Charcoal, TextAnchor.MiddleLeft);
            UIFactory.Layout(value.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0)); value.raycastTarget = true;
            var placeholder = UIFactory.Text("Placeholder", inputRect, "Например: Нортбридж", 14, UIFactory.Muted, TextAnchor.MiddleLeft);
            UIFactory.Layout(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            input.textComponent = value; input.placeholder = placeholder;
            var create = UIFactory.StretchedPaintedButton("CreateFolder", prompt, "СОЗДАТЬ", "Sprites/UI/ButtonPaper", 12, UIFactory.Charcoal, () =>
            {
                var name = string.IsNullOrWhiteSpace(input.text) ? "Новое дело" : input.text.Trim();
                folders.Add(new Folder { Name = name });
                Destroy(prompt.gameObject); RefreshSidebar();
            });
            UIFactory.Layout((RectTransform)create.transform, new Vector2(.2f, .07f), new Vector2(.58f, .3f), Vector2.zero, Vector2.zero);
            var cancel = UIFactory.StretchedPaintedButton("CancelFolder", prompt, "ОТМЕНА", "Sprites/UI/ButtonPaper", 12, UIFactory.Charcoal, () => Destroy(prompt.gameObject));
            UIFactory.Layout((RectTransform)cancel.transform, new Vector2(.62f, .07f), new Vector2(.9f, .3f), Vector2.zero, Vector2.zero);
            input.Select(); input.ActivateInputField();
        }

        private void AddToFolder(Folder folder, PostData post)
        {
            if (post == null) return;
            if (!folder.Posts.Contains(post)) folder.Posts.Add(post);
            notifications.Show("Пост добавлен в папку «" + folder.Name + "»");
            RefreshSidebar();
        }

        private void ShowFolder(Folder folder)
        {
            Clear(contentHost);
            var paper = UIFactory.TexturedRect("FolderView", contentHost, "Art/UIPaperTexture", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            var heading = UIFactory.Text("FolderHeading", paper, folder.Name.ToUpperInvariant(), 22, UIFactory.Charcoal, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(heading.rectTransform, new Vector2(.04f, .86f), new Vector2(.96f, .98f), Vector2.zero, Vector2.zero);
            var grid = UIFactory.Rect("FolderGrid", paper, new Vector2(.04f, .06f), new Vector2(.96f, .84f), Vector2.zero, Vector2.zero, Color.clear);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(150, 145); layout.spacing = new Vector2(12, 12); layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 2;
            foreach (var post in folder.Posts)
            {
                var tile = UIFactory.StretchedPaintedButton("FolderPost_" + post.id, grid, "", "Sprites/UI/ButtonPaper", 11, UIFactory.Charcoal, () => ShowSnapshot(post));
                var title = UIFactory.Text("FolderPostTitle", tile.transform, post.displayName + "\n<size=11><color=#6B5B49>" + Short(post.text, 46) + "</color></size>", 14, UIFactory.Charcoal, TextAnchor.UpperLeft, FontStyle.Bold);
                UIFactory.Layout(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -58));
                if (post.image != null)
                    UIFactory.Artwork("FolderPostImage", tile.transform, post.image, new Vector2(.08f, .06f), new Vector2(.92f, .43f), Vector2.zero, Vector2.zero, true);
                if (post.deleted)
                {
                    var deleted = UIFactory.Text("DeletedTag", tile.transform, "УДАЛЁН", 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIFactory.Layout(deleted.rectTransform, new Vector2(.56f, .04f), new Vector2(.94f, .2f), Vector2.zero, Vector2.zero);
                    var background = tile.GetComponent<Image>(); if (background != null) background.color = UIFactory.Hex("8F3438");
                }
            }
        }

        private void ShowSnapshot(PostData post)
        {
            var snapshot = UIFactory.TexturedRect("PostSnapshot", contentHost, "Art/UIPaperTexture", new Vector2(.06f, .05f), new Vector2(.94f, .95f), Vector2.zero, Vector2.zero, Color.white);
            snapshot.SetAsLastSibling(); UIFactory.InkOutline(snapshot.gameObject, 3f);
            var close = UIFactory.StretchedPaintedButton("CloseSnapshot", snapshot, "×", "Sprites/UI/ButtonPaper", 18, UIFactory.Charcoal, () => Destroy(snapshot.gameObject));
            UIFactory.Layout((RectTransform)close.transform, new Vector2(.88f, .91f), new Vector2(.97f, .98f), Vector2.zero, Vector2.zero);
            var heading = UIFactory.Text("SnapshotHeading", snapshot, "ПОСТ ИЗ ПАПКИ", 15, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(heading.rectTransform, new Vector2(.04f, .91f), new Vector2(.84f, .98f), Vector2.zero, Vector2.zero);
            var postHost = UIFactory.Rect("SnapshotPostHost", snapshot, new Vector2(.025f, .025f), new Vector2(.975f, .9f), Vector2.zero, Vector2.zero, Color.clear);
            renderPost?.Invoke(postHost, post);
        }

        private static string Short(string value, int length) => string.IsNullOrEmpty(value) ? "" : value.Length <= length ? value : value.Substring(0, length - 1) + "…";
        private static void Clear(RectTransform root)
        {
            ManualLayoutPersistence.CaptureBeforeRebuild(root);
            for (var i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            ManualLayoutPersistence.RequestReapply(root);
        }

        private sealed class FolderDropTarget : MonoBehaviour, IDropHandler
        {
            private Folder folder; private ModerationFolders owner;
            public void Initialize(Folder target, ModerationFolders controller) { folder = target; owner = controller; }
            public void OnDrop(PointerEventData eventData) => owner.AddToFolder(folder, PostDragSource.DraggedPost);
        }
    }

    public sealed class PostDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static PostData DraggedPost { get; private set; }
        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 start;
        public void Initialize(PostData post) { DraggedPost = null; data = post; rect = (RectTransform)transform; group = gameObject.AddComponent<CanvasGroup>(); }
        private PostData data;
        public void OnBeginDrag(PointerEventData eventData) { DraggedPost = data; start = rect.anchoredPosition; group.blocksRaycasts = false; group.alpha = .72f; }
        public void OnDrag(PointerEventData eventData) { rect.anchoredPosition += eventData.delta; }
        public void OnEndDrag(PointerEventData eventData) { rect.anchoredPosition = start; group.blocksRaycasts = true; group.alpha = 1; DraggedPost = null; }
    }
}
