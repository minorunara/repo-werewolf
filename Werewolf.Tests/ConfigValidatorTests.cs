using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ConfigValidatorTests
    {
        [Fact]
        public void Validate_DefaultConfig_NoIssues()
        {
            var issues = ConfigValidator.Validate(new GameConfig(), playerCount: 5);

            Assert.Empty(issues);
        }

        [Fact]
        public void Validate_WerewolfCountEqualsPlayerCount_ReportsIssue()
        {
            var config = new GameConfig { WerewolfCount = 5 };

            var issues = ConfigValidator.Validate(config, playerCount: 5);

            Assert.Contains(ConfigIssue.WerewolfCountExceedsPlayers, issues);
        }

        [Fact]
        public void Validate_WerewolfCountExceedsPlayerCount_ReportsIssue()
        {
            var config = new GameConfig { WerewolfCount = 6 };

            var issues = ConfigValidator.Validate(config, playerCount: 5);

            Assert.Contains(ConfigIssue.WerewolfCountExceedsPlayers, issues);
        }

        [Fact]
        public void Validate_WerewolfCountBelowPlayerCount_NoIssue()
        {
            var config = new GameConfig { WerewolfCount = 4 };

            var issues = ConfigValidator.Validate(config, playerCount: 5);

            Assert.DoesNotContain(ConfigIssue.WerewolfCountExceedsPlayers, issues);
        }

        [Fact]
        public void Validate_WerewolfCountDefault_NoIssueAtMinimumPlayers()
        {
            var config = new GameConfig();

            var issues = ConfigValidator.Validate(config, playerCount: 3);

            Assert.DoesNotContain(ConfigIssue.WerewolfCountExceedsPlayers, issues);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_RoundSecondsNotPositive_ReportsIssue(int roundSeconds)
        {
            var config = new GameConfig { RoundSeconds = roundSeconds };

            var issues = ConfigValidator.Validate(config, playerCount: 5);

            Assert.Contains(ConfigIssue.RoundSecondsNotPositive, issues);
        }

        [Fact]
        public void Validate_MultipleIssues_ReportsAll()
        {
            var config = new GameConfig { WerewolfCount = 5, RoundSeconds = 0 };

            var issues = ConfigValidator.Validate(config, playerCount: 5);

            Assert.Equal(2, issues.Count);
            Assert.Contains(ConfigIssue.WerewolfCountExceedsPlayers, issues);
            Assert.Contains(ConfigIssue.RoundSecondsNotPositive, issues);
        }
    }
}
