using System;

namespace Werewolf.Core
{
    public static class BombDamageRules
    {
        public static int TargetDamage(int playerDamage, int currentHealth)
        {
            if (playerDamage <= 0 || currentHealth <= 1) return 0;

            int halfRoundedUp = playerDamage / 2 + playerDamage % 2;
            return Math.Min(halfRoundedUp, currentHealth - 1);
        }
    }
}
