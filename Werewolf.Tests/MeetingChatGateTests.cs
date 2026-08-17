using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingChatGateTests
    {
        [Theory]
        [InlineData(GamePhase.Meeting, true, true)]
        [InlineData(GamePhase.Meeting, false, false)]
        [InlineData(GamePhase.Play, true, false)]
        [InlineData(GamePhase.Play, false, false)]
        [InlineData(GamePhase.GameOver, true, false)]
        [InlineData(GamePhase.Lobby, true, false)]
        public void IsOpen_MatrixOfAllInputs(GamePhase phase, bool discussionOpen, bool expected)
        {
            Assert.Equal(expected, MeetingChatGate.IsOpen(phase, discussionOpen));
        }

        [Fact]
        public void IsOpen_FollowsExplicitDiscussionBoundary()
        {
            var state = new MeetingClientState();
            const long warpAt = 10_000;
            state.ApplyStartMeeting(caller: 1, warpUnixMs: warpAt, endUnixMs: warpAt + 60_000);

            Assert.False(state.DiscussionOpen);
            Assert.False(MeetingChatGate.IsOpen(GamePhase.Meeting, state.DiscussionOpen));
            Assert.True(state.VotingUiReady(warpAt + MeetingIntro.VotingUiDelayMs));
            Assert.False(MeetingChatGate.IsOpen(GamePhase.Meeting, state.DiscussionOpen));

            state.MarkDiscussionOpen();
            Assert.True(MeetingChatGate.IsOpen(GamePhase.Meeting, state.DiscussionOpen));

            state.ApplyPhase(GamePhase.Play);
            Assert.False(MeetingChatGate.IsOpen(GamePhase.Meeting, state.DiscussionOpen));

            state.ApplyStartMeeting(caller: 2, warpUnixMs: 80_000, endUnixMs: 140_000);
            Assert.False(state.DiscussionOpen);
        }

        [Fact]
        public void IsOpen_RestoredMeetingOpensDiscussionImmediately()
        {
            var state = new MeetingClientState();
            state.RestoreFromRoomState(caller: 1, endUnixMs: 99_000);

            Assert.True(MeetingChatGate.IsOpen(GamePhase.Meeting, state.DiscussionOpen));
        }

        [Fact]
        public void IsOpen_CancelledCountdownNeverOpens()
        {
            var state = new MeetingClientState();
            const long warpAt = 10_000;
            state.ApplyStartMeeting(caller: 1, warpUnixMs: warpAt, endUnixMs: warpAt + 60_000,
                kind: ConveneKind.CorpseReport);
            state.MarkDiscussionOpen();
            Assert.True(state.DiscussionOpen);
            state.ApplyCancelled();

            Assert.False(MeetingChatGate.IsOpen(GamePhase.Meeting, state.DiscussionOpen));
            state.MarkDiscussionOpen();
            Assert.False(state.DiscussionOpen);
        }
    }
}
