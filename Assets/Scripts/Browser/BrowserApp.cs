using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Moderator.Investigation;
using Moderator.TimeSystem;
using Moderator.UI;
using Moderator.Windows;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moderator.Browser
{
    public sealed class BrowserApp : MonoBehaviour
    {
        private sealed class Visit { public string PageId; public string Title; public string Url; public string Time; }
        private sealed class TabState
        {
            public string Kind = "home";
            public string Value = "";
            public string Title = "Поиск";
        }

        private readonly List<TabState> tabs = new();
        private readonly List<TabState> tabTrail = new();
        private readonly List<TabState> forwardTrail = new();
        private readonly List<Visit> visits = new();
        private RectTransform desktop, tabStrip, page;
        private WindowManager manager;
        private NotificationController notifications;
        private InvestigationState investigation;
        private GameTimeService gameTime;
        private SearchDatabase database;
        private SearchEngine searchEngine;
        private WindowController window;
        private Text addressText;
        private InputField searchInput;
        private UnityAction<string> openProfile;
        private int activeTab;

        public void Initialize(RectTransform targetDesktop, WindowManager windowManager, NotificationController notificationController,
            InvestigationState investigationState, GameTimeService timeService)
        {
            desktop = targetDesktop; manager = windowManager; notifications = notificationController;
            investigation = investigationState; gameTime = timeService;
            database = new SearchDatabase(); searchEngine = new SearchEngine(database);
        }

        public void SetProfileOpener(UnityAction<string> callback) => openProfile = callback;
        public void Warmup()
        {
            EnsureBuilt();
            window.Close();
        }
        public void Open() { EnsureBuilt(); window.Open(); }
        public void OpenArticleHome() { EnsureBuilt(); OpenPageInNewTab("page.northbridge_portal"); window.Open(); }
        public void OpenMapArticle() => OpenSearch("Станция Нортбридж");
        public void OpenSearch(string query)
        {
            EnsureBuilt();
            if (database.TryGet(query, out _)) OpenPageInNewTab(query); else NavigateSearch(query);
            window.Open();
        }

        private TabState Active => tabs[Mathf.Clamp(activeTab, 0, tabs.Count - 1)];
        private void EnsureBuilt() { if (window == null) Build(); }

        private void Build()
        {
            window = WindowController.Create("BrowserWindow", "Браузер", desktop, manager, new Vector2(660, 610), new Vector2(-345, 18), out var content);
            var titleBar = content.parent.Find("TitleBar") as RectTransform;
            var oldTab = titleBar.Find("ActiveBrowserTab"); if (oldTab != null) oldTab.gameObject.SetActive(false);
            var oldTitle = titleBar.Find("Title"); if (oldTitle != null) oldTitle.gameObject.SetActive(false);
            var oldClose = titleBar.Find("TabClose"); if (oldClose != null) oldClose.gameObject.SetActive(false);
            tabStrip = UIFactory.Rect("TabStrip", titleBar, Vector2.zero, Vector2.one, new Vector2(3, 1), new Vector2(-145, -1), Color.clear);
            var toolbar = UIFactory.TexturedRect("Toolbar", content, "Art/UIPaperTexture", new Vector2(0, 1), Vector2.one, new Vector2(0, -58), Vector2.zero, Color.white);
            UIFactory.InkOutline(toolbar.gameObject, 2f);
            NavButton(toolbar, "Back", 8, GoBack); NavButton(toolbar, "Forward", 55, GoForward); NavButton(toolbar, "Reload", 102, Reload);
            UIFactory.Artwork("Lock", toolbar, UIFactory.LoadSprite("Sprites/UI/Lock"), Vector2.zero, Vector2.one, new Vector2(151, 11), new Vector2(178, -11));
            var address = UIFactory.Rect("Address", toolbar, Vector2.zero, Vector2.one, new Vector2(180, 8), new Vector2(-96, -8), UIFactory.Hex("F4E7C9"));
            UIFactory.InkOutline(address.gameObject, 2f);
            addressText = UIFactory.Text("URL", address, "", 15, UIFactory.Charcoal);
            UIFactory.Layout(addressText.rectTransform, Vector2.zero, Vector2.one, new Vector2(13, 0), new Vector2(-8, 0));
            var history = UIFactory.PaintedButton("History", toolbar, "И", "Sprites/UI/ButtonPaper", 15, UIFactory.Charcoal, ShowHistory);
            UIFactory.Layout((RectTransform)history.transform, new Vector2(1, 0), Vector2.one, new Vector2(-48, 8), new Vector2(-10, -8));
            page = UIFactory.TexturedRect("BrowserPage", content, "Art/UIInkTexture", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -64), Color.white);
            tabs.Add(new TabState()); activeTab = 0; tabTrail.Add(tabs[0]); RefreshTabs(); ShowSearchHome();
        }

        private static Button NavButton(RectTransform parent, string name, float x, UnityAction action)
        {
            var sprite = name == "Back" ? "Sprites/UI/Back" : name == "Forward" ? "Sprites/UI/Forward" : "Sprites/UI/Reload";
            var button = UIFactory.SpriteButton(name, parent, sprite, action, Color.clear, 3);
            UIFactory.Layout((RectTransform)button.transform, Vector2.zero, new Vector2(0, 1), new Vector2(x, 8), new Vector2(x + 39, -8));
            return button;
        }

        private void RefreshTabs()
        {
            Clear(tabStrip);
            const float availableWidth = 500f;
            var width = Mathf.Min(132f, availableWidth / Mathf.Max(1, tabs.Count));
            var showTitles = width >= 68f;
            for (var i = 0; i < tabs.Count; i++)
            {
                var index = i;
                var foreground = i == activeTab ? UIFactory.Charcoal : UIFactory.Paper;
                var sprite = i == activeTab ? "Sprites/UI/BrowserTabActiveHand" : "Sprites/UI/BrowserTabInactiveHand";
                var title = showTitles ? Ellipsize(TabTitle(tabs[i]), Mathf.Max(3, Mathf.FloorToInt((width - 30f) / 7f))) : string.Empty;
                var tab = UIFactory.StretchedPaintedButton("BrowserTab_" + i, tabStrip, title, sprite, 11, foreground, () => SelectTab(index));
                var tabRect = (RectTransform)tab.transform;
                tabRect.anchorMin = tabRect.anchorMax = new Vector2(0, .5f);
                tabRect.pivot = new Vector2(0, .5f);
                tabRect.sizeDelta = new Vector2(Mathf.Max(1, width - 3), 42);
                tabRect.anchoredPosition = new Vector2(i * width + 2, 0);
                var label = tab.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = showTitles ? new Vector2(-22, 0) : Vector2.zero;
                var close = UIFactory.Button("CloseTab_" + i, tab.transform, string.Empty, 15, Color.clear, foreground, () => CloseTab(index));
                UIFactory.Layout((RectTransform)close.transform, new Vector2(1, 0), Vector2.one, new Vector2(-29, 4), new Vector2(-5, -4));
            }
        }

        private static string Ellipsize(string value, int maxCharacters)
        {
            if (string.IsNullOrEmpty(value) || maxCharacters <= 0) return string.Empty;
            if (value.Length <= maxCharacters) return value;
            return maxCharacters <= 2 ? string.Empty : value.Substring(0, maxCharacters - 1) + "…";
        }

        private static string TabTitle(TabState tab)
        {
            if (tab == null || tab.Kind == "home" || tab.Kind == "search") return "Поиск";
            if (tab.Kind == "history") return "История";
            return ShortPageTitle(tab.Value, tab.Title);
        }

        private static string ShortPageTitle(string pageId, string fallback)
        {
            switch (pageId)
            {
                case "page.northbridge_portal": return "Нортбридж";
                case "page.photographer": return "Фотограф";
                default: return string.IsNullOrEmpty(fallback) ? "Страница" : fallback.Length > 13 ? fallback.Substring(0, 12) + "…" : fallback;
            }
        }
        private void CloseTab(int index)
        {
            var closing = tabs[index];
            tabs.RemoveAt(index); tabTrail.RemoveAll(tab => tab == closing); forwardTrail.RemoveAll(tab => tab == closing);
            if (tabs.Count == 0)
            {
                var home = new TabState(); tabs.Add(home); tabTrail.Clear(); tabTrail.Add(home); forwardTrail.Clear(); activeTab = 0;
                ShowSearchHome(); RefreshTabs(); window.Close(); return;
            }
            var previous = tabTrail.LastOrDefault(tab => tabs.Contains(tab)) ?? tabs[Mathf.Clamp(index - 1, 0, tabs.Count - 1)];
            ActivateTab(previous, false); RefreshTabs(); RenderActive();
        }
        private void SelectTab(int index) { ActivateTab(tabs[index], true); RefreshTabs(); RenderActive(); }

        private void ActivateTab(TabState target, bool record)
        {
            var index = tabs.IndexOf(target); if (index < 0) return;
            activeTab = index;
            if (!record) return;
            if (tabTrail.Count == 0 || tabTrail[^1] != target) tabTrail.Add(target);
            forwardTrail.Clear();
        }

        private void NavigatePage(string id, bool recordNavigation = true)
        {
            if (!database.TryGet(id, out var data)) { notifications.Show("Страница не найдена: " + id); return; }
            Active.Kind = "page"; Active.Value = id; Active.Title = data.title;
            RecordVisit(data); RenderPage(data); RefreshTabs();
        }

        private void OpenPageInNewTab(string id)
        {
            if (!database.TryGet(id, out var data)) { notifications.Show("Страница не найдена: " + id); return; }
            var existing = tabs.FirstOrDefault(tab => tab.Kind == "page" && tab.Value == id);
            if (existing != null) { ActivateTab(existing, true); RenderActive(); RefreshTabs(); window.Open(); return; }

            TabState target;
            if (tabs.Count == 1 && Active.Kind != "page") target = Active;
            else { target = new TabState(); tabs.Add(target); }
            target.Kind = "page"; target.Value = id; target.Title = data.title;
            ActivateTab(target, true); RecordVisit(data); RenderPage(data); RefreshTabs(); window.Open();
        }

        private void NavigateSearch(string query, bool recordNavigation = true)
        {
            query = (query ?? "").Trim(); if (query.Length == 0) return;
            Active.Kind = "search"; Active.Value = query; Active.Title = "Поиск: " + query;
            investigation.RecordSearch(query);
            RenderSearch(query); RefreshTabs();
        }

        private void RenderActive()
        {
            if (Active.Kind == "page") NavigatePage(Active.Value, false);
            else if (Active.Kind == "search") NavigateSearch(Active.Value, false);
            else if (Active.Kind == "history") ShowHistory(false);
            else ShowSearchHome();
        }

        private void ShowSearchHome()
        {
            Active.Kind = "home"; Active.Value = ""; Active.Title = "Поиск";
            Clear(page); addressText.text = "https://searcher.local";
            var brand = UIFactory.Text("Brand", page, "SEARCHER", 48, UIFactory.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(brand.rectTransform, new Vector2(.18f, .68f), new Vector2(.82f, .88f), Vector2.zero, Vector2.zero);
            var box = UIFactory.TexturedRect("SearchBox", page, "Art/UIPaperTexture", new Vector2(.14f, .49f), new Vector2(.75f, .61f), Vector2.zero, Vector2.zero, Color.white);
            UIFactory.InkOutline(box.gameObject, 2f); searchInput = CreateInput(box, "Поиск в сети...");
            var submit = UIFactory.SpriteButton("Search", page, "Sprites/UI/Search", () => NavigateSearch(searchInput.text), UIFactory.Hex("6B930E"), 8);
            UIFactory.Layout((RectTransform)submit.transform, new Vector2(.765f, .49f), new Vector2(.85f, .61f), Vector2.zero, Vector2.zero);
            RefreshTabs();
        }

        private static InputField CreateInput(RectTransform parent, string placeholderValue)
        {
            var input = parent.gameObject.AddComponent<InputField>();
            var value = UIFactory.Text("InputText", parent, "", 18, UIFactory.Charcoal, TextAnchor.MiddleLeft);
            UIFactory.Layout(value.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 2), new Vector2(-12, -2)); value.raycastTarget = true;
            var placeholder = UIFactory.Text("Placeholder", parent, placeholderValue, 18, UIFactory.Hex("756B5D"), TextAnchor.MiddleLeft);
            UIFactory.Layout(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 2), new Vector2(-12, -2));
            input.textComponent = value; input.placeholder = placeholder; input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private void RenderSearch(string query)
        {
            Clear(page); addressText.text = "https://searcher.local/?q=" + Uri.EscapeDataString(query);
            var title = UIFactory.Text("SearchTitle", page, "РЕЗУЛЬТАТЫ ПОИСКА", 27, UIFactory.Paper, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(.06f, .84f), new Vector2(.94f, .96f), Vector2.zero, Vector2.zero);
            var results = searchEngine.Search(query);
            if (results.Count == 0)
            {
                var none = UIFactory.Text("NoResults", page, "Ничего не найдено.", 19, UIFactory.Paper, TextAnchor.UpperLeft);
                UIFactory.Layout(none.rectTransform, new Vector2(.07f, .6f), new Vector2(.93f, .8f), Vector2.zero, Vector2.zero); return;
            }
            for (var i = 0; i < Mathf.Min(6, results.Count); i++)
            {
                var data = results[i].Page; var top = .82f - i * .125f;
                var card = UIFactory.PaintedButton("SearchResult_" + data.id, page, "", "Sprites/UI/ButtonPaper", 13, UIFactory.Charcoal, () => OpenPageInNewTab(data.id));
                UIFactory.Layout((RectTransform)card.transform, new Vector2(.055f, top - .105f), new Vector2(.945f, top), Vector2.zero, Vector2.zero);
                var label = card.GetComponentInChildren<Text>(); label.alignment = TextAnchor.MiddleLeft;
                label.text = "<b>" + data.title + "</b>\n<size=11><color=#415D63>" + data.url + "</color></size>\n<size=11>" + data.summary + "</size>";
                label.rectTransform.offsetMin = new Vector2(14, 3); label.rectTransform.offsetMax = new Vector2(-10, -3);
            }
        }

        private void RenderPage(WebPageData data)
        {
            Clear(page); addressText.text = data.url; investigation.VisitPage(data.id);
            foreach (var evidence in data.evidenceIds ?? new List<string>()) investigation.FindEvidence(evidence);
            var scroll = CreateScroll(page, out var content);
            AddHeader(content, data);
            if (!string.IsNullOrEmpty(data.imageResource))
            {
                var image = UIFactory.Artwork("PageImage", content, UIFactory.LoadSprite(data.imageResource), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                image.gameObject.AddComponent<LayoutElement>().preferredHeight = 240;
            }
            AddParagraph(content, data.content, data.inlineLinks, 16, UIFactory.Charcoal, 150);
            foreach (var article in data.news ?? new List<NewsArticleData>()) AddArticle(content, article);
            ResetScrollToTop(scroll, content);
            StartCoroutine(ResetScrollToTopNextFrame(scroll, content));
        }

        private static void ResetScrollToTop(ScrollRect scroll, RectTransform content)
        {
            if (scroll == null || content == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
        }

        private static IEnumerator ResetScrollToTopNextFrame(ScrollRect scroll, RectTransform content)
        {
            yield return null;
            ResetScrollToTop(scroll, content);
        }

        private static ScrollRect CreateScroll(RectTransform root, out RectTransform content)
        {
            var scrollHost = UIFactory.Rect("PageScroll", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            var viewport = UIFactory.TexturedRect("PageViewport", scrollHost, "Art/UIPaperTexture", Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5), Color.white);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = UIFactory.Rect("ScrollableContent", viewport, Vector2.up, Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 28, 16, 25); layout.spacing = 14; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollHost.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.vertical = true; scroll.scrollSensitivity = 42; scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private static void AddHeader(RectTransform content, WebPageData data)
        {
            var masthead = UIFactory.Rect("Masthead", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UIFactory.Hex("082744"));
            masthead.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
            var type = UIFactory.Text("SiteName", masthead, data.pageType.ToUpperInvariant(), 17, UIFactory.Paper, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(type.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-15, 0));
            var heading = UIFactory.Text("ArticleTitle", content, string.IsNullOrEmpty(data.heading) ? data.title : data.heading, 27, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;
            if (!string.IsNullOrEmpty(data.summary))
            {
                var summary = UIFactory.Text("PageSummary", content, data.summary, 15, UIFactory.Hex("574B3E"), TextAnchor.MiddleLeft, FontStyle.Italic);
                summary.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
            }
        }

        private void AddArticle(RectTransform content, NewsArticleData article)
        {
            var card = UIFactory.TexturedRect("News_" + article.id, content, "Art/UIPaperTexture", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            var cardLayout = card.gameObject.AddComponent<LayoutElement>();
            var commentsHeight = Mathf.Max(0, article.comments?.Count ?? 0) * 58;
            cardLayout.preferredHeight = 300 + commentsHeight + (string.IsNullOrEmpty(article.imageResource) ? 0 : 185);
            UIFactory.InkOutline(card.gameObject, 2f);
            var vertical = card.gameObject.AddComponent<VerticalLayoutGroup>(); vertical.padding = new RectOffset(18, 18, 14, 16); vertical.spacing = 9;
            vertical.childControlWidth = true; vertical.childControlHeight = true; vertical.childForceExpandWidth = true; vertical.childForceExpandHeight = false;
            var title = UIFactory.Text("NewsTitle", card, article.title, 23, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
            if (!string.IsNullOrEmpty(article.timestamp))
            {
                var stamp = UIFactory.Text("NewsTimestamp", card, article.timestamp, 12, UIFactory.Hex("756B5D"), TextAnchor.MiddleLeft);
                stamp.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
            }
            if (!string.IsNullOrEmpty(article.imageResource))
            {
                var image = UIFactory.Artwork("NewsImage", card, UIFactory.LoadSprite(article.imageResource), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                image.gameObject.AddComponent<LayoutElement>().preferredHeight = 180;
            }
            var hasDetails = !string.IsNullOrEmpty(article.details);
            var collapsibleDetails = article.id == "news.police_records" || article.id == "news.witness_photo";
            AddParagraph(card, article.body, article.inlineLinks, 15, UIFactory.Charcoal, hasDetails ? 58 : 100);
            if (hasDetails && collapsibleDetails)
            {
                var detailsBlock = UIFactory.Rect("NewsDetails", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
                detailsBlock.gameObject.AddComponent<LayoutElement>().preferredHeight = Mathf.Max(105, 42 + article.details.Length * .48f);
                WrappedInlineText.Build(detailsBlock, article.details, article.detailsLinks, 15, UIFactory.Charcoal, Follow);
                detailsBlock.gameObject.SetActive(false);
                var more = UIFactory.StretchedPaintedButton("NewsMore", card, "ПОДРОБНЕЕ", "Sprites/UI/ButtonPaper", 12, UIFactory.Charcoal);
                more.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
                var moreLabel = more.GetComponentInChildren<Text>();
                moreLabel.alignment = TextAnchor.MiddleCenter;
                more.onClick.AddListener(() =>
                {
                    detailsBlock.gameObject.SetActive(!detailsBlock.gameObject.activeSelf);
                    moreLabel.text = detailsBlock.gameObject.activeSelf ? "СВЕРНУТЬ" : "ПОДРОБНЕЕ";
                    cardLayout.preferredHeight += detailsBlock.gameObject.activeSelf ? detailsBlock.GetComponent<LayoutElement>().preferredHeight : -detailsBlock.GetComponent<LayoutElement>().preferredHeight;
                    if (detailsBlock.gameObject.activeSelf)
                        foreach (var evidence in article.evidenceIds ?? new List<string>()) investigation.FindEvidence(evidence);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(card);
                });
            }
            else
            {
                if (hasDetails) AddParagraph(card, article.details, article.detailsLinks, 15, UIFactory.Charcoal, 52);
                foreach (var evidence in article.evidenceIds ?? new List<string>()) investigation.FindEvidence(evidence);
            }
            if (article.comments != null && article.comments.Count > 0)
            {
                var label = UIFactory.Text("CommentsHeader", card, "КОММЕНТАРИИ  " + article.comments.Count, 14, UIFactory.Hex("5D2184"), TextAnchor.MiddleLeft, FontStyle.Bold);
                label.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
                foreach (var comment in article.comments) AddComment(card, comment);
            }
        }

        private void AddComment(RectTransform parent, CommentData comment)
        {
            var row = UIFactory.Rect("Comment", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UIFactory.Hex("E7D8B5"));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;
            var user = UIFactory.Button("CommentUser_" + comment.username, row, comment.username, 13, Color.clear, UIFactory.Hex("096F94"), () => OpenProfile(comment.username));
            UIFactory.Layout((RectTransform)user.transform, Vector2.zero, new Vector2(.27f, 1), new Vector2(4, 2), new Vector2(0, -2));
            var text = UIFactory.Rect("CommentText", row, new Vector2(.28f, 0), new Vector2(.84f, 1), Vector2.zero, Vector2.zero, Color.clear);
            WrappedInlineText.Build(text, comment.text, null, 13, UIFactory.Charcoal, Follow);
            var time = UIFactory.Text("CommentTime", row, comment.timestamp, 11, UIFactory.Muted, TextAnchor.MiddleRight);
            UIFactory.Layout(time.rectTransform, new Vector2(.84f, 0), Vector2.one, Vector2.zero, new Vector2(-5, 0));
        }

        private void AddParagraph(RectTransform parent, string textValue, List<InlineLinkData> links, int fontSize, Color color, float minimumHeight)
        {
            var block = UIFactory.Rect("LinkedParagraph", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            var element = block.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = Mathf.Max(minimumHeight, 42 + (textValue?.Length ?? 0) * .48f);
            WrappedInlineText.Build(block, textValue, links, fontSize, color, Follow);
        }

        private void Follow(InlineLinkData link)
        {
            if (!string.IsNullOrEmpty(link.evidenceId)) investigation.FindEvidence(link.evidenceId);
            if (!string.IsNullOrEmpty(link.profileUsername)) { OpenProfile(link.profileUsername); return; }
            if (!string.IsNullOrEmpty(link.targetPageId)) OpenPageInNewTab(link.targetPageId);
            else if (!string.IsNullOrEmpty(link.searchTerm)) NavigateSearch(link.searchTerm);
        }

        private void OpenProfile(string username)
        {
            if (openProfile == null) { notifications.Show("Профиль недоступен"); return; }
            openProfile.Invoke(username);
        }

        private void RecordVisit(WebPageData data)
        {
            visits.RemoveAll(visit => visit.PageId == data.id);
            visits.Add(new Visit { PageId = data.id, Title = data.title, Url = data.url, Time = gameTime.CurrentDateTime.ToString("HH:mm") });
            if (visits.Count > 30) visits.RemoveAt(0);
        }

        private void ShowHistory()
        {
            var existing = tabs.FirstOrDefault(tab => tab.Kind == "history");
            if (existing != null) { ActivateTab(existing, true); ShowHistory(false); RefreshTabs(); return; }
            TabState target;
            if (tabs.Count == 1 && Active.Kind != "page") target = Active;
            else { target = new TabState(); tabs.Add(target); }
            target.Kind = "history"; target.Value = ""; target.Title = "История";
            ActivateTab(target, true); ShowHistory(false); RefreshTabs();
        }
        private void ShowHistory(bool recordNavigation)
        {
            Active.Kind = "history"; Active.Value = ""; Active.Title = "История";
            Clear(page); addressText.text = "browser://history";
            var panel = UIFactory.TexturedRect("HistoryPanel", page, "Art/UIPaperTexture", new Vector2(.04f, .05f), new Vector2(.96f, .95f), Vector2.zero, Vector2.zero, Color.white);
            UIFactory.InkOutline(panel.gameObject, 2f);
            var title = UIFactory.Text("HistoryTitle", panel, "ИСТОРИЯ ПОСЕЩЕНИЙ", 25, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(.04f, .86f), new Vector2(.96f, .98f), Vector2.zero, Vector2.zero);
            if (visits.Count == 0) return;
            for (var row = 0; row < Mathf.Min(7, visits.Count); row++)
            {
                var visit = visits[visits.Count - 1 - row]; var y = .83f - row * .112f;
                var button = UIFactory.StretchedPaintedButton("HistoryPage_" + row, panel, "", "Sprites/UI/ButtonPaper", 13, UIFactory.Charcoal, () => OpenPageInNewTab(visit.PageId));
                UIFactory.Layout((RectTransform)button.transform, new Vector2(.25f, y - .075f), new Vector2(.75f, y), Vector2.zero, Vector2.zero);
                var label = button.GetComponentInChildren<Text>(); label.alignment = TextAnchor.MiddleLeft;
                label.text = ShortPageTitle(visit.PageId, visit.Title);
                label.fontSize = 15; label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.offsetMin = Vector2.zero; label.rectTransform.offsetMax = Vector2.zero;
            }
            RefreshTabs();
        }

        private void GoBack()
        {
            while (tabTrail.Count > 1)
            {
                var current = tabTrail[^1]; tabTrail.RemoveAt(tabTrail.Count - 1);
                if (tabs.Contains(current)) forwardTrail.Add(current);
                var previous = tabTrail[^1];
                if (!tabs.Contains(previous)) continue;
                activeTab = tabs.IndexOf(previous); RefreshTabs(); RenderActive(); return;
            }
        }

        private void GoForward()
        {
            while (forwardTrail.Count > 0)
            {
                var target = forwardTrail[^1]; forwardTrail.RemoveAt(forwardTrail.Count - 1);
                if (!tabs.Contains(target)) continue;
                activeTab = tabs.IndexOf(target); tabTrail.Add(target); RefreshTabs(); RenderActive(); return;
            }
        }
        private void Reload() => RenderActive();

        private static string SafeName(string value) => value.Replace(" ", "_").Replace("@", "");
        private static void Clear(RectTransform root)
        {
            ManualLayoutPersistence.CaptureBeforeRebuild(root);
            for (var i = root.childCount - 1; i >= 0; i--) { var child = root.GetChild(i); child.gameObject.SetActive(false); Destroy(child.gameObject); }
            ManualLayoutPersistence.RequestReapply(root);
        }
    }
}
