using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ResultSequenceTests
    {

        [Fact]
        public void RequestReturn_FiresExactlyOnce()
        {
            var rs = new ResultSequence();
            long t0 = 1_000_000L;
            rs.Begin(t0, autoReturnSeconds: 0);

            Assert.False(rs.TickShouldReturn(t0));
            Assert.False(rs.TickShouldReturn(t0 + 60_000));

            rs.RequestReturn();
            Assert.True(rs.TickShouldReturn(t0 + 61_000));

            Assert.False(rs.TickShouldReturn(t0 + 61_001));
            rs.RequestReturn();
            Assert.False(rs.TickShouldReturn(t0 + 120_000));
        }

        [Fact]
        public void RequestReturn_BeforeBegin_IsIgnored()
        {
            var rs = new ResultSequence();
            rs.RequestReturn();
            rs.Begin(nowUnixMs: 1_000L, autoReturnSeconds: 0);

            Assert.False(rs.TickShouldReturn(2_000L));
        }

        [Fact]
        public void TickShouldReturn_WithoutBegin_AlwaysFalse()
        {
            var rs = new ResultSequence();

            Assert.False(rs.TickShouldReturn(0));
            Assert.False(rs.TickShouldReturn(1));
            Assert.False(rs.TickShouldReturn(long.MaxValue));
        }

        [Fact]
        public void AutoReturn_FiresExactlyOnceAtDeadline()
        {
            var rs = new ResultSequence();
            long t0 = 1_000_000L;
            rs.Begin(t0, autoReturnSeconds: 300);

            Assert.False(rs.TickShouldReturn(t0));
            Assert.False(rs.TickShouldReturn(t0 + 299_999));

            Assert.True(rs.TickShouldReturn(t0 + 300_000));
            Assert.False(rs.TickShouldReturn(t0 + 300_001));
        }

        [Fact]
        public void AutoReturn_Disabled_NeverFiresWithoutRequest()
        {
            var rs = new ResultSequence();
            rs.Begin(nowUnixMs: 0L, autoReturnSeconds: 0);

            Assert.False(rs.TickShouldReturn(1_000_000L));
            Assert.False(rs.TickShouldReturn(long.MaxValue));
            Assert.True(rs.Active);
        }

        [Fact]
        public void AutoReturn_NegativeSeconds_TreatedAsDisabled()
        {
            var rs = new ResultSequence();
            rs.Begin(nowUnixMs: 1_000L, autoReturnSeconds: -3);

            Assert.False(rs.TickShouldReturn(1_000L));
            Assert.False(rs.TickShouldReturn(long.MaxValue));
        }

        [Fact]
        public void Active_ReflectsLifecycle()
        {
            var rs = new ResultSequence();
            Assert.False(rs.Active);

            rs.Begin(nowUnixMs: 100L, autoReturnSeconds: 5);
            Assert.True(rs.Active);

            rs.TickShouldReturn(100L + 3_000);
            Assert.True(rs.Active);

            Assert.True(rs.TickShouldReturn(100L + 5_000));
            Assert.False(rs.Active);
        }

        [Fact]
        public void Begin_Twice_RestartsSequence_And_StillFiresOnce()
        {
            var rs = new ResultSequence();
            rs.Begin(nowUnixMs: 1_000L, autoReturnSeconds: 8);
            rs.RequestReturn();

            rs.Begin(nowUnixMs: 5_000L, autoReturnSeconds: 8);
            Assert.False(rs.TickShouldReturn(5_500L));
            Assert.False(rs.TickShouldReturn(9_000L));

            Assert.True(rs.TickShouldReturn(13_000L));
            Assert.False(rs.TickShouldReturn(13_001L));
        }

        [Fact]
        public void Begin_AfterReturn_AllowsNewSequence()
        {
            var rs = new ResultSequence();
            rs.Begin(nowUnixMs: 0L, autoReturnSeconds: 0);
            rs.RequestReturn();
            Assert.True(rs.TickShouldReturn(1_000L));

            rs.Begin(nowUnixMs: 10_000L, autoReturnSeconds: 3);
            Assert.False(rs.TickShouldReturn(10_500L));
            Assert.True(rs.TickShouldReturn(13_000L));
            Assert.False(rs.TickShouldReturn(13_500L));
        }

        [Fact]
        public void Cancel_DiscardsPendingAutoReturn()
        {
            var rs = new ResultSequence();
            long t0 = 1_000_000L;
            rs.Begin(t0, autoReturnSeconds: 60);
            rs.Cancel();

            Assert.False(rs.Active);
            Assert.False(rs.TickShouldReturn(t0 + 60_000));
            Assert.False(rs.TickShouldReturn(long.MaxValue));
        }

        [Fact]
        public void Cancel_DiscardsPendingRequest()
        {
            var rs = new ResultSequence();
            rs.Begin(nowUnixMs: 0L, autoReturnSeconds: 0);
            rs.RequestReturn();
            rs.Cancel();

            Assert.False(rs.TickShouldReturn(1_000L));
        }

        [Fact]
        public void Cancel_ThenBegin_StartsFreshSequence()
        {
            var rs = new ResultSequence();
            rs.Begin(nowUnixMs: 1_000L, autoReturnSeconds: 5);
            rs.Cancel();

            rs.Begin(nowUnixMs: 10_000L, autoReturnSeconds: 5);
            Assert.False(rs.TickShouldReturn(14_999L));
            Assert.True(rs.TickShouldReturn(15_000L));
            Assert.False(rs.TickShouldReturn(15_001L));
        }

        [Fact]
        public void Cancel_BeforeBegin_IsIdempotentNoOp()
        {
            var rs = new ResultSequence();
            rs.Cancel();
            rs.Cancel();

            Assert.False(rs.Active);
            Assert.False(rs.TickShouldReturn(1_000L));
        }

        [Fact]
        public void TickShouldReturn_HandlesBackwardsTime_WithoutFiringEarly()
        {
            var rs = new ResultSequence();
            rs.Begin(nowUnixMs: 100_000L, autoReturnSeconds: 5);

            Assert.False(rs.TickShouldReturn(99_000L));
            Assert.False(rs.TickShouldReturn(50_000L));

            Assert.True(rs.TickShouldReturn(105_000L));
            Assert.False(rs.TickShouldReturn(200_000L));
        }

        [Fact]
        public void RequestReturn_FiresEvenWithBackwardsClock()
        {
            var rs = new ResultSequence();
            rs.Begin(nowUnixMs: 100_000L, autoReturnSeconds: 300);
            rs.RequestReturn();

            Assert.True(rs.TickShouldReturn(50_000L));
            Assert.False(rs.TickShouldReturn(200_000L));
        }
    }
}
