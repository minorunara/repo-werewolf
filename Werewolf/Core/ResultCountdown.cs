using System;

namespace Werewolf.Core
{
    public sealed class ResultCountdown
    {
        private int _durationSeconds;
        private long _startedAtUnixMs;
        private bool _active;

        public bool Active => _active;

        public void Begin(long nowUnixMs, int durationSeconds)
        {
            _durationSeconds = Math.Max(0, durationSeconds);
            _startedAtUnixMs = _durationSeconds > 0 ? nowUnixMs : 0L;
            _active = _durationSeconds > 0;
        }

        public int? RemainingSeconds(long nowUnixMs)
        {
            if (!Active) return null;

            long elapsedMs = Math.Max(0L, nowUnixMs - _startedAtUnixMs);
            long remainingMs = Math.Max(0L, _durationSeconds * 1000L - elapsedMs);
            long roundedUp = (remainingMs + 999L) / 1000L;
            return (int)Math.Min(_durationSeconds, roundedUp);
        }

        public void Clear()
        {
            _durationSeconds = 0;
            _startedAtUnixMs = 0L;
            _active = false;
        }
    }
}
