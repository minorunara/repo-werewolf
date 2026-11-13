#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class BomberStatusPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const float LeftMargin = HudGeometry.BomberHud.LeftMargin;
        private const float BottomMargin = HudGeometry.BomberHud.BottomMargin;
        private const float SlotSize = HudGeometry.BomberHud.SlotSize;
        private const float SlotGap = HudGeometry.BomberHud.SlotGap;
        private const float LabelHeight = HudGeometry.BomberHud.LabelHeight;
        private const float LabelGap = HudGeometry.BomberHud.LabelGap;
        private const float LabelFontSize = 26f;
        private const float CooldownFontSize = 44f;
        private const float AmmoFontSize = 34f;

        private const string PlantSpriteKey = "perk_bomb_plant";
        private const string DetonateSpriteKey = "perk_bomb_detonate";

        private static readonly Color LockedIconTint = new Color(0.45f, 0.45f, 0.45f, 0.80f);
        private static readonly Color ReadyIconTint = new Color(1f, 1f, 1f, 1f);
        private static readonly Color LockedLabelColor = new Color(0.55f, 0.55f, 0.55f, 0.90f);
        private static readonly Color ReadyLabelColor = new Color(1f, 0.95f, 0.85f, 0.95f);
        private static readonly Color AmmoTextColor = new Color(0.35f, 0.95f, 0.35f, 1f);
        private static readonly Color CooldownGrayOverlayTint = new Color(0.25f, 0.25f, 0.25f, 0.82f);

        private sealed class Slot
        {
            public Slot(GameObject root, Image icon, TextMeshProUGUI cooldown,
                TextMeshProUGUI keyLabel)
            {
                Root = root;
                Icon = icon;
                Cooldown = cooldown;
                KeyLabel = keyLabel;
            }

            public GameObject Root { get; }
            public Image Icon { get; }
            public TextMeshProUGUI Cooldown { get; }
            public TextMeshProUGUI KeyLabel { get; }
            public Sprite? ColorSprite { get; set; }
            public Sprite? GraySprite { get; set; }
            public Image? CooldownGrayOverlay { get; set; }
            public TextMeshProUGUI? Fallback { get; set; }
            public TextMeshProUGUI? Ammo { get; set; }
        }

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, Slot plantSlot, Slot detonateSlot)
            {
                Root = root;
                PlantSlot = plantSlot;
                DetonateSlot = detonateSlot;
            }

            public GameObject Root { get; }
            public Slot PlantSlot { get; }
            public Slot DetonateSlot { get; }
            public int LastPlantBrightQ { get; set; } = -1;
            public int LastDetonateBrightQ { get; set; } = -1;
            public int LastPlantCd { get; set; } = -1;
            public int LastDetonateCd { get; set; } = -1;
            public byte LastAmmo { get; set; } = 255;
            public string? LastPlantKeyName { get; set; }
            public string? LastDetonateKeyName { get; set; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(BomberStatusPanel));

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                float rootWidth = HudGeometry.BomberHud.Width;
                float rootHeight = HudGeometry.BomberHud.Height;

                RectTransform rect = UiKit.CreateAnchoredRect(layerRoot, "WW_BomberStatusPanel",
                    UiKit.BottomLeft, Vector2.zero,
                    new Vector2(LeftMargin, BottomMargin), new Vector2(rootWidth, rootHeight));
                root = rect.gameObject;

                float iconRowCenterY = LabelHeight + LabelGap + SlotSize / 2f;

                float plantCenterX = SlotSize / 2f;
                Slot plantSlot = BuildSlot(rect, "Plant", new Vector2(plantCenterX, iconRowCenterY),
                    spriteKey: PlantSpriteKey, includeAmmo: true);
                AddProgressOverlay(plantSlot, "PlantProgressGray");

                float detonateCenterX = SlotSize + SlotGap + SlotSize / 2f;
                Slot detonateSlot = BuildSlot(rect, "Detonate", new Vector2(detonateCenterX, iconRowCenterY),
                    spriteKey: DetonateSpriteKey, includeAmmo: false);
                AddProgressOverlay(detonateSlot, "DetonateCooldownGray");

                root.SetActive(false);
                _view = new ViewRefs(root, plantSlot, detonateSlot);
                _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                _view = null;
                if (root != null)
                {
                    root.SetActive(false);
                    UnityEngine.Object.Destroy(root);
                }
                throw;
            }
        }

        public void Tick(bool visible,
                         float plantFraction, int plantCooldownSec, byte ammo,
                         float detonateBrightFraction, int detonateCooldownSec,
                         string? plantKeyName, string? detonateKeyName)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            try
            {
                if (view.Root.activeSelf != visible) view.Root.SetActive(visible);
                if (!visible)
                {
                    view.LastPlantKeyName = null;
                    view.LastDetonateKeyName = null;
                    return;
                }

                int plantBrightQ = Mathf.Clamp(Mathf.RoundToInt(plantFraction * 100f), 0, 100);
                if (plantBrightQ != view.LastPlantBrightQ || plantCooldownSec != view.LastPlantCd
                    || ammo != view.LastAmmo || plantKeyName != view.LastPlantKeyName)
                {
                    ApplySlot(view.PlantSlot, plantBrightQ / 100f, plantCooldownSec, ammo,
                        Texts.Format(TextId.HudBomberPlantKeyFormat, plantKeyName ?? "?"));
                    view.LastPlantBrightQ = plantBrightQ;
                    view.LastPlantCd = plantCooldownSec;
                    view.LastAmmo = ammo;
                    view.LastPlantKeyName = plantKeyName;
                }

                int detonateBrightQ = Mathf.Clamp(Mathf.RoundToInt(detonateBrightFraction * 100f), 0, 100);
                if (detonateBrightQ != view.LastDetonateBrightQ || detonateCooldownSec != view.LastDetonateCd
                    || detonateKeyName != view.LastDetonateKeyName)
                {
                    ApplySlot(view.DetonateSlot, detonateBrightQ / 100f, detonateCooldownSec, ammo: 0,
                        Texts.Format(TextId.HudBomberDetonateKeyFormat, detonateKeyName ?? "?"));
                    view.LastDetonateBrightQ = detonateBrightQ;
                    view.LastDetonateCd = detonateCooldownSec;
                    view.LastDetonateKeyName = detonateKeyName;
                }
            }
            catch (Exception e)
            {
                WLog.Line("bomber_hud_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (view.Root.activeSelf) view.Root.SetActive(false);
            view.LastPlantKeyName = null;
            view.LastDetonateKeyName = null;
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null) return;
            if (view.PlantSlot.Fallback != null) view.PlantSlot.Fallback.text = Texts.Get(TextId.RoleNameBomber);
            if (view.DetonateSlot.Fallback != null) view.DetonateSlot.Fallback.text = Texts.Get(TextId.RoleNameBomber);
            view.LastPlantBrightQ = -1;
            view.LastDetonateBrightQ = -1;
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            return null;
        }

        private static void ApplySlot(Slot slot, float brightness, int cooldownSec, byte ammo, string keyLabel)
        {
            if (slot == null) return;
            float b = Mathf.Clamp01(brightness);
            Color iconTint = Color.Lerp(LockedIconTint, ReadyIconTint, b);
            Color labelColor = Color.Lerp(LockedLabelColor, ReadyLabelColor, b);

            if (slot.Icon.enabled)
            {
                slot.Icon.color = iconTint;
                if (slot.CooldownGrayOverlay == null)
                {
                    Sprite? want = b >= 0.999f
                        ? slot.ColorSprite
                        : (slot.GraySprite ?? slot.ColorSprite);
                    if (want != null && slot.Icon.sprite != want) slot.Icon.sprite = want;
                }
            }
            if (slot.Fallback != null) slot.Fallback.color = iconTint;
            if (slot.CooldownGrayOverlay != null)
            {
                slot.Icon.color = ReadyIconTint;
                slot.CooldownGrayOverlay.fillAmount = 1f - b;
            }
            slot.Cooldown.text = cooldownSec > 0 ? cooldownSec.ToString() : string.Empty;
            if (slot.Ammo != null) slot.Ammo.text = ammo.ToString();
            slot.KeyLabel.text = keyLabel;
            slot.KeyLabel.color = labelColor;
        }

        private static Slot BuildSlot(RectTransform parent, string name, Vector2 centerPos,
            string spriteKey, bool includeAmmo)
        {
            RectTransform slotRect = UiKit.CreateRect(parent, name + "Slot", Vector2.zero,
                new Vector2(SlotSize, SlotSize));
            UiKit.SetAnchorsBottomLeft(slotRect, centerPos, new Vector2(SlotSize, SlotSize));

            Image icon = UiKit.CreateImage(slotRect, name + "Icon", Vector2.zero,
                new Vector2(SlotSize, SlotSize), LockedIconTint);
            Sprite? sprite = AssetCatalog.GetSprite(spriteKey);
            Sprite? gray = sprite != null ? AssetCatalog.GetGraySprite(spriteKey) : null;
            if (sprite == null) sprite = AssetCatalog.GetBombIcon();
            TextMeshProUGUI? fallback = null;
            if (sprite != null)
            {
                icon.sprite = gray ?? sprite;
                icon.preserveAspect = true;
            }
            else
            {
                icon.enabled = false;
                fallback = UiKit.CreateText(slotRect, name + "Fallback",
                    Vector2.zero, new Vector2(SlotSize, SlotSize),
                    Texts.Get(TextId.RoleNameBomber), 20f, LockedIconTint, TextAlignmentOptions.Center);
            }

            TextMeshProUGUI cooldown = UiKit.CreateText(slotRect, name + "Cooldown",
                Vector2.zero, new Vector2(SlotSize, SlotSize),
                string.Empty, CooldownFontSize, Color.white, TextAlignmentOptions.Center);
            cooldown.outlineWidth = 0.25f;
            cooldown.outlineColor = Color.black;

            TextMeshProUGUI? ammo = null;
            if (includeAmmo)
            {
                ammo = UiKit.CreateText(slotRect, name + "Ammo",
                    new Vector2(0f, -SlotSize / 4f), new Vector2(SlotSize, SlotSize / 2f),
                    string.Empty, AmmoFontSize, AmmoTextColor, TextAlignmentOptions.Center);
                ammo.outlineWidth = 0.25f;
                ammo.outlineColor = Color.black;
            }

            TextMeshProUGUI keyLabel = UiKit.CreateText(parent, name + "KeyLabel",
                new Vector2(centerPos.x, LabelHeight / 2f), new Vector2(220f, LabelHeight),
                string.Empty, LabelFontSize, ReadyLabelColor, TextAlignmentOptions.Center);
            RectTransform keyRect = keyLabel.rectTransform;
            UiKit.SetAnchorsBottomLeft(keyRect, new Vector2(centerPos.x, LabelHeight / 2f),
                new Vector2(220f, LabelHeight));

            var slot = new Slot(slotRect.gameObject, icon, cooldown, keyLabel)
            {
                ColorSprite = sprite,
                GraySprite = gray,
                Fallback = fallback,
                Ammo = ammo,
            };
            return slot;
        }

        private static void AddProgressOverlay(Slot slot, string name)
        {
            if (!slot.Icon.enabled || slot.Icon.sprite == null) return;

            slot.Icon.preserveAspect = false;
            if (slot.ColorSprite != null) slot.Icon.sprite = slot.ColorSprite;
            slot.CooldownGrayOverlay = UiKit.CreateImage(slot.Icon.rectTransform.parent,
                name, Vector2.zero, new Vector2(SlotSize, SlotSize),
                CooldownGrayOverlayTint);
            slot.CooldownGrayOverlay.sprite = slot.GraySprite ?? slot.ColorSprite;
            slot.CooldownGrayOverlay.type = Image.Type.Filled;
            slot.CooldownGrayOverlay.fillMethod = Image.FillMethod.Vertical;
            slot.CooldownGrayOverlay.fillOrigin = (int)Image.OriginVertical.Top;
            slot.CooldownGrayOverlay.fillAmount = 1f;
            slot.CooldownGrayOverlay.transform.SetSiblingIndex(slot.Icon.transform.GetSiblingIndex() + 1);
        }
    }
}
