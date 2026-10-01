using System.Collections.Generic;
using Moderator.Browser;
using Moderator.SocialNetwork.Posts;
using Moderator.UI;
using Moderator.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.SocialNetwork
{
    public sealed class SocialNetworkApp : MonoBehaviour
    {
        private readonly List<UserData> users = new();
        private readonly List<Text> queueLabels = new();
        private RectTransform desktop;
        private RectTransform postHost;
        private RectTransform profileContent;
        private WindowManager manager;
        private NotificationController notifications;
        private BrowserApp browser;
        private WindowController socialWindow;
        private WindowController profileWindow;
        private UserData selectedUser;

        public void Initialize(RectTransform targetDesktop, WindowManager windowManager, NotificationController notificationController, BrowserApp browserApp)
        {
            desktop = targetDesktop;
            manager = windowManager;
            notifications = notificationController;
            browser = browserApp;
            BuildData();
        }

        public void Open()
        {
            if (socialWindow == null) BuildSocial();
            socialWindow.Open();
        }

        public void OpenProfileHome() => OpenProfile(users[0]);

        private void BuildData()
        {
            var photo = UIFactory.LoadSprite("PostPhoto");
            var alley = UIFactory.LoadSprite("Art/PostAlley");
            users.Add(CreateUser("user-001", "@alex_92", "Alex", "2026", 2, 42, UIFactory.LoadSprite("Art/AvatarAlexV2"), alley,
                new[] { "Does anyone else remember this place?", "The lights were on again after midnight.", "Old photo from the north platform." },
                new[] { "12 min ago", "2 days ago", "9 days ago" }, 127, 18, 4, -1));
            users.Add(CreateUser("user-002", "@nova_k", "Nova K.", "2024", 5, 78, UIFactory.LoadSprite("Art/AvatarNovaV2"), photo,
                new[] { "The north exit was open again last night.", "A maintenance badge was left near the gate.", "Reposting this before it disappears." },
                new[] { "34 min ago", "4 days ago", "3 weeks ago" }, 63, 11, 9, -1));
            users.Add(CreateUser("user-003", "@mina", "Mina", "2025", 1, 19, UIFactory.LoadSprite("Art/AvatarMinaV2"), photo,
                new[] { "Found the same symbol in an old transit map.", "Archived station footage from last winter.", "The route number changed after the closure." },
                new[] { "1 h ago", "6 days ago", "1 month ago" }, 204, 27, 16, 0));
        }

        private static UserData CreateUser(string id, string username, string name, string joined, int reports, int risk, Sprite avatar, Sprite image,
            string[] texts, string[] timestamps, int likes, int comments, int shares, int mapPostIndex)
        {
            var user = new UserData { id = id, username = username, displayName = name, registrationDate = joined, reports = reports, riskScore = risk, avatar = avatar };
            for (var index = 0; index < texts.Length; index++)
            {
                user.posts.Add(new PostData
                {
                    id = id + "-post-" + (index + 1), username = username, displayName = name, text = texts[index], image = image,
                    likes = Mathf.Max(3, likes - index * 21), comments = Mathf.Max(1, comments - index * 4), shares = Mathf.Max(0, shares - index * 2),
                    timestamp = timestamps[index], hasMapLink = index == mapPostIndex
                });
            }
            return user;
        }

        private void BuildSocial()
        {
            socialWindow = WindowController.Create("SocialWindow", "FaceNet // Moderator Review", desktop, manager, new Vector2(690, 570), new Vector2(335, 18), out var content);
            var nav = UIFactory.TexturedRect("Navigation", content, "Art/UIPaperTexture", new Vector2(0, 1), Vector2.one, new Vector2(0, -67), Vector2.zero, Color.white);
            UIFactory.InkOutline(nav.gameObject, 2f);
            AddTopNavButton(nav, "Home", "Sprites/UI/Home", 0f, .14f, () => ShowUserCase(selectedUser ?? users[0]));
            AddTopNavButton(nav, "People", "Sprites/UI/Friends", .14f, .32f, () => notifications.Show("Account directory ready"));
            AddTopNavButton(nav, "Alerts", "Sprites/UI/Bell", .32f, .5f, () => notifications.Show("3 moderation alerts pending"));
            AddTopNavButton(nav, "Mail", "Sprites/UI/Mail", .5f, .68f, () => notifications.Show("Case mailbox opened"));
            var status = UIFactory.Text("Status", nav, "CASE REVIEW", 13, UIFactory.Hex("5D2184"), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(status.rectTransform, new Vector2(.68f, 0), new Vector2(.84f, 1), Vector2.zero, Vector2.zero);
            var mod = UIFactory.Text("Moderator", nav, "MODERATOR\nONLINE", 11, UIFactory.Green, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(mod.rectTransform, new Vector2(.84f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            var feed = UIFactory.TexturedRect("Feed", content, "Art/UIInkTexture", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -75), Color.white);
            BuildQueue(feed);
            postHost = UIFactory.Rect("PostHost", feed, new Vector2(.245f, 0), Vector2.one, new Vector2(9, 5), new Vector2(-6, -5));
            ShowUserCase(users[0]);
        }

        private void BuildQueue(RectTransform feed)
        {
            var queue = UIFactory.Rect("CaseQueue", feed, Vector2.zero, new Vector2(.245f, 1), new Vector2(6, 5), new Vector2(-5, -5), new Color(.035f, .08f, .13f, .96f));
            UIFactory.InkOutline(queue.gameObject, 2f);
            var title = UIFactory.Text("QueueTitle", queue, "MODERATOR QUEUE\n<size=11><color=#CDBD9B>3 ACCOUNTS UNDER REVIEW</color></size>", 15, UIFactory.Paper, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(0, .85f), Vector2.one, new Vector2(16, 12), new Vector2(-10, -8));
            for (var index = 0; index < users.Count; index++)
            {
                var user = users[index];
                var yMax = .82f - index * .195f;
                var row = UIFactory.Button("QueueUser_" + index, queue, string.Empty, 13, index == 0 ? UIFactory.Hex("492463") : UIFactory.Hex("1A2D3C"), UIFactory.Paper, () => ShowUserCase(user));
                UIFactory.Layout((RectTransform)row.transform, new Vector2(.05f, yMax - .15f), new Vector2(.95f, yMax), Vector2.zero, Vector2.zero);
                var label = row.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                queueLabels.Add(label);
            }
            RefreshQueueLabels();
            var hint = UIFactory.Text("QueueHint", queue, "SELECT AN ACCOUNT\nReview current and archived posts", 10, UIFactory.Muted, TextAnchor.LowerLeft);
            UIFactory.Layout(hint.rectTransform, new Vector2(0, .02f), new Vector2(1, .18f), new Vector2(16, 0), new Vector2(-8, 0));
        }

        private void RefreshQueueLabels()
        {
            for (var index = 0; index < queueLabels.Count; index++)
            {
                var user = users[index];
                queueLabels[index].text = Initial(user) + "   " + user.displayName.ToUpperInvariant() +
                    "\n<size=11><color=#8594A0>" + user.username + "   RISK " + user.riskScore + "%   " + user.reports + " REPORTS   " +
                    (user.isBanned ? "BANNED" : user.posts.Count + " POSTS") + "</color></size>";
            }
        }

        private void ShowUserCase(UserData selected)
        {
            selectedUser = selected;
            Clear(postHost);
            var scrollRoot = UIFactory.Rect("PostHistoryScroll", postHost, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 48;
            var viewport = UIFactory.Rect("Viewport", scrollRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UIFactory.Rect("PostHistoryContent", viewport, Vector2.up, Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 12, 4, 18);
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            foreach (var post in selected.posts)
            {
                var item = UIFactory.Rect("PostHistoryItem", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var itemLayout = item.gameObject.AddComponent<LayoutElement>();
                itemLayout.preferredHeight = 590;
                itemLayout.minHeight = 590;
                item.gameObject.AddComponent<PostView>().Build(item, post, selected, () => OpenProfile(selected), notifications.Show, browser.OpenMapArticle);
            }
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
        }

        private void OpenProfile(UserData selected)
        {
            selectedUser = selected;
            if (profileWindow == null)
                profileWindow = WindowController.Create("ProfileWindow", "FaceNet", desktop, manager, new Vector2(690, 610), new Vector2(335, 18), out profileContent);
            PopulateProfile(selected);
            profileWindow.Open();
        }

        private void PopulateProfile(UserData selected)
        {
            Clear(profileContent);
            var postData = selected.posts[0];
            UIFactory.TexturedRect("ProfilePaper", profileContent, "Art/UIPaperTexture", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white).SetAsFirstSibling();
            var socialNav = UIFactory.TexturedRect("ProfileNavigation", profileContent, "Art/UIPaperTexture", new Vector2(0, .89f), Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            UIFactory.InkOutline(socialNav.gameObject, 2f);
            AddProfileNavIcon(socialNav, "⌂", 0f, .18f, UIFactory.Hex("5D2184"));
            AddProfileNavIcon(socialNav, "♟♟", .18f, .36f, UIFactory.Charcoal);
            AddProfileNavIcon(socialNav, "♟  <color=#B62222><size=12>3</size></color>", .36f, .55f, UIFactory.Charcoal);
            AddProfileNavIcon(socialNav, "✉", .55f, .73f, UIFactory.Charcoal);
            var mode = UIFactory.Text("ModeratorMode", socialNav, "◆ MOD VIEW", 13, UIFactory.Hex("8D2428"), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(mode.rectTransform, new Vector2(.73f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            var cover = UIFactory.Rect("Cover", profileContent, new Vector2(0, .64f), new Vector2(1, .89f), Vector2.zero, Vector2.zero, UIFactory.Hex("17304B"));
            var coverArt = UIFactory.Artwork("CoverArt", cover, UIFactory.LoadSprite("Art/ProfileCoverAlley"), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
            coverArt.color = new Color(.62f, .66f, .78f, .72f);
            UIFactory.Rect("CoverShade", cover, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.06f, .02f, .1f, .34f));
            UIFactory.Rect("CoverLine", cover, Vector2.zero, new Vector2(1, .035f), Vector2.zero, Vector2.zero, UIFactory.Hex("5D2184"));
            var avatar = UIFactory.Rect("ProfileAvatar", profileContent, Vector2.up, Vector2.up, Vector2.zero, Vector2.zero, UIFactory.Blue);
            avatar.pivot = Vector2.up;
            avatar.sizeDelta = new Vector2(126, 126);
            avatar.anchoredPosition = new Vector2(30, -185);
            UIFactory.InkOutline(avatar.gameObject, 3f);
            if (selected.avatar != null) UIFactory.Artwork("Portrait", avatar, selected.avatar, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            else UIFactory.Text("Initial", avatar, Initial(selected), 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var identity = UIFactory.Text("ProfileIdentity", profileContent, selected.displayName + "\n<size=16><color=#574B3E>" + selected.username + "</color></size>", 27, UIFactory.Charcoal, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Layout(identity.rectTransform, new Vector2(.25f, .55f), new Vector2(.72f, .67f), Vector2.zero, Vector2.zero);
            var ban = UIFactory.PaintedButton("ProfileBanUser", profileContent, string.Empty, "Sprites/UI/ButtonPurple", 12, Color.white);
            UIFactory.Layout((RectTransform)ban.transform, new Vector2(.77f, .565f), new Vector2(.97f, .635f), Vector2.zero, Vector2.zero);
            var facts = UIFactory.Text("ProfileFacts", profileContent,
                "Joined: " + selected.registrationDate + "     Posts: " + selected.posts.Count + "     Reports: " + selected.reports + "     Risk: " + selected.riskScore + "%",
                13, UIFactory.Hex("574B3E"), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(facts.rectTransform, new Vector2(.25f, .49f), new Vector2(.96f, .55f), Vector2.zero, Vector2.zero);

            var tabs = UIFactory.Text("ProfileTabs", profileContent, "POSTS                     ACCOUNT INFO                     MEDIA                     CONNECTIONS", 13, UIFactory.Charcoal, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(tabs.rectTransform, new Vector2(.03f, .44f), new Vector2(.97f, .5f), Vector2.zero, Vector2.zero);
            UIFactory.Rect("ActiveTab", profileContent, new Vector2(.05f, .44f), new Vector2(.22f, .447f), Vector2.zero, Vector2.zero, UIFactory.Hex("5D2184"));

            var blockedBanner = UIFactory.Rect("UserBlockedBanner", profileContent, new Vector2(.18f, .16f), new Vector2(.82f, .42f), Vector2.zero, Vector2.zero, new Color(.18f, .04f, .05f, .96f));
            UIFactory.Text("BlockedText", blockedBanner, "USER BLOCKED\n<size=14>All posts were removed from the network</size>", 25, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            var post = UIFactory.TexturedRect("ProfilePost", profileContent, "Art/UIPaperTexture", new Vector2(0, .02f), new Vector2(1, .43f), new Vector2(28, 0), new Vector2(-36, 0), Color.white);
            var postOutline = post.gameObject.AddComponent<Outline>();
            postOutline.effectColor = UIFactory.Charcoal;
            postOutline.effectDistance = new Vector2(2, -2);
            var miniAvatar = UIFactory.Rect("PostAuthorAvatar", post, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -70), new Vector2(68, -18), UIFactory.Charcoal);
            if (selected.avatar != null) UIFactory.Artwork("Portrait", miniAvatar, selected.avatar, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            var postTitle = UIFactory.Text("PostTitle", post, selected.displayName + "\n<size=13><color=#574B3E>" + selected.username + "  •  " + postData.timestamp + "</color></size>", 17, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(postTitle.rectTransform, new Vector2(0, .8f), Vector2.one, new Vector2(80, 0), new Vector2(-18, 0));
            var photoFrame = UIFactory.Rect("ProfilePostPhoto", post, new Vector2(.025f, .18f), new Vector2(.37f, .76f), Vector2.zero, Vector2.zero, UIFactory.Hex("0D1115"));
            if (postData.image != null)
            {
                var photo = UIFactory.Rect("Photo", photoFrame, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5), Color.white);
                var photoImage = photo.GetComponent<Image>();
                photoImage.sprite = postData.image;
                photoImage.preserveAspect = true;
            }
            var profileBody = UIFactory.Rect("ProfileBody", post, new Vector2(.4f, .43f), new Vector2(.97f, .75f), Vector2.zero, Vector2.zero);
            LinkedPostText.Build(profileBody, postData, 15, browser.OpenMapArticle, true);
            var time = UIFactory.Text("ProfilePostTime", post, postData.timestamp, 13, UIFactory.Hex("6B5B49"), TextAnchor.UpperLeft);
            UIFactory.Layout(time.rectTransform, new Vector2(.4f, .28f), new Vector2(.97f, .42f), Vector2.zero, Vector2.zero);

            var actions = UIFactory.Rect("ProfilePostActions", post, new Vector2(0, .025f), new Vector2(1, .17f), new Vector2(12, 0), new Vector2(-12, 0), UIFactory.Ink);
            var save = UIFactory.PaintedButton("ProfileSavePost", actions, string.Empty, "Sprites/UI/ButtonPurple", 11, UIFactory.Paper);
            UIFactory.Layout((RectTransform)save.transform, new Vector2(0, .08f), new Vector2(.21f, .92f), Vector2.zero, Vector2.zero);
            var comment = UIFactory.PaintedButton("ProfileComment", actions, "INSPECT COMMENTS", "Sprites/UI/ButtonBlue", 10, UIFactory.Paper);
            UIFactory.Layout((RectTransform)comment.transform, new Vector2(.225f, .08f), new Vector2(.49f, .92f), Vector2.zero, Vector2.zero);
            var delete = UIFactory.PaintedButton("ProfileDeletePost", actions, string.Empty, "Sprites/UI/ButtonGray", 11, UIFactory.Charcoal);
            UIFactory.Layout((RectTransform)delete.transform, new Vector2(.505f, .08f), new Vector2(.7f, .92f), Vector2.zero, Vector2.zero);
            var counters = UIFactory.Text("ProfileCounters", actions, string.Empty, 13, UIFactory.Muted, TextAnchor.MiddleRight);
            UIFactory.Layout(counters.rectTransform, new Vector2(.71f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            var comments = UIFactory.Rect("ProfileComments", post, new Vector2(.41f, .2f), new Vector2(.98f, .81f), Vector2.zero, Vector2.zero, UIFactory.Ink);
            var commentsText = UIFactory.Text("CommentsText", comments,
                "COMMENTS\n\n@nova_k: I saw it too.\n@oldsignal: Check the archived route.\n@mina: The symbol matches the map.", 13, UIFactory.Paper, TextAnchor.UpperLeft);
            UIFactory.Layout(commentsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 12), new Vector2(-16, -12));
            comments.gameObject.SetActive(false);

            var banLabel = ban.GetComponentInChildren<Text>();
            var saveLabel = save.GetComponentInChildren<Text>();
            var deleteLabel = delete.GetComponentInChildren<Text>();
            var deleteConfirmation = false;
            void RefreshProfile()
            {
                banLabel.text = selected.isBanned ? "UNBAN USER" : "BAN USER";
                saveLabel.text = postData.saved ? "UNSAVE POST" : "SAVE POST";
                deleteLabel.text = postData.deleted ? "REMOVED" : deleteConfirmation ? "CONFIRM" : "REMOVE POST";
                delete.interactable = !postData.deleted;
                counters.text = "♥ " + postData.likes + "   ● " + postData.comments + "   ↗ " + postData.shares;
                blockedBanner.gameObject.SetActive(selected.isBanned);
                post.gameObject.SetActive(!selected.isBanned && !postData.deleted);
            }
            ban.onClick.AddListener(() =>
            {
                selected.isBanned = !selected.isBanned;
                foreach (var userPost in selected.posts)
                {
                    if (selected.isBanned && !userPost.deleted) { userPost.deleted = true; userPost.deletedByBan = true; }
                    else if (!selected.isBanned && userPost.deletedByBan) { userPost.deleted = false; userPost.deletedByBan = false; }
                }
                notifications.Show(selected.isBanned ? selected.username + " banned; all posts removed" : selected.username + " unbanned; ban removals restored");
                ShowUserCase(selected);
                RefreshQueueLabels();
                RefreshProfile();
            });
            save.onClick.AddListener(() => { postData.saved = !postData.saved; notifications.Show(postData.saved ? "Post saved to case evidence" : "Post removed from case evidence"); RefreshProfile(); });
            delete.onClick.AddListener(() =>
            {
                if (!deleteConfirmation) { deleteConfirmation = true; notifications.Show("Press CONFIRM to remove post"); }
                else { postData.deleted = true; deleteConfirmation = false; notifications.Show("Post removed by moderator"); ShowUserCase(selected); }
                RefreshProfile();
            });
            comment.onClick.AddListener(() => { comments.gameObject.SetActive(!comments.gameObject.activeSelf); comments.SetAsLastSibling(); });
            var scrollTrack = UIFactory.Rect("ProfileScrollbar", profileContent, new Vector2(.976f, .02f), new Vector2(.994f, .88f), Vector2.zero, Vector2.zero, UIFactory.Hex("29113E"));
            UIFactory.InkOutline(scrollTrack.gameObject, 1.5f);
            var scrollThumb = UIFactory.Rect("ProfileScrollbarThumb", scrollTrack, new Vector2(.12f, .48f), new Vector2(.88f, .82f), Vector2.zero, Vector2.zero, UIFactory.Hex("74419A"));
            UIFactory.InkOutline(scrollThumb.gameObject, 1f);
            RefreshProfile();
        }

        private void AddProfileNavIcon(RectTransform parent, string symbol, float xMin, float xMax, Color color)
        {
            string sprite;
            UnityEngine.Events.UnityAction action;
            if (xMin < .1f) { sprite = "Sprites/UI/Home"; action = Open; }
            else if (xMin < .3f) { sprite = "Sprites/UI/Friends"; action = () => notifications.Show("Connections attached to the active case"); }
            else if (xMin < .5f) { sprite = "Sprites/UI/Bell"; action = () => notifications.Show("3 moderation alerts pending"); }
            else { sprite = "Sprites/UI/Mail"; action = () => notifications.Show("Case mailbox opened"); }
            var icon = UIFactory.SpriteButton("NavIcon", parent, sprite, action, Color.clear, 7);
            UIFactory.Layout((RectTransform)icon.transform, new Vector2(xMin, 0), new Vector2(xMax, 1), Vector2.zero, Vector2.zero);
        }

        private static void AddTopNavButton(RectTransform parent, string name, string sprite, float xMin, float xMax,
            UnityEngine.Events.UnityAction action)
        {
            var button = UIFactory.SpriteButton(name, parent, sprite, action, Color.clear, 8);
            UIFactory.Layout((RectTransform)button.transform, new Vector2(xMin, 0), new Vector2(xMax, 1), Vector2.zero, Vector2.zero);
        }

        private static void Clear(RectTransform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var stale = root.GetChild(index);
                stale.name = "Disposed_" + stale.name;
                stale.gameObject.SetActive(false);
                Destroy(stale.gameObject);
            }
            var view = root.GetComponent<PostView>();
            if (view != null) Destroy(view);
        }

        private static string Initial(UserData user) => string.IsNullOrEmpty(user.displayName) ? "?" : user.displayName.Substring(0, 1).ToUpperInvariant();
    }
}
