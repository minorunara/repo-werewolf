#nullable enable
using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(MoonUI), nameof(MoonUI.Check))]
    internal static class MoonUiSkipPatch
    {
        private static bool Prefix()
        {
            try
            {
                WerewolfDirector? director = WerewolfDirector.Instance;
                if (director == null || !director.IsWerewolfRoundExpected()) return true;

                RunManager rm = RunManager.instance;
                if (rm == null || !SemiFunc.RunIsLevel()) return true;
                if (!rm.moonLevelChanged) return true;

                rm.moonLevelChanged = false;
                WLog.Line("moon_ui_skipped", secret: false);
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_moonui_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
