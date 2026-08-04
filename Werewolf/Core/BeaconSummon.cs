namespace Werewolf.Core
{
    public static class BeaconSummonPlan
    {
        public const float StepSeconds = 5f;

        public const float MaxSeconds = 20f;

        public static float ClampSeconds(int despawnedIndex)
        {
            if (despawnedIndex < 0) despawnedIndex = 0;
            float seconds = StepSeconds * (despawnedIndex + 1);
            return seconds > MaxSeconds ? MaxSeconds : seconds;
        }
    }

    public sealed class BeaconSummonGate
    {
        private long _nextAllowedUnixMs;

        public bool TryOpen(long nowUnixMs, int cooldownSec)
        {
            if (nowUnixMs < _nextAllowedUnixMs) return false;
            _nextAllowedUnixMs = nowUnixMs + cooldownSec * 1000L;
            return true;
        }

        public void Reset()
        {
            _nextAllowedUnixMs = 0;
        }
    }
}
