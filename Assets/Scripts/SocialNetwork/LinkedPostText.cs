using System;
using System.Collections.Generic;
using Moderator.Browser;
using Moderator.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moderator.SocialNetwork
{
    public static class LinkedPostText
    {
        public static void Build(RectTransform host, PostData post, int fontSize, UnityAction<string> search, bool darkText = false)
        {
            var textColor = darkText ? UIFactory.Charcoal : UIFactory.Paper;
            var linkedText = string.IsNullOrWhiteSpace(post.linkedText) && post.hasMapLink ? "map" : post.linkedText;
            var links = new List<InlineLinkData>();
            if (!string.IsNullOrWhiteSpace(linkedText) && post.text.IndexOf(linkedText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                links.Add(new InlineLinkData
                {
                    label = linkedText,
                    targetPageId = string.IsNullOrWhiteSpace(post.linkedSearchTerm) ? string.Empty : post.linkedSearchTerm,
                    searchTerm = string.IsNullOrWhiteSpace(post.linkedSearchTerm) ? linkedText : string.Empty
                });
            }
            WrappedInlineText.Build(host, post.text, links, fontSize, textColor,
                link => search(!string.IsNullOrEmpty(link.targetPageId) ? link.targetPageId : link.searchTerm));
        }
    }
}
