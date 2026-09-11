namespace Werewolf.Core
{
    public enum StartHoldPhase
    {
        Idle,

        Holding,

        Released,
    }

    public sealed class StartHoldGate
    {
        public StartHoldPhase Phase { get; private set; } = StartHoldPhase.Idle;

        public bool Tick(
            bool inRunLevel,
            bool operable,
            bool werewolfExpected,
            bool gameStartReceived)
        {
            if (!inRunLevel)
            {
                Phase = StartHoldPhase.Idle;
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
                    return true;

                case StartHoldPhase.Holding:
                    if (gameStartReceived)
                    {
                        Phase = StartHoldPhase.Released;
                        return false;
                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
