namespace Werewolf.Core
{
    public static class CombatRules
    {
        public static bool IsMatchLive(GamePhase phase)
        {
            return phase == GamePhase.Play || phase == GamePhase.Meeting;
        }

        public static bool OverrideSavingGrace(GamePhase phase, bool savingGrace)
        {
            return IsMatchLive(phase) ? false : savingGrace;
        }

        public static int OverrideMeleePlayerDamage(GamePhase phase, int playerDamage, int enemyDamage)
        {
            return IsMatchLive(phase) ? enemyDamage : playerDamage;
        }

        public static bool OverrideMeleePvpDurabilityHit(GamePhase phase, bool playerHit, bool arenaOrShop)
        {
            if (!IsMatchLive(phase)) return playerHit;
            if (!playerHit || arenaOrShop) return playerHit;
            return false;
        }

        public static bool ShouldDisarmGrabbedOnMeleeHit(GamePhase phase, bool grabbing, bool grabbedIsMelee)
        {
            return IsMatchLive(phase) && grabbing && !grabbedIsMelee;
        }

        public static bool ShouldSpillInventoryOnMeleeHit(GamePhase phase)
        {
            return IsMatchLive(phase);
        }

        public static bool ShouldBlockEquipContestedItem(GamePhase phase, bool grabbedByOtherPlayer)
        {
            return IsMatchLive(phase) && grabbedByOtherPlayer;
        }

        public static int PickSpillSlotIndex(int occupiedCount, float roll)
        {
            if (occupiedCount <= 0) return -1;
            if (roll < 0f) roll = 0f;

            int index = (int)(roll * occupiedCount);
            return index >= occupiedCount ? occupiedCount - 1 : index;
        }
    }
}
