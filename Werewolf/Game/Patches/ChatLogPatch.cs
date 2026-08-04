using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.ChatMessageSendRPC))]
    internal static class ChatLogPatch
    {
        private static void Postfix(PlayerAvatar __instance, string _message)
        {
            try
            {
                if (__instance == null || string.IsNullOrEmpty(_message)) return;

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsRoundActiveClient) return;
                if (dir.ClientPhase != GamePhase.Meeting) return;

                if (!DeadTextGuard.ShouldShow(__instance)) return;

                dir.RecordMeetingChatClient(__instance, _message);
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_patch_error", secret: false, ("err", e.Message));
            }
        }
    }
}
