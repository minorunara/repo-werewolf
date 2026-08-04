using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Debugging;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(ChatManager), "MessageSend")]
    internal static class ChatCommandPatch
    {
        private static readonly AccessTools.FieldRef<ChatManager, string> ChatMessageRef =
            GameRefs.ChatManager_chatMessage;

        private static bool Prefix(ChatManager __instance)
        {
            try
            {
                if (ChatMessageRef == null) return true;

                string message = ChatMessageRef(__instance);
                if (!CommandGate.TryParse(message, out _, out _)) return true;

                CheatCommands.Execute(message);
                ChatMessageRef(__instance) = "";
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
