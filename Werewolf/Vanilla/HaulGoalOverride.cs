using System;
using Werewolf.Core;
using Werewolf.Scenarios.Core;

namespace Werewolf.Vanilla
{
    internal static class HaulGoalOverride
    {
        internal static int PctOfTotal { get; private set; }

        internal static void Arm(int pctOfTotal)
        {
            PctOfTotal = ScenarioSafetyRules.ClampHaulGoalPct(pctOfTotal);
            WLog.Line("haul_goal_override_armed", secret: false, ("pct", PctOfTotal));
        }

        internal static void Clear() => PctOfTotal = 0;

        internal static bool TryApply(RoundDirector director)
        {
            if (director == null || PctOfTotal <= 0) return false;

            try
            {
                if (GameRefs.RoundDirector_haulGoal == null
                    || GameRefs.RoundDirector_haulGoalMax == null) return false;

                int total = GameRefs.RoundDirector_haulGoalMax(director);
                int goal = ScenarioSafetyRules.ResolveHaulGoal(total, PctOfTotal);
                if (goal < 0) return false;

                int before = GameRefs.RoundDirector_haulGoal(director);
                GameRefs.RoundDirector_haulGoal(director) = goal;
                WLog.Line("haul_goal_overridden", secret: false,
                    ("pct", PctOfTotal), ("total", total), ("from", before), ("to", goal));
                return true;
            }
            catch (Exception e)
            {
                WLog.Line("haul_goal_override_error", secret: false, ("err", e.Message));
                return false;
            }
        }
    }
}
