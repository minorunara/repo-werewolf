namespace Werewolf.Core
{
    public static class ChatFilter
    {
        public static bool Allows(ChatLogEntry entry, int actor)
            => entry.Kind == ChatEntryKind.System || entry.Actor == actor;
    }
}
