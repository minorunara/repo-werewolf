#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class MapStillCapture
    {
        internal sealed class Still
        {
            public Still(Texture2D texture, float minX, float maxX, float minZ, float maxZ)
            {
                Texture = texture;
                MinX = minX;
                MaxX = maxX;
                MinZ = minZ;
                MaxZ = maxZ;
            }

            public Texture2D Texture { get; }
            public float MinX { get; }
            public float MaxX { get; }
            public float MinZ { get; }
            public float MaxZ { get; }

            private byte[]? _png;

            public byte[]? PngBytes
            {
                get
                {
                    if (_png == null && Texture != null)
                    {
                        try
                        {
                            _png = ImageConversion.EncodeToPNG(Texture);
                        }
                        catch (Exception e)
                        {
                            WLog.Line("map_still_png_error", secret: false, ("err", e.Message));
                        }
                    }
                    return _png;
                }
            }
        }

        private static Still? _cached;

        internal static Still? GetOrCapture(float orthoSizeConfig, int resolutionPreset)
        {
            if (_cached != null) return _cached;
            _cached = TryCapture(orthoSizeConfig, resolutionPreset);
            return _cached;
        }

        internal static void Invalidate()
        {
            if (_cached == null) return;
            if (_cached.Texture != null) UnityEngine.Object.Destroy(_cached.Texture);
            _cached = null;
        }

        private static Still? TryCapture(float orthoSizeConfig, int resolutionPreset)
        {
            Map? map = Map.Instance;
            if (map == null || map.OverLayerParent == null)
            {
                WLog.Line("map_still_no_map", secret: false);
                return null;
            }

            var view = new MapRtView("capture");
            bool mapWasActive = map.Active;
            var layerRestore = new List<(Transform T, Vector3 LocalPos)>();
            var sweptSprites = new List<SpriteRenderer>();
            Transform? playerGraphic = map.PlayerGraphicTransform;
            bool playerGraphicWasActive = playerGraphic != null && playerGraphic.gameObject.activeSelf;
            Texture2D? tex = null;
            try
            {
                view.Open(orthoSizeConfig, resolutionPreset);
                Camera? cam = view.Camera;
                if (cam == null || cam.targetTexture == null)
                {
                    WLog.Line("map_still_no_camera", secret: false);
                    return null;
                }

                view.Tick(orthoSizeConfig);

                ApplyLayerHeights(map, layerRestore);
                SweepSprites(map.transform, sweptSprites);
                SweepSprites(map.OverLayerParent, sweptSprites);
                if (playerGraphic != null) playerGraphic.gameObject.SetActive(false);

                cam.Render();

                RenderTexture rt = cam.targetTexture;
                RenderTexture? prevActive = RenderTexture.active;
                try
                {
                    RenderTexture.active = rt;
                    tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
                    tex.Apply(false, false);
                }
                finally
                {
                    RenderTexture.active = prevActive;
                }

                float scale = map.Scale;
                float ortho = cam.orthographicSize;
                if (scale <= 1e-6f || ortho <= 1e-3f || rt.height <= 0)
                {
                    UnityEngine.Object.Destroy(tex);
                    tex = null;
                    WLog.Line("map_still_bad_projection", secret: false,
                        ("scale", scale), ("ortho", ortho));
                    return null;
                }
                float aspect = (float)rt.width / rt.height;
                Vector3 camPos = cam.transform.position;
                Vector3 origin = map.OverLayerParent.position;
                var still = new Still(
                    tex,
                    (camPos.x - ortho * aspect - origin.x) / scale,
                    (camPos.x + ortho * aspect - origin.x) / scale,
                    (camPos.z - ortho - origin.z) / scale,
                    (camPos.z + ortho - origin.z) / scale);
                WLog.Line("map_still_captured", secret: false,
                    ("w", rt.width), ("h", rt.height),
                    ("sweptSprites", sweptSprites.Count),
                    ("minX", still.MinX), ("maxX", still.MaxX),
                    ("minZ", still.MinZ), ("maxZ", still.MaxZ));
                return still;
            }
            catch (Exception e)
            {
                WLog.Line("map_still_error", secret: false, ("err", e.Message));
                if (tex != null) UnityEngine.Object.Destroy(tex);
                return null;
            }
            finally
            {
                foreach (SpriteRenderer sr in sweptSprites)
                {
                    if (sr != null) sr.enabled = true;
                }
                if (playerGraphic != null) playerGraphic.gameObject.SetActive(playerGraphicWasActive);
                foreach ((Transform t, Vector3 pos) in layerRestore)
                {
                    if (t != null) t.localPosition = pos;
                }
                view.Close();
                if (!mapWasActive)
                {
                    try { map.ActiveSet(false); }
                    catch (Exception e)
                    {
                        WLog.Line("map_still_restore_error", secret: false, ("err", e.Message));
                    }
                }
            }
        }

        private static void ApplyLayerHeights(Map map, List<(Transform T, Vector3 LocalPos)> restore)
        {
            if (map.Layers == null) return;
            foreach (MapLayer layer in map.Layers)
            {
                if (layer == null) continue;
                Transform t = layer.transform;
                restore.Add((t, t.localPosition));
                float y;
                if (layer.layer == map.PlayerLayer) y = 0f;
                else if (layer.layer == map.PlayerLayer - 1) y = map.GetLayerPosition(2).y;
                else if (layer.layer == map.PlayerLayer + 1) y = map.GetLayerPosition(3).y;
                else y = -5f;
                t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
            }
        }

        private static void SweepSprites(Transform? root, List<SpriteRenderer> swept)
        {
            if (root == null) return;
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers == null) return;
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr == null || !sr.enabled) continue;
                sr.enabled = false;
                swept.Add(sr);
            }
        }
    }
}
