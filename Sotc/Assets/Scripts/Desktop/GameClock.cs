using Moderator.TimeSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.Desktop
{
    public sealed class GameClock : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 5f;
        private Text clockText;
        private GameTimeService gameTime;
        private float nextRefreshAt;

        private void Start()
        {
            var clock = transform.Find("Taskbar/Clock");
            clockText = clock != null ? clock.GetComponent<Text>() : null;
            gameTime = GetComponent<GameTimeService>();
            nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
            RefreshDisplay();
        }

        private void Update()
        {
            if (clockText == null || Time.unscaledTime < nextRefreshAt) return;

            RefreshDisplay();
            nextRefreshAt += RefreshIntervalSeconds;
            if (nextRefreshAt <= Time.unscaledTime)
                nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
        }

        private void RefreshDisplay()
        {
            if (gameTime == null || clockText == null) return;
            clockText.text = gameTime.CurrentDateTime.ToString("dd.MM.yyyy  HH:mm");
        }
    }
}
