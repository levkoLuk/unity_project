#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Moderator.Quests;
using UnityEditor;
using UnityEngine;

namespace Moderator.EditorTools
{
    public sealed class QuestStudioWindow : EditorWindow
    {
        private const string QuestFolder = "Assets/Resources/GameData/Quests";
        private static readonly string[] ConditionTypes = { "after_minutes", "at_datetime", "objective_complete", "evidence_found", "user_blocked", "post_deleted", "quest_completed" };
        private static readonly string[] ConditionLabels = { "Через заданное время", "В указанную дату", "Задача выполнена", "Улика найдена", "Пользователь заблокирован", "Пост удалён", "Расследование завершено" };
        private QuestData quest = NewQuest();
        private Vector2 scroll;
        private string loadedPath;
        private string validationMessage = "Готово";

        [MenuItem("Moderator UI/Quest Studio")]
        public static void Open() => GetWindow<QuestStudioWindow>("Редактор расследований");

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(8);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("РАССЛЕДОВАНИЕ", EditorStyles.boldLabel);
            quest.id = EditorGUILayout.TextField("ID", quest.id);
            quest.title = EditorGUILayout.TextField("Название", quest.title);
            EditorGUILayout.LabelField("Описание");
            quest.description = EditorGUILayout.TextArea(quest.description, GUILayout.MinHeight(55));
            quest.autoStart = EditorGUILayout.Toggle("Автозапуск", quest.autoStart);

