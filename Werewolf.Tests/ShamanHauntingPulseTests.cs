using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ShamanHauntingPulseTests
    {
        [Theory]
        [InlineData(ShamanStormTier.Weak, 1f)]
        [InlineData(ShamanStormTier.Medium, 0.5f)]
        [InlineData(ShamanStormTier.Strong, 0.25f)]
        public void IntervalSeconds_MapsTierToOneTwoFourHz(
            ShamanStormTier tier, float expected)
        {
            Assert.Equal(expected, ShamanHauntingPulse.IntervalSeconds(tier));
        }

        [Fact]
        public void Tick_FiresImmediatelyThenAtTierInterval()
        {
            var pulse = new ShamanHauntingPulse();

            Assert.True(pulse.Tick(ShamanStormTier.Medium, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Medium, 0.25f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Medium, 0.25f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Medium, 0.49f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Medium, 0.01f, suspend: false));
        }

        [Fact]
        public void Tick_TierChangeFiresImmediatelyAndRestartsCadence()
        {
            var pulse = new ShamanHauntingPulse();

            Assert.True(pulse.Tick(ShamanStormTier.Weak, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Weak, 0.4f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, 0.24f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0.01f, suspend: false));
        }

        [Fact]
        public void Tick_SuspendAndNoTierResetPhase()
        {
            var pulse = new ShamanHauntingPulse();

            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, 0.1f, suspend: true));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.None, 1f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Weak, 0f, suspend: false));
        }

        [Fact]
        public void Tick_LargeFrameDropEmitsOnlyOnePulseAndDoesNotCatchUp()
        {
            var pulse = new ShamanHauntingPulse();

            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 1f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, 0.24f, suspend: false));
            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0.01f, suspend: false));
        }

        [Fact]
        public void Tick_DetectorOverloadStopsAndResetsCadenceImmediately()
        {
            var pulse = new ShamanHauntingPulse();

            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, 0.1f, suspend: false,
                detectorOverloaded: true));
            Assert.False(pulse.Tick(ShamanStormTier.Strong, 1f, suspend: false,
                detectorOverloaded: true));

            Assert.True(pulse.Tick(ShamanStormTier.Strong, 0f, suspend: false,
                detectorOverloaded: false));
        }
    }
}
