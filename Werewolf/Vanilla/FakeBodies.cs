using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal readonly struct FakeBodyVisual
    {
        public FakeBodyVisual(GameObject root, float pivotHeight, Animator animator)
        {
            Root = root;
            PivotHeight = pivotHeight;
            Animator = animator;
        }

        public GameObject Root { get; }
        public float PivotHeight { get; }
        public Animator Animator { get; }
        public bool IsEmpty => Root == null;
    }

    internal delegate FakeBodyVisual FakeBodyLook(int actor, Vector3 groundPos);

    internal static class FakeBodies
    {
        private const float CapsuleCenterHeight = 1f;

        internal const float WalkSpeedMps = 3f;

        private const float TurnSpeedDegPerSec = 540f;

        private const string MovingAnimParam = "Moving";

        private sealed class Body
        {
            public Transform Root;
            public float PivotHeight;
            public Animator Animator;
        }

        private static readonly Dictionary<int, Body> _bodies = new Dictionary<int, Body>();

        private static readonly Dictionary<int, Vector3> _destinations = new Dictionary<int, Vector3>();
        private static readonly List<int> _arrived = new List<int>();

        internal static bool Any
        {
            get
            {
                Prune();
                return _bodies.Count > 0;
            }
        }

        internal static bool TryGetPosition(int actor, out Vector3 pos)
        {
            if (_bodies.TryGetValue(actor, out Body b) && b.Root != null)
            {
                pos = GroundOf(b);
                return true;
            }
            pos = default;
            return false;
        }

        internal static List<(int actor, Vector3 pos)> Snapshot() => Collect(settled: false);

        internal static List<(int actor, Vector3 pos)> DestinationSnapshot() => Collect(settled: true);

        private static List<(int actor, Vector3 pos)> Collect(bool settled)
        {
            Prune();
            var list = new List<(int, Vector3)>(_bodies.Count);
            foreach (var kv in _bodies)
            {
                Vector3 pos = settled && _destinations.TryGetValue(kv.Key, out Vector3 dest)
                    ? dest
                    : GroundOf(kv.Value);
                list.Add((kv.Key, pos));
            }
            return list;
        }

        private static Vector3 GroundOf(Body b) => b.Root.position - Vector3.up * b.PivotHeight;

        internal static bool SpawnOrMove(int actor, Vector3 groundPos, FakeBodyLook look = null)
        {
            _destinations.Remove(actor);
            if (_bodies.TryGetValue(actor, out Body existing) && existing.Root != null)
            {
                existing.Root.position = groundPos + Vector3.up * existing.PivotHeight;
                SetMoving(existing, false);
                return false;
            }

            _bodies[actor] = Build(actor, groundPos, look);
            return true;
        }

        internal static bool MoveTo(int actor, Vector3 groundPos, FakeBodyLook look = null)
        {
            if (_bodies.TryGetValue(actor, out Body existing) && existing.Root != null)
            {
                _destinations[actor] = groundPos;
                return false;
            }
            return SpawnOrMove(actor, groundPos, look);
        }

        internal static bool SetLook(int actor, FakeBodyLook look)
        {
            if (!_bodies.TryGetValue(actor, out Body existing) || existing.Root == null) return false;
            Vector3 ground = GroundOf(existing);
            Quaternion facing = existing.Root.rotation;
            UnityEngine.Object.Destroy(existing.Root.gameObject);

            Body replaced = Build(actor, ground, look);
            replaced.Root.rotation = facing;
            _bodies[actor] = replaced;
            return true;
        }

        internal static bool IsMoving(int actor) => _destinations.ContainsKey(actor);

        internal static void Stop(int actor)
        {
            if (!_destinations.Remove(actor)) return;
            if (_bodies.TryGetValue(actor, out Body b) && b.Root != null) SetMoving(b, false);
        }

        internal static void Tick(float deltaSeconds)
        {
            if (_destinations.Count == 0 || deltaSeconds <= 0f) return;

            _arrived.Clear();
            float step = WalkSpeedMps * deltaSeconds;
            foreach (var kv in _destinations)
            {
                if (!_bodies.TryGetValue(kv.Key, out Body b) || b.Root == null)
                {
                    _arrived.Add(kv.Key);
                    continue;
                }
                Transform t = b.Root;
                Vector3 target = kv.Value + Vector3.up * b.PivotHeight;
                Vector3 next = Vector3.MoveTowards(t.position, target, step);
                FaceToward(t, target - t.position, deltaSeconds);
                t.position = next;
                SetMoving(b, true);
                if ((next - target).sqrMagnitude < 0.0001f) _arrived.Add(kv.Key);
            }
            foreach (int actor in _arrived)
            {
                _destinations.Remove(actor);
                if (_bodies.TryGetValue(actor, out Body b)) SetMoving(b, false);
            }
        }

        internal static int Clear()
        {
            int destroyed = 0;
            foreach (var kv in _bodies)
            {
                if (kv.Value.Root != null)
                {
                    UnityEngine.Object.Destroy(kv.Value.Root.gameObject);
                    destroyed++;
                }
            }
            _bodies.Clear();
            _destinations.Clear();
            return destroyed;
        }

        private static Body Build(int actor, Vector3 groundPos, FakeBodyLook look)
        {
            FakeBodyVisual visual = default;
            if (look != null)
            {
                try
                {
                    visual = look(actor, groundPos);
                }
                catch (Exception e)
                {
                    WLog.Line("fake_body_look_failed", secret: false, ("actor", actor), ("err", e.Message));
                    visual = default;
                }
            }
            if (visual.IsEmpty) visual = BuildCapsule(actor, groundPos);

            var body = new Body
            {
                Root = visual.Root.transform,
                PivotHeight = visual.PivotHeight,
                Animator = visual.Animator,
            };
            PlayerAvatar local = PlayerAvatar.instance;
            if (local != null)
            {
                FaceToward(body.Root, local.transform.position - groundPos, float.PositiveInfinity);
            }
            return body;
        }

        private static FakeBodyVisual BuildCapsule(int actor, Vector3 groundPos)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "WW_FakeBody_" + actor;
            go.transform.position = groundPos + Vector3.up * CapsuleCenterHeight;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            TryTint(go, actor);
            return new FakeBodyVisual(go, CapsuleCenterHeight, animator: null);
        }

        private static void FaceToward(Transform t, Vector3 direction, float deltaSeconds)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            t.rotation = float.IsPositiveInfinity(deltaSeconds)
                ? target
                : Quaternion.RotateTowards(t.rotation, target, TurnSpeedDegPerSec * deltaSeconds);
        }

        private static void SetMoving(Body b, bool moving)
        {
            if (b.Animator != null) b.Animator.SetBool(MovingAnimParam, moving);
        }

        private static void Prune()
        {
            List<int> stale = null;
            foreach (var kv in _bodies)
            {
                if (kv.Value.Root == null) (stale ??= new List<int>()).Add(kv.Key);
            }
            if (stale == null) return;
            foreach (int actor in stale)
            {
                _bodies.Remove(actor);
                _destinations.Remove(actor);
            }
        }

        private static void TryTint(GameObject go, int actor)
        {
            try
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) return;
                float hue = Mathf.Repeat(Mathf.Abs(actor) * 0.13f, 1f);
                renderer.material.color = Color.HSVToRGB(hue, 0.6f, 0.9f);
            }
            catch
            {
            }
        }
    }
}
