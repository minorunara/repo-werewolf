using System;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingSessionFlowTests : IDisposable
    {
        private const byte StartMeeting = 163;
        private const byte VoteProgress = 165;
        private const byte MeetingResult = 166;
        private const long Now = MeetingSessionHarness.GameStart;
        private const long Warp = Now + 5_000;
        private const long End = Warp + MeetingIntro.VotingUiDelayMs + 120_000;

        public MeetingSessionFlowTests() { WLog.Sink = (_, __) => { }; }
        public void Dispose() { WLog.Sink = null; }

        private static MeetingSessionHarness Convened(int caller = 1, Action<GameConfig> tune = null,
            params (int, Role)[] roles)
        {
            var h = MeetingSessionHarness.Create(tune: tune, forcedRoles: roles);
            Assert.Equal(ConveneRejectReason.None, h.Session.TryConvene(caller, Now));
            return h;
        }

        [Fact]
        public void Tick_BeforeWarp_StaysCountdown_AtWarp_BeginsVoting()
        {
            var h = Convened();

            h.Session.Tick(Warp - 1);
            Assert.Equal(MeetingStage.Countdown, h.Session.Stage);

            h.Session.Tick(Warp);
            Assert.Equal(MeetingStage.Voting, h.Session.Stage);
        }

        [Fact]
        public void CastVote_DuringCountdown_RejectedAsNotVotingStage()
        {
            var h = Convened();

            Assert.Equal(VoteRejectReason.NotVotingStage, h.Session.CastVote(2, 3, Warp - 100));
            h.Session.Tick(Warp);
            Assert.Equal(VoteRejectReason.None, h.Session.CastVote(2, 3, Warp));
        }

        [Fact]
        public void CastVote_EmitsVoteProgressAndShortensEnd()
        {
            var h = Convened();
            h.Session.Tick(Warp);
            h.Sent.Clear();

            Assert.Equal(VoteRejectReason.None, h.Session.CastVote(1, 2, Warp));

            var progress = Assert.Single(h.ByCode(VoteProgress).ToList());
            Assert.Equal(MessageTarget.All, progress.Target);
            var voted = (int[])progress.Payload[0];
            Assert.Equal(new[] { 1 }, voted);

            long newEnd = (long)progress.Payload[1];
            Assert.True(newEnd < End);
            Assert.Equal(Warp + (End - Warp) * 4 / 5, newEnd);
        }

        [Fact]
        public void CastVote_TimeCutDisabled_KeepsEnd()
        {
            var h = Convened(tune: c => c.VoteTimeCutEnabled = false);
            h.Session.Tick(Warp);
            h.Sent.Clear();

            h.Session.CastVote(1, 2, Warp);

            var progress = Assert.Single(h.ByCode(VoteProgress).ToList());
            Assert.Equal(End, (long)progress.Payload[1]);
        }

        [Fact]
        public void AllVotesIn_TriggersTallyAndResult()
        {
            var h = Convened();
            h.Session.Tick(Warp);
            h.Sent.Clear();

            for (int voter = 1; voter <= 5; voter++)
                h.Session.CastVote(voter, 2, Warp);

            h.Session.Tick(Warp);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            Assert.Equal(2, result.Payload[0]);
            Assert.Equal(new[] { 2 }, (int[])result.Payload[1]);
            Assert.Equal(new[] { 5 }, (int[])result.Payload[2]);
            Assert.Equal(2, Assert.Single(h.Executed));
        }

        [Fact]
        public void TimerExpiry_TriggersTally_EvenWithoutAllVotes()
        {
            var h = Convened();
            h.Session.Tick(Warp);
            h.Session.CastVote(1, 2, Warp);
            h.Session.CastVote(3, 2, Warp);
            h.Session.CastVote(4, 2, Warp);
            h.Sent.Clear();

            h.Session.Tick(End);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            Assert.Equal(2, result.Payload[0]);
        }

        [Fact]
        public void ClosingHoldExpiry_RequestsPlayAndUpdatesSuppressionOrigin()
        {
            var h = Convened();
            h.Session.Tick(Warp);
            for (int voter = 1; voter <= 5; voter++) h.Session.CastVote(voter, 2, Warp);
            long closeAt = Warp;
            h.Session.Tick(closeAt);
            h.PhaseRequests.Clear();
            h.MeetingStates.Clear();

            h.Session.Tick(closeAt + 6_000);

            Assert.Equal(MeetingStage.Idle, h.Session.Stage);
            Assert.Equal(GamePhase.Play, Assert.Single(h.PhaseRequests));
            Assert.Equal((-1, 0L), Assert.Single(h.MeetingStates));
            Assert.Equal(closeAt + 6_000, h.Session.LastMeetingEndUnixMs);
        }

        [Fact]
        public void ClosingHold_DoesNotFinishBeforeHoldElapses()
        {
            var h = Convened();
            h.Session.Tick(Warp);
            for (int voter = 1; voter <= 5; voter++) h.Session.CastVote(voter, 2, Warp);
            h.Session.Tick(Warp);
            h.PhaseRequests.Clear();

            h.Session.Tick(Warp + 5_999);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            Assert.Empty(h.PhaseRequests);
        }

        [Fact]
        public void ClosingHold_ShortResultDisplaySec_ClampedToKillDelayFloor()
        {
            var h = Convened(tune: c => c.ResultDisplaySec = 1);
            h.Session.Tick(Warp);
            for (int voter = 1; voter <= 5; voter++) h.Session.CastVote(voter, 2, Warp);
            h.Session.Tick(Warp);
            h.PhaseRequests.Clear();

            long floorMs = (MeetingSession.PostResultKillDelaySec + 1) * 1000L;
            h.Session.Tick(Warp + floorMs - 1);
            Assert.Equal(MeetingStage.Closing, h.Session.Stage);

            h.Session.Tick(Warp + floorMs);
            Assert.Equal(MeetingStage.Idle, h.Session.Stage);
            Assert.Equal(GamePhase.Play, Assert.Single(h.PhaseRequests));
        }

        [Fact]
        public void ExtendClosingHold_DelaysPlayReturn()
        {
            var h = Convened();
            h.Session.Tick(Warp);
            for (int voter = 1; voter <= 5; voter++) h.Session.CastVote(voter, 2, Warp);
            h.Session.Tick(Warp);
            h.PhaseRequests.Clear();

            h.Session.ExtendClosingHold(3_000);
            h.Session.Tick(Warp + 6_000);
            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            Assert.Empty(h.PhaseRequests);

            h.Session.Tick(Warp + 9_000);
            Assert.Equal(MeetingStage.Idle, h.Session.Stage);
            Assert.Equal(GamePhase.Play, Assert.Single(h.PhaseRequests));
        }

        [Fact]
        public void ClosingHoldExpiry_WhenExecutionCausesGameOver_SuppressesPlayReturn()
        {
            var h = MeetingSessionHarness.Create(playerCount: 3, forcedRoles: (1, Role.Werewolf));
            h.Session.OnExecutePlayer += a =>
            {
                h.Game.MarkNextDeathAsVote(a);
                h.Game.RecordDeath(a, Warp);
            };
            Assert.Equal(ConveneRejectReason.None, h.Session.TryConvene(2, Now));
            h.Session.Tick(Warp);

            h.Session.CastVote(1, 1, Warp);
            h.Session.CastVote(2, 1, Warp);
            h.Session.CastVote(3, 1, Warp);
            h.Session.Tick(Warp);

            Assert.Equal(1, Assert.Single(h.Executed));
            Assert.Equal(GamePhase.GameOver, h.Game.Phase);

            h.PhaseRequests.Clear();
            h.Session.Tick(Warp + 6_000);

            Assert.Equal(MeetingStage.Idle, h.Session.Stage);
            Assert.Empty(h.PhaseRequests);
        }

        [Fact]
        public void SkipMajority_NoExecution_StillReturnsToPlay()
        {
            var h = Convened();
            h.Session.Tick(Warp);
            for (int voter = 1; voter <= 5; voter++) h.Session.CastVote(voter, -1, Warp);
            h.Session.Tick(Warp);

            Assert.Equal(MeetingStage.Closing, h.Session.Stage);
            Assert.Empty(h.Executed);
            var result = Assert.Single(h.ByCode(MeetingResult).ToList());
            Assert.Equal(-1, result.Payload[0]);

            h.PhaseRequests.Clear();
            h.Session.Tick(Warp + 6_000);
            Assert.Equal(GamePhase.Play, Assert.Single(h.PhaseRequests));
        }
    }
}
