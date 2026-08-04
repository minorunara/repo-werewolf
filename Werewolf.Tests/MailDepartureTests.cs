using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MailDepartureTests : IDisposable
    {
        private const long Now = MeetingSessionHarness.GameStart;

        public MailDepartureTests() { WLog.Sink = (_, __) => { }; }
        public void Dispose() { WLog.Sink = null; }

        private static MeetingSessionHarness CreateFixedRoles()
            => MeetingSessionHarness.Create(
                playerCount: 5,
                forcedRoles: new[]
                {
                    (1, Role.Werewolf),
                    (2, Role.Villager), (3, Role.Villager),
                    (4, Role.Villager), (5, Role.Villager),
                });

        [Fact]
        public void NotifyMailDeparture_AllAboard_VillagersWinByExtraction()
        {
            var h = CreateFixedRoles();

            h.Game.NotifyMailDeparture(Now + 1_000);

            Assert.NotNull(h.Game.Winner);
            Assert.Equal(Team.Villagers, h.Game.Winner.WinningTeam);
            Assert.Equal(WinReason.ExtractionCompleted, h.Game.Winner.Reason);
            Assert.Equal(GamePhase.GameOver, h.Game.Phase);
        }

        [Fact]
        public void NotifyMailDeparture_AfterWinnerConfirmed_IsIgnored()
        {
            var h = CreateFixedRoles();
            h.Game.NotifyExtractionOutcome(completed: true, failed: false, nowUnixMs: Now + 500);
            Assert.Equal(Team.Villagers, h.Game.Winner.WinningTeam);

            h.Game.NotifyMailDeparture(Now + 1_000);
            Assert.Equal(Team.Villagers, h.Game.Winner.WinningTeam);
        }

        [Fact]
        public void SlackerSelfDestruct_AllVillagersDie_WerewolvesWinByEradication()
        {
            var h = CreateFixedRoles();

            h.Game.RecordDeath(2, Now + 11_000);
            h.Game.RecordDeath(3, Now + 11_500);
            h.Game.RecordDeath(4, Now + 12_000);
            Assert.Null(h.Game.Winner);

            h.Game.RecordDeath(5, Now + 12_500);

            Assert.NotNull(h.Game.Winner);
            Assert.Equal(Team.Werewolves, h.Game.Winner.WinningTeam);
            Assert.Equal(WinReason.VillagersEradicated, h.Game.Winner.Reason);
        }

        [Fact]
        public void SlackerSelfDestruct_SurvivorReturns_DepartureCompletesExtraction()
        {
            var h = CreateFixedRoles();
            h.Game.RecordDeath(3, Now + 11_000);
            h.Game.RecordDeath(4, Now + 11_500);
            h.Game.RecordDeath(5, Now + 12_000);
            Assert.Null(h.Game.Winner);

            h.Game.NotifyExtractionOutcome(completed: true, failed: false, nowUnixMs: Now + 20_000);

            Assert.Equal(Team.Villagers, h.Game.Winner.WinningTeam);
            Assert.Equal(WinReason.ExtractionCompleted, h.Game.Winner.Reason);
        }

        [Fact]
        public void SlackerSelfDestruct_WerewolfLeftBehindDies_VillagersWinByEradication()
        {
            var h = CreateFixedRoles();

            h.Game.RecordDeath(1, Now + 11_000);

            Assert.NotNull(h.Game.Winner);
            Assert.Equal(Team.Villagers, h.Game.Winner.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, h.Game.Winner.Reason);
        }
    }
}
