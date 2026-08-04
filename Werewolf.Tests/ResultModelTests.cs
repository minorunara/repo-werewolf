using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ResultModelTests
    {
        private static string Name(int actor) => "P" + actor;

        [Fact]
        public void Build_VillagerWinTeam_MarksVillagerRowsAsWinningSide()
        {
            var actors = new[] { 1, 2, 3 };
            var roles = new[] { (byte)Role.Villager, (byte)Role.Werewolf, (byte)Role.BlackCat };
            var deathMirror = new Dictionary<int, DeathCause> { { 2, DeathCause.Other } };

            var rows = ResultModel.Build((byte)Team.Villagers, actors, roles, deathMirror, Name);

            Assert.Equal(3, rows.Count);

            Assert.Equal(1, rows[0].ActorNumber);
            Assert.Equal("P1", rows[0].Name);
            Assert.Equal(Role.Villager, rows[0].Role);
            Assert.Equal(ResultRowStatus.Alive, rows[0].Status);
            Assert.True(rows[0].Alive);
            Assert.True(rows[0].IsWinningSide);

            Assert.Equal(Role.Werewolf, rows[1].Role);
            Assert.Equal(ResultRowStatus.Dead, rows[1].Status);
            Assert.False(rows[1].Alive);
            Assert.False(rows[1].Executed);
            Assert.False(rows[1].IsWinningSide);

            Assert.Equal(Role.BlackCat, rows[2].Role);
            Assert.Equal(ResultRowStatus.Alive, rows[2].Status);
            Assert.False(rows[2].IsWinningSide);
        }

        [Fact]
        public void Build_WerewolfWinTeam_MarksWerewolfAndBlackCatRowsAsWinningSide()
        {
            var actors = new[] { 1, 2 };
            var roles = new[] { (byte)Role.Werewolf, (byte)Role.BlackCat };

            var rows = ResultModel.Build((byte)Team.Werewolves, actors, roles, null, Name);

            Assert.True(rows[0].IsWinningSide);
            Assert.True(rows[1].IsWinningSide);
        }

        [Fact]
        public void Build_VoteCause_MapsToExecutedStatus()
        {
            var actors = new[] { 1 };
            var roles = new[] { (byte)Role.Villager };
            var deathMirror = new Dictionary<int, DeathCause> { { 1, DeathCause.Vote } };

            var rows = ResultModel.Build((byte)Team.Werewolves, actors, roles, deathMirror, Name);

            Assert.Equal(ResultRowStatus.Executed, rows[0].Status);
            Assert.True(rows[0].Executed);
            Assert.False(rows[0].Alive);
        }

        [Fact]
        public void Build_OtherCause_MapsToDeadStatus()
        {
            var actors = new[] { 1 };
            var roles = new[] { (byte)Role.Villager };
            var deathMirror = new Dictionary<int, DeathCause> { { 1, DeathCause.Other } };

            var rows = ResultModel.Build((byte)Team.Werewolves, actors, roles, deathMirror, Name);

            Assert.Equal(ResultRowStatus.Dead, rows[0].Status);
            Assert.False(rows[0].Executed);
            Assert.False(rows[0].Alive);
        }

        [Fact]
        public void Build_MixedCauses_EachRowGetsIndependentStatus()
        {
            var actors = new[] { 10, 20, 30 };
            var roles = new[] { (byte)Role.Villager, (byte)Role.Werewolf, (byte)Role.BlackCat };
            var deathMirror = new Dictionary<int, DeathCause>
            {
                { 10, DeathCause.Vote },
                { 20, DeathCause.Other },
            };

            var rows = ResultModel.Build((byte)Team.Villagers, actors, roles, deathMirror, Name);

            Assert.Equal(ResultRowStatus.Executed, rows[0].Status);
            Assert.Equal(ResultRowStatus.Dead, rows[1].Status);
            Assert.Equal(ResultRowStatus.Alive, rows[2].Status);
        }

        [Fact]
        public void Build_ActorMissingFromDeathMirror_DefaultsToAlive()
        {
            var actors = new[] { 5 };
            var roles = new[] { (byte)Role.Villager };
            var deathMirror = new Dictionary<int, DeathCause>();

            var rows = ResultModel.Build((byte)Team.Villagers, actors, roles, deathMirror, Name);

            Assert.Equal(ResultRowStatus.Alive, rows[0].Status);
            Assert.True(rows[0].Alive);
        }

        [Fact]
        public void Build_NullDeathMirror_AllRowsDefaultToAlive()
        {
            var actors = new[] { 1, 2 };
            var roles = new[] { (byte)Role.Villager, (byte)Role.Werewolf };

            var rows = ResultModel.Build((byte)Team.Villagers, actors, roles, null, Name);

            Assert.All(rows, r => Assert.True(r.Alive));
        }

        [Fact]
        public void Build_BotActorWithVoteCause_MapsToExecuted()
        {
            var actors = new[] { -101 };
            var roles = new[] { (byte)Role.Villager };
            var deathMirror = new Dictionary<int, DeathCause> { { -101, DeathCause.Vote } };

            var rows = ResultModel.Build((byte)Team.Werewolves, actors, roles, deathMirror, Name);

            Assert.Equal(ResultRowStatus.Executed, rows[0].Status);
        }

        [Fact]
        public void Build_ResolveNameReturnsNull_FallsBackToActorNumberLabel()
        {
            var actors = new[] { 7 };
            var roles = new[] { (byte)Role.Villager };

            var rows = ResultModel.Build((byte)Team.Villagers, actors, roles, null, _ => null);

            Assert.Equal("Actor7", rows[0].Name);
        }

        [Fact]
        public void Build_ActorsRolesLengthMismatch_ThrowsArgumentException()
        {
            var actors = new[] { 1, 2 };
            var roles = new[] { (byte)Role.Villager };

            Assert.Throws<ArgumentException>(() =>
                ResultModel.Build((byte)Team.Villagers, actors, roles, null, Name));
        }

        [Fact]
        public void Build_NullActors_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ResultModel.Build((byte)Team.Villagers, null, new byte[0], null, Name));
        }

        [Fact]
        public void Build_NullRoles_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ResultModel.Build((byte)Team.Villagers, new int[0], null, null, Name));
        }

        [Fact]
        public void Build_NullResolveName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ResultModel.Build((byte)Team.Villagers, new int[0], new byte[0], null, null));
        }

        [Fact]
        public void Build_SameInputTwice_ProducesEquivalentRows()
        {
            var actors = new[] { 1, -101, 3 };
            var roles = new[] { (byte)Role.Villager, (byte)Role.Werewolf, (byte)Role.BlackCat };
            var deathMirror = new Dictionary<int, DeathCause>
            {
                { -101, DeathCause.Vote },
            };

            var rowsA = ResultModel.Build((byte)Team.Werewolves, actors, roles, deathMirror, Name);
            var rowsB = ResultModel.Build((byte)Team.Werewolves, actors, roles, deathMirror, Name);

            Assert.Equal(rowsA.Count, rowsB.Count);
            for (int i = 0; i < rowsA.Count; i++)
            {
                Assert.Equal(rowsA[i].ActorNumber, rowsB[i].ActorNumber);
                Assert.Equal(rowsA[i].Name, rowsB[i].Name);
                Assert.Equal(rowsA[i].Role, rowsB[i].Role);
                Assert.Equal(rowsA[i].Status, rowsB[i].Status);
                Assert.Equal(rowsA[i].IsWinningSide, rowsB[i].IsWinningSide);
            }
        }

        [Fact]
        public void Build_AllPlayersInPayload_AppearExactlyOnceInOrder()
        {
            var actors = new[] { 3, 1, 2 };
            var roles = new[] { (byte)Role.Villager, (byte)Role.Werewolf, (byte)Role.Villager };

            var rows = ResultModel.Build((byte)Team.Villagers, actors, roles, null, Name);

            Assert.Equal(new[] { 3, 1, 2 }, new List<int> { rows[0].ActorNumber, rows[1].ActorNumber, rows[2].ActorNumber });
        }
    }
}
