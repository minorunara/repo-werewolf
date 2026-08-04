using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class DomainModelTests
    {

        [Fact]
        public void Role_HasExactNumericIds()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(Role)));
            Assert.Equal(0, (byte)Role.Villager);
            Assert.Equal(1, (byte)Role.Werewolf);
            Assert.Equal(2, (byte)Role.BlackCat);
            Assert.Equal(3, (byte)Role.Bomber);
            Assert.Equal(4, (byte)Role.Shaman);
            Assert.Equal(5, Enum.GetValues(typeof(Role)).Length);
        }

        [Fact]
        public void Team_HasExactNumericIds()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(Team)));
            Assert.Equal(0, (byte)Team.Villagers);
            Assert.Equal(1, (byte)Team.Werewolves);
            Assert.Equal(2, Enum.GetValues(typeof(Team)).Length);
        }

        [Fact]
        public void GamePhase_HasExactNumericIds()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(GamePhase)));
            Assert.Equal(0, (byte)GamePhase.Lobby);
            Assert.Equal(1, (byte)GamePhase.Play);
            Assert.Equal(2, (byte)GamePhase.Meeting);
            Assert.Equal(3, (byte)GamePhase.GameOver);
            Assert.Equal(4, Enum.GetValues(typeof(GamePhase)).Length);
        }

        [Fact]
        public void DeathCause_HasExactNumericIds()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(DeathCause)));
            Assert.Equal(0, (byte)DeathCause.Vote);
            Assert.Equal(1, (byte)DeathCause.Other);
            Assert.Equal(2, Enum.GetValues(typeof(DeathCause)).Length);
        }

        [Theory]
        [InlineData(Role.Villager, Team.Villagers)]
        [InlineData(Role.Werewolf, Team.Werewolves)]
        [InlineData(Role.BlackCat, Team.Werewolves)]
        [InlineData(Role.Bomber, Team.Werewolves)]
        [InlineData(Role.Shaman, Team.Villagers)]
        public void RoleTeamMapping_MatchesSpec(Role role, Team expected)
        {
            Assert.Equal(expected, RoleDistribution.TeamOf(role));
        }

        [Fact]
        public void WPlayer_Defaults_AliveTrue_DeathCauseNull()
        {
            var player = new WPlayer();

            Assert.True(player.Alive);
            Assert.Null(player.DeathCause);
            Assert.False(player.IsBot);
            Assert.Equal(Role.Villager, player.Role);
        }

        [Fact]
        public void WPlayer_HoldsBotIdentity_WithNegativeActorNumberAndNullSteamId()
        {
            var bot = new WPlayer
            {
                ActorNumber = -1,
                Name = "Bot1",
                SteamId = null,
                IsBot = true,
                Role = Role.Werewolf,
            };

            Assert.Equal(-1, bot.ActorNumber);
            Assert.Equal("Bot1", bot.Name);
            Assert.Null(bot.SteamId);
            Assert.True(bot.IsBot);
            Assert.Equal(Role.Werewolf, bot.Role);
        }

        [Fact]
        public void WPlayer_RecordsDeathWithCause()
        {
            var player = new WPlayer { ActorNumber = 1, Name = "Host" };

            player.Alive = false;
            player.DeathCause = Werewolf.Core.DeathCause.Vote;

            Assert.False(player.Alive);
            Assert.Equal(Werewolf.Core.DeathCause.Vote, player.DeathCause);
        }
    }
}
