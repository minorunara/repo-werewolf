using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingIntroTests
    {
        [Fact]
        public void Timeline_ComposesFromDeathRevealAndGaugeReveal()
        {
            Assert.Equal(DeathReveal.TotalDurationMs, MeetingIntro.GaugeRevealOffsetMs);
            Assert.Equal(GaugeReveal.HoldMs + GaugeReveal.DurationMs, MeetingIntro.GaugeRevealMs);
            Assert.Equal(MeetingIntro.GaugeRevealOffsetMs + MeetingIntro.GaugeRevealMs,
                MeetingIntro.VotingUiDelayMs);
        }

        [Fact]
        public void MoveProgress_ClampsAtBothEnds()
        {
            Assert.Equal(0.0, MeetingIntro.MoveProgress(-1));
            Assert.Equal(0.0, MeetingIntro.MoveProgress(0));
            Assert.Equal(1.0, MeetingIntro.MoveProgress(MeetingIntro.GaugeMoveMs));
            Assert.Equal(1.0, MeetingIntro.MoveProgress(MeetingIntro.GaugeMoveMs * 10));
        }

        [Fact]
        public void MoveProgress_IsMonotonicNonDecreasing()
        {
            double previous = -1.0;
            for (long t = -50; t <= MeetingIntro.GaugeMoveMs + 50; t += 10)
            {
                double progress = MeetingIntro.MoveProgress(t);
                Assert.True(progress >= previous, $"t={t}: {progress} < {previous}");
                previous = progress;
            }
        }

        [Fact]
        public void MoveProgress_IsEaseIn()
        {
            double atHalf = MeetingIntro.MoveProgress(MeetingIntro.GaugeMoveMs / 2);
            Assert.InRange(atHalf, 0.0001, 0.4999);
        }
    }
}
