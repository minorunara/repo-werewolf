using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RoundTimerTests
    {

        [Fact]
        public void Start_ComputesEndTimeFromNowAndRoundSeconds()
        {
            var timer = new RoundTimer();
            timer.Start(nowUnixMs: 1_000_000, roundSeconds: 1800);

            Assert.Equal(1_000_000 + 1_800_000L, timer.EndUnixMs);
        }

        [Fact]
        public void RemainingMs_IsDifferenceFromAnyNow()
        {
            var timer = new RoundTimer();
            timer.Start(1_000_000, 1800);

            Assert.Equal(1_800_000, timer.RemainingMs(1_000_000));
            Assert.Equal(1_200_000, timer.RemainingMs(1_600_000));
        }

        [Fact]
        public void RemainingMs_ClampsAtZeroAfterEnd()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);

            Assert.Equal(0, timer.RemainingMs(60_000));
            Assert.Equal(0, timer.RemainingMs(99_999_999));
        }

        [Fact]
        public void CheckExpiry_BeforeEnd_ReturnsFalse()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);

            Assert.False(timer.CheckExpiry(59_999));
            Assert.False(timer.Expired);
        }

        [Fact]
        public void CheckExpiry_AtEnd_FiresOnceOnly()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);

            Assert.True(timer.CheckExpiry(60_000));
            Assert.True(timer.Expired);
            Assert.False(timer.CheckExpiry(60_001));
        }

        [Fact]
        public void CheckExpiry_BeforeStart_ReturnsFalse()
        {
            var timer = new RoundTimer();

            Assert.False(timer.CheckExpiry(1_000_000));
            Assert.Equal(0, timer.RemainingMs(1_000_000));
        }

        [Fact]
        public void CheckExpiry_DuringMeeting_NeverFiresEvenPastEnd()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);
            timer.PauseForMeeting(30_000);

            Assert.False(timer.CheckExpiry(60_000));
            Assert.False(timer.CheckExpiry(999_999));
            Assert.False(timer.Expired);
        }

        [Fact]
        public void RemainingMs_IsFrozenDuringMeeting()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);
            timer.PauseForMeeting(30_000);

            Assert.Equal(30_000, timer.RemainingMs(55_000));
            Assert.Equal(30_000, timer.RemainingMs(999_999));
        }

        [Fact]
        public void Resume_ExtendsEndTimeByMeetingElapsed()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);
            timer.PauseForMeeting(30_000);

            long newEnd = timer.ResumeFromMeeting(50_000);

            Assert.Equal(80_000, newEnd);
            Assert.Equal(80_000, timer.EndUnixMs);
            Assert.False(timer.IsPaused);
            Assert.Equal(30_000, timer.RemainingMs(50_000));
        }

        [Fact]
        public void Resume_ThenExpiryFiresAtExtendedEnd()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);
            timer.PauseForMeeting(30_000);
            timer.ResumeFromMeeting(50_000);

            Assert.False(timer.CheckExpiry(79_999));
            Assert.True(timer.CheckExpiry(80_000));
        }

        [Fact]
        public void MultipleMeetingCycles_AccumulateExtension()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);
            timer.PauseForMeeting(10_000);
            timer.ResumeFromMeeting(20_000);
            timer.PauseForMeeting(30_000);
            long newEnd = timer.ResumeFromMeeting(45_000);

            Assert.Equal(85_000, newEnd);
        }

        [Fact]
        public void Resume_WithoutPause_IsNoOpAndReturnsCurrentEnd()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);

            Assert.Equal(60_000, timer.ResumeFromMeeting(30_000));
            Assert.Equal(60_000, timer.EndUnixMs);
        }

        [Fact]
        public void Pause_WhenAlreadyPaused_KeepsOriginalMeetingStart()
        {
            var timer = new RoundTimer();
            timer.Start(0, 60);
            timer.PauseForMeeting(30_000);
            timer.PauseForMeeting(40_000);

            long newEnd = timer.ResumeFromMeeting(50_000);

            Assert.Equal(80_000, newEnd);
        }

        [Fact]
        public void ForceExpire_MakesExpiryFireImmediately()
        {
            var timer = new RoundTimer();
            timer.Start(0, 1800);

            timer.ForceExpire(100_000);

            Assert.Equal(100_000, timer.EndUnixMs);
            Assert.True(timer.CheckExpiry(100_000));
        }

        [Fact]
        public void ForceExpire_DuringMeeting_DoesNotFireUntilResume()
        {
            var timer = new RoundTimer();
            timer.Start(0, 1800);
            timer.PauseForMeeting(10_000);
            timer.ForceExpire(20_000);

            Assert.False(timer.CheckExpiry(20_000));

            timer.ResumeFromMeeting(30_000);
            Assert.True(timer.CheckExpiry(30_000));
        }

        [Fact]
        public void Start_NonPositiveRoundSeconds_Throws()
        {
            var timer = new RoundTimer();

            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Start(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Start(0, -10));
        }
    }
}
