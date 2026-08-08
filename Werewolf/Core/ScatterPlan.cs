using System;

namespace Werewolf.Core
{
    public static class ScatterPlan
    {
        public const int MinGroupSize = 3;

        public static int[] Assign(int playerCount, int slotCount, Random rng)
        {
            if (playerCount <= 0) return Array.Empty<int>();

            var result = new int[playerCount];
            int groupCount = Math.Min(playerCount / MinGroupSize, slotCount);
            if (groupCount <= 1) return result;
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int[] order = SequenceShuffled(playerCount, rng);
            int[] slots = SequenceShuffled(slotCount, rng);

            int baseSize = playerCount / groupCount;
            int remainder = playerCount % groupCount;
            int cursor = 0;
            for (int g = 0; g < groupCount; g++)
            {
                int size = baseSize + (g < remainder ? 1 : 0);
                for (int n = 0; n < size; n++)
                {
                    result[order[cursor++]] = slots[g];
                }
            }
            return result;
        }

        public static int[] AssignUniformDebug(int playerCount, int slotCount, Random rng)
        {
            if (playerCount <= 0) return Array.Empty<int>();

            var result = new int[playerCount];
            if (slotCount <= 1) return result;
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            for (int i = 0; i < playerCount; i++)
            {
                result[i] = rng.Next(slotCount);
            }
            return result;
        }

        private static int[] SequenceShuffled(int count, Random rng)
        {
            var seq = new int[count];
            for (int i = 0; i < count; i++) seq[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (seq[i], seq[j]) = (seq[j], seq[i]);
            }
            return seq;
        }
    }
}
