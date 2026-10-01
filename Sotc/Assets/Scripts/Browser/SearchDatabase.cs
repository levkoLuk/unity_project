using System.Collections.Generic;
using UnityEngine;

namespace Moderator.Browser
{
    public sealed class SearchDatabase
    {
        private readonly Dictionary<string, WebPageData> pages = new();
        public IEnumerable<WebPageData> Pages => pages.Values;

        public SearchDatabase()
        {
            foreach (var asset in Resources.LoadAll<TextAsset>("GameData/WebPages"))
            {
                var page = JsonUtility.FromJson<WebPageData>(asset.text);
                if (page != null && !string.IsNullOrWhiteSpace(page.id)) pages[page.id] = page;
            }
        }

        public bool TryGet(string id, out WebPageData page) => pages.TryGetValue(id, out page);
    }
}
