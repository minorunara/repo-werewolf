using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class HueSpread
    {
        public static int[] Order(IReadOnlyList<float> hues, float startHue = 0f)
        {
            int count = hues?.Count ?? 0;
            var order = new int[count];
            if (count == 0) return order;

            var used = new bool[count];
            int first = 0;
            for (int i = 1; i < count; i++)
            {
                if (Distance(hues[i], startHue) < Distance(hues[first], startHue)) first = i;
            }
            order[0] = first;
            used[first] = true;

            for (int n = 1; n < count; n++)
            {
                int best = -1;
                float bestGap = -1f;
                for (int i = 0; i < count; i++)
                {
                    if (used[i]) continue;
                    float gap = float.MaxValue;
                    for (int k = 0; k < n; k++)
                    {
                        gap = Math.Min(gap, Distance(hues[i], hues[order[k]]));
                    }
                    if (gap > bestGap)
                    {
                        bestGap = gap;
                        best = i;
                    }
                }
                order[n] = best;
                used[best] = true;
            }
            return order;
        }

        public static float Distance(float a, float b)
        {
            float d = Math.Abs(Wrap(a) - Wrap(b));
            return d > 180f ? 360f - d : d;
        }

        private static float Wrap(float hue)
        {
            float h = hue % 360f;
            return h < 0f ? h + 360f : h;
        }
    }
}
