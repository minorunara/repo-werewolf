namespace Werewolf.Core
{
    public sealed class ResultSequence
    {
        private bool _active;
        private bool _fired;
        private bool _returnRequested;
        private bool _autoEnabled;
        private long _autoReturnAtUnixMs;

        public bool Active => _active;

        public void Begin(long nowUnixMs, int autoReturnSeconds)
        {
            _active = true;
            _fired = false;
            _returnRequested = false;
            _autoEnabled = autoReturnSeconds > 0;
            _autoReturnAtUnixMs = _autoEnabled ? nowUnixMs + autoReturnSeconds * 1000L : 0L;
        }

        public void RequestReturn()
        {
            if (_active && !_fired) _returnRequested = true;
        }

        public void Cancel()
        {
            _active = false;
            _returnRequested = false;
            _autoEnabled = false;
            _autoReturnAtUnixMs = 0L;
        }

        public bool TickShouldReturn(long nowUnixMs)
        {
            if (!_active || _fired) return false;
            if (!_returnRequested && (!_autoEnabled || nowUnixMs < _autoReturnAtUnixMs)) return false;

            _fired = true;
            _active = false;
            return true;
        }
    }
}
