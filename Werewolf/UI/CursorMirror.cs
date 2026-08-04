using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class CursorMirror : IClientPanel
    {
        public string LayerName => "Cursor";

        private const int CaptureLayer = 31;
        private const int CaptureTexSize = 128;

        private const float MinHeight = 12f;
        private const float MaxHeight = 200f;

        internal static bool SuppressVanillaCursor;

        public float SizeScale { get; set; } = 1f;

        private GameObject _root;
        private Image _image;
        private RectTransform _imageRect;
        private Canvas _canvas;

        private MenuCursor _capturedFrom;
        private Sprite _capturedSprite;
        private Vector2 _capturedPivot;
        private Vector2 _capturedSize;
        private Color _capturedColor = Color.white;
        private bool _captureLoggedForInstance;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_CursorMirror", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.CursorSortingOrder;
            _canvas = canvas;
            _root = go;

            var imgGo = new GameObject("Cursor", typeof(RectTransform));
            _imageRect = (RectTransform)imgGo.transform;
            _imageRect.SetParent(rect, false);
            _imageRect.anchorMin = _imageRect.anchorMax = Vector2.zero;
            _image = imgGo.AddComponent<Image>();
            _image.raycastTarget = false;
            _image.preserveAspect = true;
            imgGo.SetActive(false);
        }

        public void Tick(bool active)
        {
            if (_root == null || !active)
            {
                SetVisible(false);
                SuppressVanillaCursor = false;
                return;
            }

            MenuCursor cursor = MenuCursor.instance;
            if (!TryEnsureCapture(cursor))
            {
                SetVisible(false);
                SuppressVanillaCursor = false;
                return;
            }

            _image.sprite = _capturedSprite;
            _image.color = _capturedColor;
            _imageRect.pivot = _capturedPivot;
            _imageRect.sizeDelta = _capturedSize * Mathf.Clamp(SizeScale, 0.25f, 4f);
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            if (scale <= 0f) scale = 1f;
            _imageRect.anchoredPosition = (Vector2)Input.mousePosition / scale;
            SetVisible(true);
            SuppressVanillaCursor = true;
        }

        public void Destroy()
        {
            SuppressVanillaCursor = false;
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _image = null;
            _imageRect = null;
            _canvas = null;
        }

        private void SetVisible(bool visible)
        {
            if (_image != null && _image.gameObject.activeSelf != visible)
            {
                _image.gameObject.SetActive(visible);
            }
        }

        private bool TryEnsureCapture(MenuCursor cursor)
        {
            if (cursor == null) return false;
            if (_capturedSprite != null && _capturedFrom == cursor) return true;
            if (_capturedFrom != cursor) _captureLoggedForInstance = false;

            try
            {
                if (cursor.transform.childCount == 0) return false;
                GameObject visual = cursor.transform.GetChild(0).gameObject;

                if (!visual.activeInHierarchy) return false;

                float settleScale = cursor.transform.localScale.x;
                if (settleScale < 0.9f) return false;

                Canvas vanillaCanvas = cursor.GetComponentInParent<Canvas>();
                Canvas vanillaRoot = vanillaCanvas != null ? vanillaCanvas.rootCanvas : null;
                if (vanillaRoot == null) return false;

                LogInventoryOnce(cursor, visual, vanillaRoot);

                if (!TryGetWorldBounds(visual, out Bounds bounds)) return false;

                Sprite sprite = ResolveSprite(visual, vanillaRoot, bounds, out Color tint);
                if (sprite == null)
                {
                    WLog.Line("cursor_mirror_capture_failed", secret: false, ("reason", "no_strategy"));
                    return false;
                }

                Transform rootT = vanillaRoot.transform;
                float halfW = ProjectedHalfExtent(bounds, rootT.right);
                float halfH = ProjectedHalfExtent(bounds, rootT.up);
                if (halfW <= 0f || halfH <= 0f) return false;

                Vector3 rel = cursor.transform.position - bounds.center;
                float pivotX = Mathf.Clamp01(0.5f + Vector3.Dot(rel, rootT.right) / (2f * halfW));
                float pivotY = Mathf.Clamp01(0.5f + Vector3.Dot(rel, rootT.up) / (2f * halfH));

                RectTransform rootRect = (RectTransform)rootT;
                float canvasWorldH = rootRect.rect.height * rootT.lossyScale.y;
                float ourCanvasH = ((RectTransform)_root.transform).rect.height;
                float height = canvasWorldH > 0f
                    ? Mathf.Clamp(2f * halfH / settleScale / canvasWorldH * ourCanvasH, MinHeight, MaxHeight)
                    : 48f;
                float width = height * (halfW / halfH);

                _capturedFrom = cursor;
                _capturedSprite = sprite;
                _capturedPivot = new Vector2(pivotX, pivotY);
                _capturedSize = new Vector2(width, height);
                _capturedColor = tint;
                WLog.Line("cursor_mirror_captured", secret: false,
                    ("size", $"{width:F1}x{height:F1}"),
                    ("pivot", $"{pivotX:F2},{pivotY:F2}"),
                    ("tint", tint.ToString()),
                    ("settleScale", settleScale.ToString("F3")),
                    ("worldHalf", $"{halfW:F4}x{halfH:F4}"),
                    ("canvasWorldH", canvasWorldH.ToString("F4")));
                return true;
            }
            catch (Exception e)
            {
                WLog.Line("cursor_mirror_capture_failed", secret: false, ("reason", "exception"), ("err", e.Message));
                return false;
            }
        }

        private void LogInventoryOnce(MenuCursor cursor, GameObject visual, Canvas vanillaRoot)
        {
            if (_captureLoggedForInstance) return;
            _captureLoggedForInstance = true;

            var parts = new List<string>();
            foreach (Component c in visual.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string extra = "";
                if (c is Image img) extra = $"(sprite={(img.sprite != null ? img.sprite.name : "null")})";
                else if (c is RawImage raw) extra = $"(tex={(raw.texture != null ? raw.texture.name : "null")})";
                else if (c is SpriteRenderer sr) extra = $"(sprite={(sr.sprite != null ? sr.sprite.name : "null")})";
                else if (c is MeshFilter mf) extra = $"(mesh={(mf.sharedMesh != null ? mf.sharedMesh.name : "null")})";
                else if (c is Renderer r) extra = $"(mat={(r.sharedMaterial != null ? r.sharedMaterial.name : "null")})";
                parts.Add($"{c.gameObject.name}:{c.GetType().Name}{extra}");
            }
            WLog.Line("cursor_mirror_inventory", secret: false,
                ("visual", visual.name),
                ("components", string.Join(" | ", parts)),
                ("canvasMode", vanillaRoot.renderMode),
                ("cursorScale", cursor.transform.lossyScale.ToString("F4")));
        }

        private static bool TryGetWorldBounds(GameObject visual, out Bounds bounds)
        {
            bounds = default;
            bool has = false;

            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(false))
            {
                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
            var corners = new Vector3[4];
            foreach (Graphic g in visual.GetComponentsInChildren<Graphic>(false))
            {
                g.rectTransform.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    if (!has) { bounds = new Bounds(corners[i], Vector3.zero); has = true; }
                    else bounds.Encapsulate(corners[i]);
                }
            }
            return has && bounds.size.sqrMagnitude > 1e-10f;
        }

        private static float ProjectedHalfExtent(Bounds bounds, Vector3 axis)
        {
            Vector3 e = bounds.extents;
            return Mathf.Abs(axis.x * e.x) + Mathf.Abs(axis.y * e.y) + Mathf.Abs(axis.z * e.z);
        }

        private Sprite ResolveSprite(GameObject visual, Canvas vanillaRoot, Bounds bounds, out Color tint)
        {
            tint = Color.white;
            Image img = visual.GetComponentInChildren<Image>(false);
            if (img != null && img.sprite != null)
            {
                tint = img.color;
                WLog.Line("cursor_mirror_strategy", secret: false, ("mode", "image_sprite"),
                    ("name", img.sprite.name), ("tint", tint.ToString()),
                    ("mat", img.material != null ? img.material.name : "null"));
                return img.sprite;
            }
            SpriteRenderer sr = visual.GetComponentInChildren<SpriteRenderer>(false);
            if (sr != null && sr.sprite != null)
            {
                tint = sr.color;
                WLog.Line("cursor_mirror_strategy", secret: false, ("mode", "sprite_renderer"),
                    ("name", sr.sprite.name), ("tint", tint.ToString()));
                return sr.sprite;
            }
            RawImage raw = visual.GetComponentInChildren<RawImage>(false);
            if (raw != null && raw.texture is Texture2D rawTex)
            {
                tint = raw.color;
                WLog.Line("cursor_mirror_strategy", secret: false, ("mode", "raw_image"),
                    ("name", rawTex.name), ("tint", tint.ToString()),
                    ("mat", raw.material != null ? raw.material.name : "null"));
                return Sprite.Create(rawTex, new Rect(0f, 0f, rawTex.width, rawTex.height), new Vector2(0.5f, 0.5f));
            }
            Sprite snap = SnapshotRenderers(visual, vanillaRoot, bounds);
            if (snap != null)
            {
                WLog.Line("cursor_mirror_strategy", secret: false, ("mode", "snapshot"));
            }
            return snap;
        }

        private static Sprite SnapshotRenderers(GameObject visual, Canvas vanillaRoot, Bounds bounds)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0) return null;

            Transform rootT = vanillaRoot.transform;
            float halfW = ProjectedHalfExtent(bounds, rootT.right) * 1.05f;
            float halfH = ProjectedHalfExtent(bounds, rootT.up) * 1.05f;
            if (halfW <= 0f || halfH <= 0f) return null;

            int texW = halfW >= halfH ? CaptureTexSize : Mathf.Max(8, Mathf.RoundToInt(CaptureTexSize * halfW / halfH));
            int texH = halfH >= halfW ? CaptureTexSize : Mathf.Max(8, Mathf.RoundToInt(CaptureTexSize * halfH / halfW));

            var originalLayers = new Dictionary<GameObject, int>();
            foreach (Renderer r in renderers)
            {
                originalLayers[r.gameObject] = r.gameObject.layer;
                r.gameObject.layer = CaptureLayer;
            }

            RenderTexture rt = RenderTexture.GetTemporary(texW, texH, 16, RenderTextureFormat.ARGB32);
            var camGo = new GameObject("WW_CursorSnapCam");
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                cam.orthographic = true;
                cam.orthographicSize = halfH;
                cam.aspect = halfW / halfH;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.clear;
                cam.cullingMask = 1 << CaptureLayer;
                float dist = halfW + halfH + 0.5f;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = dist * 2f + 1f;
                cam.targetTexture = rt;

                foreach (Vector3 dir in new[] { -rootT.forward, rootT.forward })
                {
                    cam.transform.position = bounds.center + dir * dist;
                    cam.transform.rotation = Quaternion.LookRotation(-dir, rootT.up);
                    cam.Render();
                    Texture2D tex = ReadRenderTexture(rt);
                    if (CountVisiblePixels(tex) >= 8)
                    {
                        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                    UnityEngine.Object.Destroy(tex);
                }
                WLog.Line("cursor_mirror_capture_failed", secret: false, ("reason", "snapshot_blank"));
                return null;
            }
            finally
            {
                foreach (KeyValuePair<GameObject, int> kv in originalLayers)
                {
                    if (kv.Key != null) kv.Key.layer = kv.Value;
                }
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.Destroy(camGo);
            }
        }

        private static Texture2D ReadRenderTexture(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        private static int CountVisiblePixels(Texture2D tex)
        {
            Color32[] pixels = tex.GetPixels32();
            int count = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 8) count++;
            }
            return count;
        }
    }
}
