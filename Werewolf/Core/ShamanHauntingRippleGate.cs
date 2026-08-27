namespace Werewolf.Core
{
    public static class ShamanHauntingRippleGate
    {
        public static bool CanShow(CorpseReportHudMode reportHudMode, bool senseSuspended)
        {
            if (senseSuspended) return false;
            return reportHudMode != CorpseReportHudMode.Hidden;
        }
    }
}
