using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CosmeticHandoffTests
    {
        private const long Start = 1_000_000;

        [Fact]
        public void ShouldGrant_ExactlyThreeMinutes_True()
        {
            Assert.True(CosmeticHandoff.ShouldGrant(Start, Start + CosmeticHandoff.MinMatchDurationMs));
        }

        [Fact]
        public void ShouldGrant_OneMsShort_False()
        {
            Assert.False(CosmeticHandoff.ShouldGrant(Start, Start + CosmeticHandoff.MinMatchDurationMs - 1));
        }

        [Fact]
        public void ShouldGrant_UnknownStartTime_FailsOpen()
        {
            Assert.True(CosmeticHandoff.ShouldGrant(0, Start));
            Assert.True(CosmeticHandoff.ShouldGrant(-1, Start));
        }

        [Fact]
        public void MinMatchDuration_IsThreeMinutes()
        {
            Assert.Equal(180_000, CosmeticHandoff.MinMatchDurationMs);
        }

        [Fact]
        public void Decide_AllConditionsMet_Inject()
        {
            var route = CosmeticHandoff.Decide(
                departedToLobbyMenu: true, roundDirectorAlive: true,
                cooldownKnown: true, cooldownSeconds: 0f, out string reason);

            Assert.Equal(CosmeticHandoff.Route.Inject, route);
            Assert.Null(reason);
        }

        [Fact]
        public void Decide_CooldownNegative_Inject()
        {
            var route = CosmeticHandoff.Decide(true, true, true, -120f, out string reason);

            Assert.Equal(CosmeticHandoff.Route.Inject, route);
            Assert.Null(reason);
        }

        [Theory]
        [InlineData(false, true, true, 0f, "not_lobby_menu_departure")]
        [InlineData(true, false, true, 0f, "no_round_director")]
        [InlineData(true, true, false, 0f, "cooldown_unknown")]
        [InlineData(true, true, true, 0.5f, "cooldown_active")]
        public void Decide_AnyConditionUnmet_DirectAddWithReason(
            bool lobbyMenu, bool rdAlive, bool cooldownKnown, float cooldown, string expectedReason)
        {
            var route = CosmeticHandoff.Decide(lobbyMenu, rdAlive, cooldownKnown, cooldown,
                out string reason);

            Assert.Equal(CosmeticHandoff.Route.DirectAdd, route);
            Assert.Equal(expectedReason, reason);
        }

        [Fact]
        public void SubtractLeading_RemovesFromLowestRarityFirst()
        {
            var counts = new[] { 2, 1, 0, 1 };

            var remaining = CosmeticHandoff.SubtractLeading(counts, 2);

            Assert.Equal(new[] { 0, 1, 0, 1 }, remaining);
        }

        [Fact]
        public void SubtractLeading_SpansMultipleRarities()
        {
            var remaining = CosmeticHandoff.SubtractLeading(new[] { 1, 1, 1, 1 }, 3);

            Assert.Equal(new[] { 0, 0, 0, 1 }, remaining);
        }

        [Fact]
        public void SubtractLeading_ZeroInjected_ReturnsSameCounts()
        {
            Assert.Equal(new[] { 1, 0, 2, 0 }, CosmeticHandoff.SubtractLeading(new[] { 1, 0, 2, 0 }, 0));
        }

        [Fact]
        public void SubtractLeading_InjectedExceedsTotal_AllZero()
        {
            Assert.Equal(new[] { 0, 0, 0, 0 }, CosmeticHandoff.SubtractLeading(new[] { 1, 1, 0, 0 }, 10));
        }

        [Fact]
        public void SubtractLeading_DoesNotMutateInput()
        {
            var counts = new[] { 2, 0, 0, 0 };

            CosmeticHandoff.SubtractLeading(counts, 1);

            Assert.Equal(new[] { 2, 0, 0, 0 }, counts);
        }

        [Fact]
        public void SubtractLeading_NullCounts_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CosmeticHandoff.SubtractLeading(null, 1));
        }

        [Fact]
        public void GameSession_Start_RecordsStartUnixMs()
        {
            var session = new GameSession();
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Name = "P1" },
                new WPlayer { ActorNumber = 2, Name = "P2" },
                new WPlayer { ActorNumber = 3, Name = "P3" },
            };

            Assert.Equal(0, session.StartUnixMs);
            Assert.True(session.Start(new GameConfig(), players, Start, new Random(1)).Success);
            Assert.Equal(Start, session.StartUnixMs);
        }
    }
}
