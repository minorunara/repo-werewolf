using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class CorpseReportHudPanel : IClientPanel
    {
        public enum Layout
        {
            AtGaugeSlot = 0,
            AboveMiniGauge = 1,
        }

        public string LayerName => "Hud";

        internal const float RightMargin = HudGeometry.HudIcon.RightMargin;
        private const float GaugeBottomY = HudGeometry.MiniGauge.BottomMargin;
        internal const float IconSize = HudGeometry.HudIcon.IconSize;
        internal const float PlatePadding = HudGeometry.HudIcon.PlatePadding;
        internal const float LabelHeight = HudGeometry.HudIcon.LabelHeight;
        internal const float LabelGap = HudGeometry.HudIcon.LabelGap;
        internal const float LabelFontSize = 26f;

        internal static float BaseY(Layout layout)
            => HudGeometry.HudIcon.ReportBottom(layout == Layout.AboveMiniGauge);

        private static readonly Color IdleIconTint = new Color(0.45f, 0.45f, 0.45f, 0.80f);
        private static readonly Color ActiveIconTint = new Color(1f, 0.95f, 0.85f, 1f);
        private static readonly Color IdleLabelColor = new Color(0.55f, 0.55f, 0.55f, 0.90f);
        private static readonly Color ActiveLabelColor = new Color(1f, 0.55f, 0.45f, 1f);

        private const float BlockedFaceSize = 64f;
        private static readonly Vector2 BlockedFaceOffset = new Vector2(34f, 34f);
        private static readonly Color CrossFallbackColor = new Color(0.90f, 0.15f, 0.15f, 0.95f);

        private const float PulseAmplitude = 0.15f;
        private const float PulseHz = 1.6f;

        private const int HauntingRippleCount = 4;
        private const int HauntingRippleTextureSize = 256;
        private const float HauntingRippleDurationSec = 0.9f;
        private const float HauntingRippleStartSize = 84f;
        private const float HauntingRippleEndSize = 250f;
        private const float HauntingRippleMaxAlpha = 0.55f;
        private static readonly Color HauntingRippleWeakColor = new Color(0.25f, 1f, 0.35f);
        private static readonly Color HauntingRippleMediumColor = new Color(1f, 0.82f, 0.10f);
        private static readonly Color HauntingRippleStrongColor = new Color(1f, 0.16f, 0.10f);

        private GameObject _root;
        private RectTransform _iconRect;
        private RectTransform _slotRect;
        private Image _icon;
        private Sprite _iconSpriteColor;
        private Sprite _iconSpriteGray;
        private TextMeshProUGUI _iconFallback;
        private TextMeshProUGUI _keyLabel;
        private GameObject _blockedRoot;
        private RawImage[] _hauntingRippleImages;
        private float[] _hauntingRippleRemainingSec;
        private Color[] _hauntingRippleColors;
        private Texture2D _hauntingRippleTexture;
        private int _nextHauntingRipple;

        private Layout _lastLayout = (Layout)(-1);
        private bool _lastNear;
        private bool _lastBlocked;
        private string _lastKeyName;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            float plate = IconSize + PlatePadding;
            float rootWidth = plate;
            float rootHeight = plate + LabelGap + LabelHeight;

            RectTransform rect = UiKit.CreateAnchoredRect(layerRoot, "WW_CorpseReportHudPanel",
                UiKit.BottomRight, new Vector2(1f, 0f),
                new Vector2(-RightMargin, GaugeBottomY), new Vector2(rootWidth, rootHeight));
            _root = rect.gameObject;

            float iconCenterX = rootWidth / 2f;
            float iconCenterY = LabelHeight + LabelGap + plate / 2f;

            _slotRect = UiKit.CreateRect(rect, "Slot", Vector2.zero, new Vector2(plate, plate));
            UiKit.SetAnchorsBottomLeft(_slotRect, new Vector2(iconCenterX, iconCenterY),
                new Vector2(plate, plate));

            BuildHauntingRipples();

            _icon = UiKit.CreateImage(_slotRect, "Icon", Vector2.zero,
                new Vector2(IconSize, IconSize), IdleIconTint);
            _iconRect = _icon.rectTransform;

            Sprite sprite = AssetCatalog.GetSprite("icon_host_megaphone");
            if (sprite != null)
            {
                _iconSpriteColor = sprite;
                _iconSpriteGray = AssetCatalog.GetGraySprite("icon_host_megaphone");
                _icon.sprite = _iconSpriteGray ?? sprite;
                _icon.preserveAspect = true;
            }
            else
            {
                _icon.enabled = false;
                _iconFallback = UiKit.CreateText(_slotRect, "IconFallback",
                    Vector2.zero, new Vector2(IconSize, IconSize),
                    "通報", 26f, IdleIconTint, TextAlignmentOptions.Center);
            }

            BuildBlockedOverlay(plate);

            _keyLabel = UiKit.CreateText(rect, "KeyLabel",
                new Vector2(iconCenterX, LabelHeight / 2f),
                new Vector2(rootWidth + 120f, LabelHeight),
                string.Empty, LabelFontSize, IdleLabelColor, TextAlignmentOptions.Center);
            UiKit.SetAnchorsBottomLeft(_keyLabel.rectTransform,
                new Vector2(iconCenterX, LabelHeight / 2f),
                new Vector2(rootWidth + 120f, LabelHeight));

            _root.SetActive(false);
        }

        public void Tick(CorpseReportHudMode mode, bool nearCorpse, string keyName, Layout layout)
        {
            if (_root == null) return;
            try
            {
                bool visible = mode != CorpseReportHudMode.Hidden;
                if (_root.activeSelf != visible) _root.SetActive(visible);
                if (!visible)
                {
                    ClearHauntingRipples();
                    _lastKeyName = null;
                    return;
                }

                if (layout != _lastLayout)
                {
                    _lastLayout = layout;
                    var r = (RectTransform)_root.transform;
                    r.anchoredPosition = new Vector2(-RightMargin, BaseY(layout));
                }

                TickHauntingRipples(Time.unscaledDeltaTime);

                bool blocked = mode == CorpseReportHudMode.Blocked;
                bool near = nearCorpse && !blocked;

                if (near && _iconRect != null)
                {
                    float pulse = 1f + PulseAmplitude
                        * Mathf.Sin(Time.unscaledTime * PulseHz * 2f * Mathf.PI);
                    _iconRect.localScale = new Vector3(pulse, pulse, 1f);
                }
                else if (_iconRect != null && _iconRect.localScale != Vector3.one)
                {
                    _iconRect.localScale = Vector3.one;
                }

                if (near == _lastNear && blocked == _lastBlocked && keyName == _lastKeyName)
                {
                    return;
                }
                _lastNear = near;
                _lastBlocked = blocked;
                _lastKeyName = keyName;

                if (_blockedRoot != null && _blockedRoot.activeSelf != blocked)
                {
                    _blockedRoot.SetActive(blocked);
                }

                Color iconTint = near ? ActiveIconTint : IdleIconTint;
                Color labelColor = near ? ActiveLabelColor : IdleLabelColor;

                if (_icon != null && _icon.enabled)
                {
                    _icon.color = iconTint;
                    Sprite want = near
                        ? (_iconSpriteColor ?? _iconSpriteGray)
                        : (_iconSpriteGray ?? _iconSpriteColor);
                    if (want != null && _icon.sprite != want) _icon.sprite = want;
                }
                if (_iconFallback != null) _iconFallback.color = iconTint;
                if (_keyLabel != null)
                {
                    _keyLabel.text = Texts.Format(TextId.HudCorpseReportKeyFormat, keyName ?? "?");
                    _keyLabel.color = labelColor;
                }
            }
            catch (Exception e)
            {
                WLog.Line("corpse_report_hud_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void PlayHauntingRipple(ShamanStormTier tier)
        {
            if (_root == null || !_root.activeSelf || _hauntingRippleImages == null) return;

            int index = _nextHauntingRipple;
            _nextHauntingRipple = (_nextHauntingRipple + 1) % _hauntingRippleImages.Length;
            RawImage image = _hauntingRippleImages[index];
            if (image == null) return;

            Color color = HauntingRippleColorOf(tier);
            _hauntingRippleColors[index] = color;
            _hauntingRippleRemainingSec[index] = HauntingRippleDurationSec;
            image.rectTransform.sizeDelta = new Vector2(
                HauntingRippleStartSize, HauntingRippleStartSize);
            image.color = WithAlpha(color, 0f);
            image.enabled = true;
        }

        public void ClearHauntingRipples()
        {
            if (_hauntingRippleImages == null) return;
            for (int i = 0; i < _hauntingRippleImages.Length; i++)
            {
                _hauntingRippleRemainingSec[i] = 0f;
                RawImage image = _hauntingRippleImages[i];
                if (image == null) continue;
                image.color = WithAlpha(_hauntingRippleColors[i], 0f);
                image.enabled = false;
            }
            _nextHauntingRipple = 0;
        }

        private static Color WithAlpha(Color color, float alpha)
            => new Color(color.r, color.g, color.b, alpha);

        private static Color HauntingRippleColorOf(ShamanStormTier tier)
        {
            switch (tier)
            {
                case ShamanStormTier.Weak: return HauntingRippleWeakColor;
                case ShamanStormTier.Medium: return HauntingRippleMediumColor;
                default: return HauntingRippleStrongColor;
            }
        }

        private void BuildHauntingRipples()
        {
            _hauntingRippleImages = new RawImage[HauntingRippleCount];
            _hauntingRippleRemainingSec = new float[HauntingRippleCount];
            _hauntingRippleColors = new Color[HauntingRippleCount];
            Texture2D texture = EnsureHauntingRippleTexture();

            for (int i = 0; i < HauntingRippleCount; i++)
            {
                var rippleGo = new GameObject("HauntingRipple" + i, typeof(RectTransform));
                var rippleRect = (RectTransform)rippleGo.transform;
                rippleRect.SetParent(_slotRect, false);
                rippleRect.anchorMin = rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
                rippleRect.pivot = new Vector2(0.5f, 0.5f);
                rippleRect.anchoredPosition = Vector2.zero;
                rippleRect.sizeDelta = new Vector2(
                    HauntingRippleStartSize, HauntingRippleStartSize);

                RawImage image = rippleGo.AddComponent<RawImage>();
                image.raycastTarget = false;
                image.texture = texture;
                image.color = WithAlpha(HauntingRippleStrongColor, 0f);
                image.enabled = false;
                _hauntingRippleColors[i] = HauntingRippleStrongColor;
                _hauntingRippleImages[i] = image;
            }
        }

        private void TickHauntingRipples(float deltaSeconds)
        {
            if (_hauntingRippleImages == null) return;
            if (deltaSeconds < 0f) deltaSeconds = 0f;

            for (int i = 0; i < _hauntingRippleImages.Length; i++)
            {
                RawImage image = _hauntingRippleImages[i];
                if (image == null || _hauntingRippleRemainingSec[i] <= 0f) continue;

                float remaining = Mathf.Max(0f,
                    _hauntingRippleRemainingSec[i] - deltaSeconds);
                _hauntingRippleRemainingSec[i] = remaining;
                float t = 1f - remaining / HauntingRippleDurationSec;
                float eased = 1f - (1f - t) * (1f - t);
                float size = Mathf.Lerp(
                    HauntingRippleStartSize, HauntingRippleEndSize, eased);
                image.rectTransform.sizeDelta = new Vector2(size, size);

                float fadeIn = Mathf.Clamp01(t / 0.08f);
                float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, t);
                float alpha = HauntingRippleMaxAlpha * fadeIn * fadeOut;
                image.color = WithAlpha(_hauntingRippleColors[i], alpha);

                if (remaining <= 0f) image.enabled = false;
            }
        }

        private Texture2D EnsureHauntingRippleTexture()
        {
            if (_hauntingRippleTexture != null) return _hauntingRippleTexture;

            var texture = new Texture2D(
                HauntingRippleTextureSize, HauntingRippleTextureSize,
                TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[HauntingRippleTextureSize * HauntingRippleTextureSize];

            for (int y = 0; y < HauntingRippleTextureSize; y++)
            {
                float dy = y / (float)(HauntingRippleTextureSize - 1) * 2f - 1f;
                for (int x = 0; x < HauntingRippleTextureSize; x++)
                {
                    float dx = x / (float)(HauntingRippleTextureSize - 1) * 2f - 1f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float outer = Mathf.Clamp01(
                        1f - Mathf.Abs(distance - 0.78f) / 0.060f);
                    float inner = Mathf.Clamp01(
                        1f - Mathf.Abs(distance - 0.58f) / 0.038f) * 0.45f;
                    byte alpha = (byte)(Mathf.Clamp01(outer + inner) * 255f);
                    pixels[y * HauntingRippleTextureSize + x] =
                        new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            texture.name = "WW_ShamanHauntingRipple";
            _hauntingRippleTexture = texture;
            return texture;
        }

        private void BuildBlockedOverlay(float plate)
        {
            var blocked = UiKit.CreateRect(_slotRect, "Blocked", Vector2.zero, new Vector2(plate, plate));
            _blockedRoot = blocked.gameObject;

            Sprite cross = AssetCatalog.GetSprite("emoji_cross_mark");
            if (cross != null)
            {
                Image x = UiKit.CreateImage(blocked, "Cross", Vector2.zero,
                    new Vector2(IconSize, IconSize), Color.white);
                x.sprite = cross;
                x.preserveAspect = true;
            }
            else
            {
                foreach (float angle in new[] { 45f, -45f })
                {
                    Image bar = UiKit.CreateImage(blocked, "CrossBar", Vector2.zero,
                        new Vector2(IconSize * 0.95f, 14f), CrossFallbackColor);
                    bar.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
                }
            }

            Sprite face = AssetCatalog.GetSprite("img_taxman_death");
            if (face != null)
            {
                Image f = UiKit.CreateImage(blocked, "TaxmanFace", BlockedFaceOffset,
                    new Vector2(BlockedFaceSize, BlockedFaceSize), Color.white);
                f.sprite = face;
                f.preserveAspect = true;
            }

            _blockedRoot.SetActive(false);
        }

        public void Hide()
        {
            ClearHauntingRipples();
            if (_root != null && _root.activeSelf) _root.SetActive(false);
            _lastKeyName = null;
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _slotRect = null; _icon = null; _iconRect = null; _iconFallback = null;
            _keyLabel = null; _blockedRoot = null;
            _hauntingRippleImages = null;
            _hauntingRippleRemainingSec = null;
            _hauntingRippleColors = null;
            if (_hauntingRippleTexture != null)
            {
                UnityEngine.Object.Destroy(_hauntingRippleTexture);
                _hauntingRippleTexture = null;
            }
            _nextHauntingRipple = 0;
            _lastLayout = (Layout)(-1);
            _lastNear = false;
            _lastBlocked = false;
            _lastKeyName = null;
        }

    }
}
