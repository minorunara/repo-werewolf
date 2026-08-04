using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(DataDirector), nameof(DataDirector.SaveDeleteCheck))]
    internal static class SaveGuardPatch
    {
        private static void Postfix(bool _leaveGame)
        {
            try
            {
                if (!_leaveGame) return;

                bool active = WorldgenApplier.CustomEnvironmentActive;
                string marked = WorldgenApplier.MarkedSaveFileName ?? "";
                string current = WorldgenApplier.GetCurrentSaveFileName();

                if (WorldgenSaveGuard.ShouldDelete(_leaveGame, active, marked, current))
                {
                    SemiFunc.SaveFileDelete(marked);
                    WLog.Line("worldgen_saveguard_deleted", secret: false,
                        ("name", marked));
                }

                WorldgenApplier.ClearSessionMarker();
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_saveguard_error", secret: false, ("err", e.Message));
            }
        }
    }
}
