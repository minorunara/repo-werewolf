using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioRosterTests
    {
        private static readonly ScenarioParticipant[] Participants =
        {
            new ScenarioParticipant(-101, 2, "BotA", Role.Villager),
            new ScenarioParticipant(-102, 3, "BotB", Role.Werewolf),
            new ScenarioParticipant(-103, 4, "BotC", Role.Villager),
        };

        [Fact]
        public void BuildBots_MirrorsTheParticipantsAsBotsInOrder()
        {
            List<WPlayer> bots = ScenarioRoster.BuildBots(Participants);

            Assert.Equal(Participants.Length, bots.Count);
            for (int i = 0; i < bots.Count; i++)
            {
                Assert.Equal(Participants[i].ActorNumber, bots[i].ActorNumber);
                Assert.Equal(Participants[i].Name, bots[i].Name);
                Assert.True(bots[i].IsBot);
            }
        }

        [Fact]
        public void BuildForcedRoles_CoversEveryBotAndNobodyElse()
        {
            Dictionary<int, Role> forced = ScenarioRoster.BuildForcedRoles(Participants);

            Assert.Equal(Participants.Length, forced.Count);
            foreach (ScenarioParticipant p in Participants)
            {
                Assert.Equal(p.Role, forced[p.ActorNumber]);
            }
            foreach (int actor in forced.Keys) Assert.True(actor < 0);
        }

        [Fact]
        public void Build_NullInput_ReturnsEmpty()
        {
            Assert.Empty(ScenarioRoster.BuildBots(null));
            Assert.Empty(ScenarioRoster.BuildForcedRoles(null));
        }
    }
}
