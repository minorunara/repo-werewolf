using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class EradicationRevealTests : IDisposable
    {
        private const long Now = 1_000_000;

        private readonly List<OutboundMessage> _sent = new List<OutboundMessage>();
        private readonly List<SessionEvent> _events = new List<SessionEvent>();

        public EradicationRevealTests()
        {
            WLog.Sink = (_, __) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Fact]
        public void EradicationRevealCode_Is192_PublicBroadcastWithSchema()
        {
            Assert.Equal(192, MessageCodes.EradicationReveal);
            Assert.True(MessageCodes.IsInRange(MessageCodes.EradicationReveal));
            Assert.False(MessageCodes.IsTargetOnly(MessageCodes.EradicationReveal));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.EradicationReveal));
            Assert.False(MessageCodes.IsSecret(MessageCodes.EradicationReveal));
            Assert.Equal(new[] { typeof(int), typeof(byte), typeof(byte), typeof(string) },
                MessageCodes.Schema(MessageCodes.EradicationReveal));
        }

        [Theory]
        [InlineData(3, Team.Villagers, false, "Alice")]
        [InlineData(-101, Team.Werewolves, true, "Bot")]
        public void Wire_RoundTrips(int actor, Team team, bool vanished, string name)
        {
            var data = EradicationRevealWire.FromWire(
                EradicationRevealWire.ToWire(actor, team, vanished, name));

            Assert.NotNull(data);
            Assert.Equal(actor, data.ActorNumber);
            Assert.Equal(team, data.WinningTeam);
            Assert.Equal(vanished, data.Vanished);
            Assert.Equal(name, data.Name);
        }

        [Fact]
        public void Wire_NullNameBecomesEmpty_MalformedIsNull()
        {
            Assert.Equal("", EradicationRevealWire.FromWire(
                EradicationRevealWire.ToWire(1, Team.Villagers, false, null)).Name);

            Assert.Null(EradicationRevealWire.FromWire(null));
            Assert.Null(EradicationRevealWire.FromWire(new object[] { 1, (byte)0 }));
            Assert.Null(EradicationRevealWire.FromWire(new object[] { "x", (byte)0, (byte)0, "n" }));
            Assert.Null(EradicationRevealWire.FromWire(new object[] { 1, (byte)255, (byte)0, "n" }));
        }

        [Theory]
        [InlineData(Team.Villagers, false, TextId.EradicationLastWerewolfDied)]
        [InlineData(Team.Villagers, true, TextId.EradicationLastWerewolfVanished)]
        [InlineData(Team.Werewolves, false, TextId.EradicationLastVillagerDied)]
        [InlineData(Team.Werewolves, true, TextId.EradicationLastVillagerVanished)]
        public void TitleId_MapsWinnerToFallenSide(Team winner, bool vanished, TextId expected)
        {
            Assert.Equal(expected, EradicationCeremony.TitleId(winner, vanished));
        }

        private GameSession CreateStartedSession()
        {
            var session = new GameSession();
            session.ReserveForcedRole(1, Role.Werewolf);

            var players = new List<WPlayer>();
            for (int i = 1; i <= 4; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }

            var config = new GameConfig { RoundSeconds = 600 };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            session.OnSend += m => _sent.Add(m);
            session.OnSessionEvent += e => _events.Add(e);
            return session;
        }

        [Fact]
        public void DecisiveDeath_Locks_Sends192AfterDeathNotice_WithVictimIdentity()
        {
            var session = CreateStartedSession();

            session.RecordDeath(1, Now + 1000);

            Assert.True(session.WinLocked);
            Assert.Null(session.Winner);

            Assert.Equal(new[] { 168, 192 }, _sent.Select(m => (int)m.Code).ToArray());
            var reveal = _sent.Single(m => m.Code == MessageCodes.EradicationReveal);
            Assert.Equal(MessageTarget.All, reveal.Target);
            var data = EradicationRevealWire.FromWire(reveal.Payload);
            Assert.Equal(1, data.ActorNumber);
            Assert.Equal(Team.Villagers, data.WinningTeam);
            Assert.False(data.Vanished);
            Assert.Equal("P1", data.Name);
        }

        [Fact]
        public void DuringLock_DeathsAreRecorded_ButOutcomeIsFrozen()
        {
            var session = CreateStartedSession();
            session.RecordDeath(1, Now + 1000);

            session.RecordDeath(2, Now + 1500);
            session.RecordDeath(3, Now + 1600);
            session.RecordDeath(4, Now + 1700);

            Assert.Equal(4, _sent.Count(m => m.Code == 168));
            Assert.Single(_sent, m => m.Code == 192);
            Assert.Null(session.Winner);

            session.ForceExpireTimer(Now + 2000);
            session.Tick(Now + 2500);
            session.NotifyExtractionOutcome(completed: true, failed: false, Now + 3000);
            Assert.Null(session.Winner);

            session.ConfirmPendingWin(Now + 1000 + EradicationCeremony.CeremonyMs);
            Assert.Equal(Team.Villagers, session.Winner.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);
        }

        [Fact]
        public void ConfirmPendingWin_WithoutLock_IsNoOp()
        {
            var session = CreateStartedSession();

            session.ConfirmPendingWin(Now + 1000);

            Assert.Null(session.Winner);
            Assert.Empty(_sent);
        }

        [Fact]
        public void DuringLock_PhaseChangeRequestsAreRejected()
        {
            var session = CreateStartedSession();
            session.RecordDeath(1, Now + 1000);

            Assert.False(session.RequestPhaseChange(GamePhase.Meeting, Now + 2000).Success);
            Assert.False(session.RequestPhaseChange(GamePhase.GameOver, Now + 2000).Success);
        }

        [Fact]
        public void DuringLock_CheckmateLockCannotEngage()
        {
            var session = CreateStartedSession();
            session.RecordDeath(1, Now + 1000);

            session.LockValueCheckmate();
            session.NotifyValueCheckmate(Now + 2000);

            Assert.Null(session.Winner);
            session.ConfirmPendingWin(Now + 1000 + EradicationCeremony.CeremonyMs);
            Assert.Equal(WinReason.WerewolvesEradicated, session.Winner.Reason);
        }

        [Fact]
        public void DuringCheckmateLock_DecisiveDeathDoesNotStartEradication()
        {
            var session = CreateStartedSession();
            session.LockValueCheckmate();

            session.RecordDeath(1, Now + 1000);

            Assert.Single(_sent, m => m.Code == 168);
            Assert.DoesNotContain(_sent, m => m.Code == 192);
            session.NotifyValueCheckmate(Now + 8000);
            Assert.Equal(WinReason.ValueCheckmate, session.Winner.Reason);
        }

        [Fact]
        public void VoidMatch_DuringLock_WinsOverPendingResult()
        {
            var session = CreateStartedSession();
            session.RecordDeath(1, Now + 1000);

            session.VoidMatch(Now + 2000);
            Assert.True(session.Voided);

            session.ConfirmPendingWin(Now + 1000 + EradicationCeremony.CeremonyMs);
            Assert.Null(session.Winner);
        }

        [Fact]
        public void DuringLock_ConveneIsRejected()
        {
            var h = MeetingSessionHarness.Create(playerCount: 4, forcedRoles: (1, Role.Werewolf));
            h.Game.RecordDeath(1, MeetingSessionHarness.GameStart + 60_000);
            Assert.True(h.Game.WinLocked);

            Assert.Equal(ConveneRejectReason.WrongPhase,
                h.Session.TryConvene(2, MeetingSessionHarness.GameStart + 61_000));
            Assert.False(h.Session.TryConveneScatterGuard(
                3, MeetingSessionHarness.GameStart + 61_000));
        }
    }
}
