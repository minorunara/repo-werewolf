using System;

namespace Werewolf.Core
{
    public sealed class MeetingTimer
    {
        public const long MinRemainingMs = 10_000;

        private bool _started;
        private bool _unlimited;

        public long EndUnixMs { get; private set; }

        public bool Unlimited => _unlimited;

        public void Start(long warpUnixMs, int durationSeconds, bool unlimited = false)
        {
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds), durationSeconds, "会議制限時間は正の秒数であること。");
            }

            EndUnixMs = warpUnixMs + durationSeconds * 1000L;
            _started = true;
            _unlimited = unlimited;
        }

        public long ReduceByVote(int remainingVoters, long nowUnixMs)
        {
            if (!_started || _unlimited) return EndUnixMs;
            if (remainingVoters <= 1) return EndUnixMs;

            long remaining = EndUnixMs - nowUnixMs;
            if (remaining < MinRemainingMs) return EndUnixMs;

            long newRemaining = remaining * (remainingVoters - 1) / remainingVoters;
            if (newRemaining < MinRemainingMs) newRemaining = MinRemainingMs;
            long newEnd = nowUnixMs + newRemaining;
            if (newEnd > EndUnixMs) newEnd = EndUnixMs;

            EndUnixMs = newEnd;
            return EndUnixMs;
        }

        public bool IsExpired(long nowUnixMs) => _started && !_unlimited && nowUnixMs >= EndUnixMs;
    }
}
