using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class PeacefulVillageTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int RoundSeconds = 600;

        private readonly List<OutboundMessage> _sent = new List<OutboundMessage>();
        private readonly List<SessionEvent> _events = new List<SessionEvent>();
        private readonly List<(string Line, bool Secret)> _log = new List<(string, bool)>();

        public PeacefulVillageTests()
        {
            WLog.Sink = (line, secret) => _log.Add((line, secret));
        }

        public void Dispose()
        {
            WLog.Sink = null;
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

        private static List<WPlayer> Make(params Role[] roles)
        {
            var list = new List<WPlayer>(roles.Length);
            for (int i = 0; i < roles.Length; i++)
            {
                list.Add(new WPlayer { ActorNumber = i + 1, Name = "P" + (i + 1), Role = roles[i] });
            }
            return list;
        }

        private static void Kill(List<WPlayer> players, params int[] actors)
        {
            foreach (int actor in actors) players.Single(p => p.ActorNumber == actor).Alive = false;
        }

        private static int CountWolfTeam(IEnumerable<WPlayer> players)
            => players.Count(p => RoleDistribution.TeamOf(p.Role) == Team.Werewolves);

        [Fact]
        public void Assign_Chance100_PutsNobodyInWolfSlots_KeepsPublicN()
        {
            var players = MakePlayers(6);
            var config = new GameConfig
            {
                WerewolfCount = 2, PeacefulVillageChancePercent = 100,
                BomberChancePercent = 100, BlackCatChancePercent = 100, ShamanChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(7));

            Assert.True(result.Peaceful);
            Assert.Equal(0, CountWolfTeam(players));
            Assert.Equal(0, result.Werewolves);
            Assert.Equal(0, result.Bombers);
            Assert.Equal(0, result.BlackCats);
            Assert.Equal(2, result.WerewolfSlots);
            Assert.All(players, p => Assert.Equal(Role.Villager, p.Role));
        }

        [Fact]
        public void Assign_Peaceful_ShamanStillMutates()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 1, PeacefulVillageChancePercent = 100, ShamanChancePercent = 100,
            };

            var result = RoleAssigner.Assign(players, config, new Random(3));

            Assert.True(result.Peaceful);
            Assert.Equal(1, result.Shamans);
            Assert.Equal(1, players.Count(p => p.Role == Role.Shaman));
            Assert.Equal(0, CountWolfTeam(players));
        }

        [Fact]
        public void Assign_Chance0_NeverPeaceful()
        {
            var config = new GameConfig { WerewolfCount = 1, PeacefulVillageChancePercent = 0, ShamanChancePercent = 0 };
            for (int seed = 0; seed < 50; seed++)
            {
                var players = MakePlayers(5);
                var result = RoleAssigner.Assign(players, config, new Random(seed));
                Assert.False(result.Peaceful);
                Assert.Equal(1, result.Werewolves);
                Assert.Equal(1, result.WerewolfSlots);
            }
        }

        [Fact]
        public void Assign_ForcedRoles_SuppressPeacefulRoll()
        {
            var players = MakePlayers(5);
            var config = new GameConfig { WerewolfCount = 1, PeacefulVillageChancePercent = 100, ShamanChancePercent = 0 };
            var forced = new Dictionary<int, Role> { { 3, Role.Villager } };

            var result = RoleAssigner.Assign(players, config, new Random(1), forced);

            Assert.False(result.Peaceful);
            Assert.Equal(1, result.Werewolves);
        }

        [Fact]
        public void Assign_ForcePeaceful_BypassesChance()
        {
            var players = MakePlayers(5);
            var config = new GameConfig { WerewolfCount = 2, PeacefulVillageChancePercent = 0, ShamanChancePercent = 0 };

            var result = RoleAssigner.Assign(players, config, new Random(1), null, forcePeaceful: true);

            Assert.True(result.Peaceful);
            Assert.Equal(0, CountWolfTeam(players));
            Assert.Equal(2, result.WerewolfSlots);
        }

        [Theory]
        [InlineData(Role.Werewolf)]
        [InlineData(Role.Bomber)]
        [InlineData(Role.BlackCat)]
        public void Assign_ForcePeaceful_WithForcedWolfSide_KeepsWolves(Role forcedRole)
        {
            var players = MakePlayers(5);
            var config = new GameConfig { WerewolfCount = 1, PeacefulVillageChancePercent = 100, ShamanChancePercent = 0 };
            var forced = new Dictionary<int, Role> { { 3, forcedRole } };

            var result = RoleAssigner.Assign(players, config, new Random(1), forced, forcePeaceful: true);

            Assert.False(result.Peaceful);
            Assert.Equal(forcedRole, players.Single(p => p.ActorNumber == 3).Role);
            Assert.True(CountWolfTeam(players) >= 1);
        }

        [Theory]
        [InlineData(Role.Villager)]
        [InlineData(Role.Shaman)]
        public void Assign_ForcePeaceful_WithForcedVillagerSide_StaysPeaceful(Role forcedRole)
        {
            var players = MakePlayers(5);
            var config = new GameConfig { WerewolfCount = 2, PeacefulVillageChancePercent = 0, ShamanChancePercent = 0 };
            var forced = new Dictionary<int, Role> { { 3, forcedRole } };

            var result = RoleAssigner.Assign(players, config, new Random(1), forced, forcePeaceful: true);

            Assert.True(result.Peaceful);
            Assert.Equal(forcedRole, players.Single(p => p.ActorNumber == 3).Role);
            Assert.Equal(0, CountWolfTeam(players));
        }

        [Fact]
        public void Assign_WerewolfSlots_IsCorrectedN_EvenWhenBomberTakesASlot()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 2, BomberChancePercent = 100, PeacefulVillageChancePercent = 0, ShamanChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.False(result.Peaceful);
            Assert.Equal(1, result.Werewolves);
            Assert.Equal(1, result.Bombers);
            Assert.Equal(2, result.WerewolfSlots);
        }

        [Fact]
        public void Assign_ChanceRoughlyMatchesRate()
        {
            var config = new GameConfig { WerewolfCount = 1, PeacefulVillageChancePercent = 15, ShamanChancePercent = 0 };
            int peaceful = 0;
            const int trials = 2000;
            for (int seed = 0; seed < trials; seed++)
            {
                if (RoleAssigner.Assign(MakePlayers(5), config, new Random(seed)).Peaceful) peaceful++;
            }
            Assert.InRange(peaceful, trials * 0.10, trials * 0.20);
        }

        [Fact]
        public void Judge_Peaceful_TwoAlive_Continues_NotWerewolvesEradicated()
        {
            var players = Make(Role.Villager, Role.Villager, Role.Shaman);
            Kill(players, 1);

            Assert.Null(WinJudge.Judge(players, peaceful: true));
            Assert.Equal(WinReason.WerewolvesEradicated, WinJudge.Judge(players).Reason);
        }

        [Fact]
        public void Judge_Peaceful_OneAlive_Collapses()
        {
            var players = Make(Role.Villager, Role.Villager, Role.Villager);
            Kill(players, 1, 2);

            var result = WinJudge.Judge(players, peaceful: true);

            Assert.NotNull(result);
            Assert.Equal(WinReason.PeacefulCollapsed, result.Reason);
            Assert.Equal(Team.Werewolves, result.WinningTeam);
            Assert.True(result.Peaceful);
        }

        [Fact]
        public void Judge_Peaceful_ExtractionCompleted_Survives()
        {
            var players = Make(Role.Villager, Role.Villager, Role.Villager);
            Kill(players, 1);

            var result = WinJudge.Judge(players, extractionCompleted: true, peaceful: true);

            Assert.Equal(WinReason.PeacefulSurvived, result.Reason);
            Assert.Equal(Team.Villagers, result.WinningTeam);
        }

        [Fact]
        public void Judge_Peaceful_CollapseBeatsExtraction()
        {
            var players = Make(Role.Villager, Role.Villager, Role.Villager);
            Kill(players, 1, 2);

            var result = WinJudge.Judge(players, extractionCompleted: true, peaceful: true);

            Assert.Equal(WinReason.PeacefulCollapsed, result.Reason);
        }

        [Fact]
        public void Judge_Peaceful_TimerExpired_Collapses()
        {
            var players = Make(Role.Villager, Role.Villager, Role.Villager);

            var result = WinJudge.Judge(players, timerExpired: true, peaceful: true);

            Assert.Equal(WinReason.PeacefulCollapsed, result.Reason);
        }

        [Fact]
        public void AsPeaceful_ReadsTeamOutcomeIntoSurvivedOrCollapsed()
        {
            Assert.Equal(WinReason.PeacefulSurvived,
                new WinResult(Team.Villagers, WinReason.ExtractionCompleted).AsPeaceful().Reason);
            Assert.Equal(WinReason.PeacefulCollapsed,
                new WinResult(Team.Werewolves, WinReason.ValueCheckmate).AsPeaceful().Reason);
            var already = new WinResult(Team.Werewolves, WinReason.PeacefulCollapsed);
            Assert.Same(already, already.AsPeaceful());
        }

        [Fact]
        public void Outcome_MapsPeacefulReasonsToSentinels_OtherwiseTeam()
        {
            Assert.Equal(TeamCodes.PeacefulSurvived,
                TeamCodes.Outcome(new WinResult(Team.Villagers, WinReason.PeacefulSurvived)));
            Assert.Equal(TeamCodes.PeacefulCollapsed,
                TeamCodes.Outcome(new WinResult(Team.Werewolves, WinReason.PeacefulCollapsed)));
            Assert.Equal((byte)Team.Villagers,
                TeamCodes.Outcome(new WinResult(Team.Villagers, WinReason.ExtractionCompleted)));
            Assert.Equal((byte)Team.Werewolves,
                TeamCodes.Outcome(new WinResult(Team.Werewolves, WinReason.TimerExpired)));
        }

        [Fact]
        public void WinnerOf_MapsOutcomeCodesToWinner_NullWhenNoWinner()
        {
            Assert.Equal<Team?>(Team.Villagers, TeamCodes.WinnerOf((byte)Team.Villagers));
            Assert.Equal<Team?>(Team.Villagers, TeamCodes.WinnerOf(TeamCodes.PeacefulSurvived));
            Assert.Equal<Team?>(Team.Werewolves, TeamCodes.WinnerOf((byte)Team.Werewolves));
            Assert.Null(TeamCodes.WinnerOf(TeamCodes.PeacefulCollapsed));
            Assert.Null(TeamCodes.WinnerOf(TeamCodes.VoidMatch));
            Assert.Null(TeamCodes.WinnerOf(200));
        }

        private GameSession CreatePeacefulSession(int players = 5)
        {
            var session = new GameSession();
            session.ReserveForcedPeaceful();
            var config = new GameConfig
            {
                WerewolfCount = 2, RoundSeconds = RoundSeconds, ShamanChancePercent = 0,
                PeacefulVillageChancePercent = 0,
            };
            var startSent = new List<OutboundMessage>();
            session.OnSend += m => startSent.Add(m);
            Assert.True(session.Start(config, MakePlayers(players), Now, new Random(1)).Success);
            Assert.True(session.Peaceful);

            var start = startSent.Single(m => m.Code == WWEventCodes.GameStart);
            Assert.Equal((byte)2, Assert.IsType<byte>(start.Payload[2]));
            Assert.DoesNotContain(startSent, m => m.Code == WWEventCodes.RevealTeammates);

            _sent.Clear();
            session.OnSend += m => _sent.Add(m);
            session.OnSessionEvent += e => _events.Add(e);
            return session;
        }

        [Fact]
        public void Start_Peaceful_IsNotDisclosedInPublicOutput()
        {
            var session = new GameSession();
            session.ReserveForcedPeaceful();
            var sent = new List<OutboundMessage>();
            session.OnSend += m => sent.Add(m);
            var config = new GameConfig { WerewolfCount = 1, RoundSeconds = RoundSeconds, ShamanChancePercent = 0 };
            Assert.True(session.Start(config, MakePlayers(4), Now, new Random(1)).Success);

            Assert.True(session.Peaceful);
            Assert.All(session.Players, p => Assert.Equal(Role.Villager, p.Role));
            var start = sent.Single(m => m.Code == WWEventCodes.GameStart);
            Assert.Equal((byte)1, Assert.IsType<byte>(start.Payload[2]));
            Assert.Contains(_log, l => l.Secret && l.Line.Contains("peaceful=true"));
            Assert.DoesNotContain(_log, l => !l.Secret && l.Line.Contains("peaceful"));
        }

        [Fact]
        public void Start_ForcedPeaceful_IsConsumedByStart()
        {
            var session = CreatePeacefulSession();
            session.RequestPhaseChange(GamePhase.GameOver, Now + 1000);

            var again = new GameSession();
            var config = new GameConfig { WerewolfCount = 1, RoundSeconds = RoundSeconds, ShamanChancePercent = 0 };
            Assert.True(again.Start(config, MakePlayers(4), Now, new Random(1)).Success);
            Assert.False(again.Peaceful);
        }

        [Fact]
        public void RecordDeath_DownToOneAlive_LocksCollapse_WithPeacefulStamp()
        {
            var session = CreatePeacefulSession();
            session.RecordDeath(1, Now + 1000);
            session.RecordDeath(2, Now + 2000);
            session.RecordDeath(3, Now + 3000);
            Assert.False(session.WinLocked);

            session.RecordDeath(4, Now + 4000);

            Assert.True(session.WinLocked);
            Assert.Null(session.Winner);
            var reveal = _sent.Last();
            Assert.Equal(MessageCodes.EradicationReveal, reveal.Code);
            var data = EradicationRevealWire.FromWire(reveal.Payload);
            Assert.Equal(TeamCodes.PeacefulCollapsed, data.Outcome);
            Assert.Equal(4, data.ActorNumber);
            Assert.False(data.Vanished);
            Assert.Equal(TextId.EradicationPeacefulCollapsed, EradicationCeremony.TitleId(data.Outcome, data.Vanished));

            session.ConfirmPendingWin(Now + 4000 + EradicationCeremony.CeremonyMs);
            Assert.Equal(WinReason.PeacefulCollapsed, session.Winner.Reason);
            var gameOver = _sent.Single(m => m.Code == WWEventCodes.GameOver);
            Assert.Equal(TeamCodes.PeacefulCollapsed, Assert.IsType<byte>(gameOver.Payload[0]));
            Assert.Equal(GamePhase.GameOver, session.Phase);
            Assert.Contains(_events, e => e.Kind == SessionEventKind.WinnerConfirmed
                && e.Winner.Reason == WinReason.PeacefulCollapsed);
        }

        [Fact]
        public void NotifyPlayerLeft_DownToOneAlive_LocksCollapse_Vanished()
        {
            var session = CreatePeacefulSession(3);
            session.RecordDeath(1, Now + 1000);

            session.NotifyPlayerLeft(2, Now + 2000);

            Assert.True(session.WinLocked);
            var data = EradicationRevealWire.FromWire(_sent.Last().Payload);
            Assert.Equal(TeamCodes.PeacefulCollapsed, data.Outcome);
            Assert.True(data.Vanished);
        }

        [Fact]
        public void MailDeparture_Peaceful_Survives_AllRowsWin()
        {
            var session = CreatePeacefulSession();
            session.RecordDeath(1, Now + 1000);

            session.NotifyMailDeparture(Now + 2000);

            Assert.Equal(WinReason.PeacefulSurvived, session.Winner.Reason);
            Assert.Equal(Team.Villagers, session.Winner.WinningTeam);
            var gameOver = _sent.Single(m => m.Code == WWEventCodes.GameOver);
            Assert.Equal(TeamCodes.PeacefulSurvived, Assert.IsType<byte>(gameOver.Payload[0]));

            var rows = ResultModel.Build(
                (byte)gameOver.Payload[0], (int[])gameOver.Payload[1], (byte[])gameOver.Payload[2],
                new Dictionary<int, DeathCause> { { 1, DeathCause.Other } }, a => "P" + a);
            Assert.All(rows, r => Assert.True(r.IsWinningSide));
        }

        [Fact]
        public void TimerExpiry_Peaceful_Collapses_Immediately()
        {
            var session = CreatePeacefulSession();

            session.Tick(Now + RoundSeconds * 1000L + 1);

            Assert.Equal(WinReason.PeacefulCollapsed, session.Winner.Reason);
            Assert.Equal(new[] { 169, 172 }, _sent.Select(m => (int)m.Code).ToArray());
            Assert.Equal(TeamCodes.PeacefulCollapsed, (byte)_sent[0].Payload[0]);
        }

        [Fact]
        public void ExtractionFailed_Peaceful_Collapses()
        {
            var session = CreatePeacefulSession();

            session.NotifyExtractionOutcome(completed: false, failed: true, Now + 1000);

            Assert.Equal(WinReason.PeacefulCollapsed, session.Winner.Reason);
        }

        [Fact]
        public void Checkmate_Peaceful_LocksAndConfirmsCollapse()
        {
            var session = CreatePeacefulSession();

            session.LockValueCheckmate();
            Assert.True(session.WinLocked);
            session.NotifyMailDeparture(Now + 1000);
            Assert.Null(session.Winner);

            session.NotifyValueCheckmate(Now + CheckmateSequence.CeremonyMs);

            Assert.Equal(WinReason.PeacefulCollapsed, session.Winner.Reason);
            Assert.Equal(TeamCodes.PeacefulCollapsed,
                (byte)_sent.Single(m => m.Code == WWEventCodes.GameOver).Payload[0]);
        }

        [Fact]
        public void NormalVillage_StillUsesTeamCodes()
        {
            var session = new GameSession();
            session.ReserveForcedRole(1, Role.Werewolf);
            var config = new GameConfig { WerewolfCount = 1, RoundSeconds = RoundSeconds, ShamanChancePercent = 0 };
            Assert.True(session.Start(config, MakePlayers(4), Now, new Random(1)).Success);
            Assert.False(session.Peaceful);
            session.OnSend += m => _sent.Add(m);

            session.NotifyMailDeparture(Now + 1000);

            Assert.Equal(WinReason.ExtractionCompleted, session.Winner.Reason);
            Assert.Equal((byte)Team.Villagers,
                (byte)_sent.Single(m => m.Code == WWEventCodes.GameOver).Payload[0]);
        }

        [Fact]
        public void ResultModel_PeacefulCollapsed_NoWinningRows()
        {
            var rows = ResultModel.Build(TeamCodes.PeacefulCollapsed, new[] { 1, 2, 3 },
                new[] { (byte)Role.Villager, (byte)Role.Shaman, (byte)Role.Villager },
                new Dictionary<int, DeathCause>(), a => "P" + a);

            Assert.All(rows, r => Assert.False(r.IsWinningSide));
        }

        [Fact]
        public void ResultModel_PeacefulSurvived_VillagerTeamRowsWin()
        {
            var rows = ResultModel.Build(TeamCodes.PeacefulSurvived, new[] { 1, 2 },
                new[] { (byte)Role.Villager, (byte)Role.Shaman },
                new Dictionary<int, DeathCause>(), a => "P" + a);

            Assert.All(rows, r => Assert.True(r.IsWinningSide));
        }

        [Fact]
        public void Texts_PeacefulOutcomesReasonsAndEpigraphs_AreDistinctAndPresent()
        {
            Texts.Current = Language.Japanese;
            var survived = ResultDigestText.OutcomeText(TeamCodes.PeacefulSurvived);
            var collapsed = ResultDigestText.OutcomeText(TeamCodes.PeacefulCollapsed);

            foreach (var text in new[] { survived, collapsed })
            {
                Assert.False(string.IsNullOrEmpty(text));
                Assert.NotEqual(ResultDigestText.OutcomeText((byte)Team.Villagers), text);
                Assert.NotEqual(ResultDigestText.OutcomeText(TeamCodes.VoidMatch), text);
            }
            Assert.NotEqual(survived, collapsed);

            Assert.False(string.IsNullOrEmpty(ResultDigestText.ReasonText((int)WinReason.PeacefulSurvived)));
            Assert.Null(ResultDigestText.ReasonText((int)WinReason.PeacefulCollapsed));

            var survivedEpigraph = ResultDigestText.EpigraphText((int)WinReason.PeacefulSurvived);
            var collapsedEpigraph = ResultDigestText.EpigraphText((int)WinReason.PeacefulCollapsed);
            Assert.False(string.IsNullOrEmpty(survivedEpigraph));
            Assert.False(string.IsNullOrEmpty(collapsedEpigraph));
            Assert.NotEqual(survivedEpigraph, collapsedEpigraph);

            Assert.False(string.IsNullOrEmpty(Texts.Get(TextId.EradicationPeacefulCollapsed)));
        }

        [Fact]
        public void Digest_MatchEnd_PeacefulCollapsed_FormatsHeadingOnly()
        {
            Texts.Current = Language.Japanese;
            var digest = new ResultDigest();
            digest.Observe(WWEventCodes.GameStart, new object[0], Now);
            digest.Observe(WWEventCodes.GameOver,
                new object[] { TeamCodes.PeacefulCollapsed, new int[0], new byte[0] }, Now + 65_000,
                new WinResult(Team.Werewolves, WinReason.PeacefulCollapsed));

            var lines = ResultDigestText.FormatLines(digest.Entries, a => "P" + a);

            Assert.Contains("01:05  " + ResultDigestText.OutcomeText(TeamCodes.PeacefulCollapsed), lines);
        }
    }
}
