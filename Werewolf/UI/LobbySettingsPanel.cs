using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class LobbySettingsPanel
    {
        private static readonly Vector2 PanelAnchorMin = new Vector2(0.55f, 0.05f);
        private static readonly Vector2 PanelAnchorMax = new Vector2(0.98f, 0.95f);

        private const float PanelWidth = 820f;
        private const float PanelHeight = 1000f;
        private const float RowWidth = 780f;
        private const float RowHeight = 44f;
        private const float SectionHeaderHeight = 52f;
        private const float SectionGap = 10f;
        private const float TopPadding = 28f;
        private const float BottomPadding = 20f;
        private const float FooterHeight = 48f;

        private const float LabelFontSize = 28f;
        private const float SectionFontSize = 34f;

        private const float ScrollSensitivity = 44f;

        private const float ScrollbarWidth = 16f;
        private static readonly Color ScrollbarBgColor = new Color(0.1f, 0.1f, 0.14f, 0.9f);
        private static readonly Color ScrollbarHandleColor = new Color(0.55f, 0.55f, 0.65f, 0.95f);

        private const float HintFontSize = 28f;
        private static readonly Vector2 HintAnchor = new Vector2(0.98f, 0.05f);
        private static readonly Vector2 HintSize = new Vector2(640f, 40f);

        private static readonly Color PanelBgColor = new Color(0.02f, 0.02f, 0.05f, 0.85f);
        private static readonly Color SectionBgColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        private static readonly Color SectionTextColor = new Color(1f, 0.9f, 0.6f, 1f);
        private static readonly Color LabelTextColor = new Color(0.9f, 0.9f, 0.95f, 1f);
        private static readonly Color HintTextColor = new Color(0.95f, 0.95f, 0.98f, 1f);

        private GameObject _root;
        private GameObject _hintRoot;
        private RectTransform _content;
        private ScrollRect _scrollRect;
        private readonly List<GameObject> _rowObjects = new List<GameObject>();

        public bool Exists => _root != null;
        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot, string toggleKeyName)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            try
            {
                var rootGo = new GameObject("WW_LobbySettingsPanel", typeof(RectTransform));
                var rootRect = (RectTransform)rootGo.transform;
                rootRect.SetParent(layerRoot, false);
                rootRect.anchorMin = PanelAnchorMin;
                rootRect.anchorMax = PanelAnchorMax;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                var subCanvas = rootGo.AddComponent<Canvas>();
                subCanvas.overrideSorting = true;
                subCanvas.sortingOrder = WerewolfUIManager.PanelSortingOrder;
                rootGo.AddComponent<GraphicRaycaster>();
                _root = rootGo;

                var bgGo = new GameObject("Bg", typeof(RectTransform));
                var bgRect = (RectTransform)bgGo.transform;
                bgRect.SetParent(rootRect, false);
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                var bgImg = bgGo.AddComponent<Image>();
                bgImg.color = PanelBgColor;
                bgImg.raycastTarget = false;

                var scrollGo = new GameObject("Scroll", typeof(RectTransform));
                var scrollRect = (RectTransform)scrollGo.transform;
                scrollRect.SetParent(rootRect, false);
                scrollRect.anchorMin = new Vector2(0f, 0f);
                scrollRect.anchorMax = new Vector2(1f, 1f);
                scrollRect.offsetMin = new Vector2(0f, FooterHeight);
                scrollRect.offsetMax = Vector2.zero;
                var scroll = scrollGo.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = false;
                scroll.scrollSensitivity = ScrollSensitivity;
                var scrollBgImg = scrollGo.AddComponent<Image>();
                scrollBgImg.color = new Color(0f, 0f, 0f, 0.001f);
                scrollBgImg.raycastTarget = true;
                _scrollRect = scroll;

                var viewportGo = new GameObject("Viewport", typeof(RectTransform));
                var viewportRect = (RectTransform)viewportGo.transform;
                viewportRect.SetParent(scrollRect, false);
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = new Vector2(-ScrollbarWidth, 0f);
                var vpImg = viewportGo.AddComponent<Image>();
                vpImg.color = new Color(0f, 0f, 0f, 0.001f);
                vpImg.raycastTarget = true;
                viewportGo.AddComponent<RectMask2D>();
                scroll.viewport = viewportRect;

                var contentGo = new GameObject("Content", typeof(RectTransform));
                var contentRect = (RectTransform)contentGo.transform;
                contentRect.SetParent(viewportRect, false);
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0f, PanelHeight);
                scroll.content = contentRect;
                _content = contentRect;

                var sbGo = new GameObject("Scrollbar", typeof(RectTransform));
                var sbRect = (RectTransform)sbGo.transform;
                sbRect.SetParent(scrollRect, false);
                sbRect.anchorMin = new Vector2(1f, 0f);
                sbRect.anchorMax = new Vector2(1f, 1f);
                sbRect.pivot = new Vector2(1f, 0.5f);
                sbRect.anchoredPosition = Vector2.zero;
                sbRect.sizeDelta = new Vector2(ScrollbarWidth, 0f);
                var sbBgImg = sbGo.AddComponent<Image>();
                sbBgImg.color = ScrollbarBgColor;
                sbBgImg.raycastTarget = true;

                var slidingGo = new GameObject("SlidingArea", typeof(RectTransform));
                var slidingRect = (RectTransform)slidingGo.transform;
                slidingRect.SetParent(sbRect, false);
                slidingRect.anchorMin = Vector2.zero;
                slidingRect.anchorMax = Vector2.one;
                slidingRect.offsetMin = new Vector2(2f, 2f);
                slidingRect.offsetMax = new Vector2(-2f, -2f);

                var handleGo = new GameObject("Handle", typeof(RectTransform));
                var handleRect = (RectTransform)handleGo.transform;
                handleRect.SetParent(slidingRect, false);
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.one;
                handleRect.offsetMin = Vector2.zero;
                handleRect.offsetMax = Vector2.zero;
                var handleImg = handleGo.AddComponent<Image>();
                handleImg.color = ScrollbarHandleColor;
                handleImg.raycastTarget = true;

                var scrollbar = sbGo.AddComponent<Scrollbar>();
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scrollbar.targetGraphic = handleImg;
                scrollbar.handleRect = handleRect;
                scroll.verticalScrollbar = scrollbar;
                scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

                string keyName = string.IsNullOrEmpty(toggleKeyName) ? "?" : toggleKeyName;
                var footerGo = new GameObject("Footer", typeof(RectTransform));
                var footerRect = (RectTransform)footerGo.transform;
                footerRect.SetParent(rootRect, false);
                footerRect.anchorMin = new Vector2(0f, 0f);
                footerRect.anchorMax = new Vector2(1f, 0f);
                footerRect.pivot = new Vector2(0.5f, 0f);
                footerRect.anchoredPosition = Vector2.zero;
                footerRect.sizeDelta = new Vector2(0f, FooterHeight);
                UiKit.CreateText(footerRect, "ToggleHintFooter",
                    new Vector2(0f, FooterHeight * 0.5f), new Vector2(RowWidth, 40f),
                    Texts.Format(TextId.LobbySettingsFooterHintFormat, keyName), HintFontSize, HintTextColor,
                    TextAlignmentOptions.Right);

                var hintGo = new GameObject("WW_LobbySettingsHint", typeof(RectTransform));
                var hintRect = (RectTransform)hintGo.transform;
                hintRect.SetParent(layerRoot, false);
                hintRect.anchorMin = HintAnchor;
                hintRect.anchorMax = HintAnchor;
                hintRect.pivot = new Vector2(1f, 1f);
                hintRect.anchoredPosition = Vector2.zero;
                hintRect.sizeDelta = HintSize;
                UiKit.CreateText(hintRect, "ToggleHintLabel",
                    Vector2.zero, HintSize,
                    Texts.Format(TextId.LobbySettingsMiniHintFormat, keyName), HintFontSize, HintTextColor, TextAlignmentOptions.Right);
                _hintRoot = hintGo;

                _root.SetActive(false);
                _hintRoot.SetActive(false);
                WLog.Line("lobby_settings_panel_built", secret: false);
            }
            catch (Exception e)
            {
                WLog.Line("lobby_settings_panel_error", secret: false, ("reason", "build_failed"), ("err", e.Message));
                Destroy();
            }
        }

        public void SetRows(IReadOnlyList<SettingRow> rows)
        {
            if (_content == null) return;
            ClearRows();
            if (rows == null || rows.Count == 0)
            {
                _content.sizeDelta = new Vector2(_content.sizeDelta.x, PanelHeight);
                return;
            }

            float totalHeight = TopPadding;
            string s = null;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r == null) continue;
                if (!string.Equals(r.Section, s, StringComparison.Ordinal))
                {
                    if (s != null) totalHeight += SectionGap;
                    totalHeight += SectionHeaderHeight;
                    s = r.Section;
                }
                totalHeight += RowHeight;
            }
            totalHeight += BottomPadding;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);

            float topEdge = totalHeight * 0.5f;
            float cursor = topEdge - TopPadding;
            string lastSection = null;

            for (int i = 0; i < rows.Count; i++)
            {
                SettingRow row = rows[i];
                if (row == null) continue;

                if (!string.Equals(row.Section, lastSection, StringComparison.Ordinal))
                {
                    if (lastSection != null) cursor -= SectionGap;
                    float sectionCenterY = cursor - SectionHeaderHeight * 0.5f;
                    BuildSectionHeader(_content, row.Section, sectionCenterY);
                    cursor -= SectionHeaderHeight;
                    lastSection = row.Section;
                }

                float rowCenterY = cursor - RowHeight * 0.5f;
                BuildRow(_content, row.Label, row.Value, rowCenterY);
                cursor -= RowHeight;
            }

            if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 1f;
        }

        public void SetVisibility(bool panelVisible, bool hintVisible)
        {
            if (_root == null) return;
            bool changed = _root.activeSelf != panelVisible
                           || (_hintRoot != null && _hintRoot.activeSelf != hintVisible);
            if (!changed) return;
            _root.SetActive(panelVisible);
            if (_hintRoot != null) _hintRoot.SetActive(hintVisible);
            if (panelVisible && _scrollRect != null) _scrollRect.verticalNormalizedPosition = 1f;
            WLog.Line("lobby_settings_panel_visible", secret: false,
                ("visible", panelVisible), ("hint", hintVisible));
        }

        public void Destroy()
        {
            ClearRows();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            if (_hintRoot != null)
            {
                UnityEngine.Object.Destroy(_hintRoot);
                _hintRoot = null;
            }
            _content = null;
            _scrollRect = null;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rowObjects.Count; i++)
            {
                GameObject go = _rowObjects[i];
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            _rowObjects.Clear();
        }

        private void BuildSectionHeader(Transform parent, string sectionName, float centerY)
        {
            Image bg = UiKit.CreateImage(parent, $"Section_Bg_{sectionName}",
                new Vector2(0f, centerY), new Vector2(RowWidth, SectionHeaderHeight), SectionBgColor);
            _rowObjects.Add(bg.gameObject);

            TextMeshProUGUI label = UiKit.CreateText(parent, $"Section_Label_{sectionName}",
                new Vector2(0f, centerY), new Vector2(RowWidth - 24f, SectionHeaderHeight),
                sectionName ?? string.Empty, SectionFontSize, SectionTextColor, TextAlignmentOptions.Center);
            _rowObjects.Add(label.gameObject);
        }

        private void BuildRow(Transform parent, string label, string value, float centerY)
        {
            string text = string.IsNullOrEmpty(value) ? (label ?? string.Empty)
                                                     : $"{label}: {value}";
            TextMeshProUGUI tmp = UiKit.CreateText(parent, "Row",
                new Vector2(0f, centerY), new Vector2(RowWidth, RowHeight),
                text, LabelFontSize, LabelTextColor, TextAlignmentOptions.Left);
            _rowObjects.Add(tmp.gameObject);
        }
    }
}
