using System;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingSessionDisconnectTests : IDisposable
    {
        private const byte VoteProgress = 165;
        private const byte MeetingResult = 166;
        private const long Now = MeetingSessionHarness.GameStart;
        private const long Warp = Now + 5_000;

        public MeetingSessionDisconnectTests() { WLog.Sink = (_, __) => { }; }
        public void Dispose() { WLog.Sink = null; }

        private static MeetingSessionHarness Voting(int caller = 1)
        {
            var h = MeetingSessionHarness.Create();
            Assert.Equal(ConveneRejectReason.None, h.Session.TryConvene(caller, Now));
            h.Session.Tick(Warp);
            Assert.Equal(MeetingStage.Voting, h.Session.Stage);
            return h;
        }

        [Fact]
        public void PlayerLeft_DuringVoting_RemovesFromExpectedAndResendsProgress()
        {
            var h = Voting();
            h.Session.CastVote(1, 2, Warp);
            h.Session.CastVote(2, 3, Warp);
            h.Session.CastVote(4, 2, Warp);
            h.Session.CastVote(5, 2, Warp);
            h.Sent.Clear();

            h.Session.NotifyPlayerLeft(3, Warp);

            Assert.NotEmpty(h.ByCode(VoteProgress).ToList());
            h.Session.Tick(Warp);
            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            Assert.Equal(2, result.Payload[0]);
        }

        [Fact]
        public void PlayerLeft_ResendsProgressEvenWhenNotAllIn()
        {
            var h = Voting();
            h.Session.CastVote(1, 2, Warp);
            h.Sent.Clear();

            h.Session.NotifyPlayerLeft(5, Warp);
            h.Session.Tick(Warp);

            Assert.Equal(MeetingStage.Voting, h.Session.Stage);
            var progress = Assert.Single(h.ByCode(VoteProgress).ToList());
            Assert.Equal(new[] { 1 }, (int[])progress.Payload[0]);
        }

        [Fact]
        public void TopTarget_Disconnected_StillExecuted()
        {
            var h = Voting();
            h.Session.CastVote(1, 3, Warp);
            h.Session.CastVote(2, 3, Warp);

            h.Session.NotifyPlayerLeft(3, Warp);
            h.Sent.Clear();

            h.Session.CastVote(4, 2, Warp);
            h.Session.CastVote(5, -1, Warp);
            h.Session.Tick(Warp);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            var targets = (int[])result.Payload[1];
            var counts = (int[])result.Payload[2];
            int idx = Array.IndexOf(targets, 3);
            Assert.True(idx >= 0);
            Assert.Equal(2, counts[idx]);
        }

        [Fact]
        public void TopTarget_Disconnected_ExecutedWhenSoleMax()
        {
            var h = Voting();
            h.Session.CastVote(1, 3, Warp);
            h.Session.CastVote(2, 3, Warp);
            h.Session.CastVote(4, 3, Warp);

            h.Session.NotifyPlayerLeft(3, Warp);
            h.Sent.Clear();

            h.Session.CastVote(5, 2, Warp);
            h.Session.Tick(Warp);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            Assert.Equal(3, result.Payload[0]);
            Assert.Equal(new[] { 3 }, h.Executed.ToArray());
        }

        [Fact]
        public void DisconnectedVoter_UnvotedShareCountsAsSkip()
        {
            var h = Voting();
            h.Session.CastVote(1, 2, Warp);
            h.Session.CastVote(3, 2, Warp);

            h.Session.NotifyPlayerLeft(4, Warp);
            h.Session.NotifyPlayerLeft(5, Warp);
            h.Session.Tick(Warp);

            h.Session.CastVote(2, -1, Warp);
            h.Session.Tick(Warp);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            var targets = (int[])result.Payload[1];
            var counts = (int[])result.Payload[2];
            int skipIdx = Array.IndexOf(targets, -1);
            Assert.True(skipIdx >= 0);
            Assert.Equal(3, counts[skipIdx]);
        }

        [Fact]
        public void TopTarget_Died_ResultsInNoExecution()
        {
            var h = Voting();
            h.Session.CastVote(1, 3, Warp);
            h.Session.CastVote(2, 3, Warp);

            h.Player(3).Alive = false;
            h.Session.NotifyPlayerDied(3);

            h.Session.CastVote(4, 2, Warp);
            h.Session.CastVote(5, -1, Warp);
            h.Session.Tick(Warp);

            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            Assert.Equal(-1, result.Payload[0]);
            Assert.Empty(h.Executed);
        }

        [Fact]
        public void PlayerLeft_BeforeConvene_ExcludedFromExpectedVoters()
        {
            var h = MeetingSessionHarness.Create();
            h.Session.NotifyPlayerLeft(5, Now - 1_000);

            Assert.Equal(ConveneRejectReason.None, h.Session.TryConvene(1, Now));
            h.Session.Tick(Warp);
            Assert.Equal(MeetingStage.Voting, h.Session.Stage);

            Assert.Equal(VoteRejectReason.TargetUnknown, h.Session.CastVote(1, 5, Warp));

            h.Session.CastVote(1, 2, Warp);
            h.Session.CastVote(2, -1, Warp);
            h.Session.CastVote(3, -1, Warp);
            h.Session.CastVote(4, -1, Warp);
            h.Session.Tick(Warp);
            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
        }

        [Fact]
        public void PlayerLeft_DuringMeeting_StillExcludedInNextMeeting()
        {
            var h = MeetingSessionHarness.Create();
            Assert.Equal(ConveneRejectReason.None, h.Session.TryConvene(1, Now));
            h.Session.Tick(Warp);
            h.Session.NotifyPlayerLeft(5, Warp);

            h.Session.CastVote(1, -1, Warp);
            h.Session.CastVote(2, -1, Warp);
            h.Session.CastVote(3, -1, Warp);
            h.Session.CastVote(4, -1, Warp);
            h.Session.Tick(Warp);
            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            long finish = Warp + 6_000;
            h.Session.Tick(finish);
            Assert.Equal(MeetingStage.Idle, h.Session.Stage);

            Assert.Equal(ConveneRejectReason.None, h.Session.TryConvene(2, finish));
            long warp2 = finish + 5_000;
            h.Session.Tick(warp2);
            Assert.Equal(MeetingStage.Voting, h.Session.Stage);

            h.Session.CastVote(1, -1, warp2);
            h.Session.CastVote(2, -1, warp2);
            h.Session.CastVote(3, -1, warp2);
            h.Session.CastVote(4, -1, warp2);
            h.Session.Tick(warp2);
            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
        }

        [Fact]
        public void CallerLeaving_DoesNotAffectMeetingProgress()
        {
            var h = Voting(caller: 1);

            h.Session.NotifyPlayerLeft(1, Warp);

            h.Session.CastVote(2, 4, Warp);
            h.Session.CastVote(3, 4, Warp);
            h.Session.CastVote(4, 4, Warp);
            h.Session.CastVote(5, 4, Warp);
            h.Session.Tick(Warp);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            Assert.Equal(4, result.Payload[0]);
        }

        [Fact]
        public void VoterDeath_DuringVoting_RemovesExpectedVoter()
        {
            var h = Voting();
            h.Session.CastVote(1, 2, Warp);
            h.Session.CastVote(3, 2, Warp);
            h.Session.CastVote(4, 2, Warp);
            h.Session.CastVote(5, 2, Warp);

            Assert.Equal(MeetingStage.Voting, h.Session.Stage);

            h.Player(2).Alive = false;
            h.Session.NotifyPlayerDied(2);
            h.Session.Tick(Warp);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
        }
    }
}
