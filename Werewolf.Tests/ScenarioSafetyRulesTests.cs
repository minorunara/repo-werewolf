using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioSafetyRulesTests
    {

        [Theory]
        [InlineData(100, 1)]
        [InlineData(100, 25)]
        [InlineData(100, 99)]
        [InlineData(100, 100)]
        [InlineData(100, 500)]
        [InlineData(20, 100)]
        [InlineData(2, 100)]
        [InlineData(300, 1000)]
        public void ClampDamage_NeverLeavesHealthBelowOne(int health, int damage)
        {
            int clamped = ScenarioSafetyRules.ClampDamage(health, damage);
            Assert.True(health - clamped >= ScenarioSafetyRules.MinSurvivableHealth);
        }

        [Fact]
        public void ClampDamage_PassesThroughWhenItCannotKill()
        {
            Assert.Equal(25, ScenarioSafetyRules.ClampDamage(100, 25));
            Assert.Equal(98, ScenarioSafetyRules.ClampDamage(100, 98));
        }

        [Fact]
        public void ClampDamage_LargeHitBecomesHealthMinusOne()
        {
            Assert.Equal(99, ScenarioSafetyRules.ClampDamage(100, 100));
            Assert.Equal(99, ScenarioSafetyRules.ClampDamage(100, 100000));
            Assert.Equal(19, ScenarioSafetyRules.ClampDamage(20, 100));
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(1, 1)]
        [InlineData(0, 50)]
        [InlineData(100, 0)]
        [InlineData(100, -5)]
        public void ClampDamage_ReturnsZeroWhenNothingCanBeTaken(int health, int damage)
        {
            Assert.Equal(0, ScenarioSafetyRules.ClampDamage(health, damage));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(-10, 0)]
        [InlineData(1, 1)]
        [InlineData(50, 50)]
        [InlineData(100, 100)]
        [InlineData(150, 100)]
        public void ClampHaulGoalPct_KeepsZeroAsNoOverride(int input, int expected)
        {
            Assert.Equal(expected, ScenarioSafetyRules.ClampHaulGoalPct(input));
        }

        [Fact]
        public void ResolveHaulGoal_IsTheGivenShareOfTheMapTotal()
        {
            Assert.Equal(500, ScenarioSafetyRules.ResolveHaulGoal(1000, 50));
            Assert.Equal(850, ScenarioSafetyRules.ResolveHaulGoal(1000, 85));
            Assert.Equal(1000, ScenarioSafetyRules.ResolveHaulGoal(1000, 100));
        }

        [Theory]
        [InlineData(0, 50)]
        [InlineData(-1, 50)]
        [InlineData(1000, 0)]
        [InlineData(1000, -20)]
        public void ResolveHaulGoal_ReturnsNegativeWhenItMustNotOverride(int total, int pct)
        {
            Assert.True(ScenarioSafetyRules.ResolveHaulGoal(total, pct) < 0);
        }

        [Fact]
        public void ResolveHaulGoal_NeverReturnsZeroQuota()
        {
            Assert.Equal(1, ScenarioSafetyRules.ResolveHaulGoal(10, 1));
        }

        [Fact]
        public void ResolveHaulGoal_DoesNotOverflowOnLargeTotals()
        {
            Assert.Equal(1_000_000_000, ScenarioSafetyRules.ResolveHaulGoal(2_000_000_000, 50));
        }
    }
}
