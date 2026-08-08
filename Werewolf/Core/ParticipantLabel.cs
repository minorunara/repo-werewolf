namespace Werewolf.Core
{
    public static class ParticipantLabel
    {
        public const int MaxLength = MeetingChatLog.MaxNameLength + 6;

        public static string Format(int id, string name)
        {
            string safe = MeetingChatLog.Sanitize(name, MeetingChatLog.MaxNameLength);
            if (safe.Length == 0) safe = MeetingChatLog.UnknownName;
            return id > 0 ? Texts.Format(TextId.IdNameFormat, id, safe) : safe;
        }
    }
}
