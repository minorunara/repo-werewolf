using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GameSessionDebugApiTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int RoundSeconds = 600;
        private const long RoundEnd = Now + 600_000L;

        private readonly List<OutboundMessage> _sent = new List<OutboundMessage>();

        public GameSessionDebugApiTests()
        {
            WLog.Sink = null;
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

            var config = new GameConfig { RoundSeconds = RoundSeconds, BlackCatRevealDelaySec = 60 };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            session.OnSend += m => _sent.Add(m);
            return session;
        }

        [Fact]
        public void ForceExpireTimer_InPlay_NextTickConfirmsWerewolfWin()
        {
            var session = CreateStartedSession();

            session.ForceExpireTimer(Now + 1000);
            session.Tick(Now + 1001);

            Assert.NotNull(session.Winner);
            Assert.Equal(Team.Werewolves, session.Winner.WinningTeam);
            Assert.Equal(WinReason.TimerExpired, session.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, session.Phase);
        }

        [Fact]
        public void ForceExpireTimer_DuringMeeting_DoesNotExpireUntilPlayResumes()
        {
            var session = CreateStartedSession();
            Assert.True(session.RequestPhaseChange(GamePhase.Meeting, Now + 1000).Success);

            session.ForceExpireTimer(Now + 2000);
            session.Tick(Now + 3000);

            Assert.Null(session.Winner);
            Assert.Equal(GamePhase.Meeting, session.Phase);

            Assert.True(session.RequestPhaseChange(GamePhase.Play, Now + 4000).Success);
            session.Tick(Now + 4001);

            Assert.NotNull(session.Winner);
            Assert.Equal(Team.Werewolves, session.Winner.WinningTeam);
        }

        [Fact]
        public void ForceExpireTimer_BeforeStartOrAfterGameOver_IsIgnored()
        {
            var idle = new GameSession();
            idle.ForceExpireTimer(Now);
            idle.Tick(Now + 1);
            Assert.Null(idle.Winner);
            Assert.Equal(GamePhase.Lobby, idle.Phase);

            var session = CreateStartedSession();
            session.ForceExpireTimer(Now + 1000);
            session.Tick(Now + 1001);
            Assert.Equal(WinReason.TimerExpired, session.Winner.Reason);

            _sent.Clear();
            session.ForceExpireTimer(Now + 2000);
            session.Tick(Now + 2001);
            Assert.Empty(_sent);
        }

        [Fact]
        public void RemainingMs_ReflectsTimerState()
        {
            var session = CreateStartedSession();

            Assert.Equal(600_000L, session.RemainingMs(Now));
            Assert.Equal(500_000L, session.RemainingMs(Now + 100_000));
            Assert.Equal(0L, session.RemainingMs(RoundEnd + 1));

            Assert.Equal(0L, new GameSession().RemainingMs(Now));
        }

        [Fact]
        public void BotFlow_FourBotsPlusSelf_SkipTimer_WerewolfWinWithBotsInRoleTable()
        {
            var session = new GameSession();
            var sent = new List<OutboundMessage>();
            session.OnSend += m => sent.Add(m);

            var players = new List<WPlayer> { new WPlayer { ActorNumber = 1, Name = "Me" } };
            for (int i = 1; i <= 4; i++)
            {
                players.Add(new WPlayer { ActorNumber = -i, Name = "Bot" + i, IsBot = true });
            }

            var config = new GameConfig
            {
                WerewolfCount = 2,
                RoundSeconds = RoundSeconds,
                BlackCatRevealDelaySec = 60,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };
            Assert.True(session.Start(config, players, Now, new Random(7)).Success);

            Assert.Equal(2, session.Players.Count(p => p.Role == Role.Werewolf));
            Assert.Equal(1, session.Players.Count(p => p.Role == Role.BlackCat));
            Assert.Equal(5, sent.Count(m => m.Code == 160));

            session.ForceExpireTimer(Now + 1000);
            session.Tick(Now + 1001);

            Assert.Equal(Team.Werewolves, session.Winner.WinningTeam);
            var gameOver = Assert.Single(sent, m => m.Code == 169);
            var actors = Assert.IsType<int[]>(gameOver.Payload[1]);
            Assert.Contains(-1, actors);
        }
    }
}
