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

        internal const float RightMargin = 25f;
        private const float GaugeBottomY = 50f;
        private const float GaugeTopY = 140f;
        private const float AboveGaugeGap = 20f;
        internal const float IconSize = 112f;
        internal const float PlatePadding = 8f;
        internal const float LabelHeight = 34f;
        internal const float LabelGap = 4f;
        internal const float LabelFontSize = 26f;

        internal const float StackHeight = IconSize + PlatePadding + LabelGap + LabelHeight;

        internal static float BaseY(Layout layout)
            => layout == Layout.AboveMiniGauge ? GaugeTopY + AboveGaugeGap : GaugeBottomY;

        private static readonly Color IdleIconTint = new Color(0.45f, 0.45f, 0.45f, 0.80f);
        private static readonly Color ActiveIconTint = new Color(1f, 0.95f, 0.85f, 1f);
        private static readonly Color IdleLabelColor = new Color(0.55f, 0.55f, 0.55f, 0.90f);
        private static readonly Color ActiveLabelColor = new Color(1f, 0.55f, 0.45f, 1f);

        private const float PulseAmplitude = 0.15f;
        private const float PulseHz = 1.6f;

        private GameObject _root;
        private RectTransform _iconRect;
        private RectTransform _slotRect;
        private Image _icon;
        private Sprite _iconSpriteColor;
        private Sprite _iconSpriteGray;
        private TextMeshProUGUI _iconFallback;
        private TextMeshProUGUI _keyLabel;

        private Layout _lastLayout = (Layout)(-1);
        private bool _lastNear;
        private string _lastKeyName;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            float plate = IconSize + PlatePadding;
            float rootWidth = plate;
            float rootHeight = plate + LabelGap + LabelHeight;

            var go = new GameObject("WW_CorpseReportHudPanel", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-RightMargin, GaugeBottomY);
            rect.sizeDelta = new Vector2(rootWidth, rootHeight);
            _root = go;

            float iconCenterX = rootWidth / 2f;
            float iconCenterY = LabelHeight + LabelGap + plate / 2f;

            _slotRect = UiKit.CreateRect(rect, "Slot", Vector2.zero, new Vector2(plate, plate));
            SetAnchorsBottomLeft(_slotRect, new Vector2(iconCenterX, iconCenterY),
                new Vector2(plate, plate));

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

            _keyLabel = UiKit.CreateText(rect, "KeyLabel",
                new Vector2(iconCenterX, LabelHeight / 2f),
                new Vector2(rootWidth + 120f, LabelHeight),
                string.Empty, LabelFontSize, IdleLabelColor, TextAlignmentOptions.Center);
            SetAnchorsBottomLeft(_keyLabel.rectTransform,
                new Vector2(iconCenterX, LabelHeight / 2f),
                new Vector2(rootWidth + 120f, LabelHeight));

            _root.SetActive(false);
            WLog.Line("corpse_report_hud_built", secret: false);
        }

        public void Tick(bool visible, bool nearCorpse, string keyName, Layout layout)
        {
            if (_root == null) return;
            try
            {
                if (_root.activeSelf != visible) _root.SetActive(visible);
                if (!visible)
                {
                    _lastKeyName = null;
                    return;
                }

                if (layout != _lastLayout)
                {
                    _lastLayout = layout;
                    var r = (RectTransform)_root.transform;
                    r.anchoredPosition = new Vector2(-RightMargin, BaseY(layout));
                }

                if (nearCorpse && _iconRect != null)
                {
                    float pulse = 1f + PulseAmplitude
                        * Mathf.Sin(Time.unscaledTime * PulseHz * 2f * Mathf.PI);
                    _iconRect.localScale = new Vector3(pulse, pulse, 1f);
                }
                else if (_iconRect != null && _iconRect.localScale != Vector3.one)
                {
                    _iconRect.localScale = Vector3.one;
                }

                if (nearCorpse == _lastNear && keyName == _lastKeyName)
                {
                    return;
                }
                _lastNear = nearCorpse;
                _lastKeyName = keyName;

                Color iconTint = nearCorpse ? ActiveIconTint : IdleIconTint;
                Color labelColor = nearCorpse ? ActiveLabelColor : IdleLabelColor;

                if (_icon != null && _icon.enabled)
                {
                    _icon.color = iconTint;
                    Sprite want = nearCorpse
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

        public void Hide()
        {
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
            _keyLabel = null;
            _lastLayout = (Layout)(-1);
            _lastNear = false;
            _lastKeyName = null;
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
