using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioQuotaBagTests
    {
        [Fact]
        public void ResolveBagDollars_CoversOnePointsShareWithAMargin()
        {
            Assert.Equal(6000, ScenarioQuotaBag.ResolveBagDollars(10000, 2, 0, 0, 0));
        }

        [Fact]
        public void ResolveBagDollars_SubtractsWhatIsAlreadyInTheZoneAndTheSurplus()
        {
            Assert.Equal(2400, ScenarioQuotaBag.ResolveBagDollars(10000, 2, 2000, 1000, 0));
        }

        [Theory]
        [InlineData(0, 2, 0, 0)]
        [InlineData(-5, 2, 0, 0)]
        [InlineData(10000, 0, 0, 0)]
        [InlineData(10000, 2, 5000, 0)]
        [InlineData(10000, 2, 0, 5000)]
        [InlineData(10000, 2, 9000, 9000)]
        public void ResolveBagDollars_IsZeroWhenNothingIsNeeded(int goal, int points, int haul, int surplus)
        {
            Assert.Equal(0, ScenarioQuotaBag.ResolveBagDollars(goal, points, haul, surplus, 0));
        }

        [Fact]
        public void ResolveBagDollars_NeverGoesBelowOneDollarWhenSomethingIsLeft()
        {
            Assert.Equal(1, ScenarioQuotaBag.ResolveBagDollars(2, 2, 0, 0, 0));
        }

        [Fact]
        public void ResolveBagDollars_TreatsNegativeHaulAndSurplusAsZero()
        {
            Assert.Equal(6000, ScenarioQuotaBag.ResolveBagDollars(10000, 2, -100, -100, 0));
        }

        [Fact]
        public void ResolveBagDollars_IsCappedAndDoesNotOverflow()
        {
            Assert.Equal(ScenarioQuotaBag.MaxBagDollars,
                ScenarioQuotaBag.ResolveBagDollars(int.MaxValue, 1, 0, 0, 0));
        }

        [Fact]
        public void ResolveBagDollars_IsZeroWhileAHealthyBagIsBeingCarried()
        {
            Assert.Equal(0, ScenarioQuotaBag.ResolveBagDollars(10000, 2, 0, 0, 6000));
        }

        [Fact]
        public void ResolveBagDollars_CoversTheGapWhenACarriedBagHasBeenSmashed()
        {
            Assert.Equal(3600, ScenarioQuotaBag.ResolveBagDollars(10000, 2, 0, 0, 2000));
        }

        [Fact]
        public void ResolveBagDollars_CoversTheGapWhenASmashedBagIsAlreadyInTheZone()
        {
            Assert.Equal(3600, ScenarioQuotaBag.ResolveBagDollars(10000, 2, 2000, 0, 2000));
        }

        [Fact]
        public void ResolveBagDollars_IsZeroOnceTheQuotaIsMet()
        {
            Assert.Equal(0, ScenarioQuotaBag.ResolveBagDollars(10000, 2, 5000, 0, 0));
            Assert.Equal(0, ScenarioQuotaBag.ResolveBagDollars(10000, 2, 0, 5000, 0));
        }

        [Fact]
        public void ResolveBagDollars_TreatsNegativeReachableAsZero()
        {
            Assert.Equal(6000, ScenarioQuotaBag.ResolveBagDollars(10000, 2, 0, 0, -100));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1000)]
        [InlineData(4999)]
        public void ResolveBagDollars_NeverFallsShortOfWhatIsStillMissing(int reachable)
        {
            int topUp = ScenarioQuotaBag.ResolveBagDollars(10000, 2, 0, 0, reachable);
            Assert.True(reachable + topUp >= 5000, $"reachable={reachable} topUp={topUp}");
        }
    }
}
