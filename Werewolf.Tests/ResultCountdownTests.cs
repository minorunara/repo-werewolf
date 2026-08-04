using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ResultCountdownTests
    {
        [Fact]
        public void RemainingSeconds_RoundsUpAndStopsAtZero()
        {
            var countdown = new ResultCountdown();
            countdown.Begin(10_000L, 60);

            Assert.Equal(60, countdown.RemainingSeconds(10_000L));
            Assert.Equal(60, countdown.RemainingSeconds(10_001L));
            Assert.Equal(59, countdown.RemainingSeconds(11_000L));
            Assert.Equal(1, countdown.RemainingSeconds(69_999L));
            Assert.Equal(0, countdown.RemainingSeconds(70_000L));
            Assert.Equal(0, countdown.RemainingSeconds(90_000L));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Begin_NonPositive_DisablesCountdown(int seconds)
        {
            var countdown = new ResultCountdown();
            countdown.Begin(10_000L, seconds);

            Assert.False(countdown.Active);
            Assert.Null(countdown.RemainingSeconds(10_000L));
        }

        [Fact]
        public void RemainingSeconds_BackwardsClock_DoesNotExceedDuration()
        {
            var countdown = new ResultCountdown();
            countdown.Begin(10_000L, 30);

            Assert.Equal(30, countdown.RemainingSeconds(1_000L));
        }

        [Fact]
        public void Begin_AtUnixEpoch_IsStillActive()
        {
            var countdown = new ResultCountdown();
            countdown.Begin(0L, 5);

            Assert.True(countdown.Active);
            Assert.Equal(5, countdown.RemainingSeconds(0L));
        }

        [Fact]
        public void Clear_DisablesCountdown()
        {
            var countdown = new ResultCountdown();
            countdown.Begin(10_000L, 30);
            countdown.Clear();

            Assert.False(countdown.Active);
            Assert.Null(countdown.RemainingSeconds(20_000L));
        }
    }
}
