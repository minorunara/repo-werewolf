using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class WerewolfUIManager
    {
        public const string MeetingLayer = "Meeting";

        public const string RoleGaugeLayer = "RoleGauge";

        public const string LobbyLayer = "Lobby";

        public const string MeetingMapLayer = "MeetingMap";

        public const string ModIntegrityLayer = "ModIntegrity";

        public const string ModIntegrityModalLayer = "ModIntegrityModal";

        public const string ManualLayer = "Manual";

        public const string ScenarioSelectLayer = "ScenarioSelect";

        internal const int PanelSortingOrder = 9999;
        internal const int TutorialBubbleSortingOrder = 10000;
        internal const int ScenarioHighlightSortingOrder = 10000;
        internal const int ScenarioGuideSortingOrder = 10001;
        internal const int ScenarioDecodeSortingOrder = 10000;
        internal const int VoidMatchSortingOrder = 10002;
        internal const int ManualSortingOrder = 10003;
        internal const int MatchSettingsSortingOrder = 10003;
        internal const int TutorialSortingOrder = 10000;
        internal const int ScenarioSelectSortingOrder = 10003;
        internal const int CursorSortingOrder = 10004;

        private GameObject _canvasRoot;
        private readonly Dictionary<string, GameObject> _layers = new Dictionary<string, GameObject>();

        public bool Exists => _canvasRoot != null;

        public bool EnsureCreated(GameObject host)
        {
            if (_canvasRoot != null) return true;
            if (host == null)
            {
                WLog.Line("ui_overlay_error", secret: false, ("reason", "no_host"));
                return false;
            }

            try
            {
                _canvasRoot = new GameObject("WW_OverlayCanvas");
                _canvasRoot.transform.SetParent(host.transform, false);

                Canvas canvas = _canvasRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = PanelSortingOrder;

                CanvasScaler scaler = _canvasRoot.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

                WLog.Line("ui_overlay_created", secret: false, ("sortingOrder", PanelSortingOrder));
                return true;
            }
            catch (Exception e)
            {
                WLog.Line("ui_overlay_error", secret: false, ("reason", "create_failed"), ("err", e.Message));
                Destroy();
                return false;
            }
        }

        public Transform GetLayerRoot(string layerName)
        {
            if (_canvasRoot == null || string.IsNullOrEmpty(layerName)) return null;
            if (_layers.TryGetValue(layerName, out GameObject existing) && existing != null)
            {
                return existing.transform;
            }

            GameObject layer = UiKit.CreateStretched(_canvasRoot.transform, $"WW_Layer_{layerName}").gameObject;
            layer.SetActive(false);

            _layers[layerName] = layer;
            return layer.transform;
        }

        public void SetLayerVisible(string layerName, bool visible)
        {
            if (!_layers.TryGetValue(layerName, out GameObject layer) || layer == null) return;
            if (layer.activeSelf == visible) return;
            layer.SetActive(visible);
        }

        public bool IsLayerVisible(string layerName)
            => _layers.TryGetValue(layerName, out GameObject layer) && layer != null && layer.activeSelf;

        public void Tick(MeetingClientState state, long nowUnixMs)
        {
            if (_canvasRoot == null) return;
            try
            {
                bool visible = state != null && state.MeetingActive && state.VotingUiReady(nowUnixMs);
                SetLayerVisible(MeetingLayer, visible);
                SetLayerVisible(MeetingMapLayer, visible);
            }
            catch (Exception e)
            {
                WLog.Line("ui_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Destroy()
        {
            if (_canvasRoot != null)
            {
                UnityEngine.Object.Destroy(_canvasRoot);
                _canvasRoot = null;
            }
            _layers.Clear();
        }
    }

    internal static class UiKit
    {
        private static TMP_FontAsset _cachedTmpFont;
        private static bool _tmpFontResolved;
        private static bool _tmpFallbackLogged;
        private static Sprite _whiteSprite;

        private const float CursorKeepAliveSeconds = 0.2f;

        public static void KeepCursorFree()
        {
            Inputs.UnlockCursor(CursorKeepAliveSeconds);
        }

        private static Sprite WhiteSprite()
        {
            if (_whiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                _whiteSprite.name = "WW_WhiteSprite";
            }
            return _whiteSprite;
        }

        internal static RectTransform CreateRect(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return rect;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static RectTransform CreateStretched(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        internal static Image CreateBackdrop(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateStretched(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static bool TryMeasureCanvas(RectTransform stretchedRect, out CanvasBounds canvas)
        {
            canvas = default;
            if (stretchedRect == null) return false;
            Vector2 size = stretchedRect.rect.size;
            canvas = new CanvasBounds(size.x, size.y);
            return canvas.IsMeasured;
        }

        internal static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        internal static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        internal static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        internal static readonly Vector2 TopRight = new Vector2(1f, 1f);
        internal static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        internal static RectTransform CreateAnchoredRect(Transform parent, string name, Vector2 anchor, Vector2 pivot,
                                                         Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return rect;
        }

        internal static void PinToCorner(RectTransform rect, Vector2 corner, Vector2 margin)
        {
            Vector2 size = rect.sizeDelta;
            float signX = corner.x < 0.5f ? 1f : -1f;
            float signY = corner.y < 0.5f ? 1f : -1f;
            rect.anchorMin = rect.anchorMax = corner;
            rect.pivot = Center;
            rect.anchoredPosition = new Vector2(
                signX * (margin.x + size.x * 0.5f),
                signY * (margin.y + size.y * 0.5f));
        }

        internal static void PinToCorner(RectTransform rect, Vector2 corner, float margin)
            => PinToCorner(rect, corner, new Vector2(margin, margin));

        internal static void SetAnchorsBottomLeft(RectTransform rect, Vector2 centerPos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centerPos;
            rect.sizeDelta = size;
        }

        internal static Image CreateImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchoredPos, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static Image CreateFilledImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            Image image = CreateImage(parent, name, anchoredPos, size, color);
            image.sprite = WhiteSprite();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 0f;
            return image;
        }

        private static Sprite _ringSprite;

        internal static Sprite RingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float outerR = size / 2f;
            float innerR = size * 0.36f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    Color c;
                    if (d > outerR || d < innerR) c = new Color(0f, 0f, 0f, 0f);
                    else c = new Color(1f, 1f, 1f, 1f);
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _ringSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            _ringSprite.name = "WW_RingSprite";
            return _ringSprite;
        }

        internal static Image CreateRadialImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            Image image = CreateImage(parent, name, anchoredPos, size, color);
            image.sprite = RingSprite();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0f;
            return image;
        }

        private static Sprite _roundedSprite;

        internal static Sprite RoundedRectSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;
            const int size = 64;
            const float radius = 16f;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _roundedSprite = Sprite.Create(
                tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            _roundedSprite.name = "WW_RoundedRectSprite";
            return _roundedSprite;
        }

        private static Sprite _bubbleTailSprite;

        internal static Sprite BubbleTailSprite()
        {
            if (_bubbleTailSprite != null) return _bubbleTailSprite;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            float diagonal = Mathf.Sqrt(2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = ((x + 0.5f) + (y + 0.5f) - size) / diagonal;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(d + 0.5f));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _bubbleTailSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            _bubbleTailSprite.name = "WW_BubbleTailSprite";
            return _bubbleTailSprite;
        }

        private static Sprite _circleSprite;

        internal static Sprite CircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            _circleSprite.name = "WW_CircleSprite";
            return _circleSprite;
        }

        private static Sprite _triangleUpSprite;

        internal static Sprite TriangleUpSprite()
        {
            if (_triangleUpSprite != null) return _triangleUpSprite;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                float halfWidth = (center - 1f) * (1f - (y + 0.5f) / size);
                for (int x = 0; x < size; x++)
                {
                    float d = halfWidth - Mathf.Abs((x + 0.5f) - center);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(d + 0.5f));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _triangleUpSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            _triangleUpSprite.name = "WW_TriangleUpSprite";
            return _triangleUpSprite;
        }

        internal static Image CreateRoundedImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            Image image = CreateImage(parent, name, anchoredPos, size, color);
            image.sprite = RoundedRectSprite();
            image.type = Image.Type.Sliced;
            return image;
        }

        internal static TextMeshProUGUI CreateText(
            Transform parent, string name, Vector2 anchoredPos, Vector2 size,
            string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(parent, name, anchoredPos, size);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = ResolveTmpFont();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.spriteAsset = EmojiSprites.Asset;
            label.textPreprocessor = EmojiSprites.Preprocessor;
            return label;
        }

        internal static TMP_FontAsset ResolveTmpFont()
        {
            if (_tmpFontResolved) return _cachedTmpFont;
            try
            {
                TMP_FontAsset chatFont = UiText.ChatFontOrNull;
                if (chatFont != null)
                {
                    _cachedTmpFont = chatFont;
                    _tmpFontResolved = true;
                    WLog.Line("ui_font", secret: false,
                        ("mode", "chatui"),
                        ("font", _cachedTmpFont != null ? _cachedTmpFont.name : "null"));
                    return _cachedTmpFont;
                }
            }
            catch (Exception e)
            {
                WLog.Line("ui_font_error", secret: false, ("src", "chatui"), ("err", e.Message));
            }
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            if (!_tmpFallbackLogged)
            {
                _tmpFallbackLogged = true;
                WLog.Line("ui_font", secret: false,
                    ("mode", "tmp_fallback"),
                    ("font", fallback != null ? fallback.name : "null"));
            }
            return fallback;
        }
    }
}
