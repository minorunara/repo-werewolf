#nullable enable
using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(ValuableDirector), nameof(ValuableDirector.SoftLockExtractionCheck))]
    internal static class ValuableSoftLockGuardPatch
    {
        private static bool _blockLogged;

        private static bool Prefix(ref bool __result)
        {
            try
            {
                WerewolfDirector? dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive)
                {
                    _blockLogged = false;
                    return true;
                }
                if (!_blockLogged)
                {
                    _blockLogged = true;
                    WLog.Line("softlock_refill_blocked", secret: false);
                }
                __result = false;
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_softlock_guard_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
