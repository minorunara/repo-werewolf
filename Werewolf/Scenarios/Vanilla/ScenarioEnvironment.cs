using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioEnvironment
    {
        internal static void Apply(RunManager run, ScenarioEnvironmentSpec spec)
        {
            run.levelsCompleted = spec.LevelsCompleted;
            run.UpdateMoonLevel();
            SemiFunc.StatSetRunLevel(spec.LevelsCompleted);
            WorldgenApplier.ApplyForcedLevel(run, spec.ForcedLevelName, spec.ForcedLevelName);
            HaulGoalOverride.Arm(spec.HaulGoalPctOfTotal);
            EnemySetOverride.Arm(spec.EnemySets);
            WorldgenApplier.ApplyScenarioUpgrades(spec.StartUpgrades);
            WLog.Line("scenario_worldgen_applied", secret: false,
                ("map", spec.ForcedLevelName), ("level", spec.LevelsCompleted + 1),
                ("haulGoalPct", spec.HaulGoalPctOfTotal),
                ("enemySets", spec.EnemySets.ToString()),
                ("upgrades", spec.StartUpgrades.Count));
        }
    }
}
