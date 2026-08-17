using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GameOverSafetyTests
    {
        [Theory]
        [InlineData(GamePhase.GameOver, true)]
        [InlineData(GamePhase.Play, false)]
        [InlineData(GamePhase.Meeting, false)]
        [InlineData(GamePhase.Lobby, false)]
        public void ShouldHoldEnemyFreeze_OnlyDuringGameOver(GamePhase phase, bool expected)
        {
            Assert.Equal(expected, GameOverSafety.ShouldHoldEnemyFreeze(phase));
        }

        [Theory]
        [InlineData(GamePhase.Meeting, true, true, true)]
        [InlineData(GamePhase.Meeting, true, false, false)]
        [InlineData(GamePhase.Meeting, false, false, false)]
        [InlineData(GamePhase.GameOver, false, false, true)]
        [InlineData(GamePhase.GameOver, true, true, true)]
        [InlineData(GamePhase.Play, false, false, false)]
        [InlineData(GamePhase.Play, false, true, false)]
        [InlineData(GamePhase.Lobby, false, false, false)]
        public void ShouldInjectInvincibility_MeetingWarpOrGameOver(
            GamePhase phase, bool meetingActive, bool warpDone, bool expected)
        {
            Assert.Equal(expected,
                GameOverSafety.ShouldInjectInvincibility(phase, meetingActive, warpDone));
        }

        [Fact]
        public void ShouldInjectInvincibility_SeamlessAcrossMeetingToGameOver()
        {
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.Meeting, true, true));
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.GameOver, true, true));
            Assert.True(GameOverSafety.ShouldInjectInvincibility(GamePhase.GameOver, false, false));
        }
    }
}
