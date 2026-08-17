namespace Werewolf.Core.Replay
{
    public static class ReplayChatGate
    {
        public static bool ShouldRecord(GamePhase phase, bool discussionOpen, bool speakerAlive)
            => MeetingChatGate.IsOpen(phase, discussionOpen) && speakerAlive;
    }
}
