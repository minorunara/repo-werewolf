using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(PlayerAvatar), "PlayerDeathRPC")]
    internal static class PlayerDeathPatch
    {
        private static void Postfix(PlayerAvatar __instance)
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

                if (!dir.Registry.IsDeadSet(__instance)) return;

                dir.HostRecordDeath(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_death_error", secret: false, ("err", e.Message));
            }
        }
    }
}
