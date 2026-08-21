using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class VoteTallyTimelineTests
    {

        [Fact]
        public void StepMs_NoVotes_ReturnsZero()
        {
            Assert.Equal(0, VoteTallyTimeline.StepMs(0));
            Assert.Equal(0, VoteTallyTimeline.StepMs(-1));
        }

        [Fact]
        public void StepMs_FewVotes_ClampedByMaxStep()
        {
            Assert.Equal(VoteTallyTimeline.MaxStepMs, VoteTallyTimeline.StepMs(1));
            Assert.Equal(VoteTallyTimeline.MaxStepMs, VoteTallyTimeline.StepMs(2));
        }

        [Fact]
        public void StepMs_HugeVotes_NeverBelowOneMs()
        {
            Assert.True(VoteTallyTimeline.StepMs(10_000) >= 1);
        }

        [Fact]
        public void TallyEnd_NeverExceedsCap_WhileStepUnclampedLow()
        {
            for (int max = 1; max <= 2500; max++)
            {
                Assert.True(VoteTallyTimeline.TallyEndMs(max) <= VoteTallyTimeline.DurationCapMs,
                    $"maxCount={max}");
            }
        }

        [Fact]
        public void Landed_MonotonicAndCapped()
        {
            long step = VoteTallyTimeline.StepMs(5);
            int prev = 0;
            for (long t = 0; t <= VoteTallyTimeline.TallyEndMs(5) + step; t += 10)
            {
                int landed = VoteTallyTimeline.Landed(5, t, step);
                Assert.True(landed >= prev, $"t={t}");
                Assert.InRange(landed, 0, 5);
                prev = landed;
            }
            Assert.Equal(5, prev);
        }

        [Fact]
        public void Landed_StartsAtZero_AndFirstVoteWaitsOneStep()
        {
            long step = VoteTallyTimeline.StepMs(5);
            Assert.Equal(0, VoteTallyTimeline.Landed(5, 0, step));
            Assert.Equal(0, VoteTallyTimeline.Landed(5, step - 1, step));
            Assert.Equal(1, VoteTallyTimeline.Landed(5, step, step));
        }

        [Fact]
        public void Landed_LowerCountFinishesEarly_AtSharedPace()
        {
            long step = VoteTallyTimeline.StepMs(10);
            Assert.Equal(2, VoteTallyTimeline.Landed(2, step * 2, step));
            Assert.Equal(2, VoteTallyTimeline.Landed(2, VoteTallyTimeline.TallyEndMs(10), step));
        }

        [Fact]
        public void Landed_ZeroStep_ReturnsFinalImmediately()
        {
            Assert.Equal(3, VoteTallyTimeline.Landed(3, 0, 0));
        }

        [Fact]
        public void BannerReady_WaitsForAllLandingsPlusDelay()
        {
            long end = VoteTallyTimeline.TallyEndMs(4);
            Assert.False(VoteTallyTimeline.BannerReady(4, end));
            Assert.False(VoteTallyTimeline.BannerReady(4, end + VoteTallyTimeline.BannerDelayMs - 1));
            Assert.True(VoteTallyTimeline.BannerReady(4, end + VoteTallyTimeline.BannerDelayMs));
        }

        [Fact]
        public void BannerReady_NoVotes_OnlyDelayApplies()
        {
            Assert.False(VoteTallyTimeline.BannerReady(0, VoteTallyTimeline.BannerDelayMs - 1));
            Assert.True(VoteTallyTimeline.BannerReady(0, VoteTallyTimeline.BannerDelayMs));
        }

        [Fact]
        public void VisibleChips_CapsAtMaxChips()
        {
            Assert.Equal(3, VoteTallyTimeline.VisibleChips(3));
            Assert.Equal(VoteTallyTimeline.MaxChips, VoteTallyTimeline.VisibleChips(VoteTallyTimeline.MaxChips));
            Assert.Equal(VoteTallyTimeline.MaxChips, VoteTallyTimeline.VisibleChips(VoteTallyTimeline.MaxChips + 30));
        }

        [Fact]
        public void TopChip_SolidWithinCap_AnyPhase()
        {
            const long step = 100;
            Assert.True(VoteTallyTimeline.TopChipVisible(20, VoteTallyTimeline.MaxChips, 10, step));
            Assert.True(VoteTallyTimeline.TopChipVisible(20, VoteTallyTimeline.MaxChips, 60, step));
        }

        [Fact]
        public void TopChip_BlinksWhileOverflowInflow()
        {
            const long step = 100;
            int landed = VoteTallyTimeline.MaxChips + 3;
            const int final = VoteTallyTimeline.MaxChips + 8;
            Assert.False(VoteTallyTimeline.TopChipVisible(final, landed, landed * step + 10, step));
            Assert.True(VoteTallyTimeline.TopChipVisible(final, landed, landed * step + 60, step));
        }

        [Fact]
        public void TopChip_SolidAfterAllLanded()
        {
            const long step = 100;
            const int final = VoteTallyTimeline.MaxChips + 8;
            Assert.True(VoteTallyTimeline.TopChipVisible(final, final, final * step + 10, step));
            Assert.True(VoteTallyTimeline.TopChipVisible(final, final, final * step + 60, step));
        }
    }
}
