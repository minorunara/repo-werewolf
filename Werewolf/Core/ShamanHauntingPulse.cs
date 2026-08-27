namespace Werewolf.Core
{
    public sealed class ShamanHauntingPulse
    {
        public const float WeakIntervalSec = 1f;
        public const float MediumIntervalSec = 0.5f;
        public const float StrongIntervalSec = 0.25f;
        private const float BoundaryEpsilonSec = 0.0001f;

        private ShamanStormTier _tier;
        private float _remainingSec;

        public bool Tick(ShamanStormTier tier, float deltaSeconds, bool suspend,
            bool detectorOverloaded = false)
        {
            if (suspend || detectorOverloaded || tier == ShamanStormTier.None)
            {
                Reset();
                return false;
            }

            if (tier != _tier)
            {
                _tier = tier;
                _remainingSec = IntervalSeconds(tier);
                return true;
            }

            if (deltaSeconds <= 0f) return false;

            _remainingSec -= deltaSeconds;
            if (_remainingSec > BoundaryEpsilonSec) return false;

            _remainingSec = IntervalSeconds(tier);
            return true;
        }

        public void Reset()
        {
            _tier = ShamanStormTier.None;
            _remainingSec = 0f;
        }

        public static float IntervalSeconds(ShamanStormTier tier)
        {
            switch (tier)
            {
                case ShamanStormTier.Strong: return StrongIntervalSec;
                case ShamanStormTier.Medium: return MediumIntervalSec;
                case ShamanStormTier.Weak: return WeakIntervalSec;
                default: return 0f;
            }
        }
    }
}
