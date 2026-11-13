#nullable enable
using System;
using System.Collections.Generic;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    internal static class EnemyShadowTargetFilter
    {
        internal static bool IsIgnored(PlayerAvatar? player)
        {
            if (player == null || EnemyDirector.instance == null) return false;
            string steamId = SemiFunc.PlayerGetSteamID(player);
            if (string.IsNullOrEmpty(steamId)) return false;
            return EnemyDirector.instance.debugNoVision.Contains(steamId);
        }

        internal static PlayerAvatar? PickEligible()
        {
            var candidates = new List<PlayerAvatar>();
            foreach (PlayerAvatar p in SemiFunc.PlayerGetList())
            {
                if (p != null && !p.isDisabled && !IsIgnored(p)) candidates.Add(p);
            }
            if (candidates.Count == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
    }

    [HarmonyPatch(typeof(EnemyShadow), nameof(EnemyShadow.UpdatePlayerTarget))]
    internal static class EnemyShadowUpdatePlayerTargetPatch
    {
        private static void Prefix(ref PlayerAvatar? _player)
        {
            try
            {
                if (_player == null || !EnemyShadowTargetFilter.IsIgnored(_player)) return;
                _player = EnemyShadowTargetFilter.PickEligible();
            }
            catch (Exception e)
            {
                WLog.Line("patch_loomtarget_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(EnemyShadow), nameof(EnemyShadow.GetAnnoyed))]
    internal static class EnemyShadowGetAnnoyedPatch
    {
        private static bool Prefix(PlayerAvatar _nearbyPlayer)
        {
            try
            {
                if (_nearbyPlayer != null && EnemyShadowTargetFilter.IsIgnored(_nearbyPlayer)) return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_loomannoy_error", secret: false, ("err", e.Message));
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(EnemyShadow), nameof(EnemyShadow.StateFollow))]
    internal static class EnemyShadowStateFollowPatch
    {
        private static void Prefix(EnemyShadow __instance)
        {
            try
            {
                PlayerAvatar? target = __instance.playerTarget;
                if (target == null || !EnemyShadowTargetFilter.IsIgnored(target)) return;
                __instance.UpdatePlayerTarget(target);
                WLog.Line("loom_follow_cut", secret: true);
            }
            catch (Exception e)
            {
                WLog.Line("patch_loomfollow_error", secret: false, ("err", e.Message));
            }
        }
    }
}
