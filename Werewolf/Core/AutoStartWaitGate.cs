namespace Werewolf.Core
{
    public enum AutoStartFireReason
    {
        None,

        AllLoaded,

        Timeout,
    }

    public sealed class AutoStartWaitGate
    {
        public const int DefaultTimeoutSec = 60;

        private readonly int _timeoutSec;
        private long _armedAtUnixMs = -1;

        public AutoStartWaitGate(int timeoutSec = DefaultTimeoutSec)
        {
            _timeoutSec = timeoutSec > 0 ? timeoutSec : DefaultTimeoutSec;
        }

        public bool Armed => _armedAtUnixMs >= 0;

        public long WaitedMs(long nowUnixMs) => Armed ? nowUnixMs - _armedAtUnixMs : 0;

        public void Arm(long nowUnixMs)
        {
            if (Armed) return;
            _armedAtUnixMs = nowUnixMs;
        }

        public void Disarm()
        {
            _armedAtUnixMs = -1;
        }

        public AutoStartFireReason ShouldFire(bool allPlayersLoaded, long nowUnixMs)
        {
            if (!Armed) return AutoStartFireReason.None;
            if (allPlayersLoaded)
            {
                Disarm();
                return AutoStartFireReason.AllLoaded;
            }
            if (nowUnixMs - _armedAtUnixMs >= _timeoutSec * 1000L)
            {
                Disarm();
                return AutoStartFireReason.Timeout;
            }
            return AutoStartFireReason.None;
        }
    }
}
