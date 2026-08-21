using System;

namespace Werewolf.Core
{
    public sealed class MeetingTimeBar
    {
        public const long LagHoldMs = 500;

        public const double LagDrainPerSec = 3.0;

        public const long RedZoneMs = MeetingTimer.MinRemainingMs;

        public const long JumpSlackMs = 500;

        private bool _started;
        private long _totalMs;
        private long _remainingMs;
        private long _dispMs;
        private long _holdUntilMs;
        private long _endUnixMs;
        private long _lastNowUnixMs;

        public bool Started => _started;

        public long TotalMs => _totalMs;

        public double FillFraction => Fraction(_remainingMs);

        public double LagFraction => Fraction(_dispMs > 0 ? _dispMs : 0);

        public double RedZoneFraction => _totalMs > 0 ? Math.Min(1.0, (double)RedZoneMs / _totalMs) : 0.0;

        public void Begin(long totalMs)
        {
            _started = true;
            _totalMs = totalMs > 0 ? totalMs : 0;
            _remainingMs = 0;
            _dispMs = -1;
            _holdUntilMs = 0;
            _endUnixMs = 0;
            _lastNowUnixMs = 0;
        }

        public void Reset()
        {
            _started = false;
            _totalMs = 0;
            _remainingMs = 0;
            _dispMs = -1;
            _holdUntilMs = 0;
            _endUnixMs = 0;
            _lastNowUnixMs = 0;
        }

        public void Tick(long remainingMs, long nowUnixMs)
        {
            if (!_started) return;
            if (remainingMs < 0) remainingMs = 0;
            if (_totalMs <= 0) _totalMs = Math.Max(1, remainingMs);
            if (remainingMs > _totalMs) remainingMs = _totalMs;

            long dtMs = _lastNowUnixMs == 0 ? 0 : Math.Max(0, nowUnixMs - _lastNowUnixMs);
            _lastNowUnixMs = nowUnixMs;

            if (remainingMs > 0)
            {
                long impliedEnd = nowUnixMs + remainingMs;
                if (_endUnixMs != 0 && impliedEnd < _endUnixMs - JumpSlackMs)
                {
                    _holdUntilMs = nowUnixMs + LagHoldMs;
                }
                _endUnixMs = impliedEnd;
            }

            _remainingMs = remainingMs;
            if (_dispMs < remainingMs)
            {
                _dispMs = remainingMs;
                return;
            }
            if (_dispMs > remainingMs && nowUnixMs >= _holdUntilMs)
            {
                long drainMs = (long)(LagDrainPerSec * _totalMs * dtMs / 1000.0);
                _dispMs = Math.Max(remainingMs, _dispMs - drainMs);
            }
        }

        private double Fraction(long ms)
        {
            if (_totalMs <= 0) return 0.0;
            double f = (double)ms / _totalMs;
            return f < 0.0 ? 0.0 : (f > 1.0 ? 1.0 : f);
        }
    }
}
