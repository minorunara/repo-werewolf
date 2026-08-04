using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WorldgenStartHealthTests
    {
        [Fact]
        public void Compute_FreshStart_OneStage_YieldsFullUpgradedHealth()
        {
            Assert.Equal(120, WorldgenStartHealth.Compute(100, 1, 1));
        }

        [Fact]
        public void Compute_FreshStart_MultipleStages_ScalesByTwentyPerStage()
        {
            Assert.Equal(160, WorldgenStartHealth.Compute(100, 3, 3));
        }

        [Fact]
        public void Compute_CarriedDamage_PreservesDamageWhileAddingUpgrade()
        {
            Assert.Equal(80, WorldgenStartHealth.Compute(60, 1, 1));
        }

        [Fact]
        public void Compute_ClampsToUpgradedMax()
        {
            Assert.Equal(120, WorldgenStartHealth.Compute(115, 1, 1));
        }

        [Fact]
        public void Compute_NegativeDelta_ReducesAndClampsToMax()
        {
            Assert.Equal(120, WorldgenStartHealth.Compute(140, -1, 1));
        }

        [Fact]
        public void Compute_NegativeDelta_NeverGoesBelowOne()
        {
            Assert.Equal(1, WorldgenStartHealth.Compute(10, -1, 1));
        }
    }
}
