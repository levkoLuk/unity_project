using System;
using System.Collections.Generic;
using System.Linq;
using Moderator.Investigation;

namespace Moderator.Browser
{
    public sealed class SearchEngine
    {
        private readonly SearchDatabase database;
        public SearchEngine(SearchDatabase source) => database = source;

        public IReadOnlyList<SearchResultData> Search(string rawQuery)
        {
            var query = InvestigationState.Normalize(rawQuery);
            if (string.IsNullOrEmpty(query)) return Array.Empty<SearchResultData>();
            var terms = query.Split(' ');
            return database.Pages.Select(page => new SearchResultData { Page = page, Score = Score(page, query, terms) })
                .Where(result => result.Score > 0).OrderByDescending(result => result.Score).ThenBy(result => result.Page.title).ToList();
        }

        private static int Score(WebPageData page, string query, IEnumerable<string> terms)
        {
            var score = 0;
            foreach (var keyword in page.searchableKeywords ?? new List<string>())
            {
                var normalized = InvestigationState.Normalize(keyword);
                if (normalized == query) score = Math.Max(score, 100);
                else if (normalized.Contains(query) || query.Contains(normalized)) score = Math.Max(score, 60);
                else score += terms.Count(term => term.Length > 2 && normalized.Contains(term)) * 8;
            }
            if (InvestigationState.Normalize(page.title).Contains(query)) score += 30;
            if (InvestigationState.Normalize(page.heading).Contains(query)) score += 20;
            foreach (var article in page.news ?? new List<NewsArticleData>())
            {
                if (InvestigationState.Normalize(article.title).Contains(query)) score += 24;
                if (InvestigationState.Normalize(article.body).Contains(query)) score += 10;
            }
            return score;
        }
    }
}
