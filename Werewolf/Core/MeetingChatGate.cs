namespace Werewolf.Core
{
    public static class MeetingChatGate
    {
        public static bool IsOpen(GamePhase phase, bool discussionOpen)
            => phase == GamePhase.Meeting && discussionOpen;
    }
}
