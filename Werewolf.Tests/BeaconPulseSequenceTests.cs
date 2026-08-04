using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class BeaconPulseSequenceTests
    {
        private const long Start = 1_000_000L;

        [Fact]
        public void DuePulses_BeforeStart_IsZero()
        {
            var seq = new BeaconPulseSequence(Start);
            Assert.Equal(0, seq.DuePulses(Start - 1));
        }

        [Fact]
        public void DuePulses_AtStart_FiresFirstPulseImmediately()
        {
            var seq = new BeaconPulseSequence(Start);
            Assert.Equal(1, seq.DuePulses(Start));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(2499, 1)]
        [InlineData(2500, 2)]
        [InlineData(5000, 3)]
        [InlineData(7499, 3)]
        [InlineData(7500, 4)]
        [InlineData(22499, 9)]
        [InlineData(22500, 10)]
        public void DuePulses_FollowsFixedInterval(long elapsedMs, int expected)
        {
            var seq = new BeaconPulseSequence(Start);
            Assert.Equal(expected, seq.DuePulses(Start + elapsedMs));
        }

        [Fact]
        public void DuePulses_LongAfterStart_IsCappedAtPulseCount()
        {
            var seq = new BeaconPulseSequence(Start);
            Assert.Equal(BeaconPulseSequence.PulseCount, seq.DuePulses(Start + 60_000));
            Assert.Equal(BeaconPulseSequence.PulseCount, seq.DuePulses(long.MaxValue));
        }

        [Fact]
        public void Constants_MatchSpec()
        {
            Assert.Equal(10, BeaconPulseSequence.PulseCount);
            Assert.Equal(2500, BeaconPulseSequence.IntervalMs);
            Assert.True((BeaconPulseSequence.PulseCount - 1) * BeaconPulseSequence.IntervalMs
                >= BeaconSummonPlan.MaxSeconds * 1000, "pulse window does not cover summon clamp max");
        }
    }
}
