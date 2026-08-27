using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class EnemySetOverride
    {
        internal static EnemySetCounts Counts { get; private set; } = EnemySetCounts.Untouched;

        private static AnimationCurve[] s_saved;

        internal static void Arm(EnemySetCounts counts)
        {
            Counts = EnemySetRules.Clamp(counts);
            WLog.Line("enemy_sets_armed", secret: false, ("sets", Counts.ToString()));
        }

        internal static void Clear() => Counts = EnemySetCounts.Untouched;

        internal static void Begin(EnemyDirector director)
        {
            End(director);
            if (director == null || !Counts.AnyOverridden) return;

            int levelsCompleted = RunManager.instance != null ? RunManager.instance.levelsCompleted : 0;
            int runsPlayed = DataDirector.instance != null
                ? DataDirector.instance.SettingValueFetch(DataDirector.Setting.RunsPlayed)
                : 0;

            int? tier1 = EnemySetRules.ResolveTier(Counts.Danger1, PoolHasEligible(director.enemiesDifficulty1, levelsCompleted, runsPlayed));
            int? tier2 = EnemySetRules.ResolveTier(Counts.Danger2, PoolHasEligible(director.enemiesDifficulty2, levelsCompleted, runsPlayed));
            int? tier3 = EnemySetRules.ResolveTier(Counts.Danger3, PoolHasEligible(director.enemiesDifficulty3, levelsCompleted, runsPlayed));

            s_saved = new[]
            {
                director.amountCurve1_1, director.amountCurve1_2,
                director.amountCurve2_1, director.amountCurve2_2,
                director.amountCurve3_1, director.amountCurve3_2,
            };
            if (tier1 != null) director.amountCurve1_1 = director.amountCurve1_2 = Constant(tier1.Value);
            if (tier2 != null) director.amountCurve2_1 = director.amountCurve2_2 = Constant(tier2.Value);
            if (tier3 != null) director.amountCurve3_1 = director.amountCurve3_2 = Constant(tier3.Value);

            WLog.Line("enemy_sets_applied", secret: false,
                ("level", levelsCompleted + 1), ("runsPlayed", runsPlayed),
                ("t1", tier1), ("t2", tier2), ("t3", tier3));
        }

        internal static void End(EnemyDirector director)
        {
            if (s_saved == null || director == null) return;
            director.amountCurve1_1 = s_saved[0];
            director.amountCurve1_2 = s_saved[1];
            director.amountCurve2_1 = s_saved[2];
            director.amountCurve2_2 = s_saved[3];
            director.amountCurve3_1 = s_saved[4];
            director.amountCurve3_2 = s_saved[5];
            s_saved = null;
        }

        internal static bool IsEligible(EnemySetup setup, int levelsCompleted, int runsPlayed)
            => EnemySetRules.IsEligible(levelsCompleted, runsPlayed,
                setup.levelsCompletedCondition, setup.levelsCompletedMin, setup.levelsCompletedMax,
                setup.runsPlayed);

        private static AnimationCurve Constant(int sets) => AnimationCurve.Constant(0f, 1f, sets);

        private static bool PoolHasEligible(List<EnemySetup> pool, int levelsCompleted, int runsPlayed)
        {
            if (pool == null) return false;
            foreach (EnemySetup setup in pool)
            {
                if (setup != null && IsEligible(setup, levelsCompleted, runsPlayed)) return true;
            }
            return false;
        }
    }
}
