using System;
using System.Collections.Generic;

namespace Moderator.Browser
{
    [Serializable]
    public sealed class WebPageData
    {
        public string id;
        public string title;
        public string url;
        public string pageType = "сайт";
        public string summary;
        public string heading;
        public string content;
        public string imageResource;
        public List<string> searchableKeywords = new();
        public List<InlineLinkData> inlineLinks = new();
        public List<NewsArticleData> news = new();
        public List<string> evidenceIds = new();
    }

    [Serializable]
    public sealed class NewsArticleData
    {
        public string id;
        public string title;
        public string timestamp;
        public string body;
        public string details;
        public string imageResource;
        public List<InlineLinkData> inlineLinks = new();
        public List<InlineLinkData> detailsLinks = new();
        public List<CommentData> comments = new();
        public List<string> evidenceIds = new();
    }

    [Serializable]
    public sealed class CommentData
    {
        public string username;
        public string text;
        public string timestamp;
    }

    [Serializable]
    public sealed class InlineLinkData
    {
        public string label;
        public string targetPageId;
        public string searchTerm;
        public string profileUsername;
        public string evidenceId;
    }

    public sealed class SearchResultData
    {
        public WebPageData Page;
        public int Score;
    }
}
