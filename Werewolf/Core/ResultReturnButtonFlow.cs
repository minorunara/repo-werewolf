namespace Werewolf.Core
{
    public enum ResultReturnButtonEvent : byte
    {
        None = 0,

        Armed = 1,

        Confirmed = 2,

        Disarmed = 3,
    }

    public sealed class ResultReturnButtonFlow
    {
        public const long FadeStartMs = 3000L;

        public const long FadeDurationMs = 400L;

        public const long ArmDelayMs = FadeStartMs + FadeDurationMs;

        private long _shownAtUnixMs;
        private bool _active;
        private bool _armed;
        private bool _confirmed;

        public bool Armed => _armed;

        public bool Confirmed => _confirmed;

        public void Begin(long nowUnixMs)
        {
            _shownAtUnixMs = nowUnixMs;
            _active = true;
            _armed = false;
            _confirmed = false;
        }

        public void Reset()
        {
            _active = false;
            _armed = false;
            _confirmed = false;
        }

        public float AlphaAt(long nowUnixMs)
        {
            if (!_active) return 0f;
            long sinceFade = nowUnixMs - _shownAtUnixMs - FadeStartMs;
            if (sinceFade <= 0L) return 0f;
            if (sinceFade >= FadeDurationMs) return 1f;
            return sinceFade / (float)FadeDurationMs;
        }

        public bool ReadyAt(long nowUnixMs)
            => _active && nowUnixMs >= _shownAtUnixMs + ArmDelayMs;

        public ResultReturnButtonEvent Tick(long nowUnixMs, bool clicked, bool pointerOnButton)
        {
            if (!_active || _confirmed || !clicked || !ReadyAt(nowUnixMs))
            {
                return ResultReturnButtonEvent.None;
            }

            if (pointerOnButton)
            {
                if (_armed)
                {
                    _confirmed = true;
                    return ResultReturnButtonEvent.Confirmed;
                }
                _armed = true;
                return ResultReturnButtonEvent.Armed;
            }

            if (_armed)
            {
                _armed = false;
                return ResultReturnButtonEvent.Disarmed;
            }
            return ResultReturnButtonEvent.None;
        }
    }
}
