using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.Scenarios.UI;
using Werewolf.UI;

namespace Werewolf.Scenarios.Game.Patches
{
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerDeath))]
    internal static class ScenarioDeathGuardPatch
    {
        private static bool Prefix(PlayerAvatar __instance)
        {
            try
            {
                if (!ScenarioRun.DeathGuardActive) return true;

                PlayerAvatar local = PlayerAvatar.instance;
                if (__instance == null || local == null || __instance != local) return true;

                WLog.Line("scenario_death_blocked", secret: false,
                    ("scenario", ScenarioRun.Scenario));
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_scenario_death_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
