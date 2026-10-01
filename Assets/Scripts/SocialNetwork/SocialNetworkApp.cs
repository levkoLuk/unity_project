using System.Collections.Generic;
using Moderator.Browser;
using Moderator.SocialNetwork.Posts;
using Moderator.UI;
using Moderator.Windows;
using Moderator.Investigation;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.SocialNetwork
{
    public sealed class SocialNetworkApp : MonoBehaviour
    {
        private readonly List<UserData> users = new();
        private readonly List<Text> queueLabels = new();
        private readonly List<UserData> profileVisits = new();
        private readonly HashSet<string> reviewedPostIds = new();
        private readonly List<Text> mailBadgeTexts = new();
        private readonly List<Text> alertsBadgeTexts = new();
        private RectTransform desktop;
        private RectTransform postHost;
        private RectTransform profileContent;
        private WindowManager manager;
        private NotificationController notifications;
        private BrowserApp browser;
        private InvestigationState investigation;
        private WindowController socialWindow;
        private WindowController profileWindow;
        private WindowController messagesWindow;
        private RectTransform messagesContent;
        private ModerationFolders moderationFolders;
        private UserData selectedUser;
        private int visibleUserCount;
        private int unreadMessages = 1;
        private bool suppressReviewSideEffects;
        private bool mainWindowVisited;

        public void Initialize(RectTransform targetDesktop, WindowManager windowManager, NotificationController notificationController, BrowserApp browserApp,
            InvestigationState investigationState)
        {
            desktop = targetDesktop;
            manager = windowManager;
            notifications = notificationController;
            browser = browserApp;
            investigation = investigationState;
            BuildData();
            visibleUserCount = investigation.UnlockedUsers.Count;
            investigation.Changed += RefreshAvailability;
        }

        public void Open()
        {
            if (users.Count == 0) BuildData();
            if (socialWindow == null) BuildSocial();
            if (!mainWindowVisited)
            {
                MarkUserReviewed(selectedUser ?? users[0]);
                mainWindowVisited = true;
            }
            socialWindow.Open();
        }

        public void Warmup()
        {
            if (users.Count == 0) BuildData();
            suppressReviewSideEffects = true;
            try
            {
                if (socialWindow == null) BuildSocial();
            }
            finally
            {
                suppressReviewSideEffects = false;
            }
            socialWindow.Close();
        }

        public void OpenProfileHome() => OpenProfile(users[0]);

        public void OpenProfileByUsername(string username)
        {
            if (users.Count == 0) BuildData();
            var normalized = (username ?? string.Empty).Trim().ToLowerInvariant();
            var user = users.Find(candidate => candidate.username.ToLowerInvariant() == normalized || candidate.username.TrimStart('@').ToLowerInvariant() == normalized.TrimStart('@'));
            if (user == null) { notifications.Show("Профиль " + username + " не найден"); return; }
            OpenProfile(user);
        }

        private void RefreshAvailability()
        {
            if (investigation.UnlockedUsers.Count == visibleUserCount) { RefreshUnreadCounters(); return; }
            visibleUserCount = investigation.UnlockedUsers.Count;
            var reopen = socialWindow != null && socialWindow.gameObject.activeSelf;
            if (profileWindow != null) { Destroy(profileWindow.gameObject); profileWindow = null; profileContent = null; }
            if (socialWindow != null) { Destroy(socialWindow.gameObject); socialWindow = null; postHost = null; queueLabels.Clear(); }
            if (reopen) { BuildSocial(); socialWindow.Open(); }
            RefreshUnreadCounters();
        }

        private void BuildData()
        {
            users.Add(CreateStoryUser("user.alex_92", "@alex_92", "Алекс", "2024", "Фотография / городские исследования",
                UIFactory.LoadSprite("Art/AvatarAlexV2"), UIFactory.LoadSprite("Art/Story/AlexNorthbridgePhoto"),
                "Сделал это фото рядом со станцией Нортбридж.", "станцией Нортбридж", "page.northbridge_portal", "18 мин. назад", 84, 12, 3));
            users.Add(CreateStoryUser("user.nightwatcher", "@nightwatcher", "Ночной наблюдатель", "2029", "Некоторые вещи исчезают. Снимки экрана — нет.",
                UIFactory.LoadSprite("Art/Story/AvatarNightwatcher"), UIFactory.LoadSprite("Art/Story/DeletedCarPost"),
                "Я снова видел ту же машину возле Нортбриджа.", "Нортбриджа", "page.northbridge_portal", "2 дня назад", 31, 8, 2));
            users.Add(CreateStoryUser("user.northbridge_news", "@northbridge_news", "Новости Нортбриджа", "2027", "Местные новости и сообщения общественной безопасности.",
                UIFactory.LoadSprite("Art/Story/AvatarNorthbridgeNews"), null,
                "Полиция утверждает, что в Нортбридже не было никаких происшествий. Но новости говорят об обратном.", "Нортбридже", "page.northbridge_portal", "1 год назад", 112, 19, 14));
            users[^1].isVerified = true;
            var police = CreateStoryUser("user.northbridge_police", "@northbridge_police", "Полиция Нортбриджа", "2018", "Официальный аккаунт городского управления полиции.",
                UIFactory.LoadSprite("Art/Story/police"), null,
                "Официальные уведомления управления полиции Нортбриджа. Следуйте опубликованным распоряжениям.", "Нортбриджа", "page.northbridge_portal", "сегодня", 0, 0, 0);
            police.isVerified = true;
            users.Add(police);
        }

        private static UserData CreateStoryUser(string id, string username, string name, string joined, string bio, Sprite avatar, Sprite image,
            string text, string linkedText, string searchTerm, string timestamp, int likes, int comments, int shares)
        {
            var user = new UserData { id = id, username = username, displayName = name, registrationDate = joined, bio = bio, avatar = avatar };
            user.posts.Add(new PostData { id = id + ".post", username = username, displayName = name, text = text, image = image,
                linkedText = linkedText, linkedSearchTerm = searchTerm, timestamp = timestamp, likes = likes, comments = comments, shares = shares });
            return user;
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
            socialWindow = WindowController.Create("SocialWindow", "FaceNet // Проверка модератора", desktop, manager, new Vector2(690, 570), new Vector2(335, 18), out var content);
            var nav = UIFactory.TexturedRect("Navigation", content, "Art/UIPaperTexture", new Vector2(0, 1), Vector2.one, new Vector2(0, -67), Vector2.zero, Color.white);
            UIFactory.InkOutline(nav.gameObject, 2f);
            AddTopNavButton(nav, "Home", "Sprites/UI/Home", 0f, .14f, () => ShowUserCase(selectedUser ?? users[0]));
            AddTopNavButton(nav, "People", "Sprites/UI/Friends", .14f, .32f, ShowProfileHistory);
            var alerts = AddTopNavButton(nav, "Alerts", "Sprites/UI/Bell", .32f, .5f, ShowUnreadSummary);
            alertsBadgeTexts.Add(AddUnreadBadge(alerts, "AlertsBadge"));
            var mail = AddTopNavButton(nav, "Mail", "Sprites/UI/Mail", .5f, .68f, ShowMessages);
            mailBadgeTexts.Add(AddUnreadBadge(mail, "MailBadge"));
            var status = UIFactory.Text("Status", nav, "ПРОВЕРКА", 13, UIFactory.Hex("5D2184"), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(status.rectTransform, new Vector2(.68f, 0), new Vector2(.84f, 1), Vector2.zero, Vector2.zero);
            var mod = UIFactory.Text("Moderator", nav, "МОДЕРАТОР\nВ СЕТИ", 11, UIFactory.Green, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(mod.rectTransform, new Vector2(.84f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            var feed = UIFactory.TexturedRect("Feed", content, "Art/UIInkTexture", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -75), Color.white);
            BuildQueue(feed);
            postHost = UIFactory.Rect("PostHost", feed, new Vector2(.245f, 0), new Vector2(.81f, 1), new Vector2(9, 5), new Vector2(-6, -5));
            var folderSidebar = UIFactory.Rect("ModerationFoldersSidebar", feed, new Vector2(.81f, 0), Vector2.one, new Vector2(4, 5), new Vector2(-5, -5), new Color(.035f, .08f, .13f, .96f));
            UIFactory.InkOutline(folderSidebar.gameObject, 2f);
            moderationFolders = gameObject.GetComponent<ModerationFolders>() ?? gameObject.AddComponent<ModerationFolders>();
            moderationFolders.Initialize(folderSidebar, postHost, notifications, RenderFolderPost);
            ShowUserCase(users[0]);
            RefreshUnreadCounters();
        }

        private void BuildQueue(RectTransform feed)
        {
            var queue = UIFactory.Rect("CaseQueue", feed, Vector2.zero, new Vector2(.245f, 1), new Vector2(6, 5), new Vector2(-5, -5), new Color(.035f, .08f, .13f, .96f));
            UIFactory.InkOutline(queue.gameObject, 2f);
            var availableCount = users.FindAll(user => investigation.UnlockedUsers.Contains(user.id)).Count;
            var title = UIFactory.Text("QueueTitle", queue, "ОЧЕРЕДЬ МОДЕРАТОРА\n<size=11><color=#CDBD9B>АККАУНТОВ НА ПРОВЕРКЕ: " + availableCount + "</color></size>", 15, UIFactory.Paper, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(0, .85f), Vector2.one, new Vector2(16, 12), new Vector2(-10, -8));
            var visibleUsers = users.FindAll(user => investigation.UnlockedUsers.Contains(user.id));
            for (var index = 0; index < visibleUsers.Count; index++)
            {
                var user = visibleUsers[index];
                var yMax = .82f - index * .195f;
                var row = UIFactory.PaintedButton("QueueUser_" + index, queue, string.Empty, "Sprites/UI/ButtonPaper", 13, UIFactory.Charcoal, () => ShowUserCase(user));
                UIFactory.Layout((RectTransform)row.transform, new Vector2(.05f, yMax - .15f), new Vector2(.95f, yMax), Vector2.zero, Vector2.zero);
                var label = row.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                queueLabels.Add(label);
            }
            RefreshQueueLabels();
            var hint = UIFactory.Text("QueueHint", queue, "ВЫБЕРИТЕ АККАУНТ\nПубликации пользователя", 10, UIFactory.Muted, TextAnchor.LowerLeft);
            UIFactory.Layout(hint.rectTransform, new Vector2(0, .02f), new Vector2(1, .18f), new Vector2(16, 0), new Vector2(-8, 0));
        }

        private void RefreshQueueLabels()
        {
            var visibleUsers = users.FindAll(user => investigation.UnlockedUsers.Contains(user.id));
            for (var index = 0; index < queueLabels.Count && index < visibleUsers.Count; index++)
            {
                var user = visibleUsers[index];
                queueLabels[index].text = Initial(user) + "   " + user.displayName.ToUpperInvariant();
            }
        }

        private void ShowUserCase(UserData selected)
        {
            selectedUser = selected;
            if (!suppressReviewSideEffects)
            {
                MarkUserReviewed(selected);
                mainWindowVisited = true;
            }
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
                item.gameObject.AddComponent<PostView>().Build(item, post, selected, () => OpenProfile(selected), notifications.Show, browser.OpenSearch,
                    savedPost => { if (selected.id == "user.nightwatcher") investigation.FindEvidence("evidence.nightwatcher_same_car"); },
                    deletedPost => investigation.SetPostDeleted(deletedPost.id, true));
                moderationFolders?.RegisterPost(item, post);
            }
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
        }

        private void MarkUserReviewed(UserData selected)
        {
            RecordProfileVisit(selected);
            foreach (var post in selected.posts) reviewedPostIds.Add(post.id);
            RefreshUnreadCounters();
        }

        private void OpenProfile(UserData selected)
        {
            selectedUser = selected;
            RecordProfileVisit(selected);
            if (profileWindow == null)
                profileWindow = WindowController.Create("ProfileWindow", "FaceNet", desktop, manager, new Vector2(690, 610), new Vector2(335, 18), out profileContent);
            PopulateProfile(selected);
            profileWindow.Open();
        }

        private void RecordProfileVisit(UserData selected)
        {
            profileVisits.Remove(selected);
            profileVisits.Add(selected);
        }

        private void ShowProfileHistory()
        {
            if (socialWindow == null) BuildSocial();
            Clear(postHost);
            var panel = UIFactory.TexturedRect("ProfileHistory", postHost, "Art/UIPaperTexture", new Vector2(.04f, .05f), new Vector2(.96f, .95f), Vector2.zero, Vector2.zero, Color.white);
            UIFactory.InkOutline(panel.gameObject, 2f);
            var title = UIFactory.Text("ProfileHistoryTitle", panel, "ИСТОРИЯ ПРОФИЛЕЙ", 24, UIFactory.Charcoal, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(.08f, .82f), new Vector2(.92f, .96f), Vector2.zero, Vector2.zero);
            for (var row = 0; row < profileVisits.Count; row++)
            {
                var user = profileVisits[profileVisits.Count - 1 - row];
                var y = .78f - row * .145f;
                var button = UIFactory.StretchedPaintedButton("ProfileHistoryUser_" + row, panel, "", "Sprites/UI/ButtonPaper", 14, UIFactory.Charcoal, () => OpenProfile(user));
                UIFactory.Layout((RectTransform)button.transform, new Vector2(.08f, y - .115f), new Vector2(.92f, y), Vector2.zero, Vector2.zero);
                var avatar = UIFactory.Artwork("ProfileHistoryAvatar", button.transform, user.avatar, new Vector2(.025f, .12f), new Vector2(.16f, .88f), Vector2.zero, Vector2.zero, true);
                var label = UIFactory.Text("ProfileHistoryName", button.transform, user.displayName + "\n<size=12><color=#756B5D>" + user.username + "</color></size>", 16, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.Layout(label.rectTransform, new Vector2(.19f, .05f), new Vector2(.95f, .95f), Vector2.zero, Vector2.zero);
            }
            socialWindow.Open();
        }

        private void ShowMessages()
        {
            if (messagesWindow == null)
                messagesWindow = WindowController.Create("MessagesWindow", "FaceNet // Сообщения", desktop, manager, new Vector2(620, 570), new Vector2(305, 8), out messagesContent);
            ShowConversationList();
            messagesWindow.Open();
        }

        private void ShowConversationList()
        {
            Clear(messagesContent);
            var panel = UIFactory.TexturedRect("MessagesListPage", messagesContent, "Art/UIPaperTexture", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            var title = UIFactory.Text("MessagesTitle", panel, "СООБЩЕНИЯ", 25, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(.055f, .84f), new Vector2(.95f, .97f), Vector2.zero, Vector2.zero);
            var police = users.Find(user => user.id == "user.northbridge_police");
            var row = UIFactory.StretchedPaintedButton("PoliceConversation", panel, "", "Sprites/UI/ButtonPaper", 16, UIFactory.Charcoal, () => ShowPoliceConversation(police));
            UIFactory.Layout((RectTransform)row.transform, new Vector2(.045f, .65f), new Vector2(.955f, .83f), Vector2.zero, Vector2.zero);
            var avatar = UIFactory.Rect("MessageAvatar", row.transform, new Vector2(0, .1f), new Vector2(.18f, .9f), new Vector2(8, 0), new Vector2(-5, 0), Color.clear);
            UIFactory.Artwork("PoliceMessageAvatar", avatar, police.avatar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            var name = UIFactory.Text("ConversationName", row.transform, "Полиция Нортбриджа  <color=#2477B8>✓</color>\n<size=13><color=#756B5D>Требуется удалить все публикации…</color></size>", 18, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(name.rectTransform, new Vector2(.2f, 0), new Vector2(.9f, 1), Vector2.zero, Vector2.zero);
            var date = UIFactory.Text("ConversationDate", row.transform, "сегодня", 12, UIFactory.Muted, TextAnchor.UpperRight);
            UIFactory.Layout(date.rectTransform, new Vector2(.78f, .63f), new Vector2(.96f, .94f), Vector2.zero, Vector2.zero);
        }

        private void ShowPoliceConversation(UserData police)
        {
            unreadMessages = 0;
            RefreshUnreadCounters();
            Clear(messagesContent);
            var panel = UIFactory.TexturedRect("PoliceConversationPage", messagesContent, "Art/UIPaperTexture", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            var back = UIFactory.StretchedPaintedButton("BackToMessages", panel, "←", "Sprites/UI/ButtonPaper", 19, UIFactory.Charcoal, ShowConversationList);
            UIFactory.Layout((RectTransform)back.transform, new Vector2(.025f, .89f), new Vector2(.11f, .97f), Vector2.zero, Vector2.zero);
            var sender = UIFactory.Button("PoliceMessageSender", panel, "Полиция Нортбриджа  <color=#2477B8>✓</color>\n<size=12><color=#574B3E>@northbridge_police</color></size>", 17, Color.clear, UIFactory.Charcoal, () => OpenProfile(police));
            UIFactory.Layout((RectTransform)sender.transform, new Vector2(.13f, .83f), new Vector2(.78f, .98f), Vector2.zero, Vector2.zero);
            sender.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            var official = UIFactory.Text("OfficialMark", panel, "ОФИЦИАЛЬНОЕ РАСПОРЯЖЕНИЕ · ДЕЛО №NB-1410", 12, UIFactory.Hex("8D2428"), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(official.rectTransform, new Vector2(.055f, .69f), new Vector2(.95f, .82f), Vector2.zero, Vector2.zero);
            var body = UIFactory.Text("PoliceMessageBody", panel,
                "Модератору FaceNet.\n\nТребуется удалить все публикации, фотографии и сообщения, содержащие сведения о станции Нортбридж и инциденте в служебной зоне. Распространение непроверенной информации мешает официальному расследованию.\n\nНачните с публикации пользователя @alex_92.",
                16, UIFactory.Charcoal, TextAnchor.UpperLeft);
            UIFactory.Layout(body.rectTransform, new Vector2(.055f, .16f), new Vector2(.95f, .69f), Vector2.zero, Vector2.zero);
            investigation.FindEvidence("evidence.police_order_read");
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
            var mode = UIFactory.Text("ModeratorMode", socialNav, "◆ РЕЖИМ МОДЕРАТОРА", 13, UIFactory.Hex("8D2428"), TextAnchor.MiddleCenter, FontStyle.Bold);
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
            var identity = UIFactory.Text("ProfileIdentity", profileContent, selected.displayName + (selected.isVerified ? "  <color=#2477B8>✓</color>" : "") + "\n<size=16><color=#574B3E>" + selected.username + "</color></size>", 27, UIFactory.Charcoal, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Layout(identity.rectTransform, new Vector2(.25f, .55f), new Vector2(.72f, .67f), Vector2.zero, Vector2.zero);
            var ban = UIFactory.PaintedButton("ProfileBanUser", profileContent, string.Empty, "Sprites/UI/ButtonRedHand", 12, UIFactory.Charcoal);
            UIFactory.Layout((RectTransform)ban.transform, new Vector2(.77f, .565f), new Vector2(.97f, .635f), Vector2.zero, Vector2.zero);
            var facts = UIFactory.Text("ProfileFacts", profileContent,
                "Регистрация: " + selected.registrationDate + "     " + selected.bio,
                13, UIFactory.Hex("574B3E"), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(facts.rectTransform, new Vector2(.25f, .49f), new Vector2(.96f, .55f), Vector2.zero, Vector2.zero);

            var tabs = UIFactory.Text("ProfileTabs", profileContent, "ПОСТЫ                     ОБ АККАУНТЕ                     МЕДИА                     СВЯЗИ", 13, UIFactory.Charcoal, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(tabs.rectTransform, new Vector2(.03f, .44f), new Vector2(.97f, .5f), Vector2.zero, Vector2.zero);
            UIFactory.Rect("ActiveTab", profileContent, new Vector2(.05f, .44f), new Vector2(.22f, .447f), Vector2.zero, Vector2.zero, UIFactory.Hex("5D2184"));

            var blockedBanner = UIFactory.Rect("UserBlockedBanner", profileContent, new Vector2(.18f, .16f), new Vector2(.82f, .42f), Vector2.zero, Vector2.zero, new Color(.18f, .04f, .05f, .96f));
            UIFactory.Text("BlockedText", blockedBanner, "ПОЛЬЗОВАТЕЛЬ ЗАБЛОКИРОВАН\n<size=14>Все публикации удалены из сети</size>", 25, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            var post = UIFactory.TexturedRect("ProfilePost", profileContent, "Art/UIPaperTexture", new Vector2(0, .02f), new Vector2(1, .43f), new Vector2(28, 0), new Vector2(-36, 0), Color.white);
            var postBackground = post.GetComponent<Image>();
            var postOutline = post.gameObject.AddComponent<Outline>();
            postOutline.effectColor = UIFactory.Charcoal;
            postOutline.effectDistance = new Vector2(2, -2);
            var miniAvatar = UIFactory.Rect("PostAuthorAvatar", post, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -70), new Vector2(68, -18), UIFactory.Charcoal);
            if (selected.avatar != null) UIFactory.Artwork("Portrait", miniAvatar, selected.avatar, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            var postTitle = UIFactory.Text("PostTitle", post, selected.displayName + (selected.isVerified ? "  <color=#2477B8>✓</color>" : "") + "\n<size=13><color=#574B3E>" + selected.username + "  •  " + postData.timestamp + "</color></size>", 17, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
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
            LinkedPostText.Build(profileBody, postData, 15, browser.OpenSearch, true);
            var time = UIFactory.Text("ProfilePostTime", post, postData.timestamp, 13, UIFactory.Hex("6B5B49"), TextAnchor.UpperLeft);
            UIFactory.Layout(time.rectTransform, new Vector2(.4f, .28f), new Vector2(.97f, .42f), Vector2.zero, Vector2.zero);

            var actions = UIFactory.Rect("ProfilePostActions", post, new Vector2(0, .025f), new Vector2(1, .17f), new Vector2(12, 0), new Vector2(-12, 0), UIFactory.Ink);
            var comment = UIFactory.PaintedButton("ProfileComment", actions, "ПРОВЕРИТЬ КОММЕНТАРИИ", "Sprites/UI/ButtonPaper", 10, UIFactory.Charcoal);
            UIFactory.Layout((RectTransform)comment.transform, new Vector2(0, .08f), new Vector2(.34f, .92f), Vector2.zero, Vector2.zero);
            var delete = UIFactory.PaintedButton("ProfileDeletePost", actions, string.Empty, "Sprites/UI/ButtonRedHand", 11, UIFactory.Charcoal);
            UIFactory.Layout((RectTransform)delete.transform, new Vector2(.36f, .08f), new Vector2(.61f, .92f), Vector2.zero, Vector2.zero);
            var counters = UIFactory.Text("ProfileCounters", actions, string.Empty, 13, UIFactory.Muted, TextAnchor.MiddleRight);
            UIFactory.Layout(counters.rectTransform, new Vector2(.63f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            var comments = UIFactory.Rect("ProfileComments", post, new Vector2(.41f, .2f), new Vector2(.98f, .81f), Vector2.zero, Vector2.zero, UIFactory.Ink);
            var commentsText = UIFactory.Text("CommentsText", comments,
                "КОММЕНТАРИИ\n\n@oldsignal: Это снято до закрытия?\n@commuter_14: Платформа уже была пустой.\n@archivist: Сохрани копию, пока она не исчезла.", 13, UIFactory.Paper, TextAnchor.UpperLeft);
            UIFactory.Layout(commentsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 12), new Vector2(-16, -12));
            comments.gameObject.SetActive(false);

            var banLabel = ban.GetComponentInChildren<Text>();
            var deleteLabel = delete.GetComponentInChildren<Text>();
            var deleteConfirmation = false;
            void RefreshProfile()
            {
                banLabel.text = selected.isBanned ? "РАЗБЛОКИРОВАТЬ" : "ЗАБЛОКИРОВАТЬ";
                deleteLabel.text = postData.deleted ? "УДАЛЕНО" : deleteConfirmation ? "ПОДТВЕРДИТЬ" : "УДАЛИТЬ ПОСТ";
                delete.interactable = !postData.deleted;
                counters.text = "♥ " + postData.likes + "   ● " + postData.comments + "   ↗ " + postData.shares;
                blockedBanner.gameObject.SetActive(selected.isBanned);
                post.gameObject.SetActive(!selected.isBanned);
                if (postBackground != null) postBackground.color = postData.deleted ? UIFactory.Hex("B94C50") : Color.white;
            }
            ban.onClick.AddListener(() =>
            {
                selected.isBanned = !selected.isBanned;
                investigation.SetBlocked(selected.id, selected.isBanned);
                foreach (var userPost in selected.posts)
                {
                    if (selected.isBanned && !userPost.deleted) { userPost.deleted = true; userPost.deletedByBan = true; }
                    else if (!selected.isBanned && userPost.deletedByBan) { userPost.deleted = false; userPost.deletedByBan = false; }
                }
                notifications.Show(selected.isBanned ? selected.username + ": аккаунт заблокирован, все посты удалены" : selected.username + ": аккаунт разблокирован, посты восстановлены");
                ShowUserCase(selected);
                RefreshQueueLabels();
                RefreshProfile();
            });
            delete.onClick.AddListener(() =>
            {
                if (!deleteConfirmation) { deleteConfirmation = true; notifications.Show("Нажмите «ПОДТВЕРДИТЬ», чтобы удалить пост"); }
                else { postData.deleted = true; investigation.SetPostDeleted(postData.id, true); deleteConfirmation = false; notifications.Show("Пост удалён модератором"); ShowUserCase(selected); }
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
            else if (xMin < .3f) { sprite = "Sprites/UI/Friends"; action = ShowProfileHistory; }
            else if (xMin < .5f) { sprite = "Sprites/UI/Bell"; action = ShowUnreadSummary; }
            else { sprite = "Sprites/UI/Mail"; action = ShowMessages; }
            var icon = UIFactory.SpriteButton("NavIcon", parent, sprite, action, Color.clear, 7);
            UIFactory.Layout((RectTransform)icon.transform, new Vector2(xMin, 0), new Vector2(xMax, 1), Vector2.zero, Vector2.zero);
            if (sprite == "Sprites/UI/Bell") alertsBadgeTexts.Add(AddUnreadBadge(icon, "ProfileAlertsBadge"));
            else if (sprite == "Sprites/UI/Mail") mailBadgeTexts.Add(AddUnreadBadge(icon, "ProfileMailBadge"));
            RefreshUnreadCounters();
        }

        private static Button AddTopNavButton(RectTransform parent, string name, string sprite, float xMin, float xMax,
            UnityEngine.Events.UnityAction action)
        {
            var button = UIFactory.SpriteButton(name, parent, sprite, action, Color.clear, 8);
            UIFactory.Layout((RectTransform)button.transform, new Vector2(xMin, 0), new Vector2(xMax, 1), Vector2.zero, Vector2.zero);
            return button;
        }

        private static Text AddUnreadBadge(Button icon, string name)
        {
            var badge = UIFactory.Artwork(name, icon.transform, UIFactory.LoadSprite("Sprites/UI/UnreadBadge"),
                new Vector2(.12f, .02f), new Vector2(.43f, .38f), Vector2.zero, Vector2.zero, true);
            badge.raycastTarget = false;
            var count = UIFactory.Text(name + "Count", badge.rectTransform, "0", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            count.raycastTarget = false;
            UIFactory.Layout(count.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return count;
        }

        private void RefreshUnreadCounters()
        {
            var unreadPosts = 0;
            foreach (var user in users)
            {
                if (!investigation.UnlockedUsers.Contains(user.id)) continue;
                foreach (var post in user.posts) if (!reviewedPostIds.Contains(post.id)) unreadPosts++;
            }
            SetBadges(mailBadgeTexts, unreadMessages);
            SetBadges(alertsBadgeTexts, unreadMessages + unreadPosts);
        }

        private static void SetBadges(List<Text> badges, int value)
        {
            for (var index = badges.Count - 1; index >= 0; index--)
            {
                var badge = badges[index];
                if (badge == null) { badges.RemoveAt(index); continue; }
                badge.transform.parent.gameObject.SetActive(value > 0);
                badge.text = value > 99 ? "99+" : value.ToString();
            }
        }

        private void ShowUnreadSummary()
        {
            if (socialWindow == null) BuildSocial();
            var unreadPosts = 0;
            foreach (var user in users)
                if (investigation.UnlockedUsers.Contains(user.id))
                    foreach (var post in user.posts) if (!reviewedPostIds.Contains(post.id)) unreadPosts++;

            Clear(postHost);
            var panel = UIFactory.TexturedRect("NotificationsPage", postHost, "Art/UIPaperTexture",
                new Vector2(.04f, .05f), new Vector2(.96f, .95f), Vector2.zero, Vector2.zero, Color.white);
            UIFactory.InkOutline(panel.gameObject, 2f);
            var title = UIFactory.Text("NotificationsTitle", panel, "УВЕДОМЛЕНИЯ", 24, UIFactory.Charcoal,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(.08f, .8f), new Vector2(.92f, .96f), Vector2.zero, Vector2.zero);

            var messages = UIFactory.StretchedPaintedButton("UnreadMessages", panel,
                "НЕПРОЧИТАННЫЕ СООБЩЕНИЯ     " + unreadMessages, "Sprites/UI/ButtonPaper", 15,
                UIFactory.Charcoal, ShowMessages);
            UIFactory.Layout((RectTransform)messages.transform, new Vector2(.08f, .61f), new Vector2(.92f, .76f), Vector2.zero, Vector2.zero);

            var posts = UIFactory.StretchedPaintedButton("UnreadPosts", panel,
                "ПОСТЫ НА ПРОВЕРКЕ     " + unreadPosts, "Sprites/UI/ButtonPaper", 15,
                UIFactory.Charcoal, () => ShowUserCase(selectedUser ?? users[0]));
            UIFactory.Layout((RectTransform)posts.transform, new Vector2(.08f, .42f), new Vector2(.92f, .57f), Vector2.zero, Vector2.zero);

            var hint = UIFactory.Text("NotificationsHint", panel,
                "Выберите раздел, чтобы перейти к новым материалам.", 14, UIFactory.Hex("574B3E"),
                TextAnchor.UpperCenter);
            UIFactory.Layout(hint.rectTransform, new Vector2(.08f, .18f), new Vector2(.92f, .36f), Vector2.zero, Vector2.zero);
            socialWindow.Open();
        }

        private void RenderFolderPost(RectTransform host, PostData post)
        {
            var owner = users.Find(user => user.posts.Contains(post));
            if (owner == null) return;
            host.gameObject.AddComponent<PostView>().Build(host, post, owner, () => OpenProfile(owner), notifications.Show, browser.OpenSearch,
                savedPost => { }, deletedPost => investigation.SetPostDeleted(deletedPost.id, true));
        }

        private static void Clear(RectTransform root)
        {
            ManualLayoutPersistence.CaptureBeforeRebuild(root);
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var stale = root.GetChild(index);
                stale.name = "Disposed_" + stale.name;
                stale.gameObject.SetActive(false);
                Destroy(stale.gameObject);
            }
            var view = root.GetComponent<PostView>();
            if (view != null) Destroy(view);
            ManualLayoutPersistence.RequestReapply(root);
        }

        private static string Initial(UserData user) => string.IsNullOrEmpty(user.displayName) ? "?" : user.displayName.Substring(0, 1).ToUpperInvariant();
    }
}
