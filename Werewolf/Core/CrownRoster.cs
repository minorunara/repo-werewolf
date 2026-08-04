using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class CrownRoster
    {
        private static readonly HashSet<int> _winners = new HashSet<int>();

        public static int Version { get; private set; }

        public static bool HasWinners => _winners.Count > 0;

        public static int Count => _winners.Count;

        public static void SetWinners(IEnumerable<int> actors)
        {
            _winners.Clear();
            Version++;
            if (actors == null) return;
            foreach (int actor in actors) _winners.Add(actor);
        }

        public static void Clear()
        {
            _winners.Clear();
            Version++;
        }

        public static bool IsWinner(int actorNumber)
        {
            return _winners.Contains(actorNumber);
        }
    }
}
