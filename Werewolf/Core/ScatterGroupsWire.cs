using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class ScatterGroupsWire
    {
        private const string TruckLabel = "truck";
        private const string TruckFallbackLabel = "truck_fallback";

        public static object[] ToWire(IReadOnlyList<(int actor, string slot)> assignments, Random rng)
        {
            if (assignments == null || assignments.Count == 0) return null;
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var labels = new List<string>();
            var members = new Dictionary<string, List<int>>();
            foreach ((int actor, string slot) in assignments)
            {
                string key = slot == TruckFallbackLabel ? TruckLabel : slot;
                if (!members.TryGetValue(key, out List<int> list))
                {
                    list = new List<int>();
                    members[key] = list;
                    labels.Add(key);
                }
                list.Add(actor);
            }
            if (labels.Count < 2) return null;

            for (int i = labels.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (labels[i], labels[j]) = (labels[j], labels[i]);
            }

            var actors = new List<int>(assignments.Count);
            var groupIds = new List<byte>(assignments.Count);
            for (int g = 0; g < labels.Count; g++)
            {
                foreach (int actor in members[labels[g]])
                {
                    actors.Add(actor);
                    groupIds.Add((byte)g);
                }
            }
            return new object[] { actors.ToArray(), groupIds.ToArray() };
        }

        public static int CountGroups(IReadOnlyList<(int actor, string slot)> assignments)
        {
            if (assignments == null) return 0;
            var seen = new HashSet<string>();
            foreach ((int actor, string slot) in assignments)
            {
                _ = actor;
                seen.Add(slot == TruckFallbackLabel ? TruckLabel : slot);
            }
            return seen.Count;
        }

        public static List<List<int>> FromWire(object[] payload)
        {
            if (payload == null || payload.Length < 2) return null;
            var actors = payload[0] as int[];
            var groupIds = payload[1] as byte[];
            if (actors == null || groupIds == null) return null;
            if (actors.Length == 0 || actors.Length != groupIds.Length) return null;

            var byId = new SortedDictionary<byte, List<int>>();
            for (int i = 0; i < actors.Length; i++)
            {
                if (!byId.TryGetValue(groupIds[i], out List<int> list))
                {
                    list = new List<int>();
                    byId[groupIds[i]] = list;
                }
                list.Add(actors[i]);
            }
            var result = new List<List<int>>(byId.Count);
            foreach (KeyValuePair<byte, List<int>> pair in byId) result.Add(pair.Value);
            return result;
        }
    }

    public static class ScatterGroupsText
    {
        public static List<string> FormatLines(List<List<int>> groups, Func<int, string> memberLabel,
                                               TextId lineFormat = TextId.NoticeScatterGroupLineFormat)
        {
            var lines = new List<string>();
            if (groups == null || memberLabel == null) return lines;

            string separator = Texts.Get(TextId.NoticeScatterGroupSeparator);
            for (int g = 0; g < groups.Count; g++)
            {
                var labels = new List<string>(groups[g].Count);
                foreach (int actor in groups[g]) labels.Add(memberLabel(actor));
                lines.Add(Texts.Format(lineFormat, (char)('A' + g), string.Join(separator, labels)));
            }
            return lines;
        }
    }
}
