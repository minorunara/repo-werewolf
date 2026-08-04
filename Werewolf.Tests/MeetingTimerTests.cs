using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingTimerTests
    {

        [Fact]
        public void Start_ComputesEndFromWarpAndDuration()
        {
            var timer = new MeetingTimer();
            timer.Start(warpUnixMs: 1_000_000, durationSeconds: 120);

            Assert.Equal(1_000_000 + 120_000L, timer.EndUnixMs);
        }

        [Fact]
        public void IsExpired_TrueOnlyAtOrAfterEnd()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            Assert.False(timer.IsExpired(0));
            Assert.False(timer.IsExpired(119_999));
            Assert.True(timer.IsExpired(120_000));
            Assert.True(timer.IsExpired(999_999));
        }

        [Fact]
        public void IsExpired_BeforeStart_IsFalse()
        {
            var timer = new MeetingTimer();

            Assert.False(timer.IsExpired(999_999));
        }

        [Fact]
        public void ReduceByVote_WithFourRemaining_CutsRemainingByOneQuarter()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            long newEnd = timer.ReduceByVote(remainingVoters: 4, nowUnixMs: 0);

            Assert.Equal(90_000, newEnd);
            Assert.Equal(90_000, timer.EndUnixMs);
        }

        [Fact]
        public void ReduceByVote_UsesRemainingFromNow_NotFromStart()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            long newEnd = timer.ReduceByVote(remainingVoters: 2, nowUnixMs: 40_000);

            Assert.Equal(80_000, newEnd);
        }

        [Fact]
        public void ReduceByVote_IsMonotonic_NeverMovesEndLater()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            long e1 = timer.ReduceByVote(4, 0);
            long e2 = timer.ReduceByVote(4, 10_000);
            long e3 = timer.ReduceByVote(4, 20_000);

            Assert.True(e2 <= e1);
            Assert.True(e3 <= e2);
            Assert.Equal(70_000, e2);
            Assert.Equal(57_500, e3);
        }

        [Fact]
        public void ReduceByVote_WithOneRemaining_DoesNotShorten()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            long newEnd = timer.ReduceByVote(remainingVoters: 1, nowUnixMs: 0);

            Assert.Equal(120_000, newEnd);
            Assert.Equal(120_000, timer.EndUnixMs);
        }

        [Fact]
        public void ReduceByVote_WithZeroRemaining_DoesNotShorten()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            Assert.Equal(120_000, timer.ReduceByVote(0, 0));
        }

        [Fact]
        public void ReduceByVote_WhenAlreadyExpired_DoesNotExtend()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            long newEnd = timer.ReduceByVote(4, 130_000);

            Assert.Equal(120_000, newEnd);
        }

        [Fact]
        public void ReduceByVote_ResultBelowFloor_ClampsToTenSeconds()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            Assert.Equal(116_000, timer.ReduceByVote(4, 104_000));

            Assert.Equal(116_000, timer.ReduceByVote(4, 106_000));

            var timer2 = new MeetingTimer();
            timer2.Start(0, 120);
            long newEnd = timer2.ReduceByVote(remainingVoters: 2, nowUnixMs: 109_000);
            Assert.Equal(109_000 + MeetingTimer.MinRemainingMs, newEnd);
        }

        [Fact]
        public void ReduceByVote_RemainingAtOrBelowFloor_DoesNotShorten()
        {
            var timer = new MeetingTimer();
            timer.Start(0, 120);

            long newEnd = timer.ReduceByVote(remainingVoters: 4, nowUnixMs: 113_000);

            Assert.Equal(120_000, newEnd);
            Assert.Equal(120_000, timer.EndUnixMs);
        }

        [Fact]
        public void Start_NonPositiveDuration_Throws()
        {
            var timer = new MeetingTimer();

            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Start(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Start(0, -5));
        }
    }
}
