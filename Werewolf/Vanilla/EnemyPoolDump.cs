using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class EnemyPoolDump
    {
        public static void Write()
        {
            EnemyDirector director = EnemyDirector.instance;
            if (director == null)
            {
                WLog.Line("enemy_pool_skip", secret: false, ("reason", "no_director"));
                return;
            }

            int levelsCompleted = RunManager.instance != null ? RunManager.instance.levelsCompleted : -1;
            int runsPlayed = DataDirector.instance != null
                ? DataDirector.instance.SettingValueFetch(DataDirector.Setting.RunsPlayed)
                : -1;

            WLog.Line("enemy_pool_begin", secret: false,
                ("levelsCompleted", levelsCompleted), ("runsPlayed", runsPlayed),
                ("t1", CountOf(director.enemiesDifficulty1)),
                ("t2", CountOf(director.enemiesDifficulty2)),
                ("t3", CountOf(director.enemiesDifficulty3)));

            WriteTier(1, director.enemiesDifficulty1, levelsCompleted, runsPlayed);
            WriteTier(2, director.enemiesDifficulty2, levelsCompleted, runsPlayed);
            WriteTier(3, director.enemiesDifficulty3, levelsCompleted, runsPlayed);
            WriteLive(director);

            WLog.Line("enemy_pool_end", secret: false);
        }

        private static void WriteLive(EnemyDirector director)
        {
            List<EnemyParent> spawned = director.enemiesSpawned;
            if (spawned == null)
            {
                WLog.Line("enemy_live", secret: false, ("count", 0));
                return;
            }

            int t1 = 0, t2 = 0, t3 = 0, alive = 0;
            var names = new List<string>();
            foreach (EnemyParent parent in spawned)
            {
                if (parent == null) continue;
                switch (parent.difficulty)
                {
                    case EnemyParent.Difficulty.Difficulty1: t1++; break;
                    case EnemyParent.Difficulty.Difficulty2: t2++; break;
                    case EnemyParent.Difficulty.Difficulty3: t3++; break;
                }
                names.Add(string.IsNullOrEmpty(parent.enemyName) ? "?" : parent.enemyName);
                try
                {
                    if (GameRefs.EnemyParent_Spawned != null && GameRefs.EnemyParent_Spawned(parent)) alive++;
                }
                catch (Exception e)
                {
                    WLog.Line("enemy_pool_error", secret: false, ("err", e.Message));
                }
            }

            WLog.Line("enemy_live", secret: false,
                ("count", names.Count), ("alive", alive),
                ("t1", t1), ("t2", t2), ("t3", t3), ("names", names));
        }

        private static void WriteTier(int tier, List<EnemySetup> pool, int levelsCompleted, int runsPlayed)
        {
            if (pool == null) return;

            foreach (EnemySetup setup in pool)
            {
                if (setup == null)
                {
                    WLog.Line("enemy_pool", secret: false, ("tier", tier), ("set", null));
                    continue;
                }

                var kinds = new List<string>();
                int bodies = 0;
                int objs = 0;
                if (setup.spawnObjects != null)
                {
                    objs = setup.spawnObjects.Count;
                    foreach (PrefabRef spawnObject in setup.spawnObjects)
                    {
                        if (spawnObject == null) continue;
                        EnemyParent parent = ResolveEnemyParent(setup, spawnObject);
                        if (parent != null) bodies++;
                        kinds.Add(Kind(spawnObject, parent));
                    }
                }

                WLog.Line("enemy_pool", secret: false,
                    ("tier", tier), ("set", setup.name),
                    ("bodies", bodies), ("objs", objs), ("kinds", kinds),
                    ("lvcond", setup.levelsCompletedCondition),
                    ("lvmin", setup.levelsCompletedMin), ("lvmax", setup.levelsCompletedMax),
                    ("runs", setup.runsPlayed),
                    ("rarity", setup.rarityPreset != null ? setup.rarityPreset.chance : 100f),
                    ("eligible", Eligible(setup, levelsCompleted, runsPlayed)));
            }
        }

        private static EnemyParent ResolveEnemyParent(EnemySetup setup, PrefabRef spawnObject)
        {
            try
            {
                GameObject prefab = spawnObject.Prefab;
                return prefab != null ? prefab.GetComponent<EnemyParent>() : null;
            }
            catch (Exception e)
            {
                WLog.Line("enemy_pool_error", secret: false,
                    ("set", setup.name), ("path", spawnObject.ResourcePath), ("err", e.Message));
                return null;
            }
        }

        private static string Kind(PrefabRef spawnObject, EnemyParent parent)
        {
            string name = spawnObject.PrefabName;
            if (string.IsNullOrEmpty(name)) name = spawnObject.ResourcePath;
            if (string.IsNullOrEmpty(name)) name = "?";
            return name + ":" + Digit(parent);
        }

        private static string Digit(EnemyParent parent)
        {
            if (parent == null) return "?";
            switch (parent.difficulty)
            {
                case EnemyParent.Difficulty.Difficulty1: return "1";
                case EnemyParent.Difficulty.Difficulty2: return "2";
                case EnemyParent.Difficulty.Difficulty3: return "3";
                default: return "?";
            }
        }

        private static object Eligible(EnemySetup setup, int levelsCompleted, int runsPlayed)
        {
            if (levelsCompleted < 0 || runsPlayed < 0) return null;
            return EnemySetOverride.IsEligible(setup, levelsCompleted, runsPlayed);
        }

        private static int CountOf(List<EnemySetup> pool) => pool != null ? pool.Count : 0;
    }
}
