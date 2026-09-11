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

        [Fact]
        public void Defaults_EnemySetsLeaveVanillaAlone()
        {
            var config = new GameConfig();

            Assert.False(config.EnemySets.AnyOverridden);
        }
    }
}
