using System;
using System.Collections.Generic;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    internal static class DeadIsolation
    {
        internal static bool RoundActive()
        {
            WerewolfDirector dir = WerewolfDirector.Instance;
            return dir != null && dir.IsRoundActiveClient;
        }
    }

    [HarmonyPatch(typeof(PlayerDeathHead), nameof(PlayerDeathHead.SpectatedSet))]
    internal static class DeathHeadSpectatePatch
    {
        private static readonly HashSet<int> _loggedInstanceIds = new HashSet<int>();
        private static bool _wasRoundActive;

        private static bool Prefix(bool _active, PlayerDeathHead __instance)
        {
            try
            {
                if (!_active) return true;
                bool active = DeadIsolation.RoundActive();
                if (!active)
                {
                    if (_wasRoundActive)
                    {
                        _wasRoundActive = false;
                        _loggedInstanceIds.Clear();
                    }
                    return true;
                }
                _wasRoundActive = true;

                int instanceId = __instance != null ? __instance.GetInstanceID() : 0;
                if (_loggedInstanceIds.Add(instanceId))
                {
                    WLog.Line("deathhead_blocked", secret: false,
                        ("active", _active), ("instId", instanceId));
                }
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_deathhead_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.Revive))]
    internal static class RevivePatch
    {
        private static bool Prefix()
        {
            try
            {
                if (!DeadIsolation.RoundActive()) return true;
                WLog.Line("revive_blocked", secret: false, ("via", "Revive"));
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_revive_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "ReviveRPC")]
    internal static class ReviveRpcPatch
    {
        private static bool Prefix()
        {
            try
            {
                if (!DeadIsolation.RoundActive()) return true;
                WLog.Line("revive_blocked", secret: false, ("via", "ReviveRPC"));
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_revive_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
