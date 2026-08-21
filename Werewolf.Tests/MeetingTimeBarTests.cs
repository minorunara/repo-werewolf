using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingTimeBarTests
    {
        private const long T0 = 1_000_000;

        [Fact]
        public void Begin_FirstTickInitializesDisplayToActual()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(180_000);
            bar.Tick(175_000, T0);

            Assert.Equal(180_000, bar.TotalMs);
            Assert.Equal(175_000.0 / 180_000.0, bar.FillFraction, 6);
            Assert.Equal(bar.FillFraction, bar.LagFraction, 6);
        }

        [Fact]
        public void NaturalCountdown_KeepsLagEqualToFill()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(120_000);
            long end = T0 + 120_000;
            for (long now = T0; now <= T0 + 2_000; now += 16)
            {
                bar.Tick(end - now, now);
            }

            Assert.Equal(bar.FillFraction, bar.LagFraction, 6);
        }

        [Fact]
        public void VoteJump_HoldsLagThenDrainsToActual()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(120_000);
            long end = T0 + 120_000;
            bar.Tick(end - T0, T0);
            long t1 = T0 + 10_000;
            bar.Tick(end - t1, t1);

            long newEnd = end - 40_000;
            bar.Tick(newEnd - t1, t1);
            Assert.True(bar.LagFraction > bar.FillFraction);

            long t2 = t1 + MeetingTimeBar.LagHoldMs - 100;
            double lagDuringHold = bar.LagFraction;
            bar.Tick(newEnd - t2, t2);
            Assert.Equal(lagDuringHold, bar.LagFraction, 6);
            Assert.True(bar.LagFraction >= bar.FillFraction);

            long t3 = t2 + 5_000;
            bar.Tick(newEnd - t3, t3);
            Assert.Equal(bar.FillFraction, bar.LagFraction, 6);
        }

        [Fact]
        public void ConsecutiveJumps_ExtendHold()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(120_000);
            long end = T0 + 120_000;
            bar.Tick(end - T0, T0);

            long t1 = T0 + 1_000;
            long end2 = end - 30_000;
            bar.Tick(end2 - t1, t1);

            long t2 = t1 + MeetingTimeBar.LagHoldMs - 200;
            long end3 = end2 - 20_000;
            bar.Tick(end3 - t2, t2);

            long t3 = t2 + MeetingTimeBar.LagHoldMs - 200;
            double lagBefore = bar.LagFraction;
            bar.Tick(end3 - t3, t3);
            Assert.Equal(lagBefore, bar.LagFraction, 6);
        }

        [Fact]
        public void BeginWithUnknownTotal_AdoptsFirstRemaining()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(0);
            bar.Tick(90_000, T0);

            Assert.Equal(90_000, bar.TotalMs);
            Assert.Equal(1.0, bar.FillFraction, 6);
        }

        [Fact]
        public void Expiry_DrainsLagToZeroWithoutJumpDetection()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(60_000);
            long end = T0 + 60_000;
            bar.Tick(end - T0, T0);

            bar.Tick(0, end + 1_000);
            bar.Tick(0, end + 2_000);

            Assert.Equal(0.0, bar.FillFraction, 6);
            Assert.Equal(0.0, bar.LagFraction, 6);
        }

        [Fact]
        public void RemainingBeyondTotal_IsClampedToFull()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(60_000);
            bar.Tick(75_000, T0);

            Assert.Equal(1.0, bar.FillFraction, 6);
            Assert.Equal(1.0, bar.LagFraction, 6);
        }

        [Fact]
        public void RedZoneFraction_IsFloorShareOfTotal_ClampedToFullBar()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(180_000);
            Assert.Equal((double)MeetingTimeBar.RedZoneMs / 180_000, bar.RedZoneFraction, 6);

            var tiny = new MeetingTimeBar();
            tiny.Begin(MeetingTimeBar.RedZoneMs / 2);
            Assert.Equal(1.0, tiny.RedZoneFraction, 6);
        }

        [Fact]
        public void Reset_ClearsFractions()
        {
            var bar = new MeetingTimeBar();
            bar.Begin(60_000);
            bar.Tick(30_000, T0);
            bar.Reset();

            Assert.False(bar.Started);
            Assert.Equal(0.0, bar.FillFraction, 6);
            Assert.Equal(0.0, bar.LagFraction, 6);
            Assert.Equal(0.0, bar.RedZoneFraction, 6);
        }
    }
}
