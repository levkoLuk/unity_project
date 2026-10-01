using Moderator.Browser;
using Moderator.SocialNetwork;
using Moderator.UI;
using Moderator.Windows;
using Moderator.Quests;
using Moderator.TimeSystem;
using Moderator.Investigation;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.Desktop
{
    public sealed class DesktopController : MonoBehaviour
    {
        private RectTransform desktop;

        public void Build(RectTransform target)
        {
            desktop = target;
            var gameTime = target.gameObject.AddComponent<GameTimeService>();
            var investigation = target.gameObject.AddComponent<InvestigationState>();
            var manager = target.gameObject.AddComponent<WindowManager>();
            var notifications = target.gameObject.AddComponent<NotificationController>();
            notifications.Initialize(target);
            var browser = target.gameObject.AddComponent<BrowserApp>();
            browser.Initialize(target, manager, notifications, investigation, gameTime);
            var social = target.gameObject.AddComponent<SocialNetworkApp>();
            social.Initialize(target, manager, notifications, browser, investigation);
            browser.SetProfileOpener(social.OpenProfileByUsername);

            BuildWallpaper();
            CreateDesktopIcon("BrowserIcon", "B", "Браузер", "Sprites/UI/BrowserLogo", new Vector2(66, -78), browser.Open, UIFactory.Blue);
            CreateDesktopIcon("SocialIcon", "P", "Соцсеть", "Sprites/UI/SocialLogo", new Vector2(66, -190), social.Open, UIFactory.Hex("7B6D94"));
            var taskbar = BuildTaskbar();
            manager.InitializeTaskbar(taskbar);
            manager.RegisterTaskbarApplication("browser", "Sprites/UI/BrowserLogo", UIFactory.Blue, browser.Open);
            manager.RegisterTaskbarApplication("social", "Sprites/UI/SocialLogo", UIFactory.Hex("7B4AA8"), social.Open);
            target.gameObject.AddComponent<GameClock>();
            target.gameObject.AddComponent<QuestRuntime>().Initialize(gameTime, notifications, investigation);
            target.gameObject.AddComponent<StarterInvestigationDirector>().Initialize(target, investigation, gameTime, notifications);

            // Build the two heavy application windows while the desktop itself is loading.
            // Opening an app afterwards only activates its already-created hierarchy.
            browser.Warmup();
            social.Warmup();
            notifications.transform.SetAsLastSibling();
        }

        private void BuildWallpaper()
        {
            var wallpaper = UIFactory.Artwork("Wallpaper", desktop, UIFactory.LoadSprite("Art/DesktopWallpaperNeutral"), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false);
            wallpaper.type = Image.Type.Simple;
            wallpaper.transform.SetAsFirstSibling();
            var veil = UIFactory.Rect("WallpaperVeil", desktop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.01f, .025f, .05f, .16f));
            veil.SetAsFirstSibling();
            var id = UIFactory.Text("WorkstationId", desktop, "ТЕРМИНАЛ МОДЕРАЦИИ PULSE", 12, new Color(.78f, .71f, .56f, .72f), TextAnchor.UpperRight);
            UIFactory.Layout(id.rectTransform, new Vector2(.65f, .78f), new Vector2(.97f, .95f), Vector2.zero, Vector2.zero);
            id.rectTransform.SetAsFirstSibling();
        }

        private void CreateDesktopIcon(string name, string symbol, string label, string spritePath, Vector2 position, UnityEngine.Events.UnityAction action, Color color)
        {
            var button = UIFactory.Button(name, desktop, string.Empty, 12, new Color(0,0,0,0), Color.white, action);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(96, 102);
            rect.anchoredPosition = position;
            UIFactory.Artwork("Symbol", rect, UIFactory.LoadSprite(spritePath), new Vector2(.18f, .32f), new Vector2(.82f, .94f), Vector2.zero, Vector2.zero);
            var text = UIFactory.Text("Name", rect, label, 14, UIFactory.Paper, TextAnchor.LowerCenter);
            UIFactory.Layout(text.rectTransform, Vector2.zero, new Vector2(1, .34f), Vector2.zero, Vector2.zero);
        }

        private RectTransform BuildTaskbar()
        {
            var bar = UIFactory.Rect("Taskbar", desktop, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 42), new Color(.045f, .06f, .075f, .96f));
            var start = UIFactory.Text("Terminal", bar, "▣  СТОЛ МОДЕРАТОРА", 13, UIFactory.Paper, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(start.rectTransform, Vector2.zero, new Vector2(.3f, 1), new Vector2(16, 0), Vector2.zero);
            var clock = UIFactory.Text("Clock", bar, "22.11.2034  00:00", 12, UIFactory.Muted, TextAnchor.MiddleRight);
            UIFactory.Layout(clock.rectTransform, new Vector2(.6f, 0), Vector2.one, Vector2.zero, new Vector2(-16, 0));
            return bar;
        }
    }
}
