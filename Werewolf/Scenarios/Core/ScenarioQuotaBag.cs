using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioQuotaBag
    {
        public const int MarginPct = 20;

        public const int MaxBagDollars = 1_000_000;

        public static int ResolveBagDollars(int haulGoal, int extractionPoints, int currentHaul,
                                            int surplus, int reachableBagDollars)
        {
            if (haulGoal <= 0 || extractionPoints <= 0) return 0;

            long pointGoal = haulGoal / extractionPoints;
            long banked = currentHaul > 0 ? currentHaul : 0;
            long reachable = reachableBagDollars > 0 ? reachableBagDollars : 0;
            long covered = banked > reachable ? banked : reachable;
            long shortfall = pointGoal - (surplus > 0 ? surplus : 0) - covered;
            if (shortfall <= 0) return 0;

            long withMargin = shortfall + shortfall * MarginPct / 100L;
            if (withMargin < 1L) withMargin = 1L;
            return withMargin > MaxBagDollars ? MaxBagDollars : (int)withMargin;
        }
    }
}
