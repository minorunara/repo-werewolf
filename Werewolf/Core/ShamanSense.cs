using System;

namespace Werewolf.Core
{
    public enum ShamanStormTier : byte
    {
        None = 0,
        Weak = 1,
        Medium = 2,
        Strong = 3,
    }

    public sealed class ShamanSense
    {
        public const float GhostDisplaySec = 1.0f;

        public const float StormExitMarginMeters = 1f;

        public const float TranceEntrySec = 1f;

        private readonly float _gazeFullSec;
        private readonly float _cooldownSec;
        private readonly float _decayMultiplier;

        private float _gauge;
        private float _ghostRemainingSec;
        private float _cooldownRemainingSec;
        private ShamanStormTier _stormTier;
        private float _stillSec;
        private float _dripCountdownSec;

        public ShamanSense(float gazeFullSec, float cooldownSec, float decayMultiplier = 2f)
        {
            _gazeFullSec = gazeFullSec > 0f ? gazeFullSec : 1f;
            _cooldownSec = cooldownSec > 0f ? cooldownSec : 0f;
            _decayMultiplier = decayMultiplier > 0f ? decayMultiplier : 1f;
        }

        public bool GhostVisible => _ghostRemainingSec > 0f;

        public ShamanStormTier StormTier => _stormTier;

        public bool TranceActive => _stillSec >= TranceEntrySec;

        public bool GazeArmed => TranceActive
            && _stormTier == ShamanStormTier.None
            && _ghostRemainingSec <= 0f
            && _cooldownRemainingSec <= 0f;

        public bool TickGaze(bool corpseInView, bool stationary, float deltaSeconds, bool suspend,
            out bool dripFired)
        {
            dripFired = false;
            if (suspend)
            {
                _gauge = 0f;
                _ghostRemainingSec = 0f;
                _stillSec = 0f;
                _dripCountdownSec = 0f;
                return false;
            }
            if (deltaSeconds <= 0f) return false;

            if (stationary)
            {
                _stillSec += deltaSeconds;
            }
            else
            {
                _stillSec = 0f;
                _dripCountdownSec = 0f;
            }

            if (_ghostRemainingSec > 0f)
            {
                _ghostRemainingSec -= deltaSeconds;
                if (_ghostRemainingSec <= 0f)
                {
                    _ghostRemainingSec = 0f;
                    _cooldownRemainingSec = _cooldownSec;
                }
                return false;
            }

            if (_cooldownRemainingSec > 0f)
            {
                _cooldownRemainingSec -= deltaSeconds;
                if (_cooldownRemainingSec < 0f) _cooldownRemainingSec = 0f;
                return false;
            }

            if (_stormTier != ShamanStormTier.None) corpseInView = false;

            if (!TranceActive) corpseInView = false;

            _gauge = corpseInView
                ? _gauge + deltaSeconds
                : _gauge - deltaSeconds * _decayMultiplier;
            if (_gauge < 0f) _gauge = 0f;

            if (_gauge >= _gazeFullSec)
            {
                _gauge = 0f;
                _ghostRemainingSec = GhostDisplaySec;
                _dripCountdownSec = 0f;
                return true;
            }

            if (GazeArmed)
            {
                if (_dripCountdownSec <= 0f)
                {
                    _dripCountdownSec = _gazeFullSec;
                }
                else
                {
                    _dripCountdownSec -= deltaSeconds;
                    if (_dripCountdownSec <= 0f)
                    {
                        dripFired = true;
                        _dripCountdownSec = _gazeFullSec;
                    }
                }
            }
            return false;
        }

        public ShamanStormTier TickStorm(float? nearestDistanceMeters, bool suspend,
            float weakMeters, float mediumMeters, float strongMeters)
        {
            if (suspend || nearestDistanceMeters == null)
            {
                _stormTier = ShamanStormTier.None;
                return _stormTier;
            }

            float d = nearestDistanceMeters.Value;
            ShamanStormTier target = RawTier(d, weakMeters, mediumMeters, strongMeters);

            if (target < _stormTier
                && d <= TierRadius(_stormTier, weakMeters, mediumMeters, strongMeters) + StormExitMarginMeters)
            {
                return _stormTier;
            }

            _stormTier = target;
            return _stormTier;
        }

        public void BeginCooldown()
        {
            _cooldownRemainingSec = _cooldownSec;
        }

        public void Reset()
        {
            _gauge = 0f;
            _ghostRemainingSec = 0f;
            _cooldownRemainingSec = 0f;
            _stormTier = ShamanStormTier.None;
            _stillSec = 0f;
            _dripCountdownSec = 0f;
        }

        private static ShamanStormTier RawTier(float d, float weakM, float mediumM, float strongM)
        {
            if (d <= strongM) return ShamanStormTier.Strong;
            if (d <= mediumM) return ShamanStormTier.Medium;
            if (d <= weakM) return ShamanStormTier.Weak;
            return ShamanStormTier.None;
        }

        private static float TierRadius(ShamanStormTier tier, float weakM, float mediumM, float strongM)
        {
            switch (tier)
            {
                case ShamanStormTier.Strong: return strongM;
                case ShamanStormTier.Medium: return mediumM;
                case ShamanStormTier.Weak: return weakM;
                default: return 0f;
            }
        }
    }
}
