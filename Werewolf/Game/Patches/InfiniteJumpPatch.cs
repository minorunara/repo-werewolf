using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(PlayerController), "Update")]
    internal static class InfiniteJumpPatch
    {
        private static readonly AccessTools.FieldRef<PlayerController, bool> JumpFirst =
            GameRefs.PlayerController_JumpFirst;

        private static readonly AccessTools.FieldRef<PlayerController, int> JumpExtraCurrent =
            GameRefs.PlayerController_JumpExtraCurrent;

        private static int _refillsThisAirTime;
        private static bool _injectedChargeAvailable;

        internal static int RefillsThisAirTime => _refillsThisAirTime;

        internal static bool InjectedChargeAvailable => _injectedChargeAvailable;

        private static void Prefix(PlayerController __instance)
        {
            try
            {
                if (JumpFirst(__instance))
                {
                    _refillsThisAirTime = 0;
                    _injectedChargeAvailable = false;
                    return;
                }

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.RolesClient.JumpActive)
                {
                    _injectedChargeAvailable = false;
                    return;
                }

                if (JumpExtraCurrent(__instance) > 0)
                {
                    _injectedChargeAvailable = _refillsThisAirTime > 0;
                    return;
                }

                int limit = dir.ClientExtraJumpCount;
                if (limit >= 0 && _refillsThisAirTime >= limit)
                {
                    _injectedChargeAvailable = false;
                    return;
                }

                JumpExtraCurrent(__instance) = 1;
                _refillsThisAirTime++;
                _injectedChargeAvailable = true;
            }
            catch (Exception e)
            {
                WLog.Line("patch_jump_error", secret: false, ("err", e.Message));
            }
        }
    }
}
