using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ValuableRecordHoldTests
    {
        private const float Step = ValuableRecordHold.HoldSeconds / 4f;

        [Fact]
        public void Tick_FiresOnceWhenHeldForFullDuration()
        {
            var hold = new ValuableRecordHold();
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i == 3, hold.Tick(held: true, pressConsumed: false, deltaSeconds: Step));
            }
        }

        [Fact]
        public void Tick_DoesNotRefireWhileStillHeld()
        {
            var hold = new ValuableRecordHold();
            for (int i = 0; i < 4; i++) hold.Tick(true, false, Step);

            for (int i = 0; i < 10; i++) Assert.False(hold.Tick(true, false, Step));
        }

        [Fact]
        public void Tick_ReleaseResetsProgressAndAllowsRefire()
        {
            var hold = new ValuableRecordHold();
            hold.Tick(true, false, Step * 3f);
            Assert.Equal(0.75f, hold.Ratio, 3);
            Assert.True(hold.IsCharging);

            hold.Tick(false, false, 0.016f);
            Assert.Equal(0f, hold.Ratio);
            Assert.False(hold.IsCharging);

            Assert.False(hold.Tick(true, false, Step * 3f));
            Assert.True(hold.Tick(true, false, Step));
        }

        [Fact]
        public void Tick_PressConsumedByCorpseReport_BlocksWholeHold()
        {
            var hold = new ValuableRecordHold();
            hold.Tick(true, pressConsumed: true, deltaSeconds: Step);
            for (int i = 0; i < 10; i++) Assert.False(hold.Tick(true, false, Step));
            Assert.Equal(0f, hold.Ratio);
            Assert.False(hold.IsCharging);
        }

        [Fact]
        public void Tick_ConsumedLatchClearsOnRelease()
        {
            var hold = new ValuableRecordHold();
            hold.Tick(true, pressConsumed: true, deltaSeconds: Step);
            hold.Tick(false, false, 0.016f);

            bool fired = false;
            for (int i = 0; i < 4; i++) fired |= hold.Tick(true, false, Step);
            Assert.True(fired);
        }

        [Fact]
        public void Ratio_IsClampedAndMonotonicWhileCharging()
        {
            var hold = new ValuableRecordHold();
            float prev = 0f;
            for (int i = 0; i < 3; i++)
            {
                hold.Tick(true, false, Step);
                Assert.InRange(hold.Ratio, prev, 1f);
                prev = hold.Ratio;
            }
            hold.Tick(true, false, ValuableRecordHold.HoldSeconds * 2f);
            Assert.Equal(1f, hold.Ratio);
        }

        [Fact]
        public void Tick_IgnoresNonPositiveDelta()
        {
            var hold = new ValuableRecordHold();
            hold.Tick(true, false, Step);
            float before = hold.Ratio;
            hold.Tick(true, false, 0f);
            hold.Tick(true, false, -1f);
            Assert.Equal(before, hold.Ratio);
        }

        [Fact]
        public void Reset_ClearsProgressAndFiredLatch()
        {
            var hold = new ValuableRecordHold();
            for (int i = 0; i < 4; i++) hold.Tick(true, false, Step);
            hold.Reset();

            Assert.Equal(0f, hold.Ratio);
            Assert.False(hold.Tick(true, false, Step * 3f));
            Assert.True(hold.Tick(true, false, Step));
        }
    }
}
