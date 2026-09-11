namespace Werewolf.Core
{
    public sealed class ButtonPressState
    {
        public enum Phase
        {
            Up,
            Down,
            Releasing,
        }

        public const float ReleaseSeconds = 0.5f;

        public Phase Current { get; private set; }

        public float ReleaseEval { get; private set; }

        public bool Sync(bool charging)
        {
            if (charging)
            {
                if (Current == Phase.Down) return false;
                Current = Phase.Down;
                ReleaseEval = 0f;
                return true;
            }
            if (Current == Phase.Down)
            {
                Current = Phase.Releasing;
                ReleaseEval = 0f;
            }
            return false;
        }

        public bool Tick(float deltaSeconds)
        {
            if (Current != Phase.Releasing) return false;
            if (deltaSeconds > 0f) ReleaseEval += deltaSeconds / ReleaseSeconds;
            if (ReleaseEval >= 1f)
            {
                ReleaseEval = 1f;
                Current = Phase.Up;
            }
            return true;
        }

        public void Reset()
        {
            Current = Phase.Up;
            ReleaseEval = 0f;
        }
    }
}
