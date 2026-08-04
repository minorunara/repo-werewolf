namespace Werewolf.Core
{
    public static class MeetingIntro
    {
        public const long GaugeRevealOffsetMs = DeathReveal.TotalDurationMs;

        public const long GaugeRevealMs = GaugeReveal.HoldMs + GaugeReveal.DurationMs;

        public const long GaugeMoveMs = 500;

        public const long VotingUiDelayMs = GaugeRevealOffsetMs + GaugeRevealMs;

        public static double MoveProgress(long sinceVotingUiReadyMs)
        {
            if (sinceVotingUiReadyMs <= 0) return 0.0;
            if (sinceVotingUiReadyMs >= GaugeMoveMs) return 1.0;
            double t = sinceVotingUiReadyMs / (double)GaugeMoveMs;
            return t * t * t;
        }
    }
}
