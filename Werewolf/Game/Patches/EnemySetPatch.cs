using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(EnemyDirector), "AmountSetup")]
    internal static class EnemySetPatch
    {
        private static void Prefix(EnemyDirector __instance)
        {
            try
            {
                EnemySetOverride.Begin(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_enemysets_error", secret: false, ("err", e.Message));
            }
        }

        private static void Postfix(EnemyDirector __instance)
        {
            try
            {
                EnemySetOverride.End(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_enemysets_error", secret: false, ("err", e.Message));
            }
        }
    }
}
