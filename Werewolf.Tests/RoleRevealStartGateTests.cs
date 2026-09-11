using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RoleRevealStartGateTests
    {
        private const long Start = 10_000;
        private const long End = 20_000;

        [Theory]
        [InlineData(null, GamePhase.RoleReveal, 1, false)]
        [InlineData(Role.Villager, GamePhase.Lobby, 1, false)]
        [InlineData(Role.Villager, GamePhase.RoleReveal, 1, true)]
        [InlineData(Role.BlackCat, GamePhase.RoleReveal, 1, true)]
        [InlineData(Role.Shaman, GamePhase.RoleReveal, 1, true)]
        [InlineData(Role.Werewolf, GamePhase.RoleReveal, 1, true)]
        [InlineData(Role.Werewolf, GamePhase.RoleReveal, 2, false)]
        [InlineData(Role.Bomber, GamePhase.RoleReveal, 1, true)]
        [InlineData(Role.Bomber, GamePhase.RoleReveal, 2, false)]
        public void ReadinessWithoutTeammatePayload(
            Role? role, GamePhase phase, int werewolfCount, bool expected)
        {
            Assert.Equal(expected, RoleRevealStartGate.IsReady(
                role, null, werewolfCount, phase, Start, End));
        }

        [Theory]
        [InlineData(Role.Werewolf)]
        [InlineData(Role.Bomber)]
        public void WolfTeamWithTeammatePayloadIsReady(Role role)
        {
            Assert.True(RoleRevealStartGate.IsReady(
                role, new[] { 1, 2 }, 2, GamePhase.RoleReveal, Start, End));
        }

        [Theory]
        [InlineData(0L, 20_000L)]
        [InlineData(10_000L, 10_000L)]
        [InlineData(20_000L, 10_000L)]
        public void InvalidWindowIsNotReady(long start, long end)
        {
            Assert.False(RoleRevealStartGate.IsReady(
                Role.Villager, null, 1, GamePhase.RoleReveal, start, end));
        }
    }
}
