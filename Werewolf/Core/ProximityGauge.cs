using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class ProximityGauge
    {
        private readonly Dictionary<int, float> _accumulated = new Dictionary<int, float>();
        private readonly float _fullSeconds;
        private readonly float _decayMultiplier;

        private bool _armed = true;
        private bool _pendingEdge;

        public ProximityGauge(float fullSeconds, float decayMultiplier = 2f)
        {
            _fullSeconds = fullSeconds > 0f ? fullSeconds : 1f;
            _decayMultiplier = decayMultiplier > 0f ? decayMultiplier : 1f;
        }

        public void Tick(int actor, bool within, float deltaSeconds, bool suspend)
        {
            if (suspend || deltaSeconds <= 0f) return;

            float current;
            _accumulated.TryGetValue(actor, out current);

            bool wasFull = current >= _fullSeconds;
            float next = within
                ? current + deltaSeconds
                : current - deltaSeconds * _decayMultiplier;

            if (next < 0f) next = 0f;
            if (next > _fullSeconds) next = _fullSeconds;

            _accumulated[actor] = next;

            if (!wasFull && next >= _fullSeconds && _armed)
            {
                _pendingEdge = true;
                _armed = false;
            }
        }

        public void DebugSetFull(int actor)
        {
            float prev;
            _accumulated.TryGetValue(actor, out prev);
            bool wasFull = prev >= _fullSeconds;
            _accumulated[actor] = _fullSeconds;
            if (!wasFull && _armed)
            {
                _pendingEdge = true;
                _armed = false;
            }
        }

        public void ResetAll()
        {
            _accumulated.Clear();
            _armed = true;
            _pendingEdge = false;
        }

        public void Remove(int actor)
        {
            _accumulated.Remove(actor);
        }

        public float Ratio(int actor)
        {
            float current;
            _accumulated.TryGetValue(actor, out current);
            return current / _fullSeconds;
        }

        public bool IsFull(int actor)
        {
            float current;
            _accumulated.TryGetValue(actor, out current);
            return current >= _fullSeconds;
        }

        public bool TryGetNotifyEdge(out bool armed)
        {
            if (!_armed && AllEmpty())
            {
                _armed = true;
            }

            bool fired = _pendingEdge;
            _pendingEdge = false;
            armed = _armed;
            return fired;
        }

        private bool AllEmpty()
        {
            foreach (var v in _accumulated.Values)
            {
                if (v > 0f) return false;
            }
            return true;
        }
    }
}
