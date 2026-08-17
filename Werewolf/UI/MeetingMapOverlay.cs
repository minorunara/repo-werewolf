using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class MeetingMapOverlay : IClientPanel
    {
        public string LayerName => WerewolfUIManager.MeetingMapLayer;

        private const float PanelWidth = 1200f;
        private const float PanelHeight = 720f;

        private const float ToggleButtonMargin = 24f;

        private static readonly Color MapTint = new Color(1f, 1f, 1f, 1f);

        private static readonly Color PanelFrameColor = new Color(0.02f, 0.02f, 0.05f, 0.95f);
        private static readonly IconToggleStyle ToggleStyle = new IconToggleStyle(
            iconSize: 96f,
            labelWidth: 240f,
            labelHeight: 32f,
            labelFontSize: 24f,
            labelColor: new Color(1f, 0.9f, 0.6f, 1f),
            fallbackText: "MAP",
            fallbackPlateColor: new Color(0.08f, 0.08f, 0.10f, 0.55f),
            fallbackTextColor: new Color(0.95f, 0.95f, 0.95f, 0.95f));

        private GameObject _root;
        private GameObject _mapPanel;
        private RectTransform _mapPanelRect;
        private RawImage _mapImage;
        private readonly IconToggleButton _toggle = new IconToggleButton();

        private readonly MapRtView _rt = new MapRtView("meeting");

        private bool _open;
        private bool _prevMeetingActive;
        private KeyCode _lastLabelKey = KeyCode.None;

        private Texture _placeholderTex;
        private bool _placeholderResolved;

        private const float GridLineThickness = 2f;
        private const float GridLabelFontSize = 26f;
        private const float GridLabelWidth = 48f;
        private const float GridLabelHeight = 30f;
        private static readonly Color GridLineColor = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color GridLabelColor = new Color(1f, 1f, 1f, 0.92f);

        private RectTransform _gridRoot;
        private bool _gridModuleRangeResolved;
        private MapGridCellRange? _gridModuleRange;
        private readonly List<RectTransform> _gridVLines = new List<RectTransform>();
        private readonly List<RectTransform> _gridHLines = new List<RectTransform>();
        private readonly List<TextMeshProUGUI> _gridColLabels = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> _gridRowLabels = new List<TextMeshProUGUI>();

        public bool Exists => _root != null;

        public bool IsOpen => _open;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            RectTransform rootRect = UiKit.CreateRect(layerRoot, "WW_MeetingMapOverlay", Vector2.zero, new Vector2(1920f, 1080f));
            _root = rootRect.gameObject;

            RectTransform panelRect = UiKit.CreateRect(rootRect, "MapPanel", Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            _mapPanel = panelRect.gameObject;
            _mapPanelRect = panelRect;

            UiKit.CreateImage(panelRect, "PanelBg", Vector2.zero, new Vector2(PanelWidth + 12f, PanelHeight + 12f), PanelFrameColor);

            var mapGo = new GameObject("MapImage", typeof(RectTransform));
            var mapRect = (RectTransform)mapGo.transform;
            mapRect.SetParent(panelRect, false);
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.anchoredPosition = Vector2.zero;
            mapRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _mapImage = mapGo.AddComponent<RawImage>();
            _mapImage.color = MapTint;
            _mapImage.raycastTarget = false;

            _gridRoot = UiKit.CreateRect(panelRect, "GridRoot", Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            _gridRoot.gameObject.SetActive(false);

            _mapPanel.SetActive(false);

            RectTransform toggleRoot = _toggle.Build(rootRect, "ToggleButton", ToggleStyle,
                "map_overlay_icon", Texts.Format(TextId.MapOverlayToggleLabelFormat, "M"));
            Vector2 toggleSize = toggleRoot.sizeDelta;
            toggleRoot.anchoredPosition = new Vector2(
                -(1920f * 0.5f) + toggleSize.x * 0.5f + ToggleButtonMargin,
                -(1080f * 0.5f) + toggleSize.y * 0.5f + ToggleButtonMargin);

            WLog.Line("mapoverlay_built", secret: false,
                ("icon", _toggle.HasIcon ? 1 : 0));
        }

        public void Tick(MeetingClientState state, long nowUnixMs, KeyCode key, float orthoSize, int resolutionPreset,
            bool gridEnabled)
        {
            if (_root == null) return;

            bool meetingActive = state != null && state.MeetingActive && state.VotingUiReady(nowUnixMs);

            if (_prevMeetingActive && !meetingActive)
            {
                ForceClose();
            }
            _prevMeetingActive = meetingActive;

            if (key != _lastLabelKey)
            {
                _toggle.SetLabel(Texts.Format(TextId.MapOverlayToggleLabelFormat, FormatKey(key)));
                _lastLabelKey = key;
            }

            if (!meetingActive)
            {
                _toggle.SetHover(false);
                return;
            }

            try
            {
                bool toolActive = MapRtView.IsMapToolActive();
                if (toolActive)
                {
                    _toggle.SetHover(false);
                    if (_open) CloseInternal();
                    return;
                }

                _toggle.SetHover(_toggle.IsPointerOverOpaqueIcon());

                bool keyPressed = key != KeyCode.None && NoTextInputsActive() && Input.GetKeyDown(key);
                bool clicked = _toggle.WasClicked();
                if (keyPressed || clicked)
                {
                    if (_open) CloseInternal();
                    else OpenInternal(orthoSize, resolutionPreset);
                }

                if (_open)
                {
                    Texture tex = _rt.Tick(orthoSize);
                    if (_mapImage != null) _mapImage.texture = tex != null ? tex : ResolvePlaceholder();
                    UpdateGrid(gridEnabled);
                }
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void ForceClose()
        {
            if (_open) CloseInternal();
        }

        public void Destroy()
        {
            if (_open) CloseInternal();
            _rt.Reset();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _mapPanel = null;
            _mapPanelRect = null;
            _mapImage = null;
            _toggle.Clear();
            _prevMeetingActive = false;
            _lastLabelKey = KeyCode.None;
            _placeholderTex = null;
            _placeholderResolved = false;
            _gridRoot = null;
            _gridModuleRangeResolved = false;
            _gridModuleRange = null;
            _gridVLines.Clear();
            _gridHLines.Clear();
            _gridColLabels.Clear();
            _gridRowLabels.Clear();
        }

        private void OpenInternal(float orthoSizeConfig, int resolutionPreset)
        {
            _open = true;
            if (_mapPanel != null) _mapPanel.SetActive(true);

            _rt.Open(orthoSizeConfig, resolutionPreset);
            Texture tex = _rt.Tick(orthoSizeConfig);
            if (_mapImage != null) _mapImage.texture = tex != null ? tex : ResolvePlaceholder();
            WLog.Line("mapoverlay_open", secret: false,
                ("orthoConfig", orthoSizeConfig),
                ("orthoApplied", _rt.Camera != null ? _rt.Camera.orthographicSize : -1f));
        }

        private void CloseInternal()
        {
            _open = false;
            if (_mapPanel != null) _mapPanel.SetActive(false);
            _rt.Close();
            _gridModuleRangeResolved = false;
            _gridModuleRange = null;
            if (_gridRoot != null) _gridRoot.gameObject.SetActive(false);
            WLog.Line("mapoverlay_close", secret: false);
        }

        private void UpdateGrid(bool show)
        {
            if (_gridRoot == null) return;
            if (!show || !_rt.BoundsResolved)
            {
                _gridRoot.gameObject.SetActive(false);
                return;
            }
            MapGridLayout layout = ComputeGridLayout();
            if (layout == null)
            {
                _gridRoot.gameObject.SetActive(false);
                return;
            }
            _gridRoot.gameObject.SetActive(true);
            ApplyGridLayout(layout);
        }

        private MapGridLayout ComputeGridLayout()
        {
            try
            {
                LevelGenerator lg = LevelGenerator.Instance;
                Map map = Map.Instance;
                Camera cam = _rt.Camera;
                if (lg == null || map == null || map.OverLayerParent == null || cam == null) return null;
                Vector3 origin = map.OverLayerParent.position;
                Vector3 camPos = cam.transform.position;

                EnsureGridModuleRange(lg);
                MapGridCellRange? range = _gridModuleRange ?? MapGridMath.RangeFromBounds(
                    lg.LevelWidth, lg.LevelHeight,
                    map.Scale, origin.x, origin.z,
                    _rt.BoundsCenterXZ.x - _rt.BoundsHalfW, _rt.BoundsCenterXZ.x + _rt.BoundsHalfW,
                    _rt.BoundsCenterXZ.y - _rt.BoundsHalfH, _rt.BoundsCenterXZ.y + _rt.BoundsHalfH);
                if (range == null) return null;

                return MapGridMath.Compute(
                    lg.LevelWidth, range.Value,
                    map.Scale, origin.x, origin.z,
                    camPos.x, camPos.z,
                    cam.orthographicSize, Mathf.Max(0.01f, cam.aspect),
                    PanelWidth, PanelHeight);
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_grid_error", secret: false, ("err", e.Message));
                return null;
            }
        }

        private void EnsureGridModuleRange(LevelGenerator lg)
        {
            if (_gridModuleRangeResolved) return;
            _gridModuleRangeResolved = true;
            try
            {
                Module[] modules = UnityEngine.Object.FindObjectsOfType<Module>();
                var centers = new List<(float X, float Z)>(modules != null ? modules.Length : 0);
                if (modules != null)
                {
                    for (int i = 0; i < modules.Length; i++)
                    {
                        if (modules[i] == null) continue;
                        Vector3 p = modules[i].transform.position;
                        centers.Add((p.x, p.z));
                    }
                }
                _gridModuleRange = MapGridMath.RangeFromModuleCenters(centers, lg.LevelWidth, lg.LevelHeight);
                WLog.Line("mapoverlay_grid_range", secret: false,
                    ("modules", centers.Count),
                    ("resolved", _gridModuleRange != null ? 1 : 0),
                    ("colMin", _gridModuleRange?.ColMin ?? -1),
                    ("colMax", _gridModuleRange?.ColMax ?? -1),
                    ("rowMin", _gridModuleRange?.RowMin ?? -1),
                    ("rowMax", _gridModuleRange?.RowMax ?? -1));
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_grid_range_error", secret: false, ("err", e.Message));
                _gridModuleRange = null;
            }
        }

        private void ApplyGridLayout(MapGridLayout g)
        {
            float rectW = g.RectRight - g.RectLeft;
            float rectH = g.RectTop - g.RectBottom;
            float centerX = (g.RectLeft + g.RectRight) * 0.5f;
            float centerY = (g.RectBottom + g.RectTop) * 0.5f;

            EnsureGridLines(_gridVLines, g.VerticalLineX.Length, "GridV");
            for (int i = 0; i < g.VerticalLineX.Length; i++)
            {
                RectTransform line = _gridVLines[i];
                line.anchoredPosition = new Vector2(g.VerticalLineX[i], centerY);
                line.sizeDelta = new Vector2(GridLineThickness, rectH);
            }
            EnsureGridLines(_gridHLines, g.HorizontalLineY.Length, "GridH");
            for (int i = 0; i < g.HorizontalLineY.Length; i++)
            {
                RectTransform line = _gridHLines[i];
                line.anchoredPosition = new Vector2(centerX, g.HorizontalLineY[i]);
                line.sizeDelta = new Vector2(rectW, GridLineThickness);
            }

            EnsureGridLabels(_gridColLabels, g.ColumnLabels.Length, "GridColLabel");
            for (int i = 0; i < g.ColumnLabels.Length; i++)
            {
                TextMeshProUGUI label = _gridColLabels[i];
                if (label.text != g.ColumnLabels[i]) label.text = g.ColumnLabels[i];
                label.rectTransform.anchoredPosition = new Vector2(g.ColumnLabelX[i], g.ColumnLabelY);
            }
            EnsureGridLabels(_gridRowLabels, g.RowLabels.Length, "GridRowLabel");
            for (int i = 0; i < g.RowLabels.Length; i++)
            {
                TextMeshProUGUI label = _gridRowLabels[i];
                if (label.text != g.RowLabels[i]) label.text = g.RowLabels[i];
                label.rectTransform.anchoredPosition = new Vector2(g.RowLabelX, g.RowLabelY[i]);
            }
        }

        private void EnsureGridLines(List<RectTransform> pool, int needed, string namePrefix)
        {
            while (pool.Count < needed)
            {
                Image img = UiKit.CreateImage(_gridRoot, namePrefix + pool.Count, Vector2.zero,
                    new Vector2(GridLineThickness, GridLineThickness), GridLineColor);
                img.raycastTarget = false;
                pool.Add(img.rectTransform);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                bool active = i < needed;
                if (pool[i].gameObject.activeSelf != active) pool[i].gameObject.SetActive(active);
            }
        }

        private void EnsureGridLabels(List<TextMeshProUGUI> pool, int needed, string namePrefix)
        {
            while (pool.Count < needed)
            {
                TextMeshProUGUI label = UiKit.CreateText(_gridRoot, namePrefix + pool.Count, Vector2.zero,
                    new Vector2(GridLabelWidth, GridLabelHeight),
                    "", GridLabelFontSize, GridLabelColor, TextAlignmentOptions.Center);
                label.raycastTarget = false;
                pool.Add(label);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                bool active = i < needed;
                if (pool[i].gameObject.activeSelf != active) pool[i].gameObject.SetActive(active);
            }
        }

        public bool IsPointerOverPanel(Vector2 screenPoint)
        {
            if (!_open || _mapPanelRect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(_mapPanelRect, screenPoint, null);
        }

        private Texture ResolvePlaceholder()
        {
            if (_placeholderResolved) return _placeholderTex;
            _placeholderResolved = true;
            try
            {
                _placeholderTex = AssetCatalog.GetTexture("mapoverlay_placeholder");
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_placeholder_error", secret: false, ("err", e.Message));
                _placeholderTex = null;
            }
            return _placeholderTex;
        }

        private static bool NoTextInputsActive()
        {
            try
            {
                return SemiFunc.NoTextInputsActive();
            }
            catch
            {
                return true;
            }
        }

        private static string FormatKey(KeyCode key)
        {
            return key == KeyCode.None ? "-" : key.ToString();
        }
    }
}
