using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    internal static class DeadTextGuard
    {
        internal static bool ShouldShow(PlayerAvatar player)
        {
            WerewolfDirector dir = WerewolfDirector.Instance;
            if (dir == null || !dir.IsRoundActiveClient) return true;
            int actor = dir.Registry.ResolveActor(player);
            return dir.ShouldShowDeadTextClient(actor);
        }
    }

    [HarmonyPatch(typeof(WorldSpaceUIParent), nameof(WorldSpaceUIParent.TTS))]
    internal static class DeadTtsBubblePatch
    {
        private static bool Prefix(PlayerAvatar _player)
        {
            try
            {
                if (_player == null) return true;
                if (DeadTextGuard.ShouldShow(_player)) return true;

                WLog.Line("dead_tts_bubble_blocked", secret: false);
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_dead_tts_error", secret: false, ("via", "bubble"), ("err", e.Message));
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "ChatMessageSpeak")]
    internal static class DeadTtsSpeakPatch
    {
        private static bool Prefix(PlayerAvatar __instance)
        {
            try
            {
                if (DeadTextGuard.ShouldShow(__instance)) return true;

                WLog.Line("dead_tts_speak_blocked", secret: false);
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_dead_tts_error", secret: false, ("via", "speak"), ("err", e.Message));
                return true;
            }
        }
    }
}
