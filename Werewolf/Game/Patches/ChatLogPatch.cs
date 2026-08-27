using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Vanilla;

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
                if (dir == null || !dir.IsChatLogWindowOpenClient) return;

                int actor = Players.ActorOf(__instance);
                dir.RecordReplayChatClient(actor, _message);

                if (!DeadTextGuard.ShouldShow(__instance)) return;

                dir.RecordMeetingChatClient(actor, _message);
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_patch_error", secret: false, ("err", e.Message));
            }
        }
    }
}
