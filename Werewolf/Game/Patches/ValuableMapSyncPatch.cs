using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    internal static class ValuableMapSyncPatch
    {
        private struct GhostRef
        {
            public GameObject Holder;
            public GameObject EntityGo;
        }

        private static readonly Dictionary<ValuableObject, GhostRef> _ghosts =
            new Dictionary<ValuableObject, GhostRef>();

        private static readonly AccessTools.FieldRef<ValuableObject, bool> DiscoveredRef =
            GameRefs.ValuableObject_discovered;

        private static readonly AccessTools.FieldRef<MapCustom, MapCustomEntity> MapCustomEntityRef =
            GameRefs.MapCustom_mapCustomEntity;

        private static ValuableMapMode _prevMode = ValuableMapMode.MeetingSync;
        private static bool _prevRoundActive;

        internal static void RefreshSnapshot(string reason)
        {
            WerewolfDirector dir = WerewolfDirector.Instance;
            if (dir == null || Map.Instance == null) return;
            if (!ValuableMapGate.ShouldRefreshSnapshotAtInventoryPoint(
                    dir.ClientValuableMapMode, dir.IsRoundActiveClient)) return;

            int vanillaRemoved = 0;
            if (Map.Instance.OverLayerParent != null)
            {
                foreach (MapValuable mv in Map.Instance.OverLayerParent.GetComponentsInChildren<MapValuable>(true))
                {
                    if (mv == null) continue;
                    UnityEngine.Object.Destroy(mv.gameObject);
                    vanillaRemoved++;
                }
            }

            int removed = 0;
            int moved = 0;
            List<ValuableObject> deadKeys = null;
            foreach (KeyValuePair<ValuableObject, GhostRef> kv in _ghosts)
            {
                if (kv.Key == null)
                {
                    DestroyGhost(kv.Value);
                    (deadKeys ?? (deadKeys = new List<ValuableObject>())).Add(kv.Key);
                    removed++;
                }
                else if (kv.Value.Holder != null)
                {
                    kv.Value.Holder.transform.position = kv.Key.transform.position;
                    moved++;
                }
            }
            if (deadKeys != null)
            {
                foreach (ValuableObject key in deadKeys) _ghosts.Remove(key);
            }

            int added = 0;
            foreach (ValuableObject valuable in UnityEngine.Object.FindObjectsOfType<ValuableObject>())
            {
                if (valuable == null || _ghosts.ContainsKey(valuable)) continue;
                if (!DiscoveredRef(valuable)) continue;
                if (PlaceSnapshotGhost(valuable)) added++;
            }

            WLog.Line("valuablemap_snapshot", secret: false, ("reason", reason),
                ("removed", removed), ("moved", moved), ("added", added), ("vanilla_removed", vanillaRemoved));
        }

        internal static void Tick()
        {
            WerewolfDirector dir = WerewolfDirector.Instance;
            if (dir == null) return;

            ValuableMapMode mode = dir.ClientValuableMapMode;
            bool roundActive = dir.IsRoundActiveClient;

            bool wasMeetingSync = _prevMode == ValuableMapMode.MeetingSync && _prevRoundActive;
            bool isMeetingSync = mode == ValuableMapMode.MeetingSync && roundActive;
            if (!wasMeetingSync && isMeetingSync)
            {
                RefreshSnapshot("gate_open_meeting_sync");
            }
            if (wasMeetingSync && !isMeetingSync)
            {
                int cleared = ClearAllGhosts();
                if (cleared > 0)
                {
                    WLog.Line("valuablemap_sweep", secret: false,
                        ("reason", "gate_close_meeting_sync"), ("removed", cleared));
                }
                RestoreDiscoveredValuables();
            }

            bool wasHidden = _prevMode == ValuableMapMode.Hidden && _prevRoundActive;
            bool isHidden = mode == ValuableMapMode.Hidden && roundActive;
            if (wasHidden && !isHidden)
            {
                RestoreDiscoveredValuables();
            }

            _prevMode = mode;
            _prevRoundActive = roundActive;
        }

        internal static void RestoreDiscoveredValuables()
        {
            if (Map.Instance == null) return;

            int restored = 0;
            foreach (ValuableObject valuable in UnityEngine.Object.FindObjectsOfType<ValuableObject>())
            {
                if (valuable == null) continue;
                if (!DiscoveredRef(valuable)) continue;
                Map.Instance.AddValuable(valuable);
                restored++;
            }
            WLog.Line("valuablemap_restore", secret: false, ("restored", restored));
        }

        internal static bool PlaceSnapshotGhost(ValuableObject valuable)
        {
            if (Map.Instance == null || valuable == null) return false;
            if (_ghosts.ContainsKey(valuable)) return false;

            try
            {
                Sprite sprite = null;
                Color color = Color.white;
                MapValuable proto = Map.Instance.ValuableObject != null
                    ? Map.Instance.ValuableObject.GetComponent<MapValuable>() : null;
                if (proto != null)
                {
                    sprite = valuable.volumeType <= ValuableVolume.Type.Medium ? proto.spriteSmall : proto.spriteBig;
                    if (proto.spriteRenderer != null) color = proto.spriteRenderer.color;
                }

                var holder = new GameObject("WW_ValuableGhost");
                holder.transform.SetPositionAndRotation(valuable.transform.position, Quaternion.identity);
                var mapCustom = holder.AddComponent<MapCustom>();
                mapCustom.autoAdd = false;
                mapCustom.sprite = sprite;
                mapCustom.color = color;

                Map.Instance.AddCustom(mapCustom, sprite, color);
                MapCustomEntity entity = MapCustomEntityRef(mapCustom);
                GameObject entityGo = entity != null ? entity.gameObject : null;
                _ghosts[valuable] = new GhostRef { Holder = holder, EntityGo = entityGo };

                WLog.Line("valuablemap_ghost_placed", secret: false,
                    ("name", valuable.gameObject.name),
                    ("pos", valuable.transform.position.ToString("F1")));
                return true;
            }
            catch (Exception e)
            {
                WLog.Line("valuablemap_ghost_error", secret: false, ("err", e.Message));
                return false;
            }
        }

        private static void DestroyGhost(GhostRef g)
        {
            if (g.EntityGo != null) UnityEngine.Object.Destroy(g.EntityGo);
            if (g.Holder != null) UnityEngine.Object.Destroy(g.Holder);
        }

        private static int ClearAllGhosts()
        {
            int removed = 0;
            foreach (KeyValuePair<ValuableObject, GhostRef> kv in _ghosts)
            {
                if (kv.Value.EntityGo != null || kv.Value.Holder != null) removed++;
                DestroyGhost(kv.Value);
            }
            _ghosts.Clear();
            return removed;
        }
    }

    [HarmonyPatch(typeof(Map), "AddValuable")]
    internal static class MapAddValuablePatch
    {
        private static bool Prefix(ValuableObject _valuable)
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null) return true;
                bool roundActive = dir.IsRoundActiveClient;
                if (roundActive) dir.MaybeShowTutorial(TutorialId.FirstValuableSeen);
                ValuableMapMode mode = dir.ClientValuableMapMode;
                if (!ValuableMapGate.ShouldSuppressAdd(mode, roundActive)) return true;
                if (ValuableMapGate.ShouldSnapshotOnDiscover(mode, roundActive))
                {
                    ValuableMapSyncPatch.PlaceSnapshotGhost(_valuable);
                }
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_valuablemap_add_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(RoundDirector), "ExtractionCompleted")]
    internal static class ExtractionCompletedPatch
    {
        private static void Postfix()
        {
            try
            {
                ValuableMapSyncPatch.RefreshSnapshot("extraction_completed");
            }
            catch (Exception e)
            {
                WLog.Line("patch_valuablemap_extraction_error", secret: false, ("err", e.Message));
            }
        }
    }
}
