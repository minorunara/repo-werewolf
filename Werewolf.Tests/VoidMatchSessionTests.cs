using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class VoidMatchSessionTests : IDisposable
    {
        private const long Now = 1_000_000;

        private readonly List<OutboundMessage> _sent = new List<OutboundMessage>();
        private readonly List<SessionEvent> _events = new List<SessionEvent>();

        public VoidMatchSessionTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private GameSession CreateStartedSession()
        {
            var session = new GameSession();
            session.ReserveForcedRole(1, Role.Werewolf);

            var players = new List<WPlayer>();
            for (int i = 1; i <= 5; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }

            var config = new GameConfig
            {
                RoundSeconds = 600,
                BlackCatRevealDelaySec = 60,
                ShamanChancePercent = 0,
            };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            session.OnSend += m => _sent.Add(m);
            session.OnSessionEvent += e => _events.Add(e);
            return session;
        }

        private OutboundMessage GameOverMessage()
            => _sent.SingleOrDefault(m => m.Code == WWEventCodes.GameOver);

        [Fact]
        public void 無効試合は勝者なし番兵で169を全員へ配信する()
        {
            var session = CreateStartedSession();

            session.VoidMatch(Now + 1000);

            OutboundMessage gameOver = GameOverMessage();
            Assert.NotNull(gameOver);
            Assert.Equal(MessageTarget.All, gameOver.Target);
            Assert.Equal(TeamCodes.VoidMatch, (byte)gameOver.Payload[0]);

            var actors = (int[])gameOver.Payload[1];
            var roles = (byte[])gameOver.Payload[2];
            Assert.Equal(5, actors.Length);
            Assert.Equal(actors.Length, roles.Length);
        }

        [Fact]
        public void 無効試合はGameOverフェーズへ遷移しVoidedが立つ()
        {
            var session = CreateStartedSession();

            session.VoidMatch(Now + 1000);

            Assert.True(session.Voided);
            Assert.Null(session.Winner);
            Assert.Equal(GamePhase.GameOver, session.Phase);
            Assert.Contains(_events, e => e.Kind == SessionEventKind.MatchVoided);
            Assert.DoesNotContain(_events, e => e.Kind == SessionEventKind.WinnerConfirmed);
        }

        [Fact]
        public void 無効試合の多重呼び出しは無視される()
        {
            var session = CreateStartedSession();

            session.VoidMatch(Now + 1000);
            session.VoidMatch(Now + 2000);

            Assert.Single(_sent, m => m.Code == WWEventCodes.GameOver);
            Assert.Single(_events, e => e.Kind == SessionEventKind.MatchVoided);
        }

        [Fact]
        public void 無効試合の後は死亡による勝敗が確定しない()
        {
            var session = CreateStartedSession();
            session.VoidMatch(Now + 1000);
            _sent.Clear();

            for (int actor = 2; actor <= 5; actor++)
            {
                session.RecordDeath(actor, Now + 2000);
            }

            Assert.Null(session.Winner);
            Assert.True(session.Voided);
            Assert.DoesNotContain(_sent, m => m.Code == WWEventCodes.GameOver);
        }

        [Fact]
        public void 勝敗確定後は無効試合にできない()
        {
            var session = CreateStartedSession();

            session.RecordDeath(1, Now + 1000);
            session.ConfirmPendingWin(Now + 1000 + EradicationCeremony.CeremonyMs);
            Assert.NotNull(session.Winner);
            _sent.Clear();

            session.VoidMatch(Now + 2000);

            Assert.False(session.Voided);
            Assert.DoesNotContain(_sent, m => m.Code == WWEventCodes.GameOver);
        }

        [Fact]
        public void 会議中でも無効試合にできる()
        {
            var session = CreateStartedSession();
            Assert.True(session.RequestPhaseChange(GamePhase.Meeting, Now + 500).Success);

            session.VoidMatch(Now + 1000);

            Assert.True(session.Voided);
            Assert.Equal(GamePhase.GameOver, session.Phase);
        }

        [Fact]
        public void 無効試合の結果行はどれも勝利側にならない()
        {
            var rows = ResultModel.Build(
                TeamCodes.VoidMatch,
                new[] { 1, 2, 3 },
                new[] { (byte)Role.Werewolf, (byte)Role.Villager, (byte)Role.BlackCat },
                new Dictionary<int, DeathCause>(),
                a => "P" + a);

            Assert.Equal(3, rows.Count);
            Assert.All(rows, r => Assert.False(r.IsWinningSide));
        }

        [Fact]
        public void 無効試合のダイジェスト行は無効試合と表示する()
        {
            var entries = new List<DigestEntry>
            {
                new DigestEntry(DigestKind.MatchEnd, 300, 0,
                    TeamCodes.VoidMatch, ResultDigest.ReasonUnknown),
            };

            List<string> lines = ResultDigestText.FormatLines(entries, a => "P" + a);

            Assert.Single(lines);
            Assert.Contains(Texts.Get(TextId.ResultBannerVoid), lines[0]);
        }
    }
}
