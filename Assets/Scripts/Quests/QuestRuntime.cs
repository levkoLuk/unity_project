using System.Collections.Generic;
using Moderator.TimeSystem;
using Moderator.UI;
using UnityEngine;
using Moderator.Investigation;

namespace Moderator.Quests
{
    public sealed class QuestRuntime : MonoBehaviour
    {
        private sealed class State
        {
            public QuestData Data;
            public int StartedAt;
            public readonly HashSet<string> CompletedObjectives = new();
            public readonly HashSet<string> FiredEvents = new();
            public bool Completed;
        }

        private readonly List<State> active = new();
        private QuestConditionEvaluator conditions;
        private QuestActionRunner actions;
        private GameTimeService gameTime;
        private NotificationController notifications;
        private InvestigationState investigation;
        private float nextEvaluation;

        public void Initialize(GameTimeService timeService, NotificationController notificationController, InvestigationState investigationState)
        {
            gameTime = timeService;
            notifications = notificationController;
            investigation = investigationState;
            conditions = new QuestConditionEvaluator();
            actions = new QuestActionRunner();
            Reload();
        }

        public void Reload()
        {
            active.Clear();
            foreach (var quest in QuestContentLoader.LoadAll())
                if (quest.autoStart) StartQuest(quest);
        }

        private void StartQuest(QuestData quest)
        {
            var state = new State { Data = quest, StartedAt = gameTime.TotalMinutes };
            active.Add(state);
            Run(quest.onStart, state);
        }

        private void Update()
        {
            if (gameTime == null || Time.unscaledTime < nextEvaluation) return;
            nextEvaluation = Time.unscaledTime + .25f;
            foreach (var state in active) Evaluate(state);
        }

        private void Evaluate(State state)
        {
            var context = Context(state);
            if (!state.Completed)
            {
                foreach (var objective in state.Data.objectives)
                {
                    if (state.CompletedObjectives.Contains(objective.id) || !conditions.Evaluate(objective.condition, context)) continue;
                    state.CompletedObjectives.Add(objective.id);
                    Run(objective.onComplete, state);
                }
            }

            foreach (var questEvent in state.Data.events)
            {
                if (questEvent.fireOnce && state.FiredEvents.Contains(questEvent.id)) continue;
                if (!conditions.Evaluate(questEvent.condition, context)) continue;
                state.FiredEvents.Add(questEvent.id);
                Run(questEvent.actions, state);
            }

            if (state.Completed) return;
            foreach (var objective in state.Data.objectives)
                if (objective.required && !state.CompletedObjectives.Contains(objective.id)) return;

            state.Completed = true;
            Run(state.Data.onComplete, state);
        }

        private QuestConditionContext Context(State state) => new QuestConditionContext
        {
            Time = gameTime,
            QuestStartedAtMinute = state.StartedAt,
            IsObjectiveComplete = state.CompletedObjectives.Contains
            , Investigation = investigation
        };

        private void Run(IEnumerable<QuestActionData> list, State state)
        {
            if (list == null) return;
            var context = new QuestActionContext
            {
                Notifications = notifications,
                CompleteObjective = id => state.CompletedObjectives.Add(id)
                , Investigation = investigation
            };
            foreach (var action in list) actions.Execute(action, context);
        }
    }
}
