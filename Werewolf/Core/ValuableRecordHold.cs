namespace Werewolf.Core
{
    public sealed class ValuableRecordHold
    {
        public const float HoldSeconds = 1f;

        private float _held;
        private bool _fired;
        private bool _consumed;

        public float Ratio
        {
            get
            {
                if (_fired) return 1f;
                if (_consumed) return 0f;
                float r = _held / HoldSeconds;
                if (r < 0f) return 0f;
                return r > 1f ? 1f : r;
            }
        }

        public bool IsCharging => !_fired && !_consumed && _held > 0f;

        public bool Tick(bool held, bool pressConsumed, float deltaSeconds)
        {
            if (!held)
            {
                _held = 0f;
                _fired = false;
                _consumed = false;
                return false;
            }
            if (pressConsumed) _consumed = true;
            if (_consumed || _fired) return false;
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
            _consumed = false;
        }
    }
}
