using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioSafetyRules
    {
        public const int MinSurvivableHealth = 1;

        public static int ClampDamage(int health, int damage)
        {
            if (damage <= 0) return 0;
            if (health <= MinSurvivableHealth) return 0;
            int room = health - MinSurvivableHealth;
            return damage < room ? damage : room;
        }

        public const int MinHaulGoalPct = 1;
        public const int MaxHaulGoalPct = 100;

        public static int ClampHaulGoalPct(int pct)
        {
            if (pct <= 0) return 0;
            if (pct < MinHaulGoalPct) return MinHaulGoalPct;
            return pct > MaxHaulGoalPct ? MaxHaulGoalPct : pct;
        }

        public static int ResolveHaulGoal(int haulGoalMax, int pctOfTotal)
        {
            if (pctOfTotal <= 0 || haulGoalMax <= 0) return -1;
            long goal = (long)haulGoalMax * ClampHaulGoalPct(pctOfTotal) / 100L;
            return goal < 1L ? 1 : (int)goal;
        }
    }
}
