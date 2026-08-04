using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class BomberHudModel
    {
        public static float PlantFraction(bool phaseAllows, int cooldownSec,
            bool hasPlantResource, IReadOnlyDictionary<int, float> ratios,
            int excludedTarget)
        {
            if (!phaseAllows || cooldownSec > 0 || !hasPlantResource || ratios == null)
                return 0f;

            float max = 0f;
            foreach (var kv in ratios)
            {
                if (kv.Key == excludedTarget) continue;
                float ratio = kv.Value;
                if (ratio < 0f) ratio = 0f;
                if (ratio > 1f) ratio = 1f;
                if (ratio > max) max = ratio;
            }
            return max;
        }
    }
}
