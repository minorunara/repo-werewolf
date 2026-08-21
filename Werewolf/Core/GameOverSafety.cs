namespace Werewolf.Core
{
    public static class GameOverSafety
    {
        public static bool ShouldHoldEnemyFreeze(GamePhase phase, bool winCeremonyActive)
            => phase == GamePhase.GameOver || winCeremonyActive;

        public static bool ShouldInjectInvincibility(
            GamePhase phase, bool meetingActive, bool warpDone, bool winCeremonyActive)
            => phase == GamePhase.GameOver || winCeremonyActive || (meetingActive && warpDone);
    }
}
