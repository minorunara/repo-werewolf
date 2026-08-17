using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CorpseReportHudGateTests
    {
        [Fact]
        public void PlayAliveNoMeetingNoLastRun_IsActive()
        {
            Assert.Equal(CorpseReportHudMode.Active, CorpseReportHudGate.Compute(
                GamePhase.Play, alive: true, meetingActive: false,
                warpedInMeeting: false, lastRunActive: false));
        }

        [Theory]
        [InlineData(GamePhase.Meeting)]
        [InlineData(GamePhase.Play)]
        public void ConveneCountdown_IsBlocked(GamePhase phase)
        {
            Assert.Equal(CorpseReportHudMode.Blocked, CorpseReportHudGate.Compute(
                phase, alive: true, meetingActive: true,
                warpedInMeeting: false, lastRunActive: false));
        }

        [Fact]
        public void WarpedInMeeting_IsHidden()
        {
            Assert.Equal(CorpseReportHudMode.Hidden, CorpseReportHudGate.Compute(
                GamePhase.Meeting, alive: true, meetingActive: true,
                warpedInMeeting: true, lastRunActive: false));
        }

        [Fact]
        public void LastRunInPlay_IsBlocked()
        {
            Assert.Equal(CorpseReportHudMode.Blocked, CorpseReportHudGate.Compute(
                GamePhase.Play, alive: true, meetingActive: false,
                warpedInMeeting: false, lastRunActive: true));
        }

        [Fact]
        public void ConveneCountdownDuringLastRun_IsBlocked()
        {
            Assert.Equal(CorpseReportHudMode.Blocked, CorpseReportHudGate.Compute(
                GamePhase.Meeting, alive: true, meetingActive: true,
                warpedInMeeting: false, lastRunActive: true));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, false)]
        [InlineData(false, false, true)]
        public void Dead_IsAlwaysHidden(bool meetingActive, bool warpedInMeeting, bool lastRunActive)
        {
            Assert.Equal(CorpseReportHudMode.Hidden, CorpseReportHudGate.Compute(
                GamePhase.Play, alive: false, meetingActive: meetingActive,
                warpedInMeeting: warpedInMeeting, lastRunActive: lastRunActive));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void OutsideSession_IsHidden(GamePhase phase)
        {
            Assert.Equal(CorpseReportHudMode.Hidden, CorpseReportHudGate.Compute(
                phase, alive: true, meetingActive: true,
                warpedInMeeting: false, lastRunActive: true));
        }

        [Fact]
        public void MeetingPhaseWithoutActiveMeeting_IsHidden()
        {
            Assert.Equal(CorpseReportHudMode.Hidden, CorpseReportHudGate.Compute(
                GamePhase.Meeting, alive: true, meetingActive: false,
                warpedInMeeting: false, lastRunActive: false));
        }
    }
}
