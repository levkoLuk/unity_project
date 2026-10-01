using System;
using System.Collections.Generic;
using Moderator.UI;
using Moderator.Investigation;

namespace Moderator.Quests
{
    public sealed class QuestActionContext
    {
        public NotificationController Notifications;
        public Action<string> CompleteObjective;
        public InvestigationState Investigation;
    }

    public sealed class QuestActionRunner
    {
        private readonly Dictionary<string, Action<QuestActionData, QuestActionContext>> handlers = new();

        public QuestActionRunner()
        {
            Register("show_notification", (data, context) => context.Notifications.Show(data.text));
            Register("complete_objective", (data, context) => context.CompleteObjective?.Invoke(data.targetId));
            Register("unlock_user", (data, context) => context.Investigation.UnlockUser(data.targetId));
            Register("complete_quest", (data, context) => context.Investigation.CompleteQuest(data.targetId));
        }

        public void Register(string type, Action<QuestActionData, QuestActionContext> handler) => handlers[type] = handler;

        public bool Execute(QuestActionData action, QuestActionContext context)
        {
            if (action == null || !handlers.TryGetValue(action.type, out var handler)) return false;
            handler(action, context);
            return true;
        }
    }
}
