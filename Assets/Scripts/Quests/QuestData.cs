using System;
using System.Collections.Generic;

namespace Moderator.Quests
{
    [Serializable]
    public sealed class QuestData
    {
        public int schemaVersion = 1;
        public string id = "quest.new";
        public string title = "Новое расследование";
        public string description = "";
        public bool autoStart = true;
        public List<QuestActionData> onStart = new();
        public List<QuestObjectiveData> objectives = new();
        public List<QuestEventData> events = new();
        public List<QuestActionData> onComplete = new();
    }

    [Serializable]
    public sealed class QuestObjectiveData
    {
        public string id = "objective.new";
        public string description = "Новая задача";
        public bool required = true;
        public QuestConditionData condition = new();
        public List<QuestActionData> onComplete = new();
    }

    [Serializable]
    public sealed class QuestEventData
    {
        public string id = "event.new";
        public bool fireOnce = true;
        public QuestConditionData condition = new();
        public List<QuestActionData> actions = new();
    }

    [Serializable]
    public sealed class QuestConditionData
    {
        public string type = "after_minutes";
        public int minutes;
        public string dateTime = "22.11.2034 00:00";
        public string targetId = "";
        public bool negate;
    }

    [Serializable]
    public sealed class QuestActionData
    {
        public string type = "show_notification";
        public string text = "";
        public string targetId = "";
        public int delayMinutes;
    }
}
