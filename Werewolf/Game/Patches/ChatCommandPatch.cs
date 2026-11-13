#nullable enable
using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Debugging;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.MessageSend))]
    internal static class ChatCommandPatch
    {
        private static bool Prefix(ChatManager __instance)
        {
            try
            {
                string message = __instance.chatMessage;
                if (!CommandGate.TryParse(message, out _, out _)) return true;

                CheatCommands.Execute(message);
                __instance.chatMessage = "";
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_chat_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
