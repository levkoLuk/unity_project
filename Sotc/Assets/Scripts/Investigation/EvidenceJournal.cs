using System.Collections.Generic;
using Moderator.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.Investigation
{
    public sealed class EvidenceJournal : MonoBehaviour
    {
        private static readonly Dictionary<string, string> Labels = new()
        {
            { "evidence.alex_near_northbridge", "Алекс находился рядом со станцией Нортбридж" },
            { "evidence.station_closed", "Станцию Нортбридж закрыли в 23:40" },
            { "evidence.photographer_seen", "Очевидцы видели уходящего фотографа" },
            { "evidence.nightwatcher_same_car", "@nightwatcher упомянул ту же машину" },
            { "evidence.news_contradiction", "Northbridge News противоречит официальным данным" },
            { "evidence.police_confirms_closure", "Официальные записи подтверждают закрытие станции" },
            { "evidence.october_witness", "Найдено свидетельство от 14 октября" },
            { "evidence.unknown_car", "Снаружи ожидал неизвестный автомобиль" }
        };

        private InvestigationState state;
        private RectTransform desktop;
        private RectTransform panel;
        private Text content;

        public void Initialize(RectTransform target, InvestigationState investigation)
        {
            desktop = target;
            state = investigation;
            state.Changed += Refresh;
            var button = UIFactory.PaintedButton("EvidenceJournalIcon", desktop, "УЛИКИ", "Sprites/UI/ButtonPaper", 11, UIFactory.Charcoal, Toggle);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(116, 34);
            rect.anchoredPosition = new Vector2(125, 46);
        }

        private void Toggle()
        {
            if (panel == null) Build(); else panel.gameObject.SetActive(!panel.gameObject.activeSelf);
            if (panel.gameObject.activeSelf) { panel.SetAsLastSibling(); Refresh(); }
        }

        private void Build()
        {
            panel = UIFactory.TexturedRect("EvidenceJournal", desktop, "Art/UIPaperTexture", new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero, Color.white);
            panel.pivot = Vector2.zero;
            panel.sizeDelta = new Vector2(470, 330);
            panel.anchoredPosition = new Vector2(18, 86);
            UIFactory.InkOutline(panel.gameObject, 3f);
            var title = UIFactory.Text("EvidenceTitle", panel, "ЖУРНАЛ РАССЛЕДОВАНИЯ", 22, UIFactory.Charcoal, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Layout(title.rectTransform, new Vector2(.06f, .82f), new Vector2(.94f, .96f), Vector2.zero, Vector2.zero);
            content = UIFactory.Text("EvidenceList", panel, "", 15, UIFactory.Charcoal, TextAnchor.UpperLeft);
            UIFactory.Layout(content.rectTransform, new Vector2(.07f, .12f), new Vector2(.93f, .8f), Vector2.zero, Vector2.zero);
            var close = UIFactory.PaintedButton("CloseEvidence", panel, "ЗАКРЫТЬ", "Sprites/UI/ButtonPaper", 11, UIFactory.Charcoal, () => panel.gameObject.SetActive(false));
            UIFactory.Layout((RectTransform)close.transform, new Vector2(.7f, .03f), new Vector2(.93f, .12f), Vector2.zero, Vector2.zero);
        }

        private void Refresh()
        {
            if (content == null) return;
            var lines = new List<string>();
            foreach (var id in state.Evidence) if (Labels.TryGetValue(id, out var label)) lines.Add("✓  " + label);
            content.text = lines.Count == 0 ? "Улики пока не найдены." : string.Join("\n\n", lines);
        }
    }
}
