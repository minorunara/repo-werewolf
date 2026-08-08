using System;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScatterGuardTests : IDisposable
    {
        private const byte StartMeeting = 163;

        public ScatterGuardTests() { WLog.Sink = (_, __) => { }; }
        public void Dispose() { WLog.Sink = null; }

        [Fact]
        public void Guard_Arm_OpensWindowForGuardSec()
        {
            var g = new ScatterGuard();
            g.Arm(1_000_000, 15);

            Assert.Equal(1_015_000, g.ArmedUntilUnixMs);
            Assert.True(g.IsArmed(1_000_000));
            Assert.True(g.IsArmed(1_014_999));
            Assert.False(g.IsArmed(1_015_000));
            Assert.False(g.IsArmed(1_015_001));
        }

        [Fact]
        public void Guard_ArmWithZeroSec_DisablesAndClosesExistingWindow()
        {
            var g = new ScatterGuard();
            g.Arm(1_000_000, 15);
            Assert.True(g.IsArmed(1_000_001));

            g.Arm(1_001_000, 0);

            Assert.Equal(0, g.ArmedUntilUnixMs);
            Assert.False(g.IsArmed(1_001_001));
        }

        [Fact]
        public void Guard_Disarm_ClosesWindow()
        {
            var g = new ScatterGuard();
            g.Arm(1_000_000, 15);

            g.Disarm();

            Assert.False(g.IsArmed(1_000_001));
        }

        [Fact]
        public void ClientWindow_AnchoredAtReceipt_OutlivesHostWindow()
        {
            var host = new ScatterGuard();
            var client = new ScatterGuard();
            host.Arm(1_000_000, 15);
            client.Arm(1_000_250, 15);

            Assert.False(host.IsArmed(host.ArmedUntilUnixMs));
            Assert.True(client.IsArmed(host.ArmedUntilUnixMs));
            Assert.False(client.IsArmed(client.ArmedUntilUnixMs));
        }

        [Fact]
        public void CountGroups_NullOrAllTruck_IsNotDistributed()
        {
            Assert.Equal(0, ScatterGroupsWire.CountGroups(null));
            Assert.Equal(1, ScatterGroupsWire.CountGroups(new[]
            {
                (1, "truck"), (2, "truck"), (3, "truck"),
            }));
        }

        [Fact]
        public void CountGroups_TruckFallback_FoldsIntoTruckGroup()
        {
            Assert.Equal(1, ScatterGroupsWire.CountGroups(new[]
            {
                (1, "truck"), (2, "truck_fallback"), (3, "truck"),
            }));
            Assert.Equal(2, ScatterGroupsWire.CountGroups(new[]
            {
                (1, "ep1"), (2, "truck_fallback"), (3, "ep1"), (4, "truck"),
            }));
        }

        [Fact]
        public void CountGroups_TruckAndExtractionPoint_IsDistributed()
        {
            Assert.Equal(2, ScatterGroupsWire.CountGroups(new[]
            {
                (1, "truck"), (2, "truck"), (3, "truck"), (4, "ep2"), (5, "ep2"), (6, "ep2"),
            }));
        }

        [Fact]
        public void TryConveneScatterGuard_WhenIdleAndPlay_WarpsImmediatelyWithKind2()
        {
            var h = MeetingSessionHarness.Create();
            long now = MeetingSessionHarness.GameStart + 100_000;

            Assert.True(h.Session.TryConveneScatterGuard(victimActor: 4, nowUnixMs: now));

            Assert.Equal(MeetingStage.Countdown, h.Session.Stage);
            Assert.Equal(ConveneKind.ScatterGuard, h.Session.CurrentKind);
            Assert.Equal(4, h.Session.CallerActor);

            var start = Assert.Single(h.ByCode(StartMeeting).ToList());
            Assert.Equal(MessageTarget.All, start.Target);
            Assert.Equal(0, MeetingSession.ScatterGuardCountdownSec);
            long expectedEnd = now + MeetingIntro.VotingUiDelayMs + 120_000;
            Assert.Equal(4, start.Payload[0]);
            Assert.Equal(now, start.Payload[1]);
            Assert.NotEqual(now + h.Config.MeetingCountdownSec * 1000L, start.Payload[1]);
            Assert.Equal(expectedEnd, start.Payload[2]);
            Assert.Equal((byte)ConveneKind.ScatterGuard, start.Payload[3]);

            Assert.Equal(GamePhase.Meeting, Assert.Single(h.PhaseRequests));
            Assert.Equal((4, expectedEnd), Assert.Single(h.MeetingStates));
        }

        [Fact]
        public void TryConveneScatterGuard_NextTick_EntersVoting()
        {
            var h = MeetingSessionHarness.Create();
            long now = MeetingSessionHarness.GameStart + 100_000;
            h.Session.TryConveneScatterGuard(4, now);

            h.Session.Tick(now);

            Assert.Equal(MeetingStage.Voting, h.Session.Stage);
        }

        [Fact]
        public void TryConveneScatterGuard_DoesNotConsumeAnyMeetingRight()
        {
            var h = MeetingSessionHarness.Create();

            h.Session.TryConveneScatterGuard(4, MeetingSessionHarness.GameStart);

            Assert.Equal(1, h.Session.RightsRemaining(4));
            Assert.Empty(h.RightsChanges);
        }

        [Fact]
        public void TryConveneScatterGuard_IgnoresSuppressionWindows()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.ConveneSuppressStartSec = 3600);

            Assert.Equal(ConveneRejectReason.Suppressed,
                h.Session.TryConvene(3, MeetingSessionHarness.GameStart + 1_000));
            Assert.True(h.Session.TryConveneScatterGuard(4, MeetingSessionHarness.GameStart + 1_000));
        }

        [Fact]
        public void TryConveneScatterGuard_DeadVictim_IsAccepted()
        {
            var h = MeetingSessionHarness.Create();
            h.Player(4).Alive = false;

            Assert.True(h.Session.TryConveneScatterGuard(4, MeetingSessionHarness.GameStart));
        }

        [Fact]
        public void TryConveneScatterGuard_DuringExistingMeeting_Rejected()
        {
            var h = MeetingSessionHarness.Create();
            Assert.Equal(ConveneRejectReason.None,
                h.Session.TryConvene(3, MeetingSessionHarness.GameStart));

            Assert.False(h.Session.TryConveneScatterGuard(4, MeetingSessionHarness.GameStart + 1_000));
            Assert.Single(h.ByCode(StartMeeting));
        }

        [Fact]
        public void TryConveneScatterGuard_NotInPlay_Rejected()
        {
            var game = new GameSession();
            var players = Enumerable.Range(1, 5)
                .Select(i => new WPlayer { ActorNumber = i, Name = "P" + i }).ToList();
            var session = new MeetingSession(new GameConfig(), game, players, 0);

            Assert.False(session.TryConveneScatterGuard(1, 100_000));
        }

        [Fact]
        public void TryConveneScatterGuard_UnknownVictim_Rejected()
        {
            var h = MeetingSessionHarness.Create();

            Assert.False(h.Session.TryConveneScatterGuard(999, MeetingSessionHarness.GameStart));
            Assert.Empty(h.ByCode(StartMeeting));
        }

        [Fact]
        public void RecordDeath_ReturnsTrueOnlyForNewDeath()
        {
            var h = MeetingSessionHarness.Create();
            long now = MeetingSessionHarness.GameStart + 10_000;

            Assert.True(h.Game.RecordDeath(4, now));
            Assert.False(h.Game.RecordDeath(4, now));
            Assert.False(h.Game.RecordDeath(999, now));
        }
    }
}
