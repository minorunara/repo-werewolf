using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class OutfitRulesTests
    {
        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void MatchLive_NotAllowed_Blocks(GamePhase phase)
        {
            Assert.True(OutfitRules.ShouldBlockOutfitChange(phase, allowedByRoomSetting: false));
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void MatchLive_Allowed_DoesNotBlock(GamePhase phase)
        {
            Assert.False(OutfitRules.ShouldBlockOutfitChange(phase, allowedByRoomSetting: true));
        }

        [Theory]
        [InlineData(GamePhase.Lobby, false)]
        [InlineData(GamePhase.Lobby, true)]
        [InlineData(GamePhase.GameOver, false)]
        [InlineData(GamePhase.GameOver, true)]
        public void OutsideMatch_NeverBlocks(GamePhase phase, bool allowed)
        {
            Assert.False(OutfitRules.ShouldBlockOutfitChange(phase, allowed));
        }
    }
}
