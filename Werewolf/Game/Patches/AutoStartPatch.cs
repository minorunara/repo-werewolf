using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
    internal static class AutoStartPatch
    {
        private static void Postfix()
        {
            try
            {
                WerewolfDirector.Instance?.OnLevelGenerated();
            }
            catch (Exception e)
            {
                WLog.Line("patch_autostart_error", secret: false, ("err", e.Message));
            }
        }
    }
}
