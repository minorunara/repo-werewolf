using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class BombDamageRulesTests
    {
        [Theory]
        [InlineData(79, 100, 40)]
        [InlineData(80, 100, 40)]
        [InlineData(81, 100, 41)]
        public void TargetDamage_IsHalfRoundedUp(int playerDamage, int health, int expected)
        {
            Assert.Equal(expected, BombDamageRules.TargetDamage(playerDamage, health));
        }

        [Theory]
        [InlineData(80, 30, 29)]
        [InlineData(80, 2, 1)]
        [InlineData(80, 1, 0)]
        [InlineData(80, 0, 0)]
        public void TargetDamage_AlwaysLeavesAtLeastOneHp(
            int playerDamage, int health, int expected)
        {
            Assert.Equal(expected, BombDamageRules.TargetDamage(playerDamage, health));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TargetDamage_NonPositiveBlastDamageDoesNothing(int playerDamage)
        {
            Assert.Equal(0, BombDamageRules.TargetDamage(playerDamage, 100));
        }
    }
}
