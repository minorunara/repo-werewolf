using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class BomberStatusPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const float LeftMargin = 48f;
        private const float BottomMargin = 150f;
        private const float SlotSize = 112f;
        private const float SlotGap = 22f;
        private const float LabelHeight = 34f;
        private const float LabelGap = 4f;
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
            public GameObject Root;
            public Image Icon;
            public Sprite ColorSprite;
            public Sprite GraySprite;
            public Image CooldownGrayOverlay;
            public TextMeshProUGUI Fallback;
            public TextMeshProUGUI Cooldown;
            public TextMeshProUGUI Ammo;
            public TextMeshProUGUI KeyLabel;
        }

        private GameObject _root;
        private Slot _plantSlot;
        private Slot _detonateSlot;

        private int _lastPlantBrightQ = -1;
        private int _lastDetonateBrightQ = -1;
        private int _lastPlantCd = -1;
        private int _lastDetonateCd = -1;
        private byte _lastAmmo = 255;
        private string _lastPlantKeyName;
        private string _lastDetonateKeyName;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            float rootWidth = SlotSize * 2f + SlotGap;
            float rootHeight = LabelHeight + LabelGap + SlotSize;

            var go = new GameObject("WW_BomberStatusPanel", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(LeftMargin, BottomMargin);
            rect.sizeDelta = new Vector2(rootWidth, rootHeight);
            _root = go;

            float iconRowCenterY = LabelHeight + LabelGap + SlotSize / 2f;

            float plantCenterX = SlotSize / 2f;
            _plantSlot = BuildSlot(rect, "Plant", new Vector2(plantCenterX, iconRowCenterY),
                spriteKey: PlantSpriteKey, includeAmmo: true);
            AddProgressOverlay(_plantSlot, "PlantProgressGray");

            float detonateCenterX = SlotSize + SlotGap + SlotSize / 2f;
            _detonateSlot = BuildSlot(rect, "Detonate", new Vector2(detonateCenterX, iconRowCenterY),
                spriteKey: DetonateSpriteKey, includeAmmo: false);
            AddProgressOverlay(_detonateSlot, "DetonateCooldownGray");

            _root.SetActive(false);
        }

        public void Tick(bool visible,
                         float plantFraction, int plantCooldownSec, byte ammo,
                         float detonateBrightFraction, int detonateCooldownSec,
                         string plantKeyName, string detonateKeyName)
        {
            if (_root == null) return;
            try
            {
                if (_root.activeSelf != visible) _root.SetActive(visible);
                if (!visible)
                {
                    _lastPlantKeyName = null;
                    _lastDetonateKeyName = null;
                    return;
                }

                int plantBrightQ = Mathf.Clamp(Mathf.RoundToInt(plantFraction * 100f), 0, 100);
                if (plantBrightQ != _lastPlantBrightQ || plantCooldownSec != _lastPlantCd
                    || ammo != _lastAmmo || plantKeyName != _lastPlantKeyName)
                {
                    ApplySlot(_plantSlot, plantBrightQ / 100f, plantCooldownSec, ammo,
                        Texts.Format(TextId.HudBomberPlantKeyFormat, plantKeyName ?? "?"));
                    _lastPlantBrightQ = plantBrightQ;
                    _lastPlantCd = plantCooldownSec;
                    _lastAmmo = ammo;
                    _lastPlantKeyName = plantKeyName;
                }

                int detonateBrightQ = Mathf.Clamp(Mathf.RoundToInt(detonateBrightFraction * 100f), 0, 100);
                if (detonateBrightQ != _lastDetonateBrightQ || detonateCooldownSec != _lastDetonateCd
                    || detonateKeyName != _lastDetonateKeyName)
                {
                    ApplySlot(_detonateSlot, detonateBrightQ / 100f, detonateCooldownSec, ammo: 0,
                        Texts.Format(TextId.HudBomberDetonateKeyFormat, detonateKeyName ?? "?"));
                    _lastDetonateBrightQ = detonateBrightQ;
                    _lastDetonateCd = detonateCooldownSec;
                    _lastDetonateKeyName = detonateKeyName;
                }
            }
            catch (Exception e)
            {
                WLog.Line("bomber_hud_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf) _root.SetActive(false);
            _lastPlantKeyName = null;
            _lastDetonateKeyName = null;
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _plantSlot = null;
            _detonateSlot = null;
            _lastPlantKeyName = null;
            _lastDetonateKeyName = null;
        }

        private static void ApplySlot(Slot slot, float brightness, int cooldownSec, byte ammo, string keyLabel)
        {
            if (slot == null) return;
            float b = Mathf.Clamp01(brightness);
            Color iconTint = Color.Lerp(LockedIconTint, ReadyIconTint, b);
            Color labelColor = Color.Lerp(LockedLabelColor, ReadyLabelColor, b);

            if (slot.Icon != null && slot.Icon.enabled)
            {
                slot.Icon.color = iconTint;
                if (slot.CooldownGrayOverlay == null)
                {
                    Sprite want = b >= 0.999f
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
            if (slot.Cooldown != null)
                slot.Cooldown.text = cooldownSec > 0 ? cooldownSec.ToString() : string.Empty;
            if (slot.Ammo != null) slot.Ammo.text = ammo.ToString();
            if (slot.KeyLabel != null)
            {
                slot.KeyLabel.text = keyLabel;
                slot.KeyLabel.color = labelColor;
            }
        }

        private static Slot BuildSlot(RectTransform parent, string name, Vector2 centerPos,
            string spriteKey, bool includeAmmo)
        {
            var slot = new Slot();

            RectTransform slotRect = UiKit.CreateRect(parent, name + "Slot", Vector2.zero,
                new Vector2(SlotSize, SlotSize));
            slotRect.anchorMin = slotRect.anchorMax = new Vector2(0f, 0f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = centerPos;
            slot.Root = slotRect.gameObject;

            slot.Icon = UiKit.CreateImage(slotRect, name + "Icon", Vector2.zero,
                new Vector2(SlotSize, SlotSize), LockedIconTint);
            Sprite sprite = AssetCatalog.GetSprite(spriteKey);
            Sprite gray = sprite != null ? AssetCatalog.GetGraySprite(spriteKey) : null;
            if (sprite == null) sprite = AssetCatalog.GetBombIcon();
            if (sprite != null)
            {
                slot.ColorSprite = sprite;
                slot.GraySprite = gray;
                slot.Icon.sprite = gray ?? sprite;
                slot.Icon.preserveAspect = true;
            }
            else
            {
                slot.Icon.enabled = false;
                slot.Fallback = UiKit.CreateText(slotRect, name + "Fallback",
                    Vector2.zero, new Vector2(SlotSize, SlotSize),
                    Texts.Get(TextId.RoleNameBomber), 20f, LockedIconTint, TextAlignmentOptions.Center);
            }

            slot.Cooldown = UiKit.CreateText(slotRect, name + "Cooldown",
                Vector2.zero, new Vector2(SlotSize, SlotSize),
                string.Empty, CooldownFontSize, Color.white, TextAlignmentOptions.Center);
            slot.Cooldown.outlineWidth = 0.25f;
            slot.Cooldown.outlineColor = Color.black;

            if (includeAmmo)
            {
                slot.Ammo = UiKit.CreateText(slotRect, name + "Ammo",
                    new Vector2(0f, -SlotSize / 4f), new Vector2(SlotSize, SlotSize / 2f),
                    string.Empty, AmmoFontSize, AmmoTextColor, TextAlignmentOptions.Center);
                slot.Ammo.outlineWidth = 0.25f;
                slot.Ammo.outlineColor = Color.black;
            }

            slot.KeyLabel = UiKit.CreateText(parent, name + "KeyLabel",
                new Vector2(centerPos.x, LabelHeight / 2f), new Vector2(220f, LabelHeight),
                string.Empty, LabelFontSize, ReadyLabelColor, TextAlignmentOptions.Center);
            RectTransform keyRect = slot.KeyLabel.rectTransform;
            keyRect.anchorMin = keyRect.anchorMax = new Vector2(0f, 0f);
            keyRect.pivot = new Vector2(0.5f, 0.5f);
            keyRect.anchoredPosition = new Vector2(centerPos.x, LabelHeight / 2f);
            keyRect.sizeDelta = new Vector2(220f, LabelHeight);

            return slot;
        }

        private static void AddProgressOverlay(Slot slot, string name)
        {
            if (slot == null || slot.Icon == null || !slot.Icon.enabled || slot.Icon.sprite == null) return;

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
