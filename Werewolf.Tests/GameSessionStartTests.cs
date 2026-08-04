using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GameSessionStartTests : IDisposable
    {
        private const long Now = 1_000_000;

        private readonly List<OutboundMessage> _sent = new List<OutboundMessage>();
        private readonly List<SessionEvent> _events = new List<SessionEvent>();
        private readonly List<(string Line, bool Secret)> _log = new List<(string, bool)>();

        public GameSessionStartTests()
        {
            WLog.Sink = (line, secret) => _log.Add((line, secret));
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private GameSession CreateSession()
        {
            var session = new GameSession();
            session.OnSend += m => _sent.Add(m);
            session.OnSessionEvent += e => _events.Add(e);
            return session;
        }

        private static List<WPlayer> MakePlayers(int count)
        {
            var list = new List<WPlayer>(count);
            for (int i = 1; i <= count; i++)
            {
                list.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            return list;
        }

        [Fact]
        public void Start_LessThanThreePlayers_RejectedWithReason()
        {
            var session = CreateSession();

            var result = session.Start(new GameConfig(), MakePlayers(2), Now, new Random(1));

            Assert.False(result.Success);
            Assert.Equal(StartRejectReason.TooFewPlayers, result.Reason);
            Assert.Equal(GamePhase.Lobby, session.Phase);
            Assert.Empty(_sent);
            Assert.Contains(_log, e => e.Line.Contains("start_rejected"));
        }

        [Fact]
        public void Start_WhenNotInLobby_RejectedWithReason()
        {
            var session = CreateSession();
            Assert.True(session.Start(new GameConfig(), MakePlayers(5), Now, new Random(1)).Success);
            int sentAfterFirst = _sent.Count;

            var result = session.Start(new GameConfig(), MakePlayers(5), Now + 1000, new Random(2));

            Assert.False(result.Success);
            Assert.Equal(StartRejectReason.NotInLobby, result.Reason);
            Assert.Equal(sentAfterFirst, _sent.Count);
        }

        [Fact]
        public void Start_FivePlayers_EmitsFiveTargetedRoleNoticesInPlayerOrder()
        {
            var session = CreateSession();

            var config = new GameConfig { ShamanChancePercent = 0 };
            var result = session.Start(config, MakePlayers(5), Now, new Random(1));

            Assert.True(result.Success);
            var notices = _sent.Take(5).ToList();
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(160, notices[i].Code);
                Assert.Equal(MessageTarget.Actors, notices[i].Target);
                Assert.Equal(new[] { i + 1 }, notices[i].TargetActors);
                var roleByte = Assert.IsType<byte>(Assert.Single(notices[i].Payload));
                Assert.InRange(roleByte, (byte)0, (byte)3);
            }
        }

        [Fact]
        public void Start_RoleNoticePayload_MatchesTable_ButBlackCatSeesVillager()
        {
            var session = CreateSession();
            session.ReserveForcedRole(2, Role.BlackCat);

            session.Start(new GameConfig(), MakePlayers(5), Now, new Random(1));

            Assert.Equal(Role.BlackCat, session.Players.Single(p => p.ActorNumber == 2).Role);
            var catNotice = _sent.Single(m => m.Code == 160 && m.TargetActors.Single() == 2);
            Assert.Equal((byte)Role.Villager, Assert.Single(catNotice.Payload));
            foreach (var player in session.Players.Where(p => p.Role != Role.BlackCat))
            {
                var notice = _sent.Single(m => m.Code == 160 && m.TargetActors.Single() == player.ActorNumber);
                Assert.Equal((byte)player.Role, Assert.Single(notice.Payload));
            }
        }

        [Fact]
        public void Start_MessageOrder_RoleNotices_TeammatesReveal_GameStart_PhaseChanged()
        {
            var session = CreateSession();
            var config = new GameConfig { WerewolfCount = 2, ShamanChancePercent = 0 };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            var codes = _sent.Select(m => (int)m.Code).ToList();
            Assert.Equal(new[] { 160, 160, 160, 160, 160, 162, 162, 170, 172 }, codes);

            var wolves = session.Players.Where(p => p.Role == Role.Werewolf)
                .Select(p => p.ActorNumber).OrderBy(a => a).ToArray();
            var reveals = _sent.Where(m => m.Code == 162).ToList();
            Assert.Equal(wolves, reveals.Select(r => r.TargetActors.Single()).OrderBy(a => a).ToArray());
            foreach (var reveal in reveals)
            {
                Assert.Equal(2, reveal.Payload.Length);
                var listed = Assert.IsType<int[]>(reveal.Payload[0]);
                var roles = Assert.IsType<byte[]>(reveal.Payload[1]);
                Assert.Equal(wolves, listed.OrderBy(a => a).ToArray());
                Assert.Equal(listed.Length, roles.Length);
                Assert.All(roles, r => Assert.Equal((byte)Role.Werewolf, r));
            }
        }

        [Fact]
        public void Start_SingleWerewolf_NoTeammatesReveal()
        {
            var session = CreateSession();
            var config = new GameConfig
            {
                WerewolfCount = 1,
                BlackCatChancePercent = 0,
                BomberChancePercent = 0,
            };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            Assert.DoesNotContain(_sent, m => m.Code == 162);
            Assert.Equal(new[] { 160, 160, 160, 160, 160, 170, 172 },
                _sent.Select(m => (int)m.Code).ToArray());
        }

        [Fact]
        public void Start_GameStartPayload_CarriesEndTimeAndConfigAndBlackCatPossibleFlag()
        {
            var session = CreateSession();
            var config = new GameConfig
            {
                RoundSeconds = 600,
                BlackCatRevealDelaySec = 45,
                WerewolfCount = 3,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            var start = _sent.Single(m => m.Code == 170);
            Assert.Equal(MessageTarget.All, start.Target);
            Assert.Equal(6, start.Payload.Length);
            Assert.Equal(Now + 600_000L, Assert.IsType<long>(start.Payload[0]));
            Assert.Equal(600, Assert.IsType<int>(start.Payload[1]));
            Assert.Equal((byte)3, Assert.IsType<byte>(start.Payload[2]));
            Assert.Equal((byte)1, Assert.IsType<byte>(start.Payload[3]));
            Assert.Equal(45, Assert.IsType<int>(start.Payload[4]));
            Assert.Equal((byte)0, Assert.IsType<byte>(start.Payload[5]));
        }

        [Fact]
        public void Start_GameStartPayload_DebugMode_MirrorsHostConfig()
        {
            var session = CreateSession();
            var config = new GameConfig { DebugMode = true };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            var start = _sent.Single(m => m.Code == 170);
            Assert.Equal((byte)1, Assert.IsType<byte>(start.Payload[5]));
        }

        [Fact]
        public void Start_GameStartPayload_BlackCatChanceZero_PossibleFlagFalse()
        {
            var session = CreateSession();
            var config = new GameConfig { BlackCatChancePercent = 0, BomberChancePercent = 0 };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            var start = _sent.Single(m => m.Code == 170);
            Assert.Equal((byte)0, Assert.IsType<byte>(start.Payload[3]));
            Assert.Equal(0, session.Players.Count(p => p.Role == Role.BlackCat));
        }

        [Fact]
        public void Start_GameStartPayload_NoVillagerSlot_PossibleFlagFalse()
        {
            var session = CreateSession();
            var config = new GameConfig
            {
                WerewolfCount = 4,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            var start = _sent.Single(m => m.Code == 170);
            Assert.Equal((byte)0, Assert.IsType<byte>(start.Payload[3]));
            Assert.Equal(0, session.Players.Count(p => p.Role == Role.BlackCat));
        }

        [Fact]
        public void Start_GameStartPayload_BlackCatChanceFullAndSlotAvailable_PossibleFlagTrue()
        {
            var session = CreateSession();
            var config = new GameConfig
            {
                WerewolfCount = 2,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };

            session.Start(config, MakePlayers(4), Now, new Random(1));

            var start = _sent.Single(m => m.Code == 170);
            Assert.Equal((byte)1, Assert.IsType<byte>(start.Payload[3]));
            Assert.Equal(1, session.Players.Count(p => p.Role == Role.BlackCat));
        }

        [Fact]
        public void Start_GameStartPayload_PossibleFlagTrue_EvenWhenActualIsZeroDueToLottery()
        {
            var session = CreateSession();
            var config = new GameConfig
            {
                WerewolfCount = 2,
                BlackCatChancePercent = 1,
                BomberChancePercent = 0,
            };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            var start = _sent.Single(m => m.Code == 170);
            Assert.Equal((byte)1, Assert.IsType<byte>(start.Payload[3]));
            Assert.Equal(0, session.Players.Count(p => p.Role == Role.BlackCat));
        }

        [Fact]
        public void Start_BlackCatActualCount_DoesNotAppearInNonSecretLog()
        {
            var session = CreateSession();

            session.Start(new GameConfig(), MakePlayers(5), Now, new Random(1));

            var countLines = _log.Where(e => e.Line.Contains("blackcats=")).ToList();
            Assert.NotEmpty(countLines);
            Assert.All(countLines, e => Assert.True(e.Secret));
        }

        [Fact]
        public void Start_TransitionsLobbyToPlay_AndBroadcastsPhaseChanged()
        {
            var session = CreateSession();
            var config = new GameConfig { RoundSeconds = 600 };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            Assert.Equal(GamePhase.Play, session.Phase);
            var phase = _sent.Single(m => m.Code == 172);
            Assert.Equal(MessageTarget.All, phase.Target);
            Assert.Equal((byte)GamePhase.Play, Assert.IsType<byte>(phase.Payload[0]));
            Assert.Equal(Now, Assert.IsType<long>(phase.Payload[1]));
            Assert.Equal(Now + 600_000L, Assert.IsType<long>(phase.Payload[2]));
            Assert.Contains(_log, e => e.Line.Contains("phase Lobby->Play"));
            Assert.Contains(_events, e => e.Kind == SessionEventKind.PhaseChanged
                && e.Phase == GamePhase.Play && e.RoundEndUnixMs == Now + 600_000L);
        }

        [Fact]
        public void Start_NoBroadcastCarriesRoleTableOrRoleBytes()
        {
            var session = CreateSession();
            var config = new GameConfig { WerewolfCount = 2 };

            session.Start(config, MakePlayers(7), Now, new Random(1));

            foreach (var message in _sent.Where(m => m.Target == MessageTarget.All))
            {
                Assert.Contains((int)message.Code, new[] { 170, 172 });
                Assert.DoesNotContain(message.Payload, p => p is Array);
            }
            foreach (var message in _sent.Where(m => m.Code == 160 || m.Code == 162))
            {
                Assert.Equal(MessageTarget.Actors, message.Target);
                Assert.Single(message.TargetActors);
            }
        }

        [Fact]
        public void Start_RoleTableHeldInSessionMemory()
        {
            var session = CreateSession();
            var config = new GameConfig { WerewolfCount = 2, ShamanChancePercent = 0 };

            session.Start(config, MakePlayers(5), Now, new Random(1));

            Assert.Equal(5, session.Players.Count);
            Assert.Equal(2, session.Players.Count(p => p.Role == Role.Werewolf));
            Assert.Equal(0, session.Players.Count(p => p.Role == Role.BlackCat));
            Assert.Equal(0, session.Players.Count(p => p.Role == Role.Bomber));
            Assert.Equal(3, session.Players.Count(p => p.Role == Role.Villager));
            Assert.All(session.Players, p => Assert.True(p.Alive));
        }

        [Fact]
        public void ReserveForcedRole_IsConsumedByStart()
        {
            var session = CreateSession();
            session.ReserveForcedRole(4, Role.Werewolf);

            session.Start(new GameConfig(), MakePlayers(5), Now, new Random(1));

            Assert.Equal(Role.Werewolf, session.Players.Single(p => p.ActorNumber == 4).Role);
        }

        [Fact]
        public void Start_WerewolfCountExceedsPlayerCount_RejectedWithReason()
        {
            var session = CreateSession();
            var config = new GameConfig { WerewolfCount = 5 };

            var result = session.Start(config, MakePlayers(5), Now, new Random(1));

            Assert.False(result.Success);
            Assert.Equal(StartRejectReason.InvalidConfig, result.Reason);
            Assert.Equal(GamePhase.Lobby, session.Phase);
            Assert.Empty(_sent);
            Assert.Contains(ConfigIssue.WerewolfCountExceedsPlayers, result.Issues);
            Assert.Contains(_log, e => e.Line.Contains("start_rejected"));
        }

        [Fact]
        public void Start_ValidConfig_IssuesEmpty()
        {
            var session = CreateSession();

            var result = session.Start(new GameConfig(), MakePlayers(5), Now, new Random(1));

            Assert.True(result.Success);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void Start_NullArguments_Throw()
        {
            var session = CreateSession();

            Assert.Throws<ArgumentNullException>(
                () => session.Start(null, MakePlayers(5), Now, new Random(1)));
            Assert.Throws<ArgumentNullException>(
                () => session.Start(new GameConfig(), null, Now, new Random(1)));
            Assert.Throws<ArgumentNullException>(
                () => session.Start(new GameConfig(), MakePlayers(5), Now, null));
        }
    }
}
