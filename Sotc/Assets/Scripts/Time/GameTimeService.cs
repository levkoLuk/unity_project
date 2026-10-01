using System;
using UnityEngine;

namespace Moderator.TimeSystem
{
    public sealed class GameTimeService : MonoBehaviour
    {
        public static readonly DateTime InitialDate = new DateTime(2034, 11, 22, 0, 0, 0);
        public const float RealSecondsPerGameMinute = 1f;

        private float startedAt;

        public int TotalMinutes => Mathf.Max(0, Mathf.FloorToInt((UnityEngine.Time.unscaledTime - startedAt) / RealSecondsPerGameMinute));
        public DateTime CurrentDateTime => InitialDate.AddMinutes(TotalMinutes);

        private void Awake() => startedAt = UnityEngine.Time.unscaledTime;

        public void AddMinutes(int minutes)
        {
            startedAt -= Mathf.Max(0, minutes) * RealSecondsPerGameMinute;
        }
    }
}
