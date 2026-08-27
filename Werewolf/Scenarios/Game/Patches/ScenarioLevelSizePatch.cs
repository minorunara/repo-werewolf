using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Scenarios.Vanilla;

namespace Werewolf.Scenarios.Game.Patches
{
    [HarmonyPatch(typeof(LevelGenerator), "Start")]
    internal static class ScenarioLevelSizePatch
    {
        private static void Postfix(LevelGenerator __instance)
        {
            try
            {
                ScenarioLevelSize.ApplySize(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_scenario_levelsize_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(LevelGenerator), "SetExtractionTile")]
    internal static class ScenarioExtractionRoomsPatch
    {
        private static bool Prefix(Module.Type _type)
        {
            try
            {
                if (_type != Module.Type.Extraction) return true;
                return ScenarioLevelSize.AllowsExtractionRoom();
            }
            catch (Exception e)
            {
                WLog.Line("patch_scenario_extraction_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
