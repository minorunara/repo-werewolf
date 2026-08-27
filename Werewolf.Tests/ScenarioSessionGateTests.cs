using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioSessionGateTests
    {
        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        public void ShouldEngage(bool werewolfModeEnabled, bool scenarioActive, bool expected)
            => Assert.Equal(expected, ScenarioSessionGate.ShouldEngage(werewolfModeEnabled, scenarioActive));

        [Fact]
        public void PlayerCountSatisfied_NormalRun_KeepsExistingRule()
        {
            for (int players = 0; players <= PlayerCountGate.MinimumPlayers + 1; players++)
            {
                Assert.Equal(PlayerCountGate.IsSatisfied(players, false),
                    ScenarioSessionGate.PlayerCountSatisfied(players, false, false));
                Assert.Equal(PlayerCountGate.IsSatisfied(players, true),
                    ScenarioSessionGate.PlayerCountSatisfied(players, true, false));
            }
        }

        [Fact]
        public void PlayerCountSatisfied_Scenario_PassesWithLocalPlayerAlone()
        {
            Assert.False(PlayerCountGate.IsSatisfied(1, false));
            Assert.True(ScenarioSessionGate.PlayerCountSatisfied(1, false, true));
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        public void RequiresConfigValidation(bool debugMode, bool scenarioActive, bool expected)
            => Assert.Equal(expected, ScenarioSessionGate.RequiresConfigValidation(debugMode, scenarioActive));

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        public void ChatLogEnabled(bool userSetting, bool scenarioActive, bool expected)
            => Assert.Equal(expected, ScenarioSessionGate.ChatLogEnabled(userSetting, scenarioActive));

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        public void MapGridEnabled(bool userSetting, bool scenarioActive, bool expected)
            => Assert.Equal(expected, ScenarioSessionGate.MapGridEnabled(userSetting, scenarioActive));

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void VoidMatchEnabled(bool scenarioActive, bool expected)
            => Assert.Equal(expected, ScenarioSessionGate.VoidMatchEnabled(scenarioActive));

        [Fact]
        public void ReturnsToTitle_OnlyForScenario()
        {
            Assert.True(ScenarioSessionGate.ReturnsToTitle(true));
            Assert.False(ScenarioSessionGate.ReturnsToTitle(false));
        }

    }
}
