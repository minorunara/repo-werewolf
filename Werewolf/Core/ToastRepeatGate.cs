namespace Werewolf.Core
{
    public sealed class ToastRepeatGate
    {
        private long _nextUnixMs;

        public bool TryPass(long nowUnixMs, int suppressSec)
        {
            if (nowUnixMs < _nextUnixMs) return false;

            _nextUnixMs = suppressSec > 0 ? nowUnixMs + suppressSec * 1000L : 0;
            return true;
        }

        public void Reset()
        {
            _nextUnixMs = 0;
        }
    }
}
