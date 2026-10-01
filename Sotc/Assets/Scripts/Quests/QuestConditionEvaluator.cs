using System;
using System.Collections.Generic;
using System.Globalization;
using Moderator.TimeSystem;
using Moderator.Investigation;

namespace Moderator.Quests
{
    public sealed class QuestConditionContext
    {
        public GameTimeService Time;
        public int QuestStartedAtMinute;
        public Func<string, bool> IsObjectiveComplete;
        public InvestigationState Investigation;
    }

    public sealed class QuestConditionEvaluator
    {
        private readonly Dictionary<string, Func<QuestConditionData, QuestConditionContext, bool>> handlers = new();

        public QuestConditionEvaluator()
        {
            Register("after_minutes", (data, context) =>
                context.Time.TotalMinutes >= context.QuestStartedAtMinute + Math.Max(0, data.minutes));
            Register("at_datetime", AtDateTime);
            Register("objective_complete", (data, context) =>
                context.IsObjectiveComplete != null && context.IsObjectiveComplete(data.targetId));
            Register("evidence_found", (data, context) => context.Investigation.Evidence.Contains(data.targetId));
            Register("user_blocked", (data, context) => context.Investigation.BlockedUsers.Contains(data.targetId));
            Register("post_deleted", (data, context) => context.Investigation.DeletedPosts.Contains(data.targetId));
            Register("quest_completed", (data, context) => context.Investigation.CompletedQuests.Contains(data.targetId));
        }

        public void Register(string type, Func<QuestConditionData, QuestConditionContext, bool> handler) => handlers[type] = handler;

        public bool Evaluate(QuestConditionData condition, QuestConditionContext context)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.type)) return false;
            var result = handlers.TryGetValue(condition.type, out var handler) && handler(condition, context);
            return condition.negate ? !result : result;
        }

        private static bool AtDateTime(QuestConditionData data, QuestConditionContext context)
        {
            return DateTime.TryParseExact(data.dateTime, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out var target) && context.Time.CurrentDateTime >= target;
        }
    }
}
