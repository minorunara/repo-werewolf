using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ChatFilterTests
    {
        private static ChatLogEntry Entry(int actor, ChatEntryKind kind, ChatSpeaker speaker = ChatSpeaker.Alive)
            => new ChatLogEntry(actor, $"P{actor}", "text", speaker, kind);

        [Fact]
        public void Allows_SystemEntry_RegardlessOfTargetActor()
        {
            var system = Entry(MeetingChatLog.SystemActor, ChatEntryKind.System);

            Assert.True(ChatFilter.Allows(system, 1));
            Assert.True(ChatFilter.Allows(system, -101));
        }

        [Fact]
        public void Allows_Message_OnlyFromTheTargetActor()
        {
            Assert.True(ChatFilter.Allows(Entry(1, ChatEntryKind.Message), 1));
            Assert.False(ChatFilter.Allows(Entry(2, ChatEntryKind.Message), 1));
        }

        [Fact]
        public void Allows_VoteNotice_OnlyFromTheTargetActor()
        {
            Assert.True(ChatFilter.Allows(Entry(1, ChatEntryKind.Vote), 1));
            Assert.False(ChatFilter.Allows(Entry(2, ChatEntryKind.Vote), 1));
        }

        [Fact]
        public void Allows_DeadSpeakerMessageOfTheTarget()
        {
            Assert.True(ChatFilter.Allows(Entry(1, ChatEntryKind.Message, ChatSpeaker.Dead), 1));
        }
    }
}
