using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ParticipantIdsTests
    {
        private static List<WPlayer> Players(params int[] actors)
        {
            var list = new List<WPlayer>(actors.Length);
            foreach (int actor in actors)
            {
                list.Add(new WPlayer { ActorNumber = actor, Name = "P" + actor, IsBot = actor < 0 });
            }
            return list;
        }

        [Fact]
        public void AssignOrder_RealPlayers_SortedByActorAscending()
        {
            var order = ParticipantIds.AssignOrder(Players(5, 1, 3, 2));

            Assert.Equal(new[] { 1, 2, 3, 5 }, order);
        }

        [Fact]
        public void AssignOrder_BotsAfterRealPlayers_InCreationOrder()
        {
            var order = ParticipantIds.AssignOrder(Players(-102, 3, -101, 1));

            Assert.Equal(new[] { 1, 3, -101, -102 }, order);
        }

        [Fact]
        public void AssignOrder_IsIndependentOfInputOrder()
        {
            var actors = new[] { 7, -103, 2, -101, 9, 4, -102 };
            var shuffled = new[] { -101, 9, -103, 4, 7, -102, 2 };

            Assert.Equal(ParticipantIds.AssignOrder(Players(actors)),
                ParticipantIds.AssignOrder(Players(shuffled)));
        }

        [Fact]
        public void AssignOrder_SinglePlayer_SoloActorOne()
        {
            Assert.Equal(new[] { 1 }, ParticipantIds.AssignOrder(Players(1)));
        }

        [Fact]
        public void AssignOrder_HundredPlayers_YieldsCompleteRoster()
        {
            var actors = Enumerable.Range(1, 100).Reverse().ToArray();

            var order = ParticipantIds.AssignOrder(Players(actors));

            Assert.Equal(Enumerable.Range(1, 100).ToArray(), order);
        }

        [Fact]
        public void AssignOrder_NoDuplicates_AllPlayersListed()
        {
            var actors = new[] { 4, -101, 8, 2, -105 };

            var order = ParticipantIds.AssignOrder(Players(actors));

            Assert.Equal(actors.Length, order.Length);
            Assert.Equal(actors.OrderBy(a => a), order.OrderBy(a => a));
        }

        [Fact]
        public void AssignOrder_EmptyList_ReturnsEmpty()
        {
            Assert.Empty(ParticipantIds.AssignOrder(new List<WPlayer>()));
        }

        [Fact]
        public void AssignOrder_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ParticipantIds.AssignOrder(null));
        }
    }
}
