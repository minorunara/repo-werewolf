using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class EnemySetRulesTests
    {

        [Theory]
        [InlineData(-1, EnemySetRules.Vanilla)]
        [InlineData(-5, EnemySetRules.Vanilla)]
        [InlineData(0, 0)]
        [InlineData(7, 7)]
        [InlineData(20, 20)]
        [InlineData(21, 20)]
        [InlineData(999, 20)]
        public void Clamp_KeepsVanillaForNegatives_AndCapsAtMax(int sets, int expected)
        {
            Assert.Equal(expected, EnemySetRules.Clamp(sets));
        }

        [Fact]
        public void Clamp_TreatsEachTierIndependently()
        {
            var clamped = EnemySetRules.Clamp(new EnemySetCounts(-9, 0, 50));

            Assert.Equal(EnemySetRules.Vanilla, clamped.Danger1);
            Assert.Equal(0, clamped.Danger2);
            Assert.Equal(EnemySetRules.MaxSets, clamped.Danger3);
        }

        [Fact]
        public void Untouched_OverridesNothing()
        {
            Assert.False(EnemySetCounts.Untouched.AnyOverridden);
            Assert.True(new EnemySetCounts(-1, -1, 0).AnyOverridden);
        }

        [Fact]
        public void IsEligible_IgnoresLevelBounds_WhenTheSetHasNoLevelCondition()
        {
            Assert.True(EnemySetRules.IsEligible(levelsCompleted: 0, runsPlayed: 0,
                levelsCompletedCondition: false, levelsCompletedMin: 5, levelsCompletedMax: 9, runsPlayedRequired: 0));
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(4, true)]
        [InlineData(5, false)]
        public void IsEligible_AppliesLevelBounds_WhenTheSetHasALevelCondition(int levelsCompleted, bool expected)
        {
            Assert.Equal(expected, EnemySetRules.IsEligible(levelsCompleted, runsPlayed: 99,
                levelsCompletedCondition: true, levelsCompletedMin: 2, levelsCompletedMax: 4, runsPlayedRequired: 0));
        }

        [Fact]
        public void IsEligible_TreatsZeroMaxAsUnbounded()
        {
            Assert.True(EnemySetRules.IsEligible(levelsCompleted: 500, runsPlayed: 0,
                levelsCompletedCondition: true, levelsCompletedMin: 2, levelsCompletedMax: 0, runsPlayedRequired: 0));
        }

        [Theory]
        [InlineData(4, false)]
        [InlineData(5, true)]
        public void IsEligible_RequiresTheSavesRunCount(int runsPlayed, bool expected)
        {
            Assert.Equal(expected, EnemySetRules.IsEligible(levelsCompleted: 1, runsPlayed,
                levelsCompletedCondition: true, levelsCompletedMin: 0, levelsCompletedMax: 2, runsPlayedRequired: 5));
        }

        [Fact]
        public void ResolveTier_LeavesTheCurveAlone_WhenVanilla()
        {
            Assert.Null(EnemySetRules.ResolveTier(EnemySetRules.Vanilla, poolHasEligible: true));
            Assert.Null(EnemySetRules.ResolveTier(-3, poolHasEligible: false));
        }

        [Fact]
        public void ResolveTier_FallsToZero_WhenNoSetIsEligible()
        {
            Assert.Equal(0, EnemySetRules.ResolveTier(8, poolHasEligible: false));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(8, 8)]
        [InlineData(40, 20)]
        public void ResolveTier_UsesTheClampedRequest_WhenTheDrawCanSucceed(int requested, int expected)
        {
            Assert.Equal(expected, EnemySetRules.ResolveTier(requested, poolHasEligible: true));
        }
    }
}
