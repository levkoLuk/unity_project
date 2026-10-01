using System;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.Desktop
{
    public sealed class GameClock : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 5f;
        private const int MinutesPerDay = 24 * 60;
        private static readonly DateTime StartDate = new DateTime(2034, 11, 22);

        private Text clockText;
        private float startedAt;
        private float nextRefreshAt;

        private void Start()
        {
            var clock = transform.Find("Taskbar/Clock");
            clockText = clock != null ? clock.GetComponent<Text>() : null;
            startedAt = Time.unscaledTime;
            nextRefreshAt = startedAt + RefreshIntervalSeconds;
            RefreshDisplay(startedAt);
        }

        private void Update()
        {
            if (clockText == null || Time.unscaledTime < nextRefreshAt) return;

            RefreshDisplay(Time.unscaledTime);
            nextRefreshAt += RefreshIntervalSeconds;
            if (nextRefreshAt <= Time.unscaledTime)
                nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
        }

        private void RefreshDisplay(float now)
        {
            var elapsedGameMinutes = Mathf.FloorToInt(now - startedAt);
            var minutesToday = elapsedGameMinutes % MinutesPerDay;
            var hours = minutesToday / 60;
            var minutes = minutesToday % 60;
            var date = StartDate.AddDays(elapsedGameMinutes / MinutesPerDay);
            clockText.text = $"{date:dd.MM.yyyy}  {hours:00}:{minutes:00}";
        }
    }
}
