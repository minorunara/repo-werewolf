using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioRoster
    {
        public const int FirstBotActor = WPlayer.FirstBotActor;

        public static ScenarioParticipant[] Bots(
            params (int ParticipantId, string Name, Role Role)[] bots)
        {
            if (bots == null) return new ScenarioParticipant[0];

            var roster = new ScenarioParticipant[bots.Length];
            for (int i = 0; i < bots.Length; i++)
            {
                roster[i] = new ScenarioParticipant(
                    FirstBotActor - i, bots[i].ParticipantId, bots[i].Name, bots[i].Role);
            }
            return roster;
        }

        public static List<WPlayer> BuildBots(IReadOnlyList<ScenarioParticipant> participants)
        {
            var bots = new List<WPlayer>();
            if (participants == null) return bots;

            for (int i = 0; i < participants.Count; i++)
            {
                bots.Add(new WPlayer
                {
                    ActorNumber = participants[i].ActorNumber,
                    Name = participants[i].Name,
                    IsBot = true,
                });
            }
            return bots;
        }

        public static Dictionary<int, Role> BuildForcedRoles(IReadOnlyList<ScenarioParticipant> participants)
        {
            var roles = new Dictionary<int, Role>();
            if (participants == null) return roles;

            for (int i = 0; i < participants.Count; i++)
            {
                roles[participants[i].ActorNumber] = participants[i].Role;
            }
            return roles;
        }
    }
}
