using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(TruckScreenText), nameof(TruckScreenText.ChatMessageLevel))]
    internal static class MailSendWinPatch
    {
        private static readonly AccessTools.FieldRef<TruckScreenText, TruckScreenText.PlayerChatBoxState> StateRef =
            GameRefs.TruckScreenText_playerChatBoxState;

        private static void Postfix(TruckScreenText __instance)
        {
            try
            {
                if (__instance == null) return;
                TruckScreenText.PlayerChatBoxState state = StateRef(__instance);
                if (state != TruckScreenText.PlayerChatBoxState.LockedStartingTruck &&
                    state != TruckScreenText.PlayerChatBoxState.LockedDestroySlackers) return;

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
                if (dir.HostPhase != GamePhase.Play) return;
                if (!SemiFunc.RunIsLevel()) return;

                if (state == TruckScreenText.PlayerChatBoxState.LockedStartingTruck)
                {
                    dir.HostNotifyMailDeparture();
                    WLog.Line("mail_send_win", secret: false,
                        ("via", "chat_message_level"), ("state", state));
                }
                else
                {
                    WLog.Line("mail_send_slackers", secret: false,
                        ("via", "chat_message_level"));
                }
            }
            catch (Exception e)
            {
                WLog.Line("patch_mail_send_error", secret: false, ("err", e.Message));
            }
        }
    }
}
