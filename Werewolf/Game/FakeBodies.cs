using System.Collections.Generic;
using UnityEngine;

namespace Werewolf.Game
{
    internal static class FakeBodies
    {
        private const float CapsuleCenterHeight = 1f;

        private static readonly Dictionary<int, Transform> _bodies = new Dictionary<int, Transform>();

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
            if (_bodies.TryGetValue(actor, out Transform t) && t != null)
            {
                pos = GroundOf(t);
                return true;
            }
            pos = default;
            return false;
        }

        internal static List<(int actor, Vector3 pos)> Snapshot()
        {
            Prune();
            var list = new List<(int, Vector3)>(_bodies.Count);
            foreach (var kv in _bodies)
            {
                list.Add((kv.Key, GroundOf(kv.Value)));
            }
            return list;
        }

        private static Vector3 GroundOf(Transform t) => t.position - Vector3.up * CapsuleCenterHeight;

        internal static bool SpawnOrMove(int actor, Vector3 groundPos)
        {
            Vector3 center = groundPos + Vector3.up * CapsuleCenterHeight;
            if (_bodies.TryGetValue(actor, out Transform existing) && existing != null)
            {
                existing.position = center;
                return false;
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "WW_FakeBody_" + actor;
            go.transform.position = center;
            Object.Destroy(go.GetComponent<Collider>());
            TryTint(go, actor);
            _bodies[actor] = go.transform;
            return true;
        }

        internal static int Clear()
        {
            int destroyed = 0;
            foreach (var kv in _bodies)
            {
                if (kv.Value != null)
                {
                    Object.Destroy(kv.Value.gameObject);
                    destroyed++;
                }
            }
            _bodies.Clear();
            return destroyed;
        }

        private static void Prune()
        {
            List<int> stale = null;
            foreach (var kv in _bodies)
            {
                if (kv.Value == null) (stale ??= new List<int>()).Add(kv.Key);
            }
            if (stale == null) return;
            foreach (int actor in stale) _bodies.Remove(actor);
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
