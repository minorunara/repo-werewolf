using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GameOverSafetyTests
    {
        [Theory]
        [InlineData(GamePhase.GameOver, false, true)]
        [InlineData(GamePhase.GameOver, true, true)]
        [InlineData(GamePhase.Play, false, false)]
        [InlineData(GamePhase.Play, true, true)]
        [InlineData(GamePhase.Meeting, false, false)]
        [InlineData(GamePhase.Meeting, true, true)]
        [InlineData(GamePhase.Lobby, false, false)]
        public void ShouldHoldEnemyFreeze_DuringGameOverOrWinCeremony(
            GamePhase phase, bool winCeremonyActive, bool expected)
        {
            Assert.Equal(expected, GameOverSafety.ShouldHoldEnemyFreeze(phase, winCeremonyActive));
        }

        [Theory]
        [InlineData(GamePhase.Meeting, true, true, false, true)]
        [InlineData(GamePhase.Meeting, true, false, false, false)]
        [InlineData(GamePhase.Meeting, false, false, false, false)]
        [InlineData(GamePhase.GameOver, false, false, false, true)]
        [InlineData(GamePhase.GameOver, true, true, false, true)]
        [InlineData(GamePhase.Play, false, false, true, true)]
        [InlineData(GamePhase.Meeting, true, false, true, true)]
        [InlineData(GamePhase.Play, false, false, false, false)]
        [InlineData(GamePhase.Play, false, true, false, false)]
        [InlineData(GamePhase.Lobby, false, false, false, false)]
        public void ShouldInjectInvincibility_MeetingWarpOrCeremonyOrGameOver(
            GamePhase phase, bool meetingActive, bool warpDone, bool winCeremonyActive, bool expected)
        {
            Assert.Equal(expected, GameOverSafety.ShouldInjectInvincibility(
                phase, meetingActive, warpDone, winCeremonyActive));
        }

        [Fact]
        public void ShouldInjectInvincibility_SeamlessAcrossMeetingToGameOver()
        {
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.Meeting, true, true, false));
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.Meeting, true, true, true));
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.GameOver, true, true, true));
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.GameOver, false, false, false));
        }

        [Fact]
        public void SafetyWindows_SeamlessAcrossCeremonyToGameOver()
        {
            Assert.True(GameOverSafety.ShouldHoldEnemyFreeze(GamePhase.Play, true));
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.Play, false, false, true));
            Assert.True(GameOverSafety.ShouldHoldEnemyFreeze(GamePhase.GameOver, true));
            Assert.True(GameOverSafety.ShouldHoldEnemyFreeze(GamePhase.GameOver, false));
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.GameOver, false, false, false));
        }
    }
}
