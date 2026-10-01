using System;
using Moderator.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moderator.SocialNetwork
{
    public static class LinkedPostText
    {
        public static void Build(RectTransform host, PostData post, int fontSize, UnityAction openMapArticle, bool darkText = false)
        {
            var textColor = darkText ? UIFactory.Charcoal : UIFactory.Paper;
            if (!post.hasMapLink || post.text.IndexOf("map", StringComparison.OrdinalIgnoreCase) < 0)
            {
                UIFactory.Text("PostText", host, post.text, fontSize, textColor, TextAnchor.MiddleLeft);
                return;
            }

            var index = post.text.IndexOf("map", StringComparison.OrdinalIgnoreCase);
            var layout = host.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 2;

            AddSegment(host, post.text.Substring(0, index), fontSize, textColor);
            var link = UIFactory.Button("LinkedWord_map", host, "map", fontSize, UIFactory.Hex("B8D5DB"), UIFactory.Hex("096F94"), openMapArticle);
            var linkLayout = link.gameObject.AddComponent<LayoutElement>();
            linkLayout.preferredWidth = fontSize * 2.45f;
            linkLayout.flexibleWidth = 0;
            AddSegment(host, post.text.Substring(index + 3), fontSize, textColor);
        }

        private static void AddSegment(Transform parent, string value, int fontSize, Color color)
        {
            if (string.IsNullOrEmpty(value)) return;
            var text = UIFactory.Text("TextSegment", parent, value, fontSize, color, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 0;
        }
    }
}
