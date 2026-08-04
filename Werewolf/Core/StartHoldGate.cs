namespace Werewolf.Core
{
    public enum StartHoldPhase
    {
        Idle,

        Holding,

        Released,
    }

    public enum StartHoldRelease
    {
        None,

        GameStart,

        Failsafe,
    }

    public sealed class StartHoldGate
    {
        public const int FailsafeSec = 20;

        private long _heldAtUnixMs;
        private long _failsafeReleasedAtUnixMs;
        private bool _lateGameStartPending;

        public StartHoldPhase Phase { get; private set; } = StartHoldPhase.Idle;

        public long HeldMs(long nowUnixMs) => Phase == StartHoldPhase.Holding ? nowUnixMs - _heldAtUnixMs : 0;

        public long LateGameStartGapMs { get; private set; } = -1;

        public bool Tick(
            bool inRunLevel,
            bool operable,
            bool werewolfExpected,
            bool gameStartReceived,
            long nowUnixMs,
            out StartHoldRelease released)
        {
            released = StartHoldRelease.None;
            LateGameStartGapMs = -1;

            if (!inRunLevel)
            {
                Phase = StartHoldPhase.Idle;
                _lateGameStartPending = false;
                return false;
            }

            switch (Phase)
            {
                case StartHoldPhase.Idle:
                    if (!operable) return false;
                    if (!werewolfExpected || gameStartReceived)
                    {
                        Phase = StartHoldPhase.Released;
                        return false;
                    }
                    Phase = StartHoldPhase.Holding;
                    _heldAtUnixMs = nowUnixMs;
                    return true;

                case StartHoldPhase.Holding:
                    if (gameStartReceived)
                    {
                        Phase = StartHoldPhase.Released;
                        released = StartHoldRelease.GameStart;
                        return false;
                    }
                    if (nowUnixMs - _heldAtUnixMs >= FailsafeSec * 1000L)
                    {
                        Phase = StartHoldPhase.Released;
                        released = StartHoldRelease.Failsafe;
                        _failsafeReleasedAtUnixMs = nowUnixMs;
                        _lateGameStartPending = true;
                        return false;
                    }
                    return true;

                default:
                    if (_lateGameStartPending && gameStartReceived)
                    {
                        _lateGameStartPending = false;
                        LateGameStartGapMs = nowUnixMs - _failsafeReleasedAtUnixMs;
                    }
                    return false;
            }
        }
    }
}
