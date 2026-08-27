using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    public static class TruckZone
    {
        public static bool IsNearTruck(Vector3 pos, float radiusMeters)
        {
            SpawnPoint[] spawns = TruckWarper.ResolveTruckSpawnPoints();
            if (spawns == null || spawns.Length == 0)
            {
                WLog.Line("truck_zone_no_spawn", secret: false, ("radiusM", radiusMeters));
                return false;
            }
            return MinDistance(spawns, pos) <= radiusMeters;
        }

        public static float MinDistance(SpawnPoint[] spawns, Vector3 pos)
        {
            float best = float.PositiveInfinity;
            if (spawns == null) return best;
            foreach (SpawnPoint sp in spawns)
            {
                if (sp == null) continue;
                float d = Vector3.Distance(sp.transform.position, pos);
                if (d < best) best = d;
            }
            return best;
        }
    }
}
