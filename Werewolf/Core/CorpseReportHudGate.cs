namespace Werewolf.Core
{
    public enum CorpseReportHudMode : byte
    {
        Hidden = 0,

        Active = 1,

        Blocked = 2,
    }

    public static class CorpseReportHudGate
    {
        public static CorpseReportHudMode Compute(
            GamePhase phase, bool alive, bool meetingActive, bool warpedInMeeting, bool lastRunActive)
        {
            if (!alive) return CorpseReportHudMode.Hidden;
            if (phase != GamePhase.Play && phase != GamePhase.Meeting) return CorpseReportHudMode.Hidden;
            if (warpedInMeeting) return CorpseReportHudMode.Hidden;
            if (meetingActive) return CorpseReportHudMode.Blocked;
            if (phase != GamePhase.Play) return CorpseReportHudMode.Hidden;
            return lastRunActive ? CorpseReportHudMode.Blocked : CorpseReportHudMode.Active;
        }
    }
}
