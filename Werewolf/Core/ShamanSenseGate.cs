namespace Werewolf.Core
{
    public static class ShamanSenseGate
    {
        public static bool ShouldSuspend(
            GamePhase phase,
            bool localAlive,
            bool meetingActive,
            bool warpDone,
            ConveneKind kind)
        {
            if (!localAlive) return true;
            if (phase != GamePhase.Play && phase != GamePhase.Meeting) return true;

            if (!meetingActive)
            {
                return phase != GamePhase.Play;
            }

            return warpDone || kind == ConveneKind.CorpseReport;
        }
    }
}
