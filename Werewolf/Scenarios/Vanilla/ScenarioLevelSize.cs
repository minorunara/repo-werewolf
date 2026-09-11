using System;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioLevelSize
    {
        private static int s_extractionRooms;

        private static bool s_capLogged;

        internal static void ApplySize(LevelGenerator generator)
        {
            s_extractionRooms = 0;
            s_capLogged = false;
            if (generator == null || !TryGetSpec(out var spec)) return;

            try
            {
                float multiplier = ScenarioLevelSizeRules.SizeMultiplier(
                    spec.LevelsCompleted, spec.ModuleAmount);
                if (multiplier <= 1f) return;
                if (GameRefs.LevelGenerator_DebugLevelSize == null)
                {
                    ScenarioVanillaProbe.Failed("level_size_ref", "fieldref_null");
                    return;
                }

                GameRefs.LevelGenerator_DebugLevelSize(generator) = multiplier;
                WLog.Line("scenario_level_size", secret: false,
                    ("level", spec.LevelsCompleted + 1),
                    ("vanillaModules", ScenarioLevelSizeRules.VanillaModuleAmount(spec.LevelsCompleted)),
                    ("modules", ScenarioLevelSizeRules.ScaledModuleAmount(spec.LevelsCompleted, multiplier)),
                    ("multiplier", multiplier));
            }
            catch (Exception e)
            {
                WLog.Line("scenario_level_size_error", secret: false, ("err", e.Message));
            }
        }

        internal static bool AllowsExtractionRoom()
        {
            if (!TryGetSpec(out var spec) || spec.ExtractionPoints <= 0) return true;

            int limit = ScenarioLevelSizeRules.ExtractionRooms(spec.ExtractionPoints);
            if (s_extractionRooms < limit)
            {
                s_extractionRooms++;
                return true;
            }

            if (!s_capLogged)
            {
                s_capLogged = true;
                WLog.Line("scenario_level_extraction", secret: false,
                    ("points", spec.ExtractionPoints), ("rooms", limit));
            }
            return false;
        }

        private static bool TryGetSpec(out ScenarioEnvironmentSpec spec)
        {
            spec = default;
            ScenarioDefinition definition = ScenarioRunState.Definition;
            if (definition == null) return false;
            spec = definition.Environment;
            return spec.LevelSizeOverridden;
        }
    }
}
