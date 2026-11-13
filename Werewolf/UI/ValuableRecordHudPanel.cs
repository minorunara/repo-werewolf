#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class ValuableRecordHudPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const string IconKey = "icon_valuable_record";

        private const float RightMargin = HudGeometry.HudIcon.RightMargin;
        private const float IconSize = HudGeometry.HudIcon.IconSize;
        private const float PlatePadding = HudGeometry.HudIcon.PlatePadding;
        private const float LabelHeight = HudGeometry.HudIcon.LabelHeight;
        private const float LabelGap = HudGeometry.HudIcon.LabelGap;
        private const float LabelFontSize = CorpseReportHudPanel.LabelFontSize;
        private const float LabelExtraWidth = 300f;

        private static readonly Color IconTint = new Color(1f, 0.95f, 0.85f, 1f);
        private static readonly Color GrayOverlayTint = new Color(0.25f, 0.25f, 0.25f, 0.82f);
        private static readonly Color IdleLabelColor = new Color(0.55f, 0.55f, 0.55f, 0.90f);
        private static readonly Color ActiveLabelColor = new Color(0.55f, 0.90f, 1f, 1f);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, Image? grayOverlay,
                TextMeshProUGUI? iconFallback, TextMeshProUGUI keyLabel)
            {
                Root = root;
                GrayOverlay = grayOverlay;
                IconFallback = iconFallback;
                KeyLabel = keyLabel;
            }

            public GameObject Root { get; }
            public Image? GrayOverlay { get; }
            public TextMeshProUGUI? IconFallback { get; }
            public TextMeshProUGUI KeyLabel { get; }
            public float LastY { get; set; } = float.NaN;
            public bool LastOn { get; set; }
            public string? LastKeyName { get; set; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(ValuableRecordHudPanel));

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                float plate = IconSize + PlatePadding;
                float rootWidth = plate;
                float rootHeight = plate + LabelGap + LabelHeight;

                RectTransform rect = UiKit.CreateAnchoredRect(layerRoot, "WW_ValuableRecordHudPanel",
                    UiKit.BottomRight, new Vector2(1f, 0f),
                    new Vector2(-RightMargin, HudGeometry.HudIcon.ReportBottom(true)),
                    new Vector2(rootWidth, rootHeight));
                root = rect.gameObject;

                float iconCenterX = rootWidth / 2f;
                float iconCenterY = LabelHeight + LabelGap + plate / 2f;

                RectTransform slotRect = UiKit.CreateRect(rect, "Slot", Vector2.zero, new Vector2(plate, plate));
                UiKit.SetAnchorsBottomLeft(slotRect, new Vector2(iconCenterX, iconCenterY),
                    new Vector2(plate, plate));

                Image icon = UiKit.CreateImage(slotRect, "Icon", Vector2.zero,
                    new Vector2(IconSize, IconSize), IconTint);

                Image? grayOverlay = null;
                TextMeshProUGUI? iconFallback = null;
                Sprite? sprite = AssetCatalog.GetSprite(IconKey);
                if (sprite != null)
                {
                    Sprite? graySprite = AssetCatalog.GetGraySprite(IconKey);
                    icon.sprite = sprite;
                    icon.preserveAspect = false;

                    grayOverlay = UiKit.CreateImage(slotRect, "RecordGray", Vector2.zero,
                        new Vector2(IconSize, IconSize), GrayOverlayTint);
                    grayOverlay.sprite = graySprite ?? sprite;
                    grayOverlay.type = Image.Type.Filled;
                    grayOverlay.fillMethod = Image.FillMethod.Vertical;
                    grayOverlay.fillOrigin = (int)Image.OriginVertical.Top;
                    grayOverlay.fillAmount = 1f;
                    grayOverlay.transform.SetSiblingIndex(icon.transform.GetSiblingIndex() + 1);
                }
                else
                {
                    icon.enabled = false;
                    iconFallback = UiKit.CreateText(slotRect, "IconFallback",
                        Vector2.zero, new Vector2(IconSize, IconSize),
                        "記録", 26f, IdleLabelColor, TextAlignmentOptions.Center);
                }

                TextMeshProUGUI keyLabel = UiKit.CreateText(rect, "KeyLabel",
                    new Vector2(iconCenterX, LabelHeight / 2f),
                    new Vector2(rootWidth + LabelExtraWidth, LabelHeight),
                    string.Empty, LabelFontSize, IdleLabelColor, TextAlignmentOptions.Center);
                UiKit.SetAnchorsBottomLeft(keyLabel.rectTransform,
                    new Vector2(iconCenterX, LabelHeight / 2f),
                    new Vector2(rootWidth + LabelExtraWidth, LabelHeight));

                root.SetActive(false);
                _view = new ViewRefs(root, grayOverlay, iconFallback, keyLabel);
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

        public void Tick(bool visible, bool recordOn, float holdRatio, bool holdCharging,
                         string keyName, CorpseReportHudPanel.Layout corpseLayout)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            try
            {
                if (view.Root.activeSelf != visible) view.Root.SetActive(visible);
                if (!visible)
                {
                    view.LastKeyName = null;
                    return;
                }

                float y = HudGeometry.HudIcon.RecordBottom(
                    corpseLayout == CorpseReportHudPanel.Layout.AboveMiniGauge);
                if (!Mathf.Approximately(y, view.LastY))
                {
                    view.LastY = y;
                    ((RectTransform)view.Root.transform).anchoredPosition = new Vector2(-RightMargin, y);
                }

                float ratio = holdCharging ? Mathf.Clamp01(holdRatio) : 0f;
                float coverage = recordOn ? ratio : 1f - ratio;
                if (view.GrayOverlay != null) view.GrayOverlay.fillAmount = coverage;
                if (view.IconFallback != null)
                {
                    view.IconFallback.color = Color.Lerp(ActiveLabelColor, IdleLabelColor, coverage);
                }
                view.KeyLabel.color = Color.Lerp(ActiveLabelColor, IdleLabelColor, coverage);

                if (recordOn == view.LastOn && keyName == view.LastKeyName) return;
                view.LastOn = recordOn;
                view.LastKeyName = keyName;

                view.KeyLabel.text = Texts.Format(
                    recordOn ? TextId.HudValuableRecordOnFormat : TextId.HudValuableRecordOffFormat,
                    keyName ?? "?");
            }
            catch (Exception e)
            {
                WLog.Line("valuable_record_hud_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (view.Root.activeSelf) view.Root.SetActive(false);
            view.LastKeyName = null;
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view != null) view.LastKeyName = null;
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

    }
}
