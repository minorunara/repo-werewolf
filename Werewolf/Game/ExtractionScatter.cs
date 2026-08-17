using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class ExtractionScatter
    {
        private const float SampleRadius = 3f;

        private const float SampleMaxDistance = 5f;

        private const int SampleAttempts = 8;

        private readonly System.Random _rng = new System.Random();

        public readonly List<(int actor, string slot)> LastAssignments = new List<(int actor, string slot)>();

        public object[] BuildGroupsWire() => ScatterGroupsWire.ToWire(LastAssignments, _rng);

        public long LastScatterUnixMs { get; private set; }

        public void LogDiagnostics()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("scatter_diag_skip", secret: false, ("reason", "not_host"));
                return;
            }

            RoundDirector round = RoundDirector.instance;
            WLog.Line("scatter_diag", secret: false,
                ("points", round != null ? GameRefs.RoundDirector_extractionPoints(round) : -1),
                ("completed", round != null ? GameRefs.RoundDirector_extractionPointsCompleted(round) : -1),
                ("allCompleted", round != null && GameRefs.RoundDirector_allExtractionPointsCompleted(round)));

            List<LevelPoint> levelPoints = null;
            try { levelPoints = SemiFunc.LevelPointsGetAll(); }
            catch {  }

            SpawnPoint[] truckSpawns = TruckWarper.ResolveTruckSpawnPoints();

            foreach (ExtractionPoint ep in UnityEngine.Object.FindObjectsOfType<ExtractionPoint>())
            {
                ExtractionPoint.State state = GameRefs.ExtractionPoint_currentState(ep);
                bool startRoom = ep.GetComponentInParent<StartRoom>() != null;
                RoomVolume epRoom = ep.roomVolume != null ? ep.roomVolume.GetComponent<RoomVolume>() : null;

                int roomPoints = 0;
                float nearestLp = -1f;
                if (levelPoints != null)
                {
                    foreach (LevelPoint lp in levelPoints)
                    {
                        if (lp == null) continue;
                        float d = Vector3.Distance(lp.transform.position, ep.transform.position);
                        if (nearestLp < 0f || d < nearestLp) nearestLp = d;
                        if (epRoom != null && lp.Room == epRoom) roomPoints++;
                    }
                }

                bool navmeshOk = TrySampleDestination(ep.transform.position, out Vector3 dest);
                float truckDist = NearestDistance(truckSpawns, ep.transform.position);

                WLog.Line("scatter_ep", secret: false,
                    ("name", ep.name.Replace(' ', '_')),
                    ("state", state),
                    ("locked", ep.isLocked),
                    ("startRoom", startRoom),
                    ("roomVolActive", ep.roomVolume != null && ep.roomVolume.activeInHierarchy),
                    ("roomPoints", roomPoints),
                    ("nearestLevelPointM", FormatMeters(nearestLp)),
                    ("navmeshOk", navmeshOk),
                    ("dest", navmeshOk ? TruckWarper.FormatVec(dest) : "none"),
                    ("truckDistM", FormatMeters(truckDist)),
                    ("pos", TruckWarper.FormatVec(ep.transform.position)));
            }
        }

        private sealed class PlannedEntry
        {
            public int Actor;
            public PlayerAvatar Avatar;
            public Vector3 Pos;
            public Quaternion Rot;
            public string Label;
            public bool Physical;
            public bool IsBot;
        }

        private readonly List<PlannedEntry> _plan = new List<PlannedEntry>();
        private int _planSlotCount;
        private int _planEpCount;
        private int _planAliveCount;
        private int _planBotCount;
        private int _planFallbacks;
        private bool _planAllCompleted;

        public bool HasPlan => _plan.Count > 0;

        public void ClearPlan() => _plan.Clear();

        public bool WarpScatter(Func<PlayerAvatar, bool> isAlive, bool warpTruckSlot = true,
            bool uniformDebug = false, IReadOnlyList<int> botActors = null)
        {
            return PlanScatter(isAlive, warpTruckSlot, uniformDebug, botActors)
                && ExecutePlan(isAlive);
        }

        public bool PlanScatter(Func<PlayerAvatar, bool> isAlive, bool warpTruckSlot = true,
            bool uniformDebug = false, IReadOnlyList<int> botActors = null)
        {
            LastAssignments.Clear();
            _plan.Clear();
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("scatter_warp_skip", secret: false, ("reason", "not_host"));
                return false;
            }

            SpawnPoint[] truckSpawns = TruckWarper.ResolveTruckSpawnPoints();
            if (truckSpawns == null || truckSpawns.Length == 0)
            {
                WLog.Line("scatter_warp_error", secret: false, ("reason", "no_spawnpoint"));
                return false;
            }

            List<PlayerAvatar> players = GameDirector.instance != null ? GameDirector.instance.PlayerList : null;
            if (players == null || players.Count == 0)
            {
                WLog.Line("scatter_warp_error", secret: false, ("reason", "no_players"));
                return false;
            }

            RoundDirector round = RoundDirector.instance;
            bool allCompleted = round != null && GameRefs.RoundDirector_allExtractionPointsCompleted(round);
            List<ExtractionPoint> slotEps = allCompleted
                ? new List<ExtractionPoint>()
                : CollectEligibleExtractionPoints();

            var alive = new List<PlayerAvatar>();
            foreach (PlayerAvatar p in players)
            {
                if (p == null) continue;
                if (isAlive != null && isAlive(p))
                {
                    alive.Add(p);
                }
                else
                {
                    WLog.Line("scatter_skip_dead", secret: false, ("actor", ActorOf(p)));
                }
            }
            if (alive.Count == 0)
            {
                WLog.Line("scatter_warp_error", secret: false, ("reason", "no_alive_players"));
                return false;
            }

            int botCount = botActors != null ? botActors.Count : 0;
            int slotCount = 1 + slotEps.Count;
            int totalCount = alive.Count + botCount;
            int[] slotByIndex = uniformDebug
                ? ScatterPlan.AssignUniformDebug(totalCount, slotCount, _rng)
                : ScatterPlan.Assign(totalCount, slotCount, _rng);

            var epPos = new Vector3[alive.Count];
            var epRot = new Quaternion[alive.Count];
            var slotFailed = new bool[slotCount];
            for (int i = 0; i < alive.Count; i++)
            {
                int slot = slotByIndex[i];
                if (slot == 0 || slotFailed[slot]) continue;

                ExtractionPoint ep = slotEps[slot - 1];
                if (TrySampleDestination(ep.transform.position, out Vector3 dest))
                {
                    epPos[i] = dest;
                    Vector3 toEp = ep.transform.position - dest;
                    toEp.y = 0f;
                    epRot[i] = toEp.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toEp) : Quaternion.identity;
                }
                else
                {
                    slotFailed[slot] = true;
                    WLog.Line("scatter_team_fallback", secret: false,
                        ("slot", "ep" + slot), ("name", ep.name.Replace(' ', '_')));
                }
            }

            int truckMembers = 0;
            int fallbacks = 0;
            for (int i = 0; i < alive.Count; i++)
            {
                PlayerAvatar avatar = alive[i];
                int slot = slotByIndex[i];
                string label = "truck";
                Vector3 pos = default;
                Quaternion rot = Quaternion.identity;
                bool placed = false;

                if (slot != 0 && !slotFailed[slot])
                {
                    pos = epPos[i];
                    rot = epRot[i];
                    label = "ep" + slot;
                    placed = true;
                }
                else if (slot != 0)
                {
                    label = "truck_fallback";
                    fallbacks++;
                }

                int actor = ActorOf(avatar);

                if (!placed && warpTruckSlot)
                {
                    SpawnPoint sp = truckSpawns[truckMembers % truckSpawns.Length];
                    int wrap = truckMembers / truckSpawns.Length;
                    pos = sp.transform.position;
                    if (wrap > 0)
                    {
                        float d = TruckWarper.WrapOffset * wrap;
                        pos += new Vector3(d, 0f, d);
                    }
                    rot = sp.transform.rotation;
                    truckMembers++;
                    placed = true;
                }

                _plan.Add(new PlannedEntry
                {
                    Actor = actor, Avatar = avatar, Pos = pos, Rot = rot,
                    Label = label, Physical = placed,
                });
                LastAssignments.Add((actor, label));
            }

            for (int b = 0; b < botCount; b++)
            {
                int slot = slotByIndex[alive.Count + b];
                string botLabel = slot == 0 ? "truck"
                    : slotFailed[slot] ? "truck_fallback"
                    : "ep" + slot;
                if (slot != 0 && slotFailed[slot]) fallbacks++;
                _plan.Add(new PlannedEntry
                {
                    Actor = botActors[b], Avatar = null, Label = botLabel, Physical = false, IsBot = true,
                });
                LastAssignments.Add((botActors[b], botLabel));
            }

            _planSlotCount = slotCount;
            _planEpCount = slotEps.Count;
            _planAliveCount = alive.Count;
            _planBotCount = botCount;
            _planFallbacks = fallbacks;
            _planAllCompleted = allCompleted;
            WLog.Line("scatter_plan", secret: false,
                ("slots", slotCount), ("eps", slotEps.Count), ("alive", alive.Count),
                ("bots", botCount), ("fallbacks", fallbacks), ("allCompleted", allCompleted));
            return true;
        }

        public bool ExecutePlan(Func<PlayerAvatar, bool> isAlive)
        {
            if (_plan.Count == 0) return false;

            int warped = 0;
            foreach (PlannedEntry entry in _plan)
            {
                if (!entry.Physical)
                {
                    WLog.Line(entry.IsBot ? "scatter_bot" : "scatter_stay", secret: false,
                        ("actor", entry.Actor), ("slot", entry.Label));
                    continue;
                }

                if (entry.Avatar == null || (isAlive != null && !isAlive(entry.Avatar)))
                {
                    WLog.Line("scatter_skip_gone", secret: false, ("actor", entry.Actor), ("slot", entry.Label));
                    continue;
                }

                try
                {
                    Vector3 beforePos = entry.Avatar.transform.position;
                    TruckWarper.WarpAliveAvatar(entry.Avatar, entry.Pos, entry.Rot);
                    warped++;
                    WLog.Line("scatter_warp", secret: false,
                        ("actor", entry.Actor), ("slot", entry.Label),
                        ("beforePos", TruckWarper.FormatVec(beforePos)),
                        ("targetPos", TruckWarper.FormatVec(entry.Pos)),
                        ("isLocal", entry.Avatar == PlayerAvatar.instance),
                        ("isTumbling", TruckWarper.IsTumbling(entry.Avatar)));
                }
                catch (Exception e)
                {
                    WLog.Line("scatter_warp_player_error", secret: false,
                        ("actor", entry.Actor), ("err", e.Message));
                }
            }

            LastScatterUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            WLog.Line("scatter_done", secret: false,
                ("slots", _planSlotCount), ("eps", _planEpCount), ("alive", _planAliveCount),
                ("bots", _planBotCount), ("warped", warped), ("fallbacks", _planFallbacks),
                ("allCompleted", _planAllCompleted));
            _plan.Clear();
            return true;
        }

        private static List<ExtractionPoint> CollectEligibleExtractionPoints()
        {
            var eligible = new List<ExtractionPoint>();
            foreach (ExtractionPoint ep in UnityEngine.Object.FindObjectsOfType<ExtractionPoint>())
            {
                ExtractionPoint.State state = GameRefs.ExtractionPoint_currentState(ep);
                if (state != ExtractionPoint.State.Complete) continue;
                if (ep.GetComponentInParent<StartRoom>() != null) continue;
                eligible.Add(ep);
            }
            return eligible;
        }

        private const float EdgeSafetyMargin = 1.0f;

        private const float MaxFloorSlopeDeg = 10f;

        private static bool TrySampleDestination(Vector3 anchor, out Vector3 dest)
        {
            return TrySampleDestination(anchor, EdgeSafetyMargin, out dest)
                || TrySampleDestination(anchor, 0f, out dest);
        }

        private static bool TrySampleDestination(Vector3 anchor, float edgeMargin, out Vector3 dest)
        {
            for (int i = 0; i < SampleAttempts; i++)
            {
                Vector3 probe = anchor + UnityEngine.Random.insideUnitSphere * SampleRadius;
                if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, SampleMaxDistance, -1)) continue;
                if (!Physics.Raycast(hit.position, Vector3.down, out RaycastHit floor, 5f, LayerMask.GetMask("Default"))) continue;
                if (Vector3.Angle(floor.normal, Vector3.up) > MaxFloorSlopeDeg) continue;
                if (edgeMargin > 0f
                    && NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, -1)
                    && edge.distance < edgeMargin)
                {
                    continue;
                }
                dest = hit.position;
                return true;
            }
            dest = Vector3.zero;
            return false;
        }

        private static int ActorOf(PlayerAvatar avatar) =>
            avatar != null && avatar.photonView != null ? avatar.photonView.OwnerActorNr : -1;

        private static float NearestDistance(SpawnPoint[] points, Vector3 pos)
        {
            float nearest = -1f;
            if (points == null) return nearest;
            foreach (SpawnPoint sp in points)
            {
                if (sp == null) continue;
                float d = Vector3.Distance(sp.transform.position, pos);
                if (nearest < 0f || d < nearest) nearest = d;
            }
            return nearest;
        }

        private static string FormatMeters(float meters) =>
            meters < 0f ? "na" : meters.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
    }
}
