using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GameSessionRunTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int RoundSeconds = 600;
        private const long RoundEnd = Now + 600_000L;
        private const int DelaySec = 60;

        private readonly List<OutboundMessage> _sent = new List<OutboundMessage>();
        private readonly List<SessionEvent> _events = new List<SessionEvent>();
        private readonly List<(string Line, bool Secret)> _log = new List<(string, bool)>();

        public GameSessionRunTests()
        {
            WLog.Sink = (line, secret) => _log.Add((line, secret));
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private GameSession CreateStartedSession()
        {
            var session = new GameSession();
            session.ReserveForcedRole(1, Role.Werewolf);
            session.ReserveForcedRole(2, Role.BlackCat);

            var players = new List<WPlayer>();
            for (int i = 1; i <= 5; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }

            var config = new GameConfig
            {
                RoundSeconds = RoundSeconds,
                BlackCatRevealDelaySec = DelaySec,
                ShamanChancePercent = 0,
            };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            session.OnSend += m => _sent.Add(m);
            session.OnSessionEvent += e => _events.Add(e);
            return session;
        }

        [Fact]
        public void Tick_AfterDelay_EmitsSelfAwarenessOnce()
        {
            var session = CreateStartedSession();

            session.Tick(Now + DelaySec * 1000L);
            session.Tick(Now + DelaySec * 1000L + 100);

            var reveal = Assert.Single(_sent, m => m.Code == 161);
            Assert.Equal(MessageTarget.Actors, reveal.Target);
            Assert.Equal(new[] { 2 }, reveal.TargetActors);
            Assert.Equal((byte)Role.BlackCat, Assert.Single(reveal.Payload));
        }

        [Fact]
        public void Tick_DuringMeeting_DefersSelfAwarenessUntilPlayResumes()
        {
            var session = CreateStartedSession();
            Assert.True(session.RequestPhaseChange(GamePhase.Meeting, Now + 1000).Success);

            session.Tick(Now + DelaySec * 1000L);
            session.Tick(Now + DelaySec * 1000L + 5000);
            Assert.DoesNotContain(_sent, m => m.Code == 161);

            Assert.True(session.RequestPhaseChange(GamePhase.Play, Now + DelaySec * 1000L + 10_000).Success);
            session.Tick(Now + DelaySec * 1000L + 10_001);

            var reveal = Assert.Single(_sent, m => m.Code == 161);
            Assert.Equal(new[] { 2 }, reveal.TargetActors);
        }

        [Fact]
        public void Tick_SelfAwareness_DoesNotBroadcastAnythingToAll()
        {
            var session = CreateStartedSession();

            session.Tick(Now + DelaySec * 1000L);

            var msg = Assert.Single(_sent);
            Assert.Equal(161, msg.Code);
            Assert.Equal(MessageTarget.Actors, msg.Target);
        }

        [Fact]
        public void Tick_TimerExpiry_ConfirmsWerewolfWin_WithFullSequence()
        {
            var session = CreateStartedSession();

            session.Tick(RoundEnd);

            Assert.NotNull(session.Winner);
            Assert.Equal(Team.Werewolves, session.Winner.WinningTeam);
            Assert.Equal(WinReason.TimerExpired, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);

            Assert.Equal(new[] { 161, 169, 172 }, _sent.Select(m => (int)m.Code).ToArray());

            var gameOver = _sent.Single(m => m.Code == 169);
            Assert.Equal(MessageTarget.All, gameOver.Target);
            Assert.Equal((byte)Team.Werewolves, Assert.IsType<byte>(gameOver.Payload[0]));
            var actors = Assert.IsType<int[]>(gameOver.Payload[1]);
            var roles = Assert.IsType<byte[]>(gameOver.Payload[2]);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, actors);
            Assert.Equal((byte)Role.Werewolf, roles[0]);
            Assert.Equal((byte)Role.BlackCat, roles[1]);
            Assert.All(roles.Skip(2), r => Assert.Equal((byte)Role.Villager, r));

            var phase = _sent.Single(m => m.Code == 172);
            Assert.Equal((byte)GamePhase.GameOver, Assert.IsType<byte>(phase.Payload[0]));

            Assert.Contains(_events, e => e.Kind == SessionEventKind.WinnerConfirmed
                && e.Winner.Reason == WinReason.TimerExpired);
        }

        [Fact]
        public void Tick_AfterGameOver_DoesNotJudgeOrSendAgain()
        {
            var session = CreateStartedSession();
            session.Tick(RoundEnd);
            int sentCount = _sent.Count;

            session.Tick(RoundEnd + 10_000);

            Assert.Equal(sentCount, _sent.Count);
        }

        [Fact]
        public void RecordDeath_Villager_EmitsDeathNoticeWithCauseOther_GameContinues()
        {
            var session = CreateStartedSession();

            session.RecordDeath(3, Now + 1000);

            var died = Assert.Single(_sent, m => m.Code == 168);
            Assert.Equal(MessageTarget.All, died.Target);
            Assert.Equal(3, Assert.IsType<int>(died.Payload[0]));
            Assert.Equal((byte)DeathCause.Other, Assert.IsType<byte>(died.Payload[1]));

            var player = session.Players.Single(p => p.ActorNumber == 3);
            Assert.False(player.Alive);
            Assert.Equal(DeathCause.Other, player.DeathCause);

            Assert.Null(session.Winner);
            Assert.Equal(GamePhase.Play, session.Phase);
            Assert.Contains(_events, e => e.Kind == SessionEventKind.PlayerDied
                && e.ActorNumber == 3 && e.DeathCause == DeathCause.Other);
        }

        [Fact]
        public void RecordDeath_WithVoteMark_RecordsVote_AndConsumesMark()
        {
            var session = CreateStartedSession();
            session.MarkNextDeathAsVote(3);

            session.RecordDeath(3, Now + 1000);
            session.RecordDeath(4, Now + 2000);

            var death3 = _sent.Single(m => m.Code == 168 && (int)m.Payload[0] == 3);
            Assert.Equal((byte)DeathCause.Vote, death3.Payload[1]);
            Assert.Equal(DeathCause.Vote, session.Players.Single(p => p.ActorNumber == 3).DeathCause);

            var death4 = _sent.Single(m => m.Code == 168 && (int)m.Payload[0] == 4);
            Assert.Equal((byte)DeathCause.Other, death4.Payload[1]);
        }

        [Fact]
        public void RecordDeath_Duplicate_IsIgnored()
        {
            var session = CreateStartedSession();

            session.RecordDeath(3, Now + 1000);
            session.RecordDeath(3, Now + 2000);

            Assert.Single(_sent, m => m.Code == 168);
        }

        [Fact]
        public void RecordDeath_UnknownActor_IsIgnoredWithLog()
        {
            var session = CreateStartedSession();

            session.RecordDeath(99, Now + 1000);

            Assert.DoesNotContain(_sent, m => m.Code == 168);
            Assert.Contains(_log, e => e.Line.Contains("drop"));
        }

        [Fact]
        public void RecordDeath_WerewolfEradicated_VillagersWin_SequenceObservable()
        {
            var session = CreateStartedSession();

            session.RecordDeath(1, Now + 1000);

            Assert.Equal(new[] { 168, 169, 172 }, _sent.Select(m => (int)m.Code).ToArray());
            Assert.Equal(Team.Villagers, session.Winner.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);
        }

        [Fact]
        public void RecordDeath_BlackCatOnly_GameContinues()
        {
            var session = CreateStartedSession();

            session.RecordDeath(2, Now + 1000);

            Assert.Null(session.Winner);
            Assert.Equal(GamePhase.Play, session.Phase);
        }

        [Fact]
        public void RecordDeath_AfterWinnerConfirmed_IsIgnored()
        {
            var session = CreateStartedSession();
            session.Tick(RoundEnd);
            int sentCount = _sent.Count;

            session.RecordDeath(3, RoundEnd + 1000);

            Assert.Equal(sentCount, _sent.Count);
            Assert.True(session.Players.Single(p => p.ActorNumber == 3).Alive);
        }

        [Fact]
        public void NotifyPlayerLeft_LastWerewolf_VillagersWin_WithoutDeathNotice()
        {
            var session = CreateStartedSession();

            session.NotifyPlayerLeft(1, Now + 1000);

            Assert.Equal(new[] { 169, 172 }, _sent.Select(m => (int)m.Code).ToArray());
            Assert.Equal(Team.Villagers, session.Winner.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);

            var player = session.Players.Single(p => p.ActorNumber == 1);
            Assert.False(player.Alive);
            Assert.Null(player.DeathCause);
        }

        [Fact]
        public void NotifyPlayerLeft_LastVillager_WerewolvesWin()
        {
            var session = CreateStartedSession();
            session.RecordDeath(3, Now + 1000);
            session.RecordDeath(4, Now + 2000);

            session.NotifyPlayerLeft(5, Now + 3000);

            Assert.Equal(Team.Werewolves, session.Winner.WinningTeam);
            Assert.Equal(WinReason.VillagersEradicated, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);
        }

        [Fact]
        public void NotifyPlayerLeft_Villager_GameContinues_WithoutAnyMessage()
        {
            var session = CreateStartedSession();

            session.NotifyPlayerLeft(3, Now + 1000);

            Assert.Null(session.Winner);
            Assert.Equal(GamePhase.Play, session.Phase);
            Assert.Empty(_sent);
            Assert.False(session.Players.Single(p => p.ActorNumber == 3).Alive);
        }

        [Fact]
        public void NotifyPlayerLeft_DuringMeeting_ConfirmsWinner()
        {
            var session = CreateStartedSession();
            Assert.True(session.RequestPhaseChange(GamePhase.Meeting, Now + 1000).Success);

            session.NotifyPlayerLeft(1, Now + 2000);

            Assert.Equal(Team.Villagers, session.Winner.WinningTeam);
            Assert.Equal(GamePhase.GameOver, session.Phase);
        }

        [Fact]
        public void NotifyPlayerLeft_UnknownOrDeadActor_IsIgnored()
        {
            var session = CreateStartedSession();
            session.RecordDeath(3, Now + 1000);
            int sentCount = _sent.Count;

            session.NotifyPlayerLeft(99, Now + 2000);
            session.NotifyPlayerLeft(3, Now + 3000);

            Assert.Equal(sentCount, _sent.Count);
            Assert.Null(session.Winner);
            Assert.Equal(DeathCause.Other, session.Players.Single(p => p.ActorNumber == 3).DeathCause);
        }

        [Fact]
        public void NotifyPlayerLeft_AfterGameOver_IsIgnored()
        {
            var session = CreateStartedSession();
            session.Tick(RoundEnd);
            int sentCount = _sent.Count;

            session.NotifyPlayerLeft(3, RoundEnd + 1000);

            Assert.Equal(sentCount, _sent.Count);
            Assert.Equal(WinReason.TimerExpired, session.Winner.Reason);
            Assert.True(session.Players.Single(p => p.ActorNumber == 3).Alive);
        }

        [Fact]
        public void NotifyExtractionOutcome_Completed_VillagersWin()
        {
            var session = CreateStartedSession();

            session.NotifyExtractionOutcome(completed: true, failed: false, nowUnixMs: Now + 1000);

            Assert.Equal(Team.Villagers, session.Winner.WinningTeam);
            Assert.Equal(WinReason.ExtractionCompleted, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);
            Assert.Contains(_sent, m => m.Code == 169);
        }

        [Fact]
        public void NotifyExtractionOutcome_Failed_WerewolvesWin()
        {
            var session = CreateStartedSession();

            session.NotifyExtractionOutcome(completed: false, failed: true, nowUnixMs: Now + 1000);

            Assert.Equal(Team.Werewolves, session.Winner.WinningTeam);
            Assert.Equal(WinReason.ExtractionFailed, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);
        }

        [Fact]
        public void NotifyExtractionOutcome_AfterGameOver_IsIgnored()
        {
            var session = CreateStartedSession();
            session.Tick(RoundEnd);
            int sentCount = _sent.Count;

            session.NotifyExtractionOutcome(true, false, RoundEnd + 1000);

            Assert.Equal(sentCount, _sent.Count);
            Assert.Equal(WinReason.TimerExpired, session.Winner.Reason);
        }

        [Fact]
        public void RequestPhaseChange_PlayToMeeting_StopsTimerExpiry()
        {
            var session = CreateStartedSession();

            var result = session.RequestPhaseChange(GamePhase.Meeting, Now + 100_000);

            Assert.True(result.Success);
            Assert.Equal(GamePhase.Meeting, session.Phase);
            var phase = _sent.Single(m => m.Code == 172);
            Assert.Equal((byte)GamePhase.Meeting, phase.Payload[0]);

            session.Tick(RoundEnd + 999_999);
            Assert.Null(session.Winner);
            Assert.Equal(GamePhase.Meeting, session.Phase);
        }

        [Fact]
        public void RequestPhaseChange_MeetingToPlay_ExtendsRoundEndByMeetingElapsed()
        {
            var session = CreateStartedSession();
            session.RequestPhaseChange(GamePhase.Meeting, Now + 100_000);

            var result = session.RequestPhaseChange(GamePhase.Play, Now + 160_000);

            Assert.True(result.Success);
            Assert.Equal(GamePhase.Play, session.Phase);

            var resume = _sent.Where(m => m.Code == 172).Last();
            Assert.Equal((byte)GamePhase.Play, resume.Payload[0]);
            Assert.Equal(RoundEnd + 60_000L, Assert.IsType<long>(resume.Payload[2]));

            session.Tick(RoundEnd + 59_999);
            Assert.Null(session.Winner);
            session.Tick(RoundEnd + 60_000);
            Assert.Equal(WinReason.TimerExpired, session.Winner.Reason);
        }

        [Fact]
        public void RequestPhaseChange_ForcedGameOver_FromPlay_Succeeds()
        {
            var session = CreateStartedSession();

            var result = session.RequestPhaseChange(GamePhase.GameOver, Now + 1000);

            Assert.True(result.Success);
            Assert.Equal(GamePhase.GameOver, session.Phase);
            Assert.Null(session.Winner);
            Assert.DoesNotContain(_sent, m => m.Code == 169);
        }

        [Fact]
        public void RequestPhaseChange_InvalidTransitions_RejectedWithoutSideEffects()
        {
            var session = CreateStartedSession();

            var toSame = session.RequestPhaseChange(GamePhase.Play, Now + 1000);
            Assert.False(toSame.Success);
            Assert.Equal(PhaseChangeRejectReason.InvalidTransition, toSame.Reason);

            var toLobby = session.RequestPhaseChange(GamePhase.Lobby, Now + 1000);
            Assert.False(toLobby.Success);

            Assert.Empty(_sent);
        }

        [Fact]
        public void RequestPhaseChange_AfterGameOver_Rejected()
        {
            var session = CreateStartedSession();
            session.Tick(RoundEnd);
            int sentCount = _sent.Count;

            var result = session.RequestPhaseChange(GamePhase.Play, RoundEnd + 1000);

            Assert.False(result.Success);
            Assert.Equal(sentCount, _sent.Count);
        }

        [Fact]
        public void RequestPhaseChange_FromLobby_Rejected()
        {
            var session = new GameSession();
            var sent = new List<OutboundMessage>();
            session.OnSend += sent.Add;

            var result = session.RequestPhaseChange(GamePhase.Meeting, Now);

            Assert.False(result.Success);
            Assert.Empty(sent);
        }

        [Fact]
        public void NotifyDisclosureCondition_SendsTargetedTeammatesReveal_Once()
        {
            var session = CreateStartedSession();

            session.NotifyDisclosureCondition(DisclosureKind.BlackCatSeesWerewolves);
            session.NotifyDisclosureCondition(DisclosureKind.BlackCatSeesWerewolves);

            var reveal = Assert.Single(_sent, m => m.Code == 162);
            Assert.Equal(new[] { 2 }, reveal.TargetActors);
            Assert.Equal(new[] { 1 }, Assert.IsType<int[]>(reveal.Payload[0]));
        }
    }
}
