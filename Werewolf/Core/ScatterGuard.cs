namespace Werewolf.Core
{
    public sealed class ScatterGuard
    {
        public long ArmedUntilUnixMs { get; private set; }

        public void Arm(long nowUnixMs, int guardSec)
        {
            ArmedUntilUnixMs = guardSec > 0 ? nowUnixMs + guardSec * 1000L : 0;
        }

        public void Disarm() => ArmedUntilUnixMs = 0;

        public bool IsArmed(long nowUnixMs) => ArmedUntilUnixMs != 0 && nowUnixMs < ArmedUntilUnixMs;
    }
}
