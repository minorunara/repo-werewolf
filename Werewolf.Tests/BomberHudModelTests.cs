using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class BomberHudModelTests
    {
        private static readonly IReadOnlyDictionary<int, float> Ratios =
            new Dictionary<int, float> { [2] = 0.25f, [3] = 0.75f, [4] = 1f };

        [Fact]
        public void PlantFraction_ReturnsHighestEligibleRatio()
        {
            Assert.Equal(0.75f,
                BomberHudModel.PlantFraction(true, 0, true, Ratios, excludedTarget: 4));
        }

        [Fact]
        public void PlantFraction_IncludesAllActorsWhenNoBombExists()
        {
            Assert.Equal(1f,
                BomberHudModel.PlantFraction(true, 0, true, Ratios, excludedTarget: -1));
        }

        [Theory]
        [InlineData(false, 0, true)]
        [InlineData(true, 1, true)]
        [InlineData(true, 0, false)]
        public void PlantFraction_ClosedGateReturnsZero(bool phaseAllows, int cooldownSec,
            bool hasResource)
        {
            Assert.Equal(0f, BomberHudModel.PlantFraction(
                phaseAllows, cooldownSec, hasResource, Ratios, excludedTarget: -1));
        }

        [Fact]
        public void PlantFraction_NullOrOnlyExcludedCandidateReturnsZero()
        {
            Assert.Equal(0f, BomberHudModel.PlantFraction(true, 0, true, null, -1));
            Assert.Equal(0f, BomberHudModel.PlantFraction(true, 0, true,
                new Dictionary<int, float> { [7] = 1f }, excludedTarget: 7));
        }

        [Fact]
        public void PlantFraction_ClampsMalformedRatios()
        {
            Assert.Equal(1f, BomberHudModel.PlantFraction(true, 0, true,
                new Dictionary<int, float> { [2] = -1f, [3] = 2f }, excludedTarget: -1));
        }
    }
}
