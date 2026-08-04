namespace Werewolf.Core
{
    public sealed class CatAwakenToastGate
    {
        private bool _shown;

        public void Reset()
        {
            _shown = false;
        }

        public bool ShouldFire(GamePhase phase, bool catPossible,
                               long gameStartUnixMs, int revealDelaySec, long nowUnixMs)
        {
            if (_shown) return false;
            if (!catPossible) return false;
            if (gameStartUnixMs == 0) return false;
            if (phase != GamePhase.Play) return false;
            if (nowUnixMs < gameStartUnixMs + revealDelaySec * 1000L) return false;

            _shown = true;
            return true;
        }
    }
}
