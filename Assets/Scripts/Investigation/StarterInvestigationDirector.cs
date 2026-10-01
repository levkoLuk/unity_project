using Moderator.TimeSystem;
using Moderator.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.Investigation
{
    public sealed class StarterInvestigationDirector : MonoBehaviour
    {
        private InvestigationState state;
        private GameTimeService time;
        private NotificationController notifications;
        private RectTransform desktop;
        private bool alexBanSeen;
        private int alexBanMinute;
        private bool motherMessage;
        private bool finalShown;

        public void Initialize(RectTransform target, InvestigationState investigation, GameTimeService gameTime, NotificationController notificationController)
        {
            desktop = target;
            state = investigation;
            time = gameTime;
            notifications = notificationController;
            notifications.Show("ЗАДАНИЕ МОДЕРАТОРА: проверьте @alex_92 и заблокируйте аккаунт");
        }

        private void Update()
        {
            if (!alexBanSeen && state.BlockedUsers.Contains("user.alex_92"))
            {
                alexBanSeen = true;
                alexBanMinute = time.TotalMinutes;
            }

            if (alexBanSeen && !motherMessage && time.TotalMinutes >= alexBanMinute + 5)
            {
                motherMessage = true;
                notifications.Show("Почему вы заблокировали моего сына? Вы хотя бы проверили, чем он занимался?");
                Invoke(nameof(ShowFirstInvestigation), 2.2f);
            }

            if (!finalShown && state.CompletedQuests.Contains("quest.wrong_account"))
            {
                finalShown = true;
                state.CompleteQuest("quest.wrong_account");
                ShowFinalWarning();
            }
        }

        private bool Has(params string[] ids)
        {
            foreach (var id in ids) if (!state.Evidence.Contains(id)) return false;
            return true;
        }

        private void ShowFirstInvestigation() => notifications.Show("НОВОЕ РАССЛЕДОВАНИЕ: выясните, кто такой Алекс.");

        private void ShowFinalWarning()
        {
            var shade = UIFactory.Rect("SystemWarningShade", desktop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, .72f));
            var panel = UIFactory.TexturedRect("SystemWarning", shade, "Art/UIPaperTexture", new Vector2(.23f, .25f), new Vector2(.77f, .75f), Vector2.zero, Vector2.zero, Color.white);
            UIFactory.InkOutline(panel.gameObject, 3f);
            var title = UIFactory.Text("WarningTitle", panel, "СИСТЕМА МОДЕРАЦИИ", 25, UIFactory.Hex("8D2428"), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(.06f, .74f), new Vector2(.94f, .94f), Vector2.zero, Vector2.zero);
            var text = UIFactory.Text("WarningText", panel,
                "ОБНАРУЖЕНА НОВАЯ СВЯЗЬ\n\n@alex_92\n↓\n@nightwatcher\n↓\n@northbridge_news\n\nВаше расследование выходит за рамки задания.\nПрекратите поиски.",
                17, UIFactory.Charcoal, TextAnchor.MiddleCenter);
            UIFactory.Layout(text.rectTransform, new Vector2(.08f, .2f), new Vector2(.92f, .75f), Vector2.zero, Vector2.zero);
            var ok = UIFactory.PaintedButton("WarningOK", panel, "OK", "Sprites/UI/ButtonPaper", 14, UIFactory.Charcoal,
                () => { Destroy(shade.gameObject); notifications.Show("Продолжайте модерацию."); });
            UIFactory.Layout((RectTransform)ok.transform, new Vector2(.36f, .05f), new Vector2(.64f, .18f), Vector2.zero, Vector2.zero);
        }
    }
}
