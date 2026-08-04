using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ChangeLevel))]
    internal static class WorldgenLevelPatch
    {
        private static void Prefix(RunManager.ChangeLevelType _changeLevelType)
        {
            try
            {
                WorldgenApplier.TryApplyOnDeparture(_changeLevelType);
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_prefix_error", secret: false, ("err", e.Message));
            }
        }

        private static void Postfix()
        {
            try
            {
                if (WorldgenApplier.DebugLevelArmed)
                {
                    WorldgenApplier.ClearDebugLevel();
                }
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_postfix_error", secret: false, ("err", e.Message));
            }
        }
    }
}