            EditorGUILayout.Space(12);
            DrawObjectives();
            EditorGUILayout.Space(12);
            DrawEvents();
            EditorGUILayout.Space(12);
            DrawActions("ПРИ ЗАПУСКЕ", quest.onStart);
            DrawActions("ПРИ ЗАВЕРШЕНИИ", quest.onComplete);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(validationMessage, validationMessage.StartsWith("OK") ? MessageType.Info : MessageType.Warning);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Создать", EditorStyles.toolbarButton)) { quest = NewQuest(); loadedPath = null; validationMessage = "Новое расследование"; }
            if (GUILayout.Button("Открыть JSON", EditorStyles.toolbarButton)) OpenJson();
            if (GUILayout.Button("Сохранить", EditorStyles.toolbarButton)) Save();
            if (GUILayout.Button("Сохранить как", EditorStyles.toolbarButton)) SaveAs();
            if (GUILayout.Button("Проверить", EditorStyles.toolbarButton)) Validate();
            if (GUILayout.Button("Открыть папку", EditorStyles.toolbarButton)) { EnsureFolder(); EditorUtility.RevealInFinder(QuestFolder); }
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.IsNullOrEmpty(loadedPath) ? "не сохранено" : Path.GetFileName(loadedPath));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawObjectives()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ЗАДАЧИ", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Добавить", GUILayout.Width(90))) quest.objectives.Add(new QuestObjectiveData());
            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < quest.objectives.Count; i++)
            {
                var objective = quest.objectives[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Задача {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Удалить", GUILayout.Width(70))) { quest.objectives.RemoveAt(i--); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                objective.id = EditorGUILayout.TextField("ID", objective.id);
                objective.description = EditorGUILayout.TextField("Описание", objective.description);
                objective.required = EditorGUILayout.Toggle("Обязательная", objective.required);
                DrawCondition(objective.condition);
                DrawActions("При выполнении", objective.onComplete);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawEvents()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("СОБЫТИЯ ПО ВРЕМЕНИ", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Добавить", GUILayout.Width(90))) quest.events.Add(new QuestEventData());
            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < quest.events.Count; i++)
            {
                var questEvent = quest.events[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Событие {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Удалить", GUILayout.Width(70))) { quest.events.RemoveAt(i--); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                questEvent.id = EditorGUILayout.TextField("ID", questEvent.id);
                questEvent.fireOnce = EditorGUILayout.Toggle("Только один раз", questEvent.fireOnce);
                DrawCondition(questEvent.condition);
                DrawActions("Действия", questEvent.actions);
                EditorGUILayout.EndVertical();
            }
        }

        private static void DrawCondition(QuestConditionData condition)
        {
            EditorGUILayout.LabelField("Условие", EditorStyles.miniBoldLabel);
            var index = Math.Max(0, Array.IndexOf(ConditionTypes, condition.type));
            condition.type = ConditionTypes[EditorGUILayout.Popup("Тип", index, ConditionLabels)];
            if (condition.type == "after_minutes") condition.minutes = EditorGUILayout.IntField("Через игровых минут", condition.minutes);
            else if (condition.type == "at_datetime") condition.dateTime = EditorGUILayout.TextField("Дата и время", condition.dateTime);
            else condition.targetId = EditorGUILayout.TextField("ID цели", condition.targetId);
            condition.negate = EditorGUILayout.Toggle("Инвертировать", condition.negate);
        }

        private static void DrawActions(string title, List<QuestActionData> list)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            if (GUILayout.Button("+ Уведомление", GUILayout.Width(120))) list.Add(new QuestActionData { type = "show_notification", text = "Новое событие" });
            EditorGUILayout.EndHorizontal();
            for (var i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i].text = EditorGUILayout.TextField(list[i].text);
                if (GUILayout.Button("×", GUILayout.Width(24))) list.RemoveAt(i--);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void OpenJson()
        {
            EnsureFolder();
            var path = EditorUtility.OpenFilePanel("Открыть расследование", QuestFolder, "json");
            if (string.IsNullOrEmpty(path)) return;
            try { quest = JsonUtility.FromJson<QuestData>(File.ReadAllText(path)); loadedPath = path; Validate(); }
            catch (Exception exception) { validationMessage = "Не удалось открыть: " + exception.Message; }
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(loadedPath)) { SaveAs(); return; }
            Write(loadedPath);
        }

        private void SaveAs()
        {
            EnsureFolder();
            var defaultName = SafeFileName(quest.id) + ".json";
            var path = EditorUtility.SaveFilePanel("Сохранить расследование", QuestFolder, defaultName, "json");
            if (!string.IsNullOrEmpty(path)) { loadedPath = path; Write(path); }
        }

        private void Write(string path)
        {
            Validate();
            if (!validationMessage.StartsWith("OK")) return;
            File.WriteAllText(path, JsonUtility.ToJson(quest, true));
            AssetDatabase.Refresh();
            validationMessage = "OK — сохранено: " + Path.GetFileName(path);
        }

        private void Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(quest.id)) errors.Add("Не указан ID расследования");
            if (string.IsNullOrWhiteSpace(quest.title)) errors.Add("Не указано название");
            var ids = new HashSet<string>();
            foreach (var objective in quest.objectives)
            {
                if (string.IsNullOrWhiteSpace(objective.id)) errors.Add("Не указан ID задачи");
                else if (!ids.Add(objective.id)) errors.Add("Повторяющийся ID задачи: " + objective.id);
                ValidateCondition(objective.condition, errors);
            }
            foreach (var questEvent in quest.events) ValidateCondition(questEvent.condition, errors);
            validationMessage = errors.Count == 0 ? "OK — данные расследования корректны" : string.Join("\n", errors);
        }

        private static void ValidateCondition(QuestConditionData condition, List<string> errors)
        {
            if (condition == null || Array.IndexOf(ConditionTypes, condition.type) < 0) errors.Add("Неизвестный тип условия");
            else if (condition.type == "after_minutes" && condition.minutes < 0) errors.Add("Время не может быть отрицательным");
            else if (condition.type == "at_datetime" && !DateTime.TryParseExact(condition.dateTime, "dd.MM.yyyy HH:mm", null,
                         System.Globalization.DateTimeStyles.None, out _)) errors.Add("Дата должна иметь формат ДД.ММ.ГГГГ ЧЧ:ММ");
        }

        private static QuestData NewQuest() => new QuestData
        {
            id = "quest.new_investigation",
            title = "Новое расследование",
            objectives = new List<QuestObjectiveData>(),
            events = new List<QuestEventData>(),
            onStart = new List<QuestActionData>(),
            onComplete = new List<QuestActionData>()
        };

        private static string SafeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "quest" : value;
        }

        private static void EnsureFolder()
        {
            if (!Directory.Exists(QuestFolder)) Directory.CreateDirectory(QuestFolder);
        }
    }
}
#endif
