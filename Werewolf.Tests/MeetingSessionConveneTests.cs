using System;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingSessionConveneTests : IDisposable
    {
        private const byte StartMeeting = 163;

        public MeetingSessionConveneTests() { WLog.Sink = (_, __) => { }; }
        public void Dispose() { WLog.Sink = null; }

        [Fact]
        public void TryConvene_WhenEligible_AcceptsAndEmitsStartAndPhaseAndRoomState()
        {
            var h = MeetingSessionHarness.Create();

            var reason = h.Session.TryConvene(callerActor: 3, nowUnixMs: MeetingSessionHarness.GameStart);

            Assert.Equal(ConveneRejectReason.None, reason);
            Assert.Equal(MeetingStage.Countdown, h.Session.Stage);
            Assert.Equal(3, h.Session.CallerActor);

            var start = Assert.Single(h.ByCode(StartMeeting).ToList());
            Assert.Equal(MessageTarget.All, start.Target);
            long expectedWarp = MeetingSessionHarness.GameStart + 5_000;
            long expectedEnd = expectedWarp + MeetingIntro.VotingUiDelayMs + 120_000;
            Assert.Equal(3, start.Payload[0]);
            Assert.Equal(expectedWarp, start.Payload[1]);
            Assert.Equal(expectedEnd, start.Payload[2]);

            Assert.Equal(GamePhase.Meeting, Assert.Single(h.PhaseRequests));
            Assert.Equal((3, expectedEnd), Assert.Single(h.MeetingStates));
        }

        [Fact]
        public void TryConvene_ConsumesRightOnlyOnAcceptance()
        {
            var h = MeetingSessionHarness.Create();
            Assert.Equal(1, h.Session.RightsRemaining(3));

            h.Session.TryConvene(3, MeetingSessionHarness.GameStart);

            Assert.Equal(0, h.Session.RightsRemaining(3));
        }

        [Fact]
        public void TryConvene_OnAcceptance_EmitsOnRightsChangedOnce()
        {
            var h = MeetingSessionHarness.Create();

            h.Session.TryConvene(3, MeetingSessionHarness.GameStart);

            var change = Assert.Single(h.RightsChanges);
            Assert.Equal(3, change.Actor);
            Assert.Equal(0, change.Remaining);
        }

        [Fact]
        public void TryConvene_WhenRejected_DoesNotEmitOnRightsChanged()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.MeetingRightsPerPlayer = 0);

            h.Session.TryConvene(3, MeetingSessionHarness.GameStart);

            Assert.Empty(h.RightsChanges);
        }

        [Fact]
        public void TryConvene_NoRight_Rejected()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.MeetingRightsPerPlayer = 0);

            Assert.Equal(ConveneRejectReason.NoRight,
                h.Session.TryConvene(3, MeetingSessionHarness.GameStart));
            Assert.Equal(MeetingStage.Idle, h.Session.Stage);
            Assert.Empty(h.ByCode(StartMeeting));
        }

        [Fact]
        public void TryConvene_SecondByRightlessCaller_Rejected()
        {
            var h = MeetingSessionHarness.Create();
            h.Session.TryConvene(3, MeetingSessionHarness.GameStart);

            DriveToIdle(h, MeetingSessionHarness.GameStart);

            Assert.Equal(ConveneRejectReason.NoRight,
                h.Session.TryConvene(3, MeetingSessionHarness.GameStart + 500_000));
        }

        [Fact]
        public void TryConvene_WithinStartSuppression_Rejected()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.ConveneSuppressStartSec = 60);

            Assert.Equal(ConveneRejectReason.Suppressed,
                h.Session.TryConvene(3, MeetingSessionHarness.GameStart + 30_000));

            Assert.Equal(ConveneRejectReason.None,
                h.Session.TryConvene(3, MeetingSessionHarness.GameStart + 60_000));
        }

        [Fact]
        public void TryConvene_NotInPlay_Rejected()
        {
            var game = new GameSession();
            var players = Enumerable.Range(1, 5)
                .Select(i => new WPlayer { ActorNumber = i, Name = "P" + i }).ToList();
            var config = new GameConfig { ConveneSuppressStartSec = 0 };
            var session = new MeetingSession(config, game, players, 0);

            Assert.Equal(GamePhase.Lobby, game.Phase);
            Assert.Equal(ConveneRejectReason.WrongPhase, session.TryConvene(1, 100_000));
        }

        [Fact]
        public void TryConvene_DeadCaller_Rejected()
        {
            var h = MeetingSessionHarness.Create();
            h.Player(3).Alive = false;

            Assert.Equal(ConveneRejectReason.CallerDead,
                h.Session.TryConvene(3, MeetingSessionHarness.GameStart));
        }

        [Fact]
        public void TryConvene_SecondConcurrentRequest_RejectedAsAlreadyMeeting()
        {
            var h = MeetingSessionHarness.Create();
            Assert.Equal(ConveneRejectReason.None, h.Session.TryConvene(3, MeetingSessionHarness.GameStart));

            Assert.Equal(ConveneRejectReason.AlreadyMeeting,
                h.Session.TryConvene(4, MeetingSessionHarness.GameStart));
            Assert.Equal(1, h.Session.RightsRemaining(4));
        }

        [Fact]
        public void TryConvene_UnknownCaller_Rejected()
        {
            var h = MeetingSessionHarness.Create();

            Assert.Equal(ConveneRejectReason.UnknownCaller,
                h.Session.TryConvene(999, MeetingSessionHarness.GameStart));
        }

        [Fact]
        public void TryConvene_DuringPlay_IsAcceptedRegardlessOfExtractionProgress()
        {
            var h = MeetingSessionHarness.Create();

            Assert.Equal(GamePhase.Play, h.Game.Phase);
            Assert.Equal(ConveneRejectReason.None,
                h.Session.TryConvene(2, MeetingSessionHarness.GameStart));
        }

        [Fact]
        public void TryConvene_RejectedRequest_DoesNotConsumeRight()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.ConveneSuppressStartSec = 60);

            h.Session.TryConvene(3, MeetingSessionHarness.GameStart + 10_000);

            Assert.Equal(1, h.Session.RightsRemaining(3));
        }

        [Fact]
        public void AbortForGameOver_DuringMeeting_ResetsToIdleAndClearsRoomState()
        {
            var h = MeetingSessionHarness.Create();
            h.Session.TryConvene(3, MeetingSessionHarness.GameStart);
            Assert.Equal(MeetingStage.Countdown, h.Session.Stage);
            h.MeetingStates.Clear();

            Assert.True(h.Session.AbortForGameOver());

            Assert.Equal(MeetingStage.Idle, h.Session.Stage);
            Assert.Equal((-1, 0L), Assert.Single(h.MeetingStates));
        }

        [Fact]
        public void AbortForGameOver_MidVoting_PendingVotesNeverTally()
        {
            var h = MeetingSessionHarness.Create();
            long start = MeetingSessionHarness.GameStart;
            h.Session.TryConvene(3, start);
            long warp = start + 5_000;
            h.Session.Tick(warp);
            h.Session.CastVote(3, 4, warp + 1_000);

            Assert.True(h.Session.AbortForGameOver());

            h.PhaseRequests.Clear();
            h.Session.Tick(warp + MeetingIntro.VotingUiDelayMs + 200_000);
            Assert.Equal(MeetingStage.Idle, h.Session.Stage);
            Assert.Empty(h.PhaseRequests);
        }

        [Fact]
        public void AbortForGameOver_WhenIdle_IsNoOp()
        {
            var h = MeetingSessionHarness.Create();

            Assert.False(h.Session.AbortForGameOver());
            Assert.Empty(h.MeetingStates);
        }

        private static void DriveToIdle(MeetingSessionHarness h, long conveneNow)
        {
            long warp = conveneNow + 5_000;
            h.Session.Tick(warp);
            long end = warp + MeetingIntro.VotingUiDelayMs + 120_000;
            h.Session.Tick(end);
            h.Session.Tick(end + h.Session.ResultCeremonyDelayMs + 6_000);
        }
    }
}
