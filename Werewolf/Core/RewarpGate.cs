using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class RewarpGate
    {
        private readonly long _farSustainMs;
        private readonly long _cooldownMs;

        private readonly Dictionary<int, long> _farSince = new Dictionary<int, long>();
        private readonly Dictionary<int, long> _lastFire = new Dictionary<int, long>();

        public RewarpGate(long farSustainMs, long cooldownMs)
        {
            _farSustainMs = farSustainMs;
            _cooldownMs = cooldownMs;
        }

        public bool Tick(int key, bool isFar, long nowMs)
        {
            if (!isFar)
            {
                _farSince.Remove(key);
                return false;
            }

            if (!_farSince.TryGetValue(key, out long farSince))
            {
                farSince = nowMs;
                _farSince[key] = farSince;
            }

            if (nowMs - farSince < _farSustainMs) return false;

            if (_lastFire.TryGetValue(key, out long lastFire) && nowMs - lastFire < _cooldownMs)
            {
                return false;
            }

            _lastFire[key] = nowMs;
            _farSince.Remove(key);
            return true;
        }

        public void Reset()
        {
            _farSince.Clear();
            _lastFire.Clear();
        }
    }
}
