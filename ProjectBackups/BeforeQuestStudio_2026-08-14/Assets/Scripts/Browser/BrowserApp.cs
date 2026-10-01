using Moderator.UI;
using Moderator.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.Browser
{
    public sealed class BrowserApp : MonoBehaviour
    {
        private RectTransform desktop;
        private RectTransform page;
        private WindowManager manager;
        private NotificationController notifications;
        private WindowController window;
        private Text addressText;
        private bool showingMapArticle;

        public void Initialize(RectTransform targetDesktop, WindowManager windowManager, NotificationController notificationController)
        {
            desktop = targetDesktop;
            manager = windowManager;
            notifications = notificationController;
        }

        public void Open()
        {
            if (window == null) Build();
            window.Open();
        }

        public void OpenArticleHome()
        {
            if (window == null) Build();
            ShowMapArticle();
            window.Open();
        }

        public void OpenMapArticle()
        {
            if (window == null) Build();
            ShowMapArticle();
            window.Open();
            notifications.Show("Linked article opened in Browser");
        }

private void Build()
        {
            window = WindowController.Create("BrowserWindow", "Web Browser", desktop, manager, new Vector2(660, 610), new Vector2(-345, 18), out var content);
            var toolbar = UIFactory.TexturedRect("Toolbar", content, "Art/UIPaperTexture", new Vector2(0, 1), Vector2.one, new Vector2(0, -58), Vector2.zero, Color.white);
            UIFactory.InkOutline(toolbar.gameObject, 2f);

            NavButton(toolbar, "Back", string.Empty, 8, ShowSearchHome);
            NavButton(toolbar, "Forward", string.Empty, 55, () => notifications.Show("No next page"));
            NavButton(toolbar, "Reload", string.Empty, 102, Reload);
            UIFactory.Artwork("Lock", toolbar, UIFactory.LoadSprite("Sprites/UI/Lock"), Vector2.zero, Vector2.one,
                new Vector2(151, 11), new Vector2(178, -11));

            var address = UIFactory.Rect("Address", toolbar, Vector2.zero, Vector2.one, new Vector2(180, 8), new Vector2(-12, -8), UIFactory.Hex("F4E7C9"));
            UIFactory.InkOutline(address.gameObject, 2f);
            addressText = UIFactory.Text("URL", address, string.Empty, 16, UIFactory.Charcoal);
            UIFactory.Layout(addressText.rectTransform, Vector2.zero, Vector2.one, new Vector2(13, 0), new Vector2(-8, 0));

            page = UIFactory.TexturedRect("BrowserPage", content, "Art/UIInkTexture", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -64), Color.white);
            UIFactory.InkOutline(page.gameObject, 2f);
            ShowSearchHome();
        }

        private static Button NavButton(RectTransform parent, string name, string label, float x, UnityEngine.Events.UnityAction action)
        {
            var spritePath = name == "Back" ? "Sprites/UI/Back" : name == "Forward" ? "Sprites/UI/Forward" : "Sprites/UI/Reload";
            var button = UIFactory.SpriteButton(name, parent, spritePath, action, Color.clear, 3);
            UIFactory.Layout((RectTransform)button.transform, Vector2.zero, new Vector2(0, 1), new Vector2(x, 8), new Vector2(x + 39, -8));
            return button;
        }

        private void Reload()
        {
            if (showingMapArticle) ShowMapArticle(); else ShowSearchHome();
            notifications.Show("Page reloaded");
        }

private void ShowSearchHome()
        {
            showingMapArticle = false;
            ClearPage();
            addressText.text = "https://searcher.local";

            var brand = UIFactory.Text("Brand", page, "SEARCHER", 52, UIFactory.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(brand.rectTransform, new Vector2(.18f, .67f), new Vector2(.82f, .87f), Vector2.zero, Vector2.zero);
            UIFactory.Rect("BrandUnderline", page, new Vector2(.38f, .67f), new Vector2(.62f, .68f), Vector2.zero, Vector2.zero, UIFactory.Hex("1A91BD"));

            var search = UIFactory.TexturedRect("SearchBox", page, "Art/UIPaperTexture", new Vector2(.16f, .53f), new Vector2(.74f, .64f), Vector2.zero, Vector2.zero, Color.white);
            UIFactory.InkOutline(search.gameObject, 2f);
            var placeholder = UIFactory.Text("Placeholder", search, "Search the web...", 19, UIFactory.Charcoal);
            UIFactory.Layout(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-8, 0));
            var submit = UIFactory.SpriteButton("Search", page, "Sprites/UI/Search", OpenMapArticle, UIFactory.Hex("6B930E"), 8);
            UIFactory.Layout((RectTransform)submit.transform, new Vector2(.755f, .53f), new Vector2(.84f, .64f), Vector2.zero, Vector2.zero);

            var mapResult = UIFactory.PaintedButton("MapSearchResult", page, "OPEN TRANSIT MAP", "Sprites/UI/ButtonPaper", 12, UIFactory.Charcoal, OpenMapArticle);
            UIFactory.Layout((RectTransform)mapResult.transform, new Vector2(.34f, .39f), new Vector2(.66f, .48f), Vector2.zero, Vector2.zero);
        }

        private void OpenMapFromSearch() => OpenMapArticle();

        private static void AddTrend(RectTransform parent, string value, float y, Color color)
        {
            var text = UIFactory.Text("Trend", parent, value, 17, color, TextAnchor.MiddleLeft);
            UIFactory.Layout(text.rectTransform, new Vector2(0, y), new Vector2(1, y + .2f), new Vector2(22, 0), new Vector2(-12, 0));
        }

        private void ShowMapArticle()
        {
            showingMapArticle = true;
            ClearPage();
            addressText.text = "https://dailyobserver.local/archive/map";

            UIFactory.TexturedRect("ArticlePaper", page, "Art/UIPaperTexture", Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5), Color.white);
            var masthead = UIFactory.Rect("Masthead", page, new Vector2(0, .84f), Vector2.one, new Vector2(6, 0), new Vector2(-6, -6), UIFactory.Hex("082744"));
            var eye = UIFactory.Text("Eye", masthead, "◉", 27, UIFactory.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(eye.rectTransform, Vector2.zero, new Vector2(.09f, 1), Vector2.zero, Vector2.zero);
            var paperName = UIFactory.Text("PaperName", masthead, "DAILY OBSERVER", 22, UIFactory.Paper, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(paperName.rectTransform, new Vector2(.08f, 0), new Vector2(.55f, 1), Vector2.zero, Vector2.zero);
            UIFactory.Rect("ArticleNavBack", page, new Vector2(0, .76f), new Vector2(1, .84f), new Vector2(6, 0), new Vector2(-6, 0), UIFactory.Hex("12354D"));
            UIFactory.Rect("ActiveSection", page, new Vector2(.23f, .76f), new Vector2(.34f, .84f), Vector2.zero, Vector2.zero, UIFactory.Parchment);
            var nav = UIFactory.Text("ArticleNav", page, "Home      World      CITY      Tech      Culture      Opinion                         ⌕", 15, UIFactory.Paper, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(nav.rectTransform, new Vector2(0, .76f), new Vector2(1, .84f), new Vector2(24, 0), new Vector2(-20, 0));
            nav.transform.SetAsLastSibling();

            var headline = UIFactory.Text("ArticleTitle", page, "CITY POWER OUTAGE", 30, UIFactory.Charcoal, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Layout(headline.rectTransform, new Vector2(.035f, .62f), new Vector2(.69f, .75f), Vector2.zero, Vector2.zero);
            var sub = UIFactory.Text("ArticleSubtitle", page, "Downtown without electricity", 17, UIFactory.Hex("4D4338"), TextAnchor.UpperLeft);
            UIFactory.Layout(sub.rectTransform, new Vector2(.035f, .56f), new Vector2(.69f, .63f), Vector2.zero, Vector2.zero);
            var imageFrame = UIFactory.Rect("ArticleImage", page, new Vector2(.035f, .14f), new Vector2(.64f, .55f), Vector2.zero, Vector2.zero, UIFactory.Charcoal);
            var articleImage = UIFactory.Artwork("Photo", imageFrame, UIFactory.LoadSprite("Art/ArticlePowerOutage"), Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            articleImage.preserveAspect = true;
            var body = UIFactory.Text("ArticleBody", page,
                "A sudden power outage hit several blocks downtown late last night. Authorities are investigating the cause.\n\nBy Jane Reporter  •  2 hours ago",
                15, UIFactory.Charcoal, TextAnchor.UpperLeft);
            UIFactory.Layout(body.rectTransform, new Vector2(.035f, .01f), new Vector2(.66f, .14f), Vector2.zero, Vector2.zero);

            var latest = UIFactory.Rect("LatestNews", page, new Vector2(.68f, .14f), new Vector2(.965f, .73f), Vector2.zero, Vector2.zero, new Color(.9f, .84f, .7f, .72f));
            UIFactory.InkOutline(latest.gameObject, 1.5f);
            var latestText = UIFactory.Text("LatestText", latest,
                "LATEST NEWS\n\nLocal artist exhibition opens this weekend\n────────────\nRoad construction update\n────────────\nNew downtown cafe hosts live music\n────────────\nMissing cat found near Oak Street",
                14, UIFactory.Charcoal, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Layout(latestText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 14), new Vector2(-12, -12));
            AddScrollbar(page);
        }

        private static void AddScrollbar(RectTransform parent)
        {
            var track = UIFactory.Rect("PageScrollbar", parent, new Vector2(.976f, .02f), new Vector2(.994f, .75f), Vector2.zero, Vector2.zero, UIFactory.Hex("29113E"));
            UIFactory.InkOutline(track.gameObject, 1.5f);
            var thumb = UIFactory.Rect("ScrollbarThumb", track, new Vector2(.12f, .44f), new Vector2(.88f, .83f), Vector2.zero, Vector2.zero, UIFactory.Hex("74419A"));
            UIFactory.InkOutline(thumb.gameObject, 1f);
        }

        private void ClearPage()
        {
            for (var index = page.childCount - 1; index >= 0; index--)
            {
                var stale = page.GetChild(index);
                stale.name = "Disposed_" + stale.name;
                stale.gameObject.SetActive(false);
                Destroy(stale.gameObject);
            }
            ManualLayoutPersistence.RequestReapply(page);
        }
    }
}
