namespace Werewolf.Core
{
    public enum VoidMatchHoldEvent : byte
    {
        None = 0,

        Armed = 1,

        Confirmed = 2,

        Cancelled = 3,
    }

    public sealed class VoidMatchHold
    {
        public const float ArmSeconds = 2f;

        public const float ConfirmSeconds = 3f;

        public const float ArmedTimeoutSeconds = 10f;

        private float _held;
        private float _armedIdle;
        private bool _armed;
        private bool _confirmed;

        private bool _releasedSinceArm;

        public bool Armed => _armed;

        public bool Confirmed => _confirmed;

        public float ArmedRemainingSeconds
        {
            get
            {
                if (!_armed) return 0f;
                float remaining = ArmedTimeoutSeconds - _armedIdle;
                return remaining > 0f ? remaining : 0f;
            }
        }

        public float Ratio
        {
            get
            {
                if (_confirmed) return 1f;
                float goal = _armed ? ConfirmSeconds : ArmSeconds;
                float r = _held / goal;
                if (r < 0f) return 0f;
                return r > 1f ? 1f : r;
            }
        }

        public bool IsCharging => !_confirmed && _held > 0f;

        public VoidMatchHoldEvent Tick(bool held, bool available, bool cancelRequested, float deltaSeconds)
        {
            if (_confirmed) return VoidMatchHoldEvent.None;

            if (!available)
            {
                bool wasArmed = _armed;
                Reset();
                return wasArmed ? VoidMatchHoldEvent.Cancelled : VoidMatchHoldEvent.None;
            }

            if (_armed)
            {
                if (cancelRequested)
                {
                    Reset();
                    return VoidMatchHoldEvent.Cancelled;
                }

                if (!held)
                {
                    _releasedSinceArm = true;
                    _held = 0f;
                }

                if (held && _releasedSinceArm)
                {
                    if (deltaSeconds > 0f) _held += deltaSeconds;
                    _armedIdle = 0f;
                    if (_held >= ConfirmSeconds)
                    {
                        _held = ConfirmSeconds;
                        _confirmed = true;
                        return VoidMatchHoldEvent.Confirmed;
                    }
                    return VoidMatchHoldEvent.None;
                }

                if (deltaSeconds > 0f) _armedIdle += deltaSeconds;
                if (_armedIdle >= ArmedTimeoutSeconds)
                {
                    Reset();
                    return VoidMatchHoldEvent.Cancelled;
                }
                return VoidMatchHoldEvent.None;
            }

            if (!held)
            {
                _held = 0f;
                return VoidMatchHoldEvent.None;
            }

            if (deltaSeconds > 0f) _held += deltaSeconds;
            if (_held >= ArmSeconds)
            {
                _held = 0f;
                _armed = true;
                _armedIdle = 0f;
                _releasedSinceArm = false;
                return VoidMatchHoldEvent.Armed;
            }
            return VoidMatchHoldEvent.None;
        }

        public void Reset()
        {
            _held = 0f;
            _armedIdle = 0f;
            _armed = false;
            _confirmed = false;
            _releasedSinceArm = false;
        }
    }
}
