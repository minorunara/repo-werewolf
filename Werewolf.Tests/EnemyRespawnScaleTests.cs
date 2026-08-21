using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class EnemyRespawnScaleTests
    {
        [Theory]
        [InlineData(100, 0.016f, 0f)]
        [InlineData(0, 0.016f, 0.016f)]
        [InlineData(50, 0.016f, 0.008f)]
        [InlineData(25, 1f, 0.75f)]
        public void CompensationSeconds_ScalesLinearly(int percent, float dt, float expected)
        {
            Assert.Equal(expected, EnemyRespawnScale.CompensationSeconds(percent, dt), precision: 6);
        }

        [Theory]
        [InlineData(-1, 0.016f)]
        [InlineData(-100, 1f)]
        public void CompensationSeconds_ClampsBelowZero_ToFullFreeze(int percent, float dt)
        {
            Assert.Equal(dt, EnemyRespawnScale.CompensationSeconds(percent, dt), precision: 6);
        }

        [Theory]
        [InlineData(101, 0.016f)]
        [InlineData(500, 1f)]
        public void CompensationSeconds_ClampsAboveHundred_ToNoCompensation(int percent, float dt)
        {
            Assert.Equal(0f, EnemyRespawnScale.CompensationSeconds(percent, dt), precision: 6);
        }

        [Theory]
        [InlineData(0, 0f)]
        [InlineData(0, -0.016f)]
        [InlineData(50, -1f)]
        public void CompensationSeconds_NonPositiveDelta_ReturnsZero(int percent, float dt)
        {
            Assert.Equal(0f, EnemyRespawnScale.CompensationSeconds(percent, dt));
        }
    }
}
