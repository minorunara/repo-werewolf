namespace Werewolf.Core
{
    public sealed class ConveneHold
    {
        public const float HoldSeconds = 3f;

        private float _held;
        private bool _fired;

        public float Ratio
        {
            get
            {
                if (_fired) return 1f;
                float r = _held / HoldSeconds;
                if (r < 0f) return 0f;
                return r > 1f ? 1f : r;
            }
        }

        public bool IsCharging => !_fired && _held > 0f;

        public bool Tick(bool engaged, float deltaSeconds)
        {
            if (!engaged)
            {
                _held = 0f;
                _fired = false;
                return false;
            }
            if (_fired) return false;
            if (deltaSeconds > 0f) _held += deltaSeconds;
            if (_held >= HoldSeconds)
            {
                _held = HoldSeconds;
                _fired = true;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _held = 0f;
            _fired = false;
        }
    }
}
