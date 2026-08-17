namespace Werewolf.Core
{
    public static class GameOverSafety
    {
        public static bool ShouldHoldEnemyFreeze(GamePhase phase)
            => phase == GamePhase.GameOver;

        public static bool ShouldInjectInvincibility(GamePhase phase, bool meetingActive, bool warpDone)
            => phase == GamePhase.GameOver || (meetingActive && warpDone);
    }
}
