using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game
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
            float r2 = radiusMeters * radiusMeters;
            foreach (SpawnPoint sp in spawns)
            {
                if (sp == null) continue;
                if ((sp.transform.position - pos).sqrMagnitude <= r2) return true;
            }
            return false;
        }
    }
}
