using System;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Scenarios.Core;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioVanillaCheck
    {
        internal static bool VerifyForStart(RunManager run, ScenarioEnvironmentSpec spec)
        {
            bool levelOk = LevelExists(run, spec.ForcedLevelName);
            if (!levelOk)
            {
                Missing(ScenarioVanillaNameKind.Level, spec.ForcedLevelName);
            }

            foreach (ScenarioVanillaName entry in ScenarioVanillaNames.All)
            {
                if (entry.Kind == ScenarioVanillaNameKind.Resource) VerifyResource(entry);
            }

            return levelOk;
        }

        private static bool LevelExists(RunManager run, string levelName)
        {
            if (run == null || run.levels == null || string.IsNullOrEmpty(levelName)) return false;
            foreach (Level level in run.levels)
            {
                if (level != null && string.Equals(level.name, levelName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static void VerifyResource(ScenarioVanillaName entry)
        {
            GameObject prefab;
            try
            {
                prefab = Resources.Load<GameObject>(entry.Name);
            }
            catch (Exception e)
            {
                WLog.Line("scenario_vanilla_name_error", secret: false,
                    ("kind", entry.Kind), ("name", entry.Name), ("err", e.Message));
                return;
            }

            if (prefab == null)
            {
                Missing(ScenarioVanillaNameKind.Resource, entry.Name);
                return;
            }

            foreach (ScenarioVanillaName child in ScenarioVanillaNames.All)
            {
                if (child.Kind != ScenarioVanillaNameKind.ChildPath) continue;
                if (!string.Equals(child.Parent, entry.Name, StringComparison.Ordinal)) continue;
                if (prefab.transform.Find(child.Name) == null)
                {
                    Missing(ScenarioVanillaNameKind.ChildPath, entry.Name + "/" + child.Name);
                }
            }
        }

        private static void Missing(ScenarioVanillaNameKind kind, string name)
        {
            WLog.Line("scenario_vanilla_name_missing", secret: false,
                ("kind", kind), ("name", name ?? "(null)"));
        }
    }
}
