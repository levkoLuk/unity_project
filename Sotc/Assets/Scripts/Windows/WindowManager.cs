using System.Collections.Generic;
using System.Linq;
using Moderator.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Moderator.Windows
{
    public sealed class WindowManager : MonoBehaviour
    {
        private readonly List<WindowController> windows = new();
        private readonly Dictionary<string, TaskbarEntry> taskbarEntries = new();
        private RectTransform taskbar;
        private int interactionOrder;

        private sealed class TaskbarEntry
        {
            public Button Button;
            public Image Background;
            public Image Indicator;
            public Color Accent;
            public UnityAction Launch;
            public bool IsCollapsingCycle;
        }

        public void InitializeTaskbar(RectTransform target) => taskbar = target;

        public void RegisterTaskbarApplication(string applicationId, string spritePath, Color accent, UnityAction launch)
        {
            if (taskbar == null || taskbarEntries.ContainsKey(applicationId)) return;
            var index = taskbarEntries.Count;
            var button = UIFactory.SpriteButton("Taskbar_" + applicationId, taskbar, spritePath,
                () => ActivateApplication(applicationId), new Color(1f, 1f, 1f, .04f), 5f);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(48, 38);
            rect.anchoredPosition = new Vector2(235 + index * 54, 0);
            var indicator = UIFactory.Rect("RunningIndicator", rect, Vector2.zero, new Vector2(1, 0),
                new Vector2(7, 2), new Vector2(-7, 5), accent).GetComponent<Image>();
            indicator.raycastTarget = false;
            var entry = new TaskbarEntry
            {
                Button = button,
                Background = button.targetGraphic as Image,
                Indicator = indicator,
                Accent = accent,
                Launch = launch
            };
            taskbarEntries.Add(applicationId, entry);
            RefreshTaskbar(applicationId);
        }

        public void Register(WindowController window)
        {
            if (!windows.Contains(window)) windows.Add(window);
            BringToFront(window);
            NotifyWindowStateChanged(window.ApplicationId);
        }

        public void Touch(WindowController window)
        {
            if (window == null) return;
            window.SetInteractionOrder(++interactionOrder);
            NotifyWindowStateChanged(window.ApplicationId);
        }

        public void BringToFront(WindowController window)
        {
            if (window != null && window.gameObject.activeSelf)
            {
                window.transform.SetAsLastSibling();
                NotifyWindowStateChanged(window.ApplicationId);
            }
        }

        public void NotifyWindowStateChanged(string applicationId)
        {
            if (!string.IsNullOrEmpty(applicationId)) RefreshTaskbar(applicationId);
        }

        private void ActivateApplication(string applicationId)
        {
            var candidates = windows.Where(window => window != null && window.ApplicationId == applicationId).ToList();
            if (!taskbarEntries.TryGetValue(applicationId, out var entry)) return;

            var minimized = candidates.Where(window => window.DisplayState == WindowDisplayState.Minimized)
                .OrderByDescending(window => window.LastInteractionOrder).ToList();
            var open = candidates.Where(window => window.DisplayState == WindowDisplayState.Open && window.gameObject.activeSelf)
                .OrderByDescending(window => window.LastInteractionOrder).ToList();

            if (entry.IsCollapsingCycle)
            {
                if (open.Count > 0)
                {
                    open[0].Minimize();
                    if (open.Count == 1) entry.IsCollapsingCycle = false;
                }
                else entry.IsCollapsingCycle = false;
                RefreshTaskbar(applicationId);
                return;
            }

            if (minimized.Count > 0)
            {
                minimized[0].Open();
            }
            else if (open.Count > 0)
            {
                // Once every minimized window has been restored, subsequent clicks collapse
                // the open stack one window at a time in the reverse restoration order.
                entry.IsCollapsingCycle = true;
                open[0].Minimize();
                if (open.Count == 1) entry.IsCollapsingCycle = false;
            }
            else entry.Launch?.Invoke();
            RefreshTaskbar(applicationId);
        }

        private void RefreshTaskbar(string applicationId)
        {
            if (!taskbarEntries.TryGetValue(applicationId, out var entry)) return;
            var candidates = windows.Where(window => window != null && window.ApplicationId == applicationId).ToList();
            var isOpen = candidates.Any(window => window.DisplayState == WindowDisplayState.Open && window.gameObject.activeSelf);
            var isMinimized = !isOpen && candidates.Any(window => window.DisplayState == WindowDisplayState.Minimized);
            entry.Indicator.gameObject.SetActive(isOpen || isMinimized);
            entry.Indicator.color = isOpen ? entry.Accent : new Color(entry.Accent.r, entry.Accent.g, entry.Accent.b, .55f);
            if (entry.Background != null)
                entry.Background.color = isOpen ? new Color(entry.Accent.r, entry.Accent.g, entry.Accent.b, .28f) : new Color(1f, 1f, 1f, .04f);
        }

        public void CloseTopmost()
        {
            WindowController top = null;
            var topIndex = -1;
            foreach (var window in windows)
            {
                if (window == null || !window.gameObject.activeSelf) continue;
                var index = window.transform.GetSiblingIndex();
                if (index > topIndex) { top = window; topIndex = index; }
            }
            if (top != null) top.Close();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseTopmost();
        }
    }
}
