namespace Werewolf.Core
{
    public sealed class NaturalHealClock
    {
        private long _nextDueUnixMs;

        public bool ShouldHeal(bool active, long nowUnixMs, int intervalSec)
        {
            if (!active)
            {
                _nextDueUnixMs = 0;
                return false;
            }

            long intervalMs = (intervalSec < 1 ? 1 : intervalSec) * 1000L;
            if (_nextDueUnixMs == 0)
            {
                _nextDueUnixMs = nowUnixMs + intervalMs;
                return false;
            }
            if (nowUnixMs < _nextDueUnixMs) return false;

            _nextDueUnixMs += intervalMs;
            if (_nextDueUnixMs <= nowUnixMs) _nextDueUnixMs = nowUnixMs + intervalMs;
            return true;
        }

        public void Reset()
        {
            _nextDueUnixMs = 0;
        }
    }
}
