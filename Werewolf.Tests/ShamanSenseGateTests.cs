using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ShamanSenseGateTests
    {
        [Theory]
        [InlineData(GamePhase.Play, false)]
        [InlineData(GamePhase.Play, true)]
        [InlineData(GamePhase.Meeting, true)]
        public void NormalPlayAndButtonCountdown_Continue(
            GamePhase phase, bool meetingActive)
        {
            Assert.False(ShamanSenseGate.ShouldSuspend(
                phase, localAlive: true, meetingActive,
                warpDone: false, ConveneKind.Button));
        }

        [Theory]
        [InlineData(GamePhase.Meeting, true, false, ConveneKind.CorpseReport)]
        [InlineData(GamePhase.Play, true, false, ConveneKind.CorpseReport)]
        public void CorpseReportCountdown_SuspendsImmediately(
            GamePhase phase, bool meetingActive, bool warpDone, ConveneKind kind)
        {
            Assert.True(ShamanSenseGate.ShouldSuspend(
                phase, localAlive: true, meetingActive, warpDone, kind));
        }

        [Theory]
        [InlineData(ConveneKind.Button)]
        [InlineData(ConveneKind.CorpseReport)]
        public void WarpedMeeting_SuspendsRegardlessOfKind(ConveneKind kind)
        {
            Assert.True(ShamanSenseGate.ShouldSuspend(
                GamePhase.Meeting, localAlive: true,
                meetingActive: true, warpDone: true, kind));
        }

        [Theory]
        [InlineData(GamePhase.Lobby, true)]
        [InlineData(GamePhase.GameOver, true)]
        [InlineData(GamePhase.Play, false)]
        [InlineData(GamePhase.Meeting, false)]
        public void RoundOutsideDeathAndIncompleteMeetingState_Suspend(
            GamePhase phase, bool localAlive)
        {
            Assert.True(ShamanSenseGate.ShouldSuspend(
                phase, localAlive, meetingActive: false,
                warpDone: false, ConveneKind.Button));
        }
    }
}
