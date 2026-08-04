using System;

namespace Werewolf.Core
{
    public sealed class RoundTimer
    {
        private bool _started;
        private bool _expired;
        private bool _paused;
        private long _meetingStartUnixMs;

        public long EndUnixMs { get; private set; }

        public bool IsPaused => _paused;

        public bool Expired => _expired;

        public void Start(long nowUnixMs, int roundSeconds)
        {
            if (roundSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(roundSeconds), roundSeconds, "ラウンド制限時間は正の秒数であること。");
            }

            EndUnixMs = nowUnixMs + roundSeconds * 1000L;
            _started = true;
            _expired = false;
            _paused = false;
        }

        public long RemainingMs(long nowUnixMs)
        {
            if (!_started) return 0;

            long effectiveNow = _paused ? _meetingStartUnixMs : nowUnixMs;
            long remaining = EndUnixMs - effectiveNow;
            return remaining > 0 ? remaining : 0;
        }

        public bool CheckExpiry(long nowUnixMs)
        {
            if (!_started || _expired || _paused) return false;
            if (nowUnixMs < EndUnixMs) return false;

            _expired = true;
            return true;
        }

        public void PauseForMeeting(long nowUnixMs)
        {
            if (!_started || _paused) return;

            _paused = true;
            _meetingStartUnixMs = nowUnixMs;
        }

        public long ResumeFromMeeting(long nowUnixMs)
        {
            if (!_started || !_paused) return EndUnixMs;

            EndUnixMs += nowUnixMs - _meetingStartUnixMs;
            _paused = false;
            return EndUnixMs;
        }

        public void ForceExpire(long nowUnixMs)
        {
            if (!_started || _expired) return;

            EndUnixMs = _paused ? _meetingStartUnixMs : nowUnixMs;
        }
    }
}
