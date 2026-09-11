using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioProps
    {
        private const float FloorProbeUp = 1.5f;
        private const float FloorProbeLength = 6f;

        private const float BodyRadius = 0.5f;
        private const float BodyHeight = 2f;

        private const float NavSnapMeters = 1f;

        internal static bool TryResolveFarCorpseGround(out Vector3 ground)
        {
            ground = default;
            return TryTruckCenter(out Vector3 truck)
                   && TryPickSite(c => IndexOfFarthest(c, truck), out ground);
        }

        internal static bool TryResolveTruckCorpseGround(Vector3 playerPos, out Vector3 ground)
        {
            ground = default;
            SpawnPoint[] spawns = TruckWarper.ResolveTruckSpawnPoints();
            if (spawns == null || spawns.Length == 0) return false;

            SpawnPoint best = null;
            float bestSqr = -1f;
            foreach (SpawnPoint sp in spawns)
            {
                if (sp == null) continue;
                float sqr = (sp.transform.position - playerPos).sqrMagnitude;
                if (sqr <= bestSqr) continue;
                bestSqr = sqr;
                best = sp;
            }
            if (best == null) return false;

            ground = best.transform.position;
            return true;
        }

        internal static bool TryResolveStandSite(float truckZoneMeters, out Vector3 ground)
            => TryPickSite(c => ScenarioSiteRules.PickStandSite(TruckDistances(c), truckZoneMeters), out ground);

        internal static bool TryResolveFarSite(Vector3 stand, out Vector3 ground)
            => TryPickSite(c => ScenarioSiteRules.PickFarSite(DistancesFrom(c, stand)), out ground);

        internal static Vector3 ResolveAround(Vector3 baseGround, int participantId)
        {
            (float dx, float dz) = ScenarioSiteRules.GroupOffset(participantId);
            return ResolveFloorAt(baseGround + new Vector3(dx, 0f, dz));
        }

        private static bool TryClearGround(Vector3 position, out Vector3 ground)
        {
            if (!GroundProbe.TryStandableAt(position, NavSnapMeters, out _, out RaycastHit floor))
            {
                ground = default;
                return false;
            }

            ground = floor.point;
            return IsClearAt(ground);
        }

        private static bool IsClearAt(Vector3 ground)
        {
            Vector3 bottom = ground + Vector3.up * BodyRadius;
            Vector3 top = ground + Vector3.up * (BodyHeight - BodyRadius);
            return !Physics.CheckCapsule(bottom, top, BodyRadius, VisionProbe.ObstructMask,
                QueryTriggerInteraction.Ignore);
        }

        private static bool TryPickSite(Func<List<Vector3>, int> pick, out Vector3 ground)
        {
            ground = default;
            List<Vector3> candidates = CollectCandidates();
            if (candidates == null || candidates.Count == 0) return false;
            int index = pick(candidates);
            if (index < 0 || index >= candidates.Count) return false;
            ground = candidates[index];
            return true;
        }

        private static List<Vector3> CollectCandidates()
        {
            List<LevelPoint> points;
            try
            {
                points = SemiFunc.LevelPointsGetAll();
            }
            catch (Exception e)
            {
                ScenarioVanillaProbe.Failed("level_points", e);
                return null;
            }
            if (points == null)
            {
                ScenarioVanillaProbe.Failed("level_points", "null");
                return null;
            }

            var clear = new List<Vector3>(points.Count);
            var rest = new List<Vector3>();
            foreach (LevelPoint point in points)
            {
                if (point == null || !IsCorpseSite(point)) continue;
                if (!point.ModuleConnect
                    && TryClearGround(point.transform.position, out Vector3 clearGround))
                    clear.Add(clearGround);
                else
                    rest.Add(ResolveFloorAt(point.transform.position));
            }
            return clear.Count > 0 ? clear : rest;
        }

        private static bool IsCorpseSite(LevelPoint point)
        {
            if (point.Truck) return false;
            RoomVolume room = point.Room;
            return room == null || (!room.Extraction && !room.Truck);
        }

        private static List<float> TruckDistances(List<Vector3> candidates)
        {
            SpawnPoint[] spawns = TruckWarper.ResolveTruckSpawnPoints();
            var distances = new List<float>(candidates.Count);
            foreach (Vector3 candidate in candidates) distances.Add(TruckZone.MinDistance(spawns, candidate));
            return distances;
        }

        private static List<float> DistancesFrom(List<Vector3> candidates, Vector3 origin)
        {
            var distances = new List<float>(candidates.Count);
            foreach (Vector3 candidate in candidates) distances.Add(Vector3.Distance(candidate, origin));
            return distances;
        }

        private static int IndexOfFarthest(List<Vector3> candidates, Vector3 origin)
        {
            int best = -1;
            float bestSqr = -1f;
            for (int i = 0; i < candidates.Count; i++)
            {
                float sqr = (candidates[i] - origin).sqrMagnitude;
                if (sqr <= bestSqr) continue;
                bestSqr = sqr;
                best = i;
            }
            return best;
        }

        private static bool TryTruckCenter(out Vector3 center)
        {
            center = default;
            SpawnPoint[] spawns = TruckWarper.ResolveTruckSpawnPoints();
            if (spawns == null || spawns.Length == 0) return false;

            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (SpawnPoint sp in spawns)
            {
                if (sp == null) continue;
                sum += sp.transform.position;
                count++;
            }
            if (count == 0) return false;
            center = sum / count;
            return true;
        }

        private static Vector3 ResolveFloorAt(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * FloorProbeUp;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, FloorProbeLength,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }
            return position;
        }
    }
}
