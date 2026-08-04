using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GameConfigTests
    {
        [Fact]
        public void Defaults_AreNeutralForModeAndDebug()
        {
            var config = new GameConfig();

            Assert.False(config.WerewolfModeEnabled);
            Assert.False(config.DebugMode);
        }

        [Fact]
        public void Defaults_WorldgenSpecsAreEmpty()
        {
            var config = new GameConfig();

            Assert.Equal("", config.StartMapName);
            Assert.Equal("", config.StartItemsSpec);
            Assert.Equal("", config.StartUpgradesSpec);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(50, true)]
        [InlineData(100, true)]
        public void BlackCatPossible_DerivedFromChancePercent(int chancePercent, bool expected)
        {
            var config = new GameConfig { WerewolfCount = 2, BlackCatChancePercent = chancePercent };

            Assert.Equal(expected, config.BlackCatPossible(playerCount: 5));
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(50, true)]
        [InlineData(100, true)]
        public void BomberPossible_DerivedFromChancePercent(int chancePercent, bool expected)
        {
            var config = new GameConfig { WerewolfCount = 2, BomberChancePercent = chancePercent };

            Assert.Equal(expected, config.BomberPossible(playerCount: 5));
        }
    }
}
