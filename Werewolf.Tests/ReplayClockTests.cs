using System.Collections.Generic;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayClockTests
    {
        [Fact]
        public void EffectiveSpeed_WithoutPace_IsBaseSpeed()
        {
            var clock = new ReplayClock(100.0);
            Assert.Equal(8f, clock.EffectiveSpeed(), 3);
            clock.Fast = true;
            Assert.Equal(32f, clock.EffectiveSpeed(), 3);
        }

        [Fact]
        public void EffectiveSpeed_WithPace_ReflectsZoneAtCurrentT()
        {
            var pace = new ReplayPace(
                new List<(double, double)> { (100.0, 200.0) }, new List<double>());
            var clock = new ReplayClock(1000.0, pace);
            Assert.Equal(8f, clock.EffectiveSpeed(), 3);
            clock.Seek(101.0);
            Assert.Equal(4f, clock.EffectiveSpeed(), 3);
            clock.Seek(150.0);
            Assert.Equal(16f, clock.EffectiveSpeed(), 3);
            clock.Fast = true;
            Assert.Equal(32f, clock.EffectiveSpeed(), 3);
        }

        [Fact]
        public void Tick_AdvancesBySpeed_OnlyWhilePlaying()
        {
            var clock = new ReplayClock(1000.0);
            clock.Tick(1.0);
            Assert.Equal(0.0, clock.T, 3);

            clock.TogglePlay();
            clock.Tick(1.0);
            Assert.Equal(8.0, clock.T, 3);
            clock.Fast = true;
            clock.Tick(1.0);
            Assert.Equal(40.0, clock.T, 3);
        }

        [Fact]
        public void Tick_WithPace_DelegatesMeetingSpeeds()
        {
            var pace = new ReplayPace(
                new List<(double, double)> { (0.0, 100.0) }, new List<double>());
            var clock = new ReplayClock(1000.0, pace);
            clock.TogglePlay();
            clock.Tick(1.0);
            Assert.Equal(4.0, clock.T, 3);
        }

        [Fact]
        public void Tick_NegativeDt_IsClampedToZero()
        {
            var clock = new ReplayClock(100.0);
            clock.TogglePlay();
            clock.Tick(-0.5);
            Assert.Equal(0.0, clock.T, 3);
            Assert.True(clock.Playing);
        }

        [Fact]
        public void Tick_StopsAtDuration_AndRestartFromEndRewinds()
        {
            var clock = new ReplayClock(10.0);
            clock.TogglePlay();
            clock.Tick(10.0);
            Assert.Equal(10.0, clock.T, 3);
            Assert.False(clock.Playing);

            clock.TogglePlay();
            Assert.Equal(0.0, clock.T, 3);
            Assert.True(clock.Playing);
        }

        [Fact]
        public void SetPlaying_PauseAndResume_DoesNotRewindMidway()
        {
            var clock = new ReplayClock(100.0);
            clock.TogglePlay();
            clock.Tick(1.0);
            Assert.Equal(8.0, clock.T, 3);

            clock.SetPlaying(false);
            clock.Seek(50.0);
            clock.Tick(1.0);
            Assert.Equal(50.0, clock.T, 3);

            clock.SetPlaying(true);
            Assert.Equal(50.0, clock.T, 3);
            clock.Tick(1.0);
            Assert.Equal(58.0, clock.T, 3);
        }

        [Fact]
        public void SetPlaying_FromEnd_Rewinds()
        {
            var clock = new ReplayClock(10.0);
            clock.Seek(10.0);
            clock.SetPlaying(true);
            Assert.Equal(0.0, clock.T, 3);
        }

        [Fact]
        public void Seek_Clamps()
        {
            var clock = new ReplayClock(60.0);
            clock.Seek(-5);
            Assert.Equal(0.0, clock.T, 3);
            clock.Seek(999);
            Assert.Equal(60.0, clock.T, 3);
            clock.Seek(30);
            Assert.Equal(30.0, clock.T, 3);
        }
    }
}
