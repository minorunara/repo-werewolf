using System;

namespace Werewolf.Core
{
    public sealed class MeetingTimer
    {
        public const long MinRemainingMs = 10_000;

        private bool _started;

        public long EndUnixMs { get; private set; }

        public void Start(long warpUnixMs, int durationSeconds)
        {
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationSeconds), durationSeconds, "会議制限時間は正の秒数であること。");
            }

            EndUnixMs = warpUnixMs + durationSeconds * 1000L;
            _started = true;
        }

        public long ReduceByVote(int remainingVoters, long nowUnixMs)
        {
            if (!_started) return EndUnixMs;
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

        public bool IsExpired(long nowUnixMs) => _started && nowUnixMs >= EndUnixMs;
    }
}
