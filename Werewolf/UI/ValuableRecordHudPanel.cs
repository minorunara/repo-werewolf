using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ValuableRecordHudPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const string IconKey = "icon_valuable_record";

        private const float RightMargin = CorpseReportHudPanel.RightMargin;
        private const float IconSize = CorpseReportHudPanel.IconSize;
        private const float PlatePadding = CorpseReportHudPanel.PlatePadding;
        private const float LabelHeight = CorpseReportHudPanel.LabelHeight;
        private const float LabelGap = CorpseReportHudPanel.LabelGap;
        private const float LabelFontSize = CorpseReportHudPanel.LabelFontSize;
        private const float StackGap = 12f;
        private const float LabelExtraWidth = 300f;

        private static readonly Color IconTint = new Color(1f, 0.95f, 0.85f, 1f);
        private static readonly Color GrayOverlayTint = new Color(0.25f, 0.25f, 0.25f, 0.82f);
        private static readonly Color IdleLabelColor = new Color(0.55f, 0.55f, 0.55f, 0.90f);
        private static readonly Color ActiveLabelColor = new Color(0.55f, 0.90f, 1f, 1f);

        private GameObject _root;
        private RectTransform _slotRect;
        private Image _icon;
        private Image _grayOverlay;
        private Sprite _iconSpriteColor;
        private Sprite _iconSpriteGray;
        private TextMeshProUGUI _iconFallback;
        private TextMeshProUGUI _keyLabel;

        private float _lastY = float.NaN;
        private bool _lastOn;
        private string _lastKeyName;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            float plate = IconSize + PlatePadding;
            float rootWidth = plate;
            float rootHeight = plate + LabelGap + LabelHeight;

            var go = new GameObject("WW_ValuableRecordHudPanel", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-RightMargin, CorpseReportHudPanel.BaseY(
                CorpseReportHudPanel.Layout.AboveMiniGauge));
            rect.sizeDelta = new Vector2(rootWidth, rootHeight);
            _root = go;

            float iconCenterX = rootWidth / 2f;
            float iconCenterY = LabelHeight + LabelGap + plate / 2f;

            _slotRect = UiKit.CreateRect(rect, "Slot", Vector2.zero, new Vector2(plate, plate));
            SetAnchorsBottomLeft(_slotRect, new Vector2(iconCenterX, iconCenterY),
                new Vector2(plate, plate));

            _icon = UiKit.CreateImage(_slotRect, "Icon", Vector2.zero,
                new Vector2(IconSize, IconSize), IconTint);

            Sprite sprite = AssetCatalog.GetSprite(IconKey);
            if (sprite != null)
            {
                _iconSpriteColor = sprite;
                _iconSpriteGray = AssetCatalog.GetGraySprite(IconKey);
                _icon.sprite = _iconSpriteColor;
                _icon.preserveAspect = false;

                _grayOverlay = UiKit.CreateImage(_slotRect, "RecordGray", Vector2.zero,
                    new Vector2(IconSize, IconSize), GrayOverlayTint);
                _grayOverlay.sprite = _iconSpriteGray ?? _iconSpriteColor;
                _grayOverlay.type = Image.Type.Filled;
                _grayOverlay.fillMethod = Image.FillMethod.Vertical;
                _grayOverlay.fillOrigin = (int)Image.OriginVertical.Top;
                _grayOverlay.fillAmount = 1f;
                _grayOverlay.transform.SetSiblingIndex(_icon.transform.GetSiblingIndex() + 1);
            }
            else
            {
                _icon.enabled = false;
                _iconFallback = UiKit.CreateText(_slotRect, "IconFallback",
                    Vector2.zero, new Vector2(IconSize, IconSize),
                    "記録", 26f, IdleLabelColor, TextAlignmentOptions.Center);
            }

            _keyLabel = UiKit.CreateText(rect, "KeyLabel",
                new Vector2(iconCenterX, LabelHeight / 2f),
                new Vector2(rootWidth + LabelExtraWidth, LabelHeight),
                string.Empty, LabelFontSize, IdleLabelColor, TextAlignmentOptions.Center);
            SetAnchorsBottomLeft(_keyLabel.rectTransform,
                new Vector2(iconCenterX, LabelHeight / 2f),
                new Vector2(rootWidth + LabelExtraWidth, LabelHeight));

            _root.SetActive(false);
            WLog.Line("valuable_record_hud_built", secret: false);
        }

        public void Tick(bool visible, bool recordOn, float holdRatio, bool holdCharging,
                         string keyName, CorpseReportHudPanel.Layout corpseLayout)
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

                float y = CorpseReportHudPanel.BaseY(corpseLayout)
                    + CorpseReportHudPanel.StackHeight + StackGap;
                if (!Mathf.Approximately(y, _lastY))
                {
                    _lastY = y;
                    ((RectTransform)_root.transform).anchoredPosition = new Vector2(-RightMargin, y);
                }

                float ratio = holdCharging ? Mathf.Clamp01(holdRatio) : 0f;
                float coverage = recordOn ? ratio : 1f - ratio;
                if (_grayOverlay != null) _grayOverlay.fillAmount = coverage;
                if (_iconFallback != null)
                {
                    _iconFallback.color = Color.Lerp(ActiveLabelColor, IdleLabelColor, coverage);
                }
                if (_keyLabel != null)
                {
                    _keyLabel.color = Color.Lerp(ActiveLabelColor, IdleLabelColor, coverage);
                }

                if (recordOn == _lastOn && keyName == _lastKeyName) return;
                _lastOn = recordOn;
                _lastKeyName = keyName;

                if (_keyLabel != null)
                {
                    _keyLabel.text = Texts.Format(
                        recordOn ? TextId.HudValuableRecordOnFormat : TextId.HudValuableRecordOffFormat,
                        keyName ?? "?");
                }
            }
            catch (Exception e)
            {
                WLog.Line("valuable_record_hud_tick_error", secret: false, ("err", e.Message));
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
            _slotRect = null; _icon = null; _grayOverlay = null; _iconFallback = null;
            _keyLabel = null;
            _lastY = float.NaN;
            _lastOn = false;
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
