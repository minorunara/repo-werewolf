using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(RoundDirector), "StartRoundLogic")]
    internal static class HaulGoalPatch
    {
        private static void Postfix(RoundDirector __instance)
        {
            try
            {
                HaulGoalOverride.TryApply(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_haulgoal_error", secret: false, ("err", e.Message));
            }
        }
    }
}
