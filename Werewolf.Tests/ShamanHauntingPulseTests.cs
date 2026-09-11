using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ShamanHauntingPulseTests
    {
        private static float Interval(ShamanStormTier tier) => ShamanHauntingPulse.IntervalSeconds(tier);

        [Fact]
        public void IntervalSeconds_ShortensAsTheStormStrengthens()
        {
            Assert.True(Interval(ShamanStormTier.Weak) > Interval(ShamanStormTier.Medium));
            Assert.True(Interval(ShamanStormTier.Medium) > Interval(ShamanStormTier.Strong));
            Assert.True(Interval(ShamanStormTier.Strong) > 0f);
        }

        [Fact]
        public void Tick_FiresImmediatelyThenAtTierInterval()
        {
            var pulse = new ShamanHauntingPulse();
            float medium = Interval(ShamanStormTier.Medium);

            Assert.True(pulse.Tick(ShamanStormTier.Medium, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Medium, medium / 2f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Medium, medium / 2f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Medium, medium - 0.01f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Medium, 0.01f, suspend: false));
        }

        [Fact]
        public void Tick_TierChangeFiresImmediatelyAndRestartsCadence()
        {
            var pulse = new ShamanHauntingPulse();
            float strong = Interval(ShamanStormTier.Strong);

            Assert.True(pulse.Tick(ShamanStormTier.Weak, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Weak, Interval(ShamanStormTier.Weak) * 0.4f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, strong - 0.01f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0.01f, suspend: false));
        }

        [Fact]
        public void Tick_SuspendAndNoTierResetPhase()
        {
            var pulse = new ShamanHauntingPulse();
            float strong = Interval(ShamanStormTier.Strong);

            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, strong * 0.4f, suspend: true));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.None, 1f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Weak, 0f, suspend: false));
        }

        [Fact]
        public void Tick_LargeFrameDropEmitsOnlyOnePulseAndDoesNotCatchUp()
        {
            var pulse = new ShamanHauntingPulse();
            float strong = Interval(ShamanStormTier.Strong);

            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, strong * 4f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, strong - 0.01f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0.01f, suspend: false));
        }

        [Fact]
        public void Tick_DetectorOverloadStopsAndResetsCadenceImmediately()
        {
            var pulse = new ShamanHauntingPulse();
            float strong = Interval(ShamanStormTier.Strong);

            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, strong * 0.4f, suspend: false,
                detectorOverloaded: true));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, strong * 4f, suspend: false,
                detectorOverloaded: true));

            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false,
                detectorOverloaded: false));
        }
    }
}
