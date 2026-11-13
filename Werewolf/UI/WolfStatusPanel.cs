#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class WolfStatusPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const float LeftMargin = HudGeometry.WolfStatus.LeftMargin;
        private const float BottomMargin = HudGeometry.WolfStatus.BottomMargin;
        private const float MainIconSize = HudGeometry.WolfStatus.MainIconSize;
        private const float PerkIconSize = HudGeometry.WolfStatus.PerkIconSize;
        private const float SlotPadding = HudGeometry.WolfStatus.SlotPadding;
        private const float SlotGap = HudGeometry.WolfStatus.SlotGap;
        private const float BeaconGap = HudGeometry.WolfStatus.BeaconGap;
        private const float LabelHeight = HudGeometry.WolfStatus.LabelHeight;
        private const float LabelFontSize = 26f;
        private const float LabelGap = HudGeometry.WolfStatus.LabelGap;
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
            public Slot(RectTransform root, Image icon, Sprite? colorSprite,
                Sprite? graySprite, TextMeshProUGUI? fallback)
            { Root = root; Icon = icon; ColorSprite = colorSprite; GraySprite = graySprite; Fallback = fallback; }
            public RectTransform Root { get; }
            public Image Icon { get; }
            public Sprite? ColorSprite { get; }
            public Sprite? GraySprite { get; }
            public TextMeshProUGUI? Fallback { get; }
        }

        private sealed class BeaconView
        {
            public BeaconView(Slot slot, Image? grayOverlay, TextMeshProUGUI cooldown, TextMeshProUGUI charges)
            { Slot = slot; GrayOverlay = grayOverlay; Cooldown = cooldown; Charges = charges; }
            public Slot Slot { get; }
            public Image? GrayOverlay { get; }
            public TextMeshProUGUI Cooldown { get; }
            public TextMeshProUGUI Charges { get; }
        }

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, Slot mainSlot, Slot[] perkSlots,
                TextMeshProUGUI jumpCharges, TextMeshProUGUI keyLabel,
                RectTransform glowRoot, Image glowBall, Image rayFast, Image raySlow,
                BeaconView beacon, TextMeshProUGUI beaconKeyLabel)
            {
                Root = root; MainSlot = mainSlot; PerkSlots = perkSlots;
                JumpCharges = jumpCharges; KeyLabel = keyLabel; GlowRoot = glowRoot;
                GlowBall = glowBall; RayFast = rayFast; RaySlow = raySlow;
                Beacon = beacon; BeaconKeyLabel = beaconKeyLabel;
            }
            public GameObject Root { get; }
            public Slot MainSlot { get; }
            public Slot[] PerkSlots { get; }
            public TextMeshProUGUI JumpCharges { get; }
            public TextMeshProUGUI KeyLabel { get; }
            public RectTransform GlowRoot { get; }
            public Image GlowBall { get; }
            public Image RayFast { get; }
            public Image RaySlow { get; }
            public BeaconView Beacon { get; }
            public TextMeshProUGUI BeaconKeyLabel { get; }
        }

        private static Sprite? _glowSprite;
        private static Sprite? _raySprite;

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(WolfStatusPanel));
        private GameObject? _root => _view?.Root;
        private Slot? _mainSlot => _view?.MainSlot;
        private Slot[]? _perkSlots => _view?.PerkSlots;
        private TextMeshProUGUI? _jumpCharges => _view?.JumpCharges;
        private TextMeshProUGUI? _keyLabel => _view?.KeyLabel;

        private RectTransform? _glowRoot => _view?.GlowRoot;
        private Vector2 _glowCenter;
        private Image? _glowBall => _view?.GlowBall;
        private Image? _rayFast => _view?.RayFast;
        private Image? _raySlow => _view?.RaySlow;

        private Slot? _beaconSlot => _view?.Beacon.Slot;
        private Image? _beaconGrayOverlay => _view?.Beacon.GrayOverlay;
        private TextMeshProUGUI? _beaconCooldown => _view?.Beacon.Cooldown;
        private TextMeshProUGUI? _beaconCharges => _view?.Beacon.Charges;
        private TextMeshProUGUI? _beaconKeyLabel => _view?.BeaconKeyLabel;

        private WolfStatusState _lastState;
        private bool _hasLastState;
        private string? _lastWolfKeyName;
        private string? _lastBeaconKeyName;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            float mainSlotSize = HudGeometry.WolfStatus.MainSlot;
            float perkSlotSize = HudGeometry.WolfStatus.PerkSlot;
            float perkGridWidth = HudGeometry.WolfStatus.PerkGrid;
            float perkGridHeight = HudGeometry.WolfStatus.PerkGrid;
            float rootWidth = HudGeometry.WolfStatus.Width;
            float rootHeight = HudGeometry.WolfStatus.Height;

            GameObject? root = null;
            try
            {
            RectTransform rect = UiKit.CreateAnchoredRect(layerRoot, "WW_WolfStatusPanel",
                UiKit.BottomLeft, Vector2.zero,
                new Vector2(LeftMargin, BottomMargin), new Vector2(rootWidth, rootHeight));
            root = rect.gameObject;

            float perkGridBottom = LabelHeight + LabelGap;
            float iconRowCenterY = perkGridBottom + perkGridHeight / 2f;

            float mainCenterX = mainSlotSize / 2f;

            _glowCenter = new Vector2(mainCenterX, iconRowCenterY);
            var glowRootGo = new GameObject("MainGlow", typeof(RectTransform));
            RectTransform glowRoot = (RectTransform)glowRootGo.transform;
            glowRoot.SetParent(rect, false);
            UiKit.SetAnchorsBottomLeft(glowRoot, _glowCenter, Vector2.zero);

            Image glowBall = UiKit.CreateImage(glowRoot, "GlowBall", Vector2.zero,
                new Vector2(GlowSizePx, GlowSizePx), GlowColor);
            glowBall.sprite = GlowSprite();
            Image raySlow = UiKit.CreateImage(glowRoot, "RaySlow", Vector2.zero,
                new Vector2(RaySizePx * 0.8f, RaySizePx * 0.8f), RaySlowTint);
            raySlow.sprite = RaySprite();
            Image rayFast = UiKit.CreateImage(glowRoot, "RayFast", Vector2.zero,
                new Vector2(RaySizePx, RaySizePx), Color.white);
            rayFast.sprite = RaySprite();

            glowRoot.gameObject.SetActive(false);

            Slot mainSlot = BuildSlot(rect, "Main", new Vector2(mainCenterX, iconRowCenterY),
                MainIconSize, "role_werewolf", Texts.Get(TextId.RoleNameWerewolf), fallbackFontSize: 30f);
            TextMeshProUGUI keyLabel = BuildKeyLabel(rect, "KeyLabel", mainCenterX);

            string[] iconKeys = { "perk_stamina", "perk_jump", "perk_enemy_ignore", "perk_heal" };
            string[] fallbackLabels =
            {
                Texts.Get(TextId.GaugePerkStaminaLabel),
                Texts.Get(TextId.GaugePerkJumpLabel),
                Texts.Get(TextId.GaugePerkEnemyIgnoreLabel),
                Texts.Get(TextId.GaugePerkHealLabel),
            };
            var perkSlots = new Slot[iconKeys.Length];
            float perkGridLeft = mainSlotSize + SlotGap;
            for (int i = 0; i < iconKeys.Length; i++)
            {
                int column = i % 2;
                int rowFromTop = i / 2;
                float x = perkGridLeft + perkSlotSize / 2f + column * (perkSlotSize + SlotGap);
                float y = perkGridBottom + perkGridHeight - perkSlotSize / 2f
                    - rowFromTop * (perkSlotSize + SlotGap);
                perkSlots[i] = BuildSlot(rect, "Perk" + i, new Vector2(x, y),
                    PerkIconSize, iconKeys[i], fallbackLabels[i], fallbackFontSize: 14f);
            }

            TextMeshProUGUI jumpCharges = UiKit.CreateText(perkSlots[1].Root, "JumpCharges",
                new Vector2(0f, -PerkIconSize / 4f), new Vector2(PerkIconSize, PerkIconSize / 2f),
                string.Empty, JumpChargesFontSize, ChargesTextColor, TextAlignmentOptions.Center);
            jumpCharges.outlineWidth = 0.25f;
            jumpCharges.outlineColor = Color.black;

            float beaconCenterX = perkGridLeft + perkGridWidth + BeaconGap + mainSlotSize / 2f;
            BeaconView beacon = BuildBeaconSlot(rect, new Vector2(beaconCenterX, iconRowCenterY));
            TextMeshProUGUI beaconKeyLabel = BuildKeyLabel(rect, "BeaconKeyLabel", beaconCenterX);

            root.SetActive(false);
            _view = new ViewRefs(root, mainSlot, perkSlots, jumpCharges, keyLabel,
                glowRoot, glowBall, rayFast, raySlow, beacon, beaconKeyLabel);
            _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                _view = null;
                if (root != null) UnityEngine.Object.Destroy(root);
                throw;
            }
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

                ApplySlot(_mainSlot!, state.Toggle);
                ApplySlot(_perkSlots![0], state.Stamina);
                ApplySlot(_perkSlots[1], state.Jump);
                ApplySlot(_perkSlots[2], state.EnemyIgnore);
                ApplySlot(_perkSlots[3], state.Heal);

                if (_jumpCharges != null)
                {
                    _jumpCharges.text = state.JumpCharges >= 0
                        ? state.JumpCharges.ToString()
                        : string.Empty;
                }

                _keyLabel!.text = Texts.Format(TextId.HudWolfToggleFormat, wolfKeyName ?? "?");
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

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null) return;
            SetFallback(view.MainSlot, TextId.RoleNameWerewolf);
            SetFallback(view.PerkSlots[0], TextId.GaugePerkStaminaLabel);
            SetFallback(view.PerkSlots[1], TextId.GaugePerkJumpLabel);
            SetFallback(view.PerkSlots[2], TextId.GaugePerkEnemyIgnoreLabel);
            SetFallback(view.PerkSlots[3], TextId.GaugePerkHealLabel);
            SetFallback(view.Beacon.Slot, TextId.HudBeaconLabel);
            _hasLastState = false;
        }

        private static void SetFallback(Slot slot, TextId textId)
        {
            if (slot.Fallback != null) slot.Fallback.text = Texts.Get(textId);
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
            _hasLastState = false;
            _lastWolfKeyName = null;
            _lastBeaconKeyName = null;
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;
            _view = null;
            _hasLastState = false;
            _lastWolfKeyName = null;
            _lastBeaconKeyName = null;
            return null;
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
            _glowBall!.color = color;
            _glowBall.rectTransform.localScale = new Vector3(
                1f + GlowBreathScale * breath + GlowStretchAmplitude * swayX,
                1f + GlowBreathScale * breath + GlowStretchAmplitude * swayY, 1f);

            _rayFast!.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * RayFastSpinDegPerSec);
            _raySlow!.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * RaySlowSpinDegPerSec);
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
                Sprite? want = visual == WolfPerkVisual.Locked
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
                    Sprite? want = fullGray
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
            float slotSize = iconSize + SlotPadding;

            RectTransform root = UiKit.CreateRect(parent, name + "Slot", Vector2.zero,
                new Vector2(slotSize, slotSize));
            UiKit.SetAnchorsBottomLeft(root, centerPos, new Vector2(slotSize, slotSize));

            Image icon = UiKit.CreateImage(root, name + "Icon", Vector2.zero,
                new Vector2(iconSize, iconSize), LockedIconTint);
            Sprite? sprite = AssetCatalog.GetSprite(spriteKey);
            Sprite? graySprite = null;
            TextMeshProUGUI? fallback = null;
            if (sprite != null)
            {
                graySprite = AssetCatalog.GetGraySprite(spriteKey);
                icon.sprite = graySprite ?? sprite;
                icon.preserveAspect = true;
            }
            else
            {
                icon.enabled = false;
                fallback = UiKit.CreateText(root, name + "Fallback",
                    Vector2.zero, new Vector2(slotSize - 4f, slotSize - 4f),
                    fallbackLabel, fallbackFontSize, LockedIconTint, TextAlignmentOptions.Center);
                fallback.textWrappingMode = TextWrappingModes.Normal;
            }
            return new Slot(root, icon, sprite, graySprite, fallback);
        }

        private static BeaconView BuildBeaconSlot(RectTransform parent, Vector2 centerPos)
        {
            Slot beaconSlot = BuildSlot(parent, "Beacon", centerPos,
                MainIconSize, "perk_beacon", Texts.Get(TextId.HudBeaconLabel),
                fallbackFontSize: 22f);

            Sprite? sprite = AssetCatalog.GetSprite("perk_beacon");
            Image? beaconGrayOverlay = null;
            if (sprite != null)
            {
                beaconSlot.Icon.preserveAspect = false;

                beaconGrayOverlay = UiKit.CreateImage(beaconSlot.Root, "BeaconGray",
                    Vector2.zero, new Vector2(MainIconSize, MainIconSize), BeaconGrayOverlayTint);
                beaconGrayOverlay.sprite = beaconSlot.GraySprite ?? sprite;
                beaconGrayOverlay.type = Image.Type.Filled;
                beaconGrayOverlay.fillMethod = Image.FillMethod.Vertical;
                beaconGrayOverlay.fillOrigin = (int)Image.OriginVertical.Top;
                beaconGrayOverlay.fillAmount = 0f;
            }

            TextMeshProUGUI beaconCooldown = UiKit.CreateText(beaconSlot.Root, "BeaconCooldown",
                Vector2.zero, new Vector2(MainIconSize, MainIconSize),
                string.Empty, CooldownFontSize, Color.white, TextAlignmentOptions.Center);
            beaconCooldown.outlineWidth = 0.25f;
            beaconCooldown.outlineColor = Color.black;

            TextMeshProUGUI beaconCharges = UiKit.CreateText(beaconSlot.Root, "BeaconCharges",
                new Vector2(0f, -MainIconSize / 4f), new Vector2(MainIconSize, MainIconSize / 2f),
                string.Empty, ChargesFontSize, ChargesTextColor, TextAlignmentOptions.Center);
            beaconCharges.outlineWidth = 0.25f;
            beaconCharges.outlineColor = Color.black;
            return new BeaconView(beaconSlot, beaconGrayOverlay, beaconCooldown, beaconCharges);
        }

        private static TextMeshProUGUI BuildKeyLabel(RectTransform parent, string name, float centerX)
        {
            TextMeshProUGUI label = UiKit.CreateText(parent, name,
                new Vector2(centerX, LabelHeight / 2f), new Vector2(220f, LabelHeight),
                string.Empty, LabelFontSize, ReadyLabelColor, TextAlignmentOptions.Center);
            UiKit.SetAnchorsBottomLeft(label.rectTransform, new Vector2(centerX, LabelHeight / 2f),
                new Vector2(220f, LabelHeight));
            return label;
        }

    }
}
