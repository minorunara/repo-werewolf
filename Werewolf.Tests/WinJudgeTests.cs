using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WinJudgeTests
    {
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
            foreach (int actor in actors)
            {
                players.Single(p => p.ActorNumber == actor).Alive = false;
            }
        }

        [Fact]
        public void Judge_AllWerewolvesDead_VillagersWin()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);
            Kill(players, 1);

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Villagers, result.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, result.Reason);
        }

        [Fact]
        public void Judge_WerewolvesDeadButBlackCatAlive_VillagersStillWin()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            Kill(players, 1);

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Villagers, result.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, result.Reason);
        }

        [Fact]
        public void Judge_AllVillagersDead_WerewolvesWin()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);
            Kill(players, 2, 3);

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Werewolves, result.WinningTeam);
            Assert.Equal(WinReason.VillagersEradicated, result.Reason);
        }

        [Fact]
        public void Judge_VillagersDeadBlackCatAlive_WerewolvesWin()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            Kill(players, 3, 4, 5);

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Werewolves, result.WinningTeam);
            Assert.Equal(WinReason.VillagersEradicated, result.Reason);
            Assert.Equal(Team.Werewolves, RoleDistribution.TeamOf(Role.BlackCat));
        }

        [Fact]
        public void Judge_OnlyBlackCatDead_GameContinues()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            Kill(players, 2);

            Assert.Null(WinJudge.Judge(players));
        }

        [Fact]
        public void Judge_SimultaneousEradication_VillagersWinByPriority()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            Kill(players, 1, 3, 4, 5);

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Villagers, result.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, result.Reason);
        }

        [Fact]
        public void Judge_EradicationBeatsExtraction()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);
            Kill(players, 1);

            var result = WinJudge.Judge(players, extractionCompleted: true);

            Assert.NotNull(result);
            Assert.Equal(Team.Villagers, result.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, result.Reason);
        }

        [Fact]
        public void Judge_VillagerEradicationBeatsExtraction()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);
            Kill(players, 2, 3);

            var result = WinJudge.Judge(players, extractionCompleted: true);

            Assert.NotNull(result);
            Assert.Equal(Team.Werewolves, result.WinningTeam);
            Assert.Equal(WinReason.VillagersEradicated, result.Reason);
        }

        [Fact]
        public void Judge_ExtractionCompleted_VillagersWin()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);

            var result = WinJudge.Judge(players, extractionCompleted: true);

            Assert.NotNull(result);
            Assert.Equal(Team.Villagers, result.WinningTeam);
            Assert.Equal(WinReason.ExtractionCompleted, result.Reason);
        }

        [Fact]
        public void Judge_TimerExpired_WerewolvesWin()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);

            var result = WinJudge.Judge(players, timerExpired: true);

            Assert.NotNull(result);
            Assert.Equal(Team.Werewolves, result.WinningTeam);
            Assert.Equal(WinReason.TimerExpired, result.Reason);
        }

        [Fact]
        public void Judge_ExtractionBeatsTimerExpiry()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);

            var result = WinJudge.Judge(players, extractionCompleted: true, timerExpired: true);

            Assert.NotNull(result);
            Assert.Equal(Team.Villagers, result.WinningTeam);
            Assert.Equal(WinReason.ExtractionCompleted, result.Reason);
        }

        [Fact]
        public void Judge_NoConditionMet_ReturnsNull()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);

            Assert.Null(WinJudge.Judge(players));
        }

        [Fact]
        public void Judge_NullPlayers_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => WinJudge.Judge(null));
        }

        [Fact]
        public void Judge_BotsCountSameAsRealPlayers()
        {
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Name = "Host", Role = Role.Villager },
                new WPlayer { ActorNumber = -1, Name = "Bot1", Role = Role.Werewolf, IsBot = true },
                new WPlayer { ActorNumber = -2, Name = "Bot2", Role = Role.Villager, IsBot = true },
            };
            players.Single(p => p.ActorNumber == -1).Alive = false;

            var result = WinJudge.Judge(players);

            Assert.NotNull(result);
            Assert.Equal(Team.Villagers, result.WinningTeam);
        }
    }
}
