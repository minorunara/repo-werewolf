using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioSiteRules
    {
        public const float TruckZoneMarginMeters = 1.5f;

        public const float GroupSpreadMeters = 1.2f;

        public static int PickStandSite(IReadOnlyList<float> truckDistances, float truckZoneMeters)
        {
            int count = Count(truckDistances);
            float min = truckZoneMeters + TruckZoneMarginMeters;
            int outside = ArgMin(count, i => truckDistances[i], i => truckDistances[i] > min);
            return outside >= 0 ? outside : ArgMax(count, i => truckDistances[i]);
        }

        public static int PickFarSite(IReadOnlyList<float> standDistances)
            => ArgMax(Count(standDistances), i => standDistances[i]);

        public static (float dx, float dz) GroupOffset(int participantId)
        {
            double angle = participantId * 2.399963;
            return ((float)(Math.Cos(angle) * GroupSpreadMeters),
                    (float)(Math.Sin(angle) * GroupSpreadMeters));
        }

        private static int Count(IReadOnlyList<float> list) => list?.Count ?? 0;

        private static int ArgMin(int count, Func<int, float> key, Func<int, bool> accept)
        {
            int best = -1;
            float bestKey = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!accept(i)) continue;
                float k = key(i);
                if (k >= bestKey) continue;
                bestKey = k;
                best = i;
            }
            return best;
        }

        private static int ArgMax(int count, Func<int, float> key)
        {
            int best = -1;
            float bestKey = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float k = key(i);
                if (k <= bestKey) continue;
                bestKey = k;
                best = i;
            }
            return best;
        }
    }
}
