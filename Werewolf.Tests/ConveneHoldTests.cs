using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ConveneHoldTests
    {
        private const float Step = ConveneHold.HoldSeconds / 4f;

        [Fact]
        public void Tick_FiresOnceWhenHeldForFullDuration()
        {
            var hold = new ConveneHold();
            bool fired = false;
            for (int i = 0; i < 4; i++)
            {
                bool result = hold.Tick(engaged: true, deltaSeconds: Step);
                Assert.Equal(i == 3, result);
                if (result) fired = true;
            }
            Assert.True(fired);
        }

        [Fact]
        public void Tick_DoesNotRefireWhileStillEngaged()
        {
            var hold = new ConveneHold();
            for (int i = 0; i < 4; i++) hold.Tick(true, Step);

            for (int i = 0; i < 10; i++) Assert.False(hold.Tick(true, Step));
        }

        [Fact]
        public void Tick_DisengageResetsProgressImmediately()
        {
            var hold = new ConveneHold();
            hold.Tick(true, Step * 3f);
            Assert.Equal(0.75f, hold.Ratio, 3);
            Assert.True(hold.IsCharging);

            hold.Tick(false, 0.016f);
            Assert.Equal(0f, hold.Ratio);
            Assert.False(hold.IsCharging);

            Assert.False(hold.Tick(true, Step * 3f));
            Assert.True(hold.Tick(true, Step));
        }

        [Fact]
        public void Tick_CanFireAgainAfterReleaseBetweenHolds()
        {
            var hold = new ConveneHold();
            for (int i = 0; i < 4; i++) hold.Tick(true, Step);
            hold.Tick(false, 0.016f);

            bool refired = false;
            for (int i = 0; i < 4; i++) refired |= hold.Tick(true, Step);
            Assert.True(refired);
        }

        [Fact]
        public void Ratio_IsClampedAndMonotonicWhileCharging()
        {
            var hold = new ConveneHold();
            float prev = 0f;
            for (int i = 0; i < 3; i++)
            {
                hold.Tick(true, Step);
                Assert.InRange(hold.Ratio, prev, 1f);
                prev = hold.Ratio;
            }
            hold.Tick(true, ConveneHold.HoldSeconds * 2f);
            Assert.Equal(1f, hold.Ratio);
        }

        [Fact]
        public void Tick_IgnoresNonPositiveDelta()
        {
            var hold = new ConveneHold();
            hold.Tick(true, Step);
            float before = hold.Ratio;
            hold.Tick(true, 0f);
            hold.Tick(true, -1f);
            Assert.Equal(before, hold.Ratio);
        }

        [Fact]
        public void Reset_ClearsProgressAndFiredLatch()
        {
            var hold = new ConveneHold();
            for (int i = 0; i < 4; i++) hold.Tick(true, Step);
            hold.Reset();

            Assert.Equal(0f, hold.Ratio);
            Assert.False(hold.Tick(true, Step * 3f));
            Assert.True(hold.Tick(true, Step));
        }
    }
}
