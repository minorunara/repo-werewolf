using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class WolfStatusPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const float LeftMargin = 48f;
        private const float BottomMargin = 150f;
        private const float MainIconSize = 112f;
        private const float PerkIconSize = 84f;
        private const float SlotPadding = 8f;
        private const float SlotGap = 10f;
        private const float BeaconGap = 22f;
        private const float LabelHeight = 34f;
        private const float LabelFontSize = 26f;
        private const float LabelGap = 4f;
        private const float CooldownFontSize = 44f;
        private const float ChargesFontSize = 34f;
        private const float JumpChargesFontSize = 34f;

        private static readonly Color LockedIconTint = new Color(0.45f, 0.45f, 0.45f, 0.80f);
        private static readonly Color ReadyIconTint = new Color(0.95f, 0.95f, 0.95f, 0.95f);
        private static readonly Color ActiveIconTint = Color.white;
        private static readonly Color LockedLabelColor = new Color(0.55f, 0.55f, 0.55f, 0.90f);
        private static readonly Color ReadyLabelColor = new Color(1f, 0.95f, 0.85f, 0.95f);
        private static readonly Color ActiveLabelColor = new Color(1f, 0.55f, 0.45f, 1f);

        private static readonly Color BeaconGrayOverlayTint = new Color(0.25f, 0.25f, 0.25f, 0.82f);
        private static readonly Color ChargesTextColor = new Color(0.35f, 0.95f, 0.35f, 1f);

        private const float GlowSizePx = 260f;
        private static readonly Color GlowColor = new Color(1f, 0.25f, 0.12f, 1f);
        private const float GlowBaseAlpha = 0.62f;
        private const float GlowPulseAmplitude = 0.33f;
        private const float GlowBreathScale = 0.08f;
        private const float GlowStretchAmplitude = 0.07f;
        private const float GlowSwayRangePx = 6f;
        private const float GlowFlickerSpeed = 2.5f;
        private const float GlowSwaySpeed = 0.7f;
        private const float RaySizePx = 330f;
        private const int RayCount = 7;
        private const float RaySharpness = 16f;
        private const float RayBaseAlpha = 0.35f;
        private const float RayFlickerAmplitude = 0.55f;
        private const float RayFlickerSpeed = 3.5f;
        private const float RayFastSpinDegPerSec = 16f;
        private const float RaySlowSpinDegPerSec = -9f;
        private static readonly Color RaySlowTint = new Color(1f, 0.75f, 0.55f, 1f);

        private sealed class Slot
        {
            public RectTransform Root;
            public Image Icon;
            public Sprite ColorSprite;
            public Sprite GraySprite;
            public TextMeshProUGUI Fallback;
        }

        private static Sprite _glowSprite;
        private static Sprite _raySprite;

        private GameObject _root;
        private Slot _mainSlot;
        private Slot[] _perkSlots;
        private TextMeshProUGUI _jumpCharges;
        private TextMeshProUGUI _keyLabel;

        private RectTransform _glowRoot;
        private Vector2 _glowCenter;
        private Image _glowBall;
        private Image _rayFast;
        private Image _raySlow;

        private Slot _beaconSlot;
        private Image _beaconGrayOverlay;
        private TextMeshProUGUI _beaconCooldown;
        private TextMeshProUGUI _beaconCharges;
        private TextMeshProUGUI _beaconKeyLabel;

        private WolfStatusState _lastState;
        private bool _hasLastState;
        private string _lastWolfKeyName;
        private string _lastBeaconKeyName;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            float mainSlotSize = MainIconSize + SlotPadding;
            float perkSlotSize = PerkIconSize + SlotPadding;
            float perkGridWidth = perkSlotSize * 2f + SlotGap;
            float perkGridHeight = perkSlotSize * 2f + SlotGap;
            float rootWidth = mainSlotSize + SlotGap + perkGridWidth + BeaconGap + mainSlotSize;
            float rootHeight = LabelHeight + LabelGap + perkGridHeight;

            var go = new GameObject("WW_WolfStatusPanel", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(LeftMargin, BottomMargin);
            rect.sizeDelta = new Vector2(rootWidth, rootHeight);
            _root = go;

            float perkGridBottom = LabelHeight + LabelGap;
            float iconRowCenterY = perkGridBottom + perkGridHeight / 2f;

            float mainCenterX = mainSlotSize / 2f;

            _glowCenter = new Vector2(mainCenterX, iconRowCenterY);
            var glowRootGo = new GameObject("MainGlow", typeof(RectTransform));
            _glowRoot = (RectTransform)glowRootGo.transform;
            _glowRoot.SetParent(rect, false);
            SetAnchorsBottomLeft(_glowRoot, _glowCenter, Vector2.zero);

            _glowBall = UiKit.CreateImage(_glowRoot, "GlowBall", Vector2.zero,
                new Vector2(GlowSizePx, GlowSizePx), GlowColor);
            _glowBall.sprite = GlowSprite();
            _raySlow = UiKit.CreateImage(_glowRoot, "RaySlow", Vector2.zero,
                new Vector2(RaySizePx * 0.8f, RaySizePx * 0.8f), RaySlowTint);
            _raySlow.sprite = RaySprite();
            _rayFast = UiKit.CreateImage(_glowRoot, "RayFast", Vector2.zero,
                new Vector2(RaySizePx, RaySizePx), Color.white);
            _rayFast.sprite = RaySprite();

            _glowRoot.gameObject.SetActive(false);

            _mainSlot = BuildSlot(rect, "Main", new Vector2(mainCenterX, iconRowCenterY),
                MainIconSize, "role_werewolf", Texts.Get(TextId.RoleNameWerewolf), fallbackFontSize: 30f);
            _keyLabel = BuildKeyLabel(rect, "KeyLabel", mainCenterX);

            string[] iconKeys = { "perk_stamina", "perk_jump", "perk_enemy_ignore", "perk_heal" };
            string[] fallbackLabels =
            {
                Texts.Get(TextId.GaugePerkStaminaLabel),
                Texts.Get(TextId.GaugePerkJumpLabel),
                Texts.Get(TextId.GaugePerkEnemyIgnoreLabel),
                Texts.Get(TextId.GaugePerkHealLabel),
            };
            _perkSlots = new Slot[iconKeys.Length];
            float perkGridLeft = mainSlotSize + SlotGap;
            for (int i = 0; i < iconKeys.Length; i++)
            {
                int column = i % 2;
                int rowFromTop = i / 2;
                float x = perkGridLeft + perkSlotSize / 2f + column * (perkSlotSize + SlotGap);
                float y = perkGridBottom + perkGridHeight - perkSlotSize / 2f
                    - rowFromTop * (perkSlotSize + SlotGap);
                _perkSlots[i] = BuildSlot(rect, "Perk" + i, new Vector2(x, y),
                    PerkIconSize, iconKeys[i], fallbackLabels[i], fallbackFontSize: 14f);
            }

            _jumpCharges = UiKit.CreateText(_perkSlots[1].Root, "JumpCharges",
                new Vector2(0f, -PerkIconSize / 4f), new Vector2(PerkIconSize, PerkIconSize / 2f),
                string.Empty, JumpChargesFontSize, ChargesTextColor, TextAlignmentOptions.Center);
            _jumpCharges.outlineWidth = 0.25f;
            _jumpCharges.outlineColor = Color.black;

            float beaconCenterX = perkGridLeft + perkGridWidth + BeaconGap + mainSlotSize / 2f;
            BuildBeaconSlot(rect, new Vector2(beaconCenterX, iconRowCenterY));
            _beaconKeyLabel = BuildKeyLabel(rect, "BeaconKeyLabel", beaconCenterX);

            _root.SetActive(false);
            WLog.Line("wolf_status_panel_built", secret: false);
        }

        public void Tick(WolfStatusState state, string wolfKeyName, string beaconKeyName)
        {
            if (_root == null) return;
            try
            {
                if (_root.activeSelf != state.Visible) _root.SetActive(state.Visible);
                if (!state.Visible)
                {
                    _hasLastState = false;
                    return;
                }

                TickGlow(state.Toggle == WolfPerkVisual.Active);

                if (_hasLastState && SameVisuals(_lastState, state)
                    && _lastWolfKeyName == wolfKeyName && _lastBeaconKeyName == beaconKeyName)
                {
                    return;
                }
                _lastState = state;
                _hasLastState = true;
                _lastWolfKeyName = wolfKeyName;
                _lastBeaconKeyName = beaconKeyName;

                ApplySlot(_mainSlot, state.Toggle);
                ApplySlot(_perkSlots[0], state.Stamina);
                ApplySlot(_perkSlots[1], state.Jump);
                ApplySlot(_perkSlots[2], state.EnemyIgnore);
                ApplySlot(_perkSlots[3], state.Heal);

                if (_jumpCharges != null)
                {
                    _jumpCharges.text = state.JumpCharges >= 0
                        ? state.JumpCharges.ToString()
                        : string.Empty;
                }

                _keyLabel.text = Texts.Format(TextId.HudWolfToggleFormat, wolfKeyName ?? "?");
                _keyLabel.color = LabelColor(state.Toggle);

                ApplyBeacon(state, beaconKeyName);
            }
            catch (Exception e)
            {
                WLog.Line("wolf_status_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf) _root.SetActive(false);
            if (_glowRoot != null && _glowRoot.gameObject.activeSelf) _glowRoot.gameObject.SetActive(false);
            _hasLastState = false;
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _mainSlot = null;
            _perkSlots = null;
            _jumpCharges = null;
            _keyLabel = null;
            _glowRoot = null;
            _glowBall = null;
            _rayFast = null;
            _raySlow = null;
            _beaconSlot = null;
            _beaconGrayOverlay = null;
            _beaconCooldown = null;
            _beaconCharges = null;
            _beaconKeyLabel = null;
            _hasLastState = false;
            _lastWolfKeyName = null;
            _lastBeaconKeyName = null;
        }

        private void TickGlow(bool active)
        {
            if (_glowRoot == null) return;
            if (_glowRoot.gameObject.activeSelf != active)
            {
                _glowRoot.gameObject.SetActive(active);
                WLog.Line("wolf_glow", secret: false, ("visible", active));
            }
            if (!active) return;

            float t = Time.unscaledTime;
            float breath = Mathf.PerlinNoise(t * GlowFlickerSpeed, 0f);
            float swayX = Mathf.PerlinNoise(t * GlowSwaySpeed, 10f) * 2f - 1f;
            float swayY = Mathf.PerlinNoise(t * GlowSwaySpeed, 20f) * 2f - 1f;

            _glowRoot.anchoredPosition = _glowCenter + new Vector2(swayX, swayY) * GlowSwayRangePx;

            Color color = GlowColor;
            color.a = GlowBaseAlpha + GlowPulseAmplitude * breath;
            _glowBall.color = color;
            _glowBall.rectTransform.localScale = new Vector3(
                1f + GlowBreathScale * breath + GlowStretchAmplitude * swayX,
                1f + GlowBreathScale * breath + GlowStretchAmplitude * swayY, 1f);

            _rayFast.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * RayFastSpinDegPerSec);
            _raySlow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * RaySlowSpinDegPerSec);
            SetAlpha(_rayFast, RayBaseAlpha + RayFlickerAmplitude * Mathf.PerlinNoise(t * RayFlickerSpeed, 40f));
            SetAlpha(_raySlow, RayBaseAlpha + RayFlickerAmplitude * Mathf.PerlinNoise(t * RayFlickerSpeed, 60f));
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static Sprite GlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _glowSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            _glowSprite.name = "WW_WolfGlowSprite";
            return _glowSprite;
        }

        private static Sprite RaySprite()
        {
            if (_raySprite != null) return _raySprite;

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float radius = size / 2f;
            var tipColor = new Color(1f, 0.30f, 0.08f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float theta = Mathf.Atan2(dy, dx);
                    float spike = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(RayCount * theta), RaySharpness);
                    float length = 0.78f + 0.22f * Mathf.Sin(3f * theta + 1.3f);
                    float radial = Mathf.Pow(Mathf.Clamp01(1f - d / length), 1.4f);
                    Color color = Color.Lerp(Color.white, tipColor, Mathf.SmoothStep(0.10f, 0.75f, d));
                    color.a = radial * spike;
                    pixels[y * size + x] = color;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _raySprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            _raySprite.name = "WW_WolfRaySprite";
            return _raySprite;
        }

        private static bool SameVisuals(WolfStatusState a, WolfStatusState b)
            => a.Toggle == b.Toggle && a.Stamina == b.Stamina
               && a.Jump == b.Jump && a.JumpCharges == b.JumpCharges
               && a.EnemyIgnore == b.EnemyIgnore && a.Heal == b.Heal
               && a.BeaconCharges == b.BeaconCharges
               && a.BeaconCooldownSec == b.BeaconCooldownSec
               && (int)(a.BeaconGrayFraction * 100f) == (int)(b.BeaconGrayFraction * 100f);

        private static Color LabelColor(WolfPerkVisual visual)
            => visual == WolfPerkVisual.Locked ? LockedLabelColor
                : visual == WolfPerkVisual.Active ? ActiveLabelColor
                : ReadyLabelColor;

        private static void ApplySlot(Slot slot, WolfPerkVisual visual)
        {
            if (slot == null) return;
            Color tint;
            switch (visual)
            {
                case WolfPerkVisual.Active: tint = ActiveIconTint; break;
                case WolfPerkVisual.Ready: tint = ReadyIconTint; break;
                default: tint = LockedIconTint; break;
            }
            if (slot.Icon != null && slot.Icon.enabled)
            {
                slot.Icon.color = tint;
                Sprite want = visual == WolfPerkVisual.Locked
                    ? (slot.GraySprite ?? slot.ColorSprite)
                    : slot.ColorSprite;
                if (want != null && slot.Icon.sprite != want) slot.Icon.sprite = want;
            }
            if (slot.Fallback != null) slot.Fallback.color = tint;
        }

        private void ApplyBeacon(WolfStatusState state, string beaconKeyName)
        {
            bool usable = state.BeaconCooldownSec <= 0 && state.BeaconCharges > 0;

            if (_beaconSlot != null)
            {
                if (_beaconSlot.Icon != null && _beaconSlot.Icon.enabled)
                {
                    bool fullGray = state.BeaconGrayFraction >= 1f;
                    _beaconSlot.Icon.color = fullGray ? LockedIconTint : ReadyIconTint;
                    Sprite want = fullGray
                        ? (_beaconSlot.GraySprite ?? _beaconSlot.ColorSprite)
                        : _beaconSlot.ColorSprite;
                    if (want != null && _beaconSlot.Icon.sprite != want)
                    {
                        _beaconSlot.Icon.sprite = want;
                    }
                }
                if (_beaconSlot.Fallback != null)
                {
                    _beaconSlot.Fallback.color = usable ? ReadyIconTint : LockedIconTint;
                }
            }
            if (_beaconGrayOverlay != null)
            {
                float fill = state.BeaconGrayFraction < 1f ? state.BeaconGrayFraction : 1f;
                _beaconGrayOverlay.fillAmount = fill > 0f ? fill : 0f;
            }
            if (_beaconCooldown != null)
            {
                _beaconCooldown.text = state.BeaconCooldownSec > 0
                    ? state.BeaconCooldownSec.ToString()
                    : string.Empty;
            }
            if (_beaconCharges != null)
            {
                _beaconCharges.text = state.BeaconCharges.ToString();
            }
            if (_beaconKeyLabel != null)
            {
                _beaconKeyLabel.text = Texts.Format(TextId.HudBeaconKeyFormat, beaconKeyName ?? "?");
                _beaconKeyLabel.color = usable ? ReadyLabelColor : LockedLabelColor;
            }
        }

        private static Slot BuildSlot(RectTransform parent, string name, Vector2 centerPos,
            float iconSize, string spriteKey, string fallbackLabel, float fallbackFontSize)
        {
            var slot = new Slot();
            float slotSize = iconSize + SlotPadding;

            slot.Root = UiKit.CreateRect(parent, name + "Slot", Vector2.zero,
                new Vector2(slotSize, slotSize));
            SetAnchorsBottomLeft(slot.Root, centerPos, new Vector2(slotSize, slotSize));

            slot.Icon = UiKit.CreateImage(slot.Root, name + "Icon", Vector2.zero,
                new Vector2(iconSize, iconSize), LockedIconTint);
            Sprite sprite = AssetCatalog.GetSprite(spriteKey);
            if (sprite != null)
            {
                slot.ColorSprite = sprite;
                slot.GraySprite = AssetCatalog.GetGraySprite(spriteKey);
                slot.Icon.sprite = slot.GraySprite ?? sprite;
                slot.Icon.preserveAspect = true;
            }
            else
            {
                slot.Icon.enabled = false;
                slot.Fallback = UiKit.CreateText(slot.Root, name + "Fallback",
                    Vector2.zero, new Vector2(slotSize - 4f, slotSize - 4f),
                    fallbackLabel, fallbackFontSize, LockedIconTint, TextAlignmentOptions.Center);
                slot.Fallback.enableWordWrapping = true;
            }
            return slot;
        }

        private void BuildBeaconSlot(RectTransform parent, Vector2 centerPos)
        {
            _beaconSlot = BuildSlot(parent, "Beacon", centerPos,
                MainIconSize, "perk_beacon", Texts.Get(TextId.HudBeaconLabel),
                fallbackFontSize: 22f);

            Sprite sprite = AssetCatalog.GetSprite("perk_beacon");
            if (sprite != null && _beaconSlot.Icon != null)
            {
                _beaconSlot.Icon.preserveAspect = false;

                _beaconGrayOverlay = UiKit.CreateImage(_beaconSlot.Root, "BeaconGray",
                    Vector2.zero, new Vector2(MainIconSize, MainIconSize), BeaconGrayOverlayTint);
                _beaconGrayOverlay.sprite = _beaconSlot.GraySprite ?? sprite;
                _beaconGrayOverlay.type = Image.Type.Filled;
                _beaconGrayOverlay.fillMethod = Image.FillMethod.Vertical;
                _beaconGrayOverlay.fillOrigin = (int)Image.OriginVertical.Top;
                _beaconGrayOverlay.fillAmount = 0f;
            }

            _beaconCooldown = UiKit.CreateText(_beaconSlot.Root, "BeaconCooldown",
                Vector2.zero, new Vector2(MainIconSize, MainIconSize),
                string.Empty, CooldownFontSize, Color.white, TextAlignmentOptions.Center);
            _beaconCooldown.outlineWidth = 0.25f;
            _beaconCooldown.outlineColor = Color.black;

            _beaconCharges = UiKit.CreateText(_beaconSlot.Root, "BeaconCharges",
                new Vector2(0f, -MainIconSize / 4f), new Vector2(MainIconSize, MainIconSize / 2f),
                string.Empty, ChargesFontSize, ChargesTextColor, TextAlignmentOptions.Center);
            _beaconCharges.outlineWidth = 0.25f;
            _beaconCharges.outlineColor = Color.black;
        }

        private static TextMeshProUGUI BuildKeyLabel(RectTransform parent, string name, float centerX)
        {
            TextMeshProUGUI label = UiKit.CreateText(parent, name,
                new Vector2(centerX, LabelHeight / 2f), new Vector2(220f, LabelHeight),
                string.Empty, LabelFontSize, ReadyLabelColor, TextAlignmentOptions.Center);
            SetAnchorsBottomLeft(label.rectTransform, new Vector2(centerX, LabelHeight / 2f),
                new Vector2(220f, LabelHeight));
            return label;
        }

        private static void SetAnchorsBottomLeft(RectTransform rect, Vector2 centerPos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centerPos;
            rect.sizeDelta = size;
        }
    }
}
