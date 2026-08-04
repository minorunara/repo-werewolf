using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WinJudgeBomberTests
    {
        private static WPlayer Player(int actor, Role role, bool alive = true)
            => new WPlayer { ActorNumber = actor, Role = role, Alive = alive };

        [Fact]
        public void Judge_WerewolfDead_BomberAlive_GameContinues()
        {
            var players = new List<WPlayer>
            {
                Player(1, Role.Werewolf, alive: false),
                Player(2, Role.Bomber, alive: true),
                Player(3, Role.Villager, alive: true),
                Player(4, Role.Villager, alive: true),
            };

            var result = WinJudge.Judge(players);

            Assert.Null(result);
        }

        [Fact]
        public void Judge_WerewolfAndBomberBothDead_VillagersWin()
        {
            var players = new List<WPlayer>
            {
                Player(1, Role.Werewolf, alive: false),
                Player(2, Role.Bomber, alive: false),
                Player(3, Role.Villager, alive: true),
            };

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Villagers, result.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, result.Reason);
        }

        [Fact]
        public void Judge_AllVillagersDead_WerewolvesWin_BomberSurvives()
        {
            var players = new List<WPlayer>
            {
                Player(1, Role.Werewolf, alive: true),
                Player(2, Role.Bomber, alive: true),
                Player(3, Role.Villager, alive: false),
            };

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Werewolves, result.WinningTeam);
            Assert.Equal(WinReason.VillagersEradicated, result.Reason);
        }

        [Fact]
        public void Judge_OnlyBomberAlive_AllVillagersDead_WerewolvesWin()
        {
            var players = new List<WPlayer>
            {
                Player(1, Role.Werewolf, alive: false),
                Player(2, Role.Bomber, alive: true),
                Player(3, Role.Villager, alive: false),
                Player(4, Role.Villager, alive: false),
            };

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Werewolves, result.WinningTeam);
            Assert.Equal(WinReason.VillagersEradicated, result.Reason);
        }

        [Fact]
        public void Judge_BomberTeamMappingIsWerewolves()
        {
            Assert.Equal(Team.Werewolves, RoleDistribution.TeamOf(Role.Bomber));
        }
    }
}
