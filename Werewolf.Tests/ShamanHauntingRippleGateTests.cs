using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ShamanHauntingRippleGateTests
    {
        [Fact]
        public void ActiveReportHud_ShowsRipple()
        {
            Assert.True(ShamanHauntingRippleGate.CanShow(
                CorpseReportHudMode.Active, senseSuspended: false));
        }

        [Fact]
        public void LastRunBlockedHud_ShowsRipple()
        {
            Assert.True(ShamanHauntingRippleGate.CanShow(
                CorpseReportHudMode.Blocked, senseSuspended: false));
        }

        [Fact]
        public void CorpseReportCountdown_SuspendedSenseHidesRipple()
        {
            Assert.False(ShamanHauntingRippleGate.CanShow(
                CorpseReportHudMode.Blocked, senseSuspended: true));
        }

        [Fact]
        public void HiddenReportHud_HidesRipple()
        {
            Assert.False(ShamanHauntingRippleGate.CanShow(
                CorpseReportHudMode.Hidden, senseSuspended: false));
        }
    }
}
