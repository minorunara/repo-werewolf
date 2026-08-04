namespace Werewolf.Core
{
    public sealed class LifecycleGate
    {
        private bool _startedThisRound;

        public bool ShouldAutoStart(bool modeEnabled, bool isRunLevel, GamePhase phase)
        {
            if (!modeEnabled) return false;
            if (!isRunLevel) return false;
            if (_startedThisRound) return false;
            if (phase != GamePhase.Lobby) return false;
            return true;
        }

        public void MarkStarted() => _startedThisRound = true;

        public void ResetForNextRound() => _startedThisRound = false;
    }
}
