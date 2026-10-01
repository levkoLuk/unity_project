using System;
using System.Collections.Generic;
using System.Linq;
using Moderator.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moderator.Browser
{
    public sealed class WrappedInlineText : LayoutGroup
    {
        public float horizontalSpacing = 4f;
        public float verticalSpacing = 5f;
        private float measuredHeight;

        public static WrappedInlineText Build(RectTransform host, string value, IList<InlineLinkData> links, int fontSize,
            Color textColor, UnityAction<InlineLinkData> follow)
        {
            var flow = host.gameObject.AddComponent<WrappedInlineText>();
            var tokens = Tokenize(value ?? string.Empty, links ?? Array.Empty<InlineLinkData>());
            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token.Text)) continue;
                RectTransform rect;
                if (token.Link != null)
                {
                    var captured = token.Link;
                    var button = UIFactory.Button("InlineLink_" + SafeName(captured.label), host, token.Text, fontSize, Color.clear,
                        UIFactory.Hex("096F94"), () => follow.Invoke(captured));
                    button.transition = Selectable.Transition.None;
                    var hitArea = button.targetGraphic as Image;
                    if (hitArea != null) { hitArea.color = new Color(1, 1, 1, .001f); hitArea.raycastTarget = true; }
                    rect = (RectTransform)button.transform;
                    var label = button.GetComponentInChildren<Text>(); label.fontStyle = FontStyle.Bold; label.alignment = TextAnchor.MiddleLeft;
                }
                else
                {
                    var label = UIFactory.Text("TextToken", host, token.Text, fontSize, textColor, TextAnchor.MiddleLeft);
                    rect = label.rectTransform;
                }
                var text = rect.GetComponentInChildren<Text>();
                var element = rect.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = Mathf.Max(4, text.preferredWidth);
                element.preferredHeight = fontSize + 8;
            }
            return flow;
        }

        public override void CalculateLayoutInputHorizontal() { base.CalculateLayoutInputHorizontal(); SetLayout(); }
        public override void CalculateLayoutInputVertical() { SetLayout(); SetLayoutInputForAxis(measuredHeight, measuredHeight, -1, 1); }
        public override void SetLayoutHorizontal() => SetLayout();
        public override void SetLayoutVertical() => SetLayout();

        private void SetLayout()
        {
            var width = Mathf.Max(40, rectTransform.rect.width - padding.horizontal);
            var x = (float)padding.left; var y = (float)padding.top; var lineHeight = 0f;
            foreach (RectTransform child in rectChildren)
            {
                var childWidth = Mathf.Min(width, LayoutUtility.GetPreferredWidth(child));
                var childHeight = Mathf.Max(20, LayoutUtility.GetPreferredHeight(child));
                if (x > padding.left && x + childWidth > padding.left + width)
                {
                    x = padding.left; y += lineHeight + verticalSpacing; lineHeight = 0;
                }
                SetChildAlongAxis(child, 0, x, childWidth); SetChildAlongAxis(child, 1, y, childHeight);
                x += childWidth + horizontalSpacing; lineHeight = Mathf.Max(lineHeight, childHeight);
            }
            measuredHeight = y + lineHeight + padding.bottom;
        }

        private sealed class Token { public string Text; public InlineLinkData Link; }
        private static List<Token> Tokenize(string value, IList<InlineLinkData> links)
        {
            var result = new List<Token>(); var index = 0;
            while (index < value.Length)
            {
                var explicitLink = links.FirstOrDefault(candidate => !string.IsNullOrEmpty(candidate.label) &&
                    index + candidate.label.Length <= value.Length && string.Compare(value, index, candidate.label, 0, candidate.label.Length, StringComparison.OrdinalIgnoreCase) == 0);
                if (explicitLink != null)
                {
                    result.Add(new Token { Text = value.Substring(index, explicitLink.label.Length), Link = explicitLink });
                    index += explicitLink.label.Length;
                    continue;
                }
                var wordEnd = index;
                while (wordEnd < value.Length && !char.IsWhiteSpace(value[wordEnd])) wordEnd++;
                var word = value.Substring(index, wordEnd - index);
                var normalizedWord = word.Trim('"', '\'', '«', '»', '(', ')', '[', ']', '.', ',', ':', ';', '!', '?').ToLowerInvariant();
                if (normalizedWord.Contains("фотограф") || normalizedWord.Contains("нортбридж"))
                {
                    result.Add(new Token
                    {
                        Text = word,
                        Link = new InlineLinkData
                        {
                            label = word,
                            targetPageId = normalizedWord.Contains("фотограф") ? "page.photographer" : "page.northbridge_portal"
                        }
                    });
                    index = wordEnd;
                    while (index < value.Length && char.IsWhiteSpace(value[index])) index++;
                    continue;
                }
                var end = index + 1;
                while (end < value.Length && !char.IsWhiteSpace(value[end]))
                {
                    if (links.Any(candidate => !string.IsNullOrEmpty(candidate.label) && end + candidate.label.Length <= value.Length &&
                        string.Compare(value, end, candidate.label, 0, candidate.label.Length, StringComparison.OrdinalIgnoreCase) == 0)) break;
                    end++;
                }
                result.Add(new Token { Text = value.Substring(index, end - index) });
                index = end;
                while (index < value.Length && char.IsWhiteSpace(value[index])) index++;
            }
            return result;
        }

        private static string SafeName(string value) => value.Replace(" ", "_").Replace("@", "");
    }
}
