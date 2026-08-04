using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(EnemyHealth), "Awake")]
    internal static class OrbDropPatch
    {
        private static bool _errorLogged;

        private static void Postfix(EnemyHealth __instance)
        {
            try
            {
                if (__instance == null) return;
                var config = Plugin.GameConfig;
                if (config == null) return;
                if (config.OrbDropMax == 3) return;
                if (!config.WerewolfModeEnabled) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

                __instance.spawnValuableMax = config.OrbDropMax;
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    WLog.Line("worldgen_orbdrop_error", secret: false, ("err", e.Message));
                }
            }
        }
    }
}
