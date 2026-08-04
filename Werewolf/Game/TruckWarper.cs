using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class TruckWarper
    {
        private static readonly AccessTools.FieldRef<PlayerAvatar, PlayerDeathHead> DeathHeadRef =
            GameRefs.PlayerAvatar_playerDeathHead;
        private static readonly AccessTools.FieldRef<PlayerDeathHead, PhysGrabObject> HeadGrabObjectRef =
            GameRefs.PlayerDeathHead_physGrabObject;
        private static readonly AccessTools.FieldRef<PlayerAvatar, bool> IsTumblingRef =
            GameRefs.PlayerAvatar_isTumbling;

        private static PhysGrabObject TumbleGrabObject(PlayerAvatar avatar)
        {
            PlayerTumble tumble = GameRefs.PlayerAvatar_tumble(avatar);
            return tumble != null ? GameRefs.PlayerTumble_physGrabObject(tumble) : null;
        }

        internal static bool TryGetDeathHeadPosition(PlayerAvatar avatar, out Vector3 pos)
        {
            pos = default;
            try
            {
                if (avatar == null) return false;
                PlayerDeathHead head = DeathHeadRef(avatar);
                if (head == null || !head.gameObject.activeInHierarchy) return false;
                PhysGrabObject grab = HeadGrabObjectRef(head);
                pos = grab != null ? grab.transform.position : head.transform.position;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private const float WrapOffset = 0.6f;

        internal const float RewarpDistance = 6f;

        internal const long RewarpSustainMs = 1500L;

        internal const long RewarpCooldownMs = 3000L;

        private readonly System.Random _rng = new System.Random();

        private readonly List<(PlayerAvatar avatar, Vector3 pos, Quaternion rot, bool wasAlive)> _assignments =
            new List<(PlayerAvatar avatar, Vector3 pos, Quaternion rot, bool wasAlive)>();

        private readonly RewarpGate _rewarpGate = new RewarpGate(RewarpSustainMs, RewarpCooldownMs);

        public Vector3? LocalPlayerWarpTarget { get; private set; }

        public static bool IsTumbling(PlayerAvatar avatar) => avatar != null && IsTumblingRef(avatar);

        public static SpawnPoint[] ResolveTruckSpawnPoints() =>
            UnityEngine.Object.FindObjectsOfType<SpawnPoint>();

        internal static string FormatVec(Vector3 v) =>
            v.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," +
            v.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," +
            v.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        public bool WarpAll(Func<PlayerAvatar, bool> isAlive)
        {
            LocalPlayerWarpTarget = null;
            _assignments.Clear();
            _rewarpGate.Reset();
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("truck_warp_skip", secret: false, ("reason", "not_host"));
                return false;
            }

            SpawnPoint[] spawns = ResolveTruckSpawnPoints();
            if (spawns == null || spawns.Length == 0)
            {
                WLog.Line("truck_warp_error", secret: false, ("reason", "no_spawnpoint"));
                return false;
            }
            Shuffle(spawns);

            List<PlayerAvatar> players = GameDirector.instance != null ? GameDirector.instance.PlayerList : null;
            if (players == null || players.Count == 0)
            {
                WLog.Line("truck_warp_error", secret: false, ("reason", "no_players"));
                return false;
            }

            var assigned = new List<KeyValuePair<PlayerAvatar, Vector3>>(players.Count);
            Vector3 sum = Vector3.zero;
            int index = 0;
            foreach (PlayerAvatar avatar in players)
            {
                if (avatar == null) continue;
                SpawnPoint sp = spawns[index % spawns.Length];
                int wrap = index / spawns.Length;
                Vector3 pos = sp.transform.position;
                if (wrap > 0)
                {
                    float d = WrapOffset * wrap;
                    pos += new Vector3(d, 0f, d);
                }
                assigned.Add(new KeyValuePair<PlayerAvatar, Vector3>(avatar, pos));
                sum += pos;
                index++;
            }
            if (assigned.Count == 0)
            {
                WLog.Line("truck_warp_error", secret: false, ("reason", "no_valid_players"));
                return false;
            }
            Vector3 center = sum / assigned.Count;

            int aliveCount = 0;
            int headCount = 0;
            foreach (KeyValuePair<PlayerAvatar, Vector3> kv in assigned)
            {
                PlayerAvatar avatar = kv.Key;
                Vector3 pos = kv.Value;

                Vector3 toCenter = center - pos;
                toCenter.y = 0f;
                Quaternion rot = toCenter.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(toCenter)
                    : Quaternion.identity;

                bool wasAlive = isAlive != null && isAlive(avatar);
                _assignments.Add((avatar, pos, rot, wasAlive));

                try
                {
                    if (wasAlive)
                    {
                        bool isLocal = avatar == PlayerAvatar.instance;
                        Vector3 beforePos = isLocal ? avatar.transform.position : Vector3.zero;
                        WarpAliveAvatar(avatar, pos, rot);
                        aliveCount++;
                        if (isLocal)
                        {
                            LocalPlayerWarpTarget = pos;
                            WLog.Line("truck_warp_local_diag", secret: false,
                                ("beforePos", FormatVec(beforePos)), ("targetPos", FormatVec(pos)),
                                ("isTumbling", IsTumbling(avatar)), ("multiplayer", SemiFunc.IsMultiplayer()));
                        }
                    }
                    else
                    {
                        PlayerDeathHead head = DeathHeadRef(avatar);
                        if (head != null)
                        {
                            PhysGrabObject grab = HeadGrabObjectRef(head);
                            if (grab != null)
                            {
                                grab.Teleport(pos, rot);
                                headCount++;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    WLog.Line("truck_warp_player_error", secret: false, ("err", e.Message));
                }
            }

            WLog.Line("truck_warp", secret: false,
                ("points", spawns.Length), ("alive", aliveCount), ("heads", headCount));
            return true;
        }

        private void WarpAliveAvatar(PlayerAvatar avatar, Vector3 pos, Quaternion rot)
        {
            avatar.Spawn(pos, rot);
            if (IsTumbling(avatar))
            {
                PhysGrabObject tumbleGrab = TumbleGrabObject(avatar);
                if (tumbleGrab != null)
                {
                    tumbleGrab.Teleport(pos, rot);
                    if (tumbleGrab.rb != null)
                    {
                        tumbleGrab.rb.velocity = Vector3.zero;
                        tumbleGrab.rb.angularVelocity = Vector3.zero;
                    }
                    WLog.Line("truck_warp_tumble", secret: false,
                        ("actor", avatar.photonView != null ? avatar.photonView.OwnerActorNr : -1));
                }
            }
        }

        public void TickWatchdog(Func<PlayerAvatar, bool> isAlive, long nowMs)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_assignments.Count == 0) return;

            try
            {
                foreach (var a in _assignments)
                {
                    PlayerAvatar avatar = a.avatar;
                    if (avatar == null) continue;

                    bool isAliveNow = isAlive != null && isAlive(avatar);
                    if (isAliveNow != a.wasAlive) continue;

                    Vector3 current;
                    if (a.wasAlive)
                    {
                        current = avatar.transform.position;
                    }
                    else
                    {
                        PlayerDeathHead head = DeathHeadRef(avatar);
                        if (head == null) continue;
                        PhysGrabObject grab = HeadGrabObjectRef(head);
                        if (grab == null) continue;
                        current = grab.transform.position;
                    }

                    if (avatar.photonView == null) continue;
                    int photonActor = avatar.photonView.OwnerActorNr;

                    float distance = Vector3.Distance(current, a.pos);
                    bool isFar = distance > RewarpDistance;

                    if (!_rewarpGate.Tick(photonActor, isFar, nowMs)) continue;

                    if (a.wasAlive)
                    {
                        WarpAliveAvatar(avatar, a.pos, a.rot);
                    }
                    else
                    {
                        PlayerDeathHead head = DeathHeadRef(avatar);
                        PhysGrabObject grab = head != null ? HeadGrabObjectRef(head) : null;
                        grab?.Teleport(a.pos, a.rot);
                    }

                    WLog.Line("truck_rewarp", secret: false,
                        ("actor", photonActor), ("distance", distance.ToString("F1")), ("alive", a.wasAlive));
                }
            }
            catch (Exception e)
            {
                WLog.Line("truck_rewarp_error", secret: false, ("err", e.Message));
            }
        }

        public void ResetWatchdog()
        {
            if (_assignments.Count == 0) return;
            _assignments.Clear();
            _rewarpGate.Reset();
            WLog.Line("truck_rewarp_reset", secret: false);
        }

        private void Shuffle(SpawnPoint[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                SpawnPoint tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
        }
    }
}
