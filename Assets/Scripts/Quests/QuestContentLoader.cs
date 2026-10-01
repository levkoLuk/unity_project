using System.Collections.Generic;
using UnityEngine;

namespace Moderator.Quests
{
    public static class QuestContentLoader
    {
        public static IReadOnlyList<QuestData> LoadAll()
        {
            var result = new List<QuestData>();
            foreach (var asset in Resources.LoadAll<TextAsset>("GameData/Quests"))
            {
                var quest = JsonUtility.FromJson<QuestData>(asset.text);
                if (quest != null && !string.IsNullOrWhiteSpace(quest.id)) result.Add(quest);
            }
            return result;
        }
    }
}
