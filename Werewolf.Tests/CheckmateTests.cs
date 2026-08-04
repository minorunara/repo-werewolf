using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class CheckmateTests : IDisposable
    {
        private const long Now = 1_000_000;

        private readonly List<OutboundMessage> _sent = new List<OutboundMessage>();
        private readonly List<(string Line, bool Secret)> _log = new List<(string, bool)>();

        public CheckmateTests()
        {
            WLog.Sink = (line, secret) => _log.Add((line, secret));
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Theory]
        [InlineData(12000, 3, 0, 12000)]
        [InlineData(12000, 3, 1, 8000)]
        [InlineData(12000, 3, 3, 0)]
        [InlineData(12000, 3, 5, 0)]
        [InlineData(10000, 3, 0, 9999)]
        [InlineData(0, 3, 0, -1)]
        [InlineData(12000, 0, 0, -1)]
        [InlineData(12000, 3, -1, 12000)]
        public void RemainingQuota_MatchesVanillaIntegerDivision(
            int haulGoal, int points, int completed, int expected)
        {
            Assert.Equal(expected, CheckmateJudge.RemainingQuotaDollars(haulGoal, points, completed));
        }

        [Theory]
        [InlineData(7999f, 8000, true)]
        [InlineData(8000f, 8000, false)]
        [InlineData(100f, 0, false)]
        [InlineData(100f, -1, false)]
        public void IsCheckmate_StrictlyBelowRemainingQuota(
            float obtainable, int remainingQuota, bool expected)
        {
            Assert.Equal(expected, CheckmateJudge.IsCheckmate(obtainable, remainingQuota));
        }

        [Fact]
        public void HaulFreeze_InitiallyNotHolding()
        {
            Assert.False(new HaulFreeze().IsHolding(Now));
        }

        [Fact]
        public void HaulFreeze_HoldsFromFirstSuckUntilClose_RegardlessOfSuckInterval()
        {
            var freeze = new HaulFreeze();
            freeze.NoteSuck(Now);

            Assert.True(freeze.IsHolding(Now + 5_000));
            Assert.True(freeze.IsHolding(Now + 30_000));

            freeze.Close();
            Assert.False(freeze.IsHolding(Now + 30_001));
        }

        [Fact]
        public void HaulFreeze_TimesOutFromLastSuck_WhenNeverClosed()
        {
            var freeze = new HaulFreeze();
            freeze.NoteSuck(Now);
            freeze.NoteSuck(Now + 10_000);

            Assert.True(freeze.IsHolding(Now + 10_000 + HaulFreeze.TimeoutMs - 1));
            Assert.False(freeze.IsHolding(Now + 10_000 + HaulFreeze.TimeoutMs));
        }

        [Fact]
        public void HaulFreeze_ReopensOnNextExtraction()
        {
            var freeze = new HaulFreeze();
            freeze.NoteSuck(Now);
            freeze.Close();

            freeze.NoteSuck(Now + 100_000);
            Assert.True(freeze.IsHolding(Now + 100_001));
        }

        [Fact]
        public void HaulFreeze_CloseIsIdempotentAndSafeBeforeOpen()
        {
            var freeze = new HaulFreeze();
            freeze.Close();
            freeze.Close();
            Assert.False(freeze.IsHolding(Now));
        }

        [Fact]
        public void Sequence_WithoutDetection_DoesNothing()
        {
            var seq = new CheckmateSequence();
            Assert.Equal(CheckmateAction.None, seq.Tick(Now, GamePhase.Play, curseActive: false));
            Assert.False(seq.CeremonyStarted);
        }

        [Fact]
        public void Sequence_DetectedInPlay_StartsCeremonyThenConfirmsAfterFixedDuration()
        {
            var seq = new CheckmateSequence();
            seq.NotifyDetected();

            Assert.Equal(CheckmateAction.StartCeremony, seq.Tick(Now, GamePhase.Play, false));
            Assert.True(seq.CeremonyStarted);

            Assert.Equal(CheckmateAction.None,
                seq.Tick(Now + CheckmateSequence.CeremonyMs - 1, GamePhase.Play, false));

            Assert.Equal(CheckmateAction.ConfirmWin,
                seq.Tick(Now + CheckmateSequence.CeremonyMs, GamePhase.Play, false));
            Assert.True(seq.Confirmed);

            Assert.Equal(CheckmateAction.None,
                seq.Tick(Now + CheckmateSequence.CeremonyMs + 1000, GamePhase.Play, false));
        }

        [Fact]
        public void Sequence_DetectedDuringMeeting_DefersUntilPlayResumes()
        {
            var seq = new CheckmateSequence();
            seq.NotifyDetected();

            Assert.Equal(CheckmateAction.None, seq.Tick(Now, GamePhase.Meeting, false));
            Assert.False(seq.CeremonyStarted);

            Assert.Equal(CheckmateAction.None, seq.Tick(Now + 1000, GamePhase.Play, curseActive: true));

            Assert.Equal(CheckmateAction.StartCeremony, seq.Tick(Now + 2000, GamePhase.Play, false));
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
            return session;
        }

        [Fact]
        public void NotifyValueCheckmate_ConfirmsWerewolfWinWithCheckmateReason()
        {
            var session = CreateStartedSession();

            session.NotifyValueCheckmate(Now + 1000);

            Assert.NotNull(session.Winner);
            Assert.Equal(Team.Werewolves, session.Winner.WinningTeam);
            Assert.Equal(WinReason.ValueCheckmate, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);

            var gameOver = Assert.Single(_sent, m => m.Code == WWEventCodes.GameOver);
            Assert.Equal((byte)Team.Werewolves, gameOver.Payload[0]);
        }

        [Fact]
        public void LockValueCheckmate_SuppressesWinByWerewolfDeathDuringCeremony()
        {
            var session = CreateStartedSession();
            session.LockValueCheckmate();

            session.RecordDeath(1, Now + 1000);

            Assert.Single(_sent, m => m.Code == WWEventCodes.PlayerDied);
            Assert.Null(session.Winner);
            Assert.NotEqual(GamePhase.GameOver, session.Phase);

            session.NotifyValueCheckmate(Now + CheckmateSequence.CeremonyMs);
            Assert.Equal(Team.Werewolves, session.Winner.WinningTeam);
            Assert.Equal(WinReason.ValueCheckmate, session.Winner.Reason);
        }

        [Fact]
        public void LockValueCheckmate_SuppressesExtractionAndTimerOutcomes()
        {
            var session = CreateStartedSession();
            session.LockValueCheckmate();

            session.NotifyExtractionOutcome(completed: true, failed: false, Now + 1000);
            Assert.Null(session.Winner);

            session.ForceExpireTimer(Now + 1500);
            session.Tick(Now + 2000);
            Assert.Null(session.Winner);

            session.NotifyValueCheckmate(Now + 3000);
            Assert.Equal(WinReason.ValueCheckmate, session.Winner.Reason);
        }

        [Fact]
        public void NotifyValueCheckmate_AfterOtherWin_IsNoOp()
        {
            var session = CreateStartedSession();

            session.RecordDeath(1, Now + 1000);
            Assert.Equal(Team.Villagers, session.Winner.WinningTeam);

            session.NotifyValueCheckmate(Now + 2000);
            Assert.Equal(WinReason.WerewolvesEradicated, session.Winner.Reason);
            Assert.Single(_sent, m => m.Code == WWEventCodes.GameOver);
        }

        [Fact]
        public void CheckmateRevealCode_IsPublicBroadcastWithSchema()
        {
            Assert.Equal(187, EventCodes.CheckmateReveal);
            Assert.True(EventCodes.IsInRange(EventCodes.CheckmateReveal));
            Assert.False(EventCodes.IsTargetOnly(EventCodes.CheckmateReveal));
            Assert.False(EventCodes.IsMasterInbound(EventCodes.CheckmateReveal));
            Assert.False(EventCodes.IsSecret(EventCodes.CheckmateReveal));
            Assert.Equal(new[] { typeof(int[]), typeof(long) },
                EventCodes.Schema(EventCodes.CheckmateReveal));
        }

        [Fact]
        public void MeetingGaugeSnapshot_FromData_RestoresThirteenElementArray()
        {
            var data = new[] { 350, 20000, 10, 20, 30, 40, 5, 7000, 4000, 12000, 25, 9000, 55 };
            MeetingGaugeSnapshot s = MeetingGaugeSnapshot.FromData(data);

            Assert.NotNull(s);
            Assert.Equal(350, s.RatioPermille);
            Assert.Equal(20000, s.BaseDollars);
            Assert.Equal(7000, s.LostDollars);
            Assert.Equal(4000, s.ExtractedDollars);
            Assert.Equal(12000, s.HaulGoalDollars);
            Assert.Equal(25, s.BombRefillPct);
            Assert.Equal(9000, s.CheckmateLossDollars);
            Assert.Equal(55, s.HealPct);
            Assert.Equal(200, s.DeliveryPermille());
            Assert.Equal(600, s.QuotaPermille());
            Assert.Equal(450, s.CheckmateLinePermille());

            Assert.Null(MeetingGaugeSnapshot.FromData(null));
            Assert.Null(MeetingGaugeSnapshot.FromData(new[] { 1, 2, 3 }));

            MeetingGaugeSnapshot legacy = MeetingGaugeSnapshot.FromData(
                new[] { 350, 20000, 10, 20, 30, 40, 5, 7000, 4000, 12000, 25 });
            Assert.Equal(-1, legacy.CheckmateLossDollars);
            Assert.Equal(-1, legacy.CheckmateLinePermille());
            Assert.Equal(0, legacy.HealPct);
        }
    }
}
