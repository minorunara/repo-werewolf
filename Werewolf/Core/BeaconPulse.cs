namespace Werewolf.Core
{
    public sealed class BeaconPulseSequence
    {
        public const int PulseCount = 10;

        public const int IntervalMs = 2500;

        private readonly long _startUnixMs;

        public BeaconPulseSequence(long startUnixMs)
        {
            _startUnixMs = startUnixMs;
        }

        public int DuePulses(long nowUnixMs)
        {
            if (nowUnixMs < _startUnixMs) return 0;
            long due = (nowUnixMs - _startUnixMs) / IntervalMs + 1;
            return due > PulseCount ? PulseCount : (int)due;
        }
    }
}
