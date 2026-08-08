using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class ParticipantIds
    {
        public static int[] AssignOrder(IReadOnlyList<WPlayer> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));

            var actors = new List<int>(players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                actors.Add(players[i].ActorNumber);
            }
            actors.Sort(CompareForIdOrder);
            return actors.ToArray();
        }

        private static int CompareForIdOrder(int a, int b)
        {
            bool aBot = a < 0;
            bool bBot = b < 0;
            if (aBot != bBot) return aBot ? 1 : -1;
            return aBot ? b.CompareTo(a) : a.CompareTo(b);
        }
    }
}
