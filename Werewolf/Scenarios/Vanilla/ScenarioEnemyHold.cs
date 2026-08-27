using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioEnemyHold
    {
        private const float HoldSeconds = 100000f;

        private const float ReleaseStepSeconds = 5f;

        private static bool s_held;

        internal static void Begin()
        {
            if (!Session.IsHost) return;

            int despawned = 0;
            int held = 0;
            foreach (EnemyParent parent in Enemies())
            {
                try
                {
                    if (GameRefs.EnemyParent_Spawned(parent))
                    {
                        parent.Despawn();
                        despawned++;
                    }
                    parent.DespawnedTimerSet(HoldSeconds);
                    held++;
                }
                catch (Exception e)
                {
                    WLog.Line("scenario_enemy_hold_error", secret: false, ("err", e.Message));
                }
            }

            s_held = held > 0;
            WLog.Line("scenario_enemy_hold", secret: false, ("despawned", despawned), ("held", held));
        }

        internal static void Release()
        {
            if (!s_held) return;
            s_held = false;

            int released = 0;
            foreach (EnemyParent parent in Enemies())
            {
                try
                {
                    if (GameRefs.EnemyParent_Spawned(parent) || parent.DespawnedTimer < HoldSeconds / 2f) continue;
                    released++;
                    parent.DespawnedTimerSet(ReleaseStepSeconds * released, _min: true);
                }
                catch (Exception e)
                {
                    WLog.Line("scenario_enemy_hold_error", secret: false, ("err", e.Message));
                }
            }

            WLog.Line("scenario_enemy_release", secret: false, ("released", released));
        }

        private static IEnumerable<EnemyParent> Enemies()
        {
            List<EnemyParent> spawned = EnemyDirector.instance != null ? EnemyDirector.instance.enemiesSpawned : null;
            if (spawned == null) yield break;
            foreach (EnemyParent parent in spawned.ToArray())
            {
                if (parent != null) yield return parent;
            }
        }
    }
}
