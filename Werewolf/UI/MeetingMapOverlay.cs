using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class MeetingMapOverlay : IClientPanel
    {
        public string LayerName => WerewolfUIManager.MeetingMapLayer;

        private const float PanelWidth = HudGeometry.MapOverlay.Width;
        private const float PanelHeight = HudGeometry.MapOverlay.Height;

        private const float ToggleButtonMargin = HudGeometry.MapToggle.Margin;

        private static readonly Color MapTint = new Color(1f, 1f, 1f, 1f);

        private static readonly Color PanelFrameColor = new Color(0.02f, 0.02f, 0.05f, 0.95f);
        private static readonly IconToggleStyle ToggleStyle = new IconToggleStyle(
            iconSize: HudGeometry.MapToggle.IconSize,
            labelWidth: HudGeometry.MapToggle.LabelWidth,
            labelHeight: HudGeometry.MapToggle.LabelHeight,
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
        private bool _mapToolBlocked;
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

        private const float PathThickness = 2f;
        private const float PathUnderThickness = 4f;
        private const float PathMarkerFillSize = 16f;
        private const float PathMarkerFrameSize = 20f;
        private const float PathMarkerIdFontSize = 12f;
        private const float PathEndMarkSize = 32f;
        private const float PathEndMarkFontSize = 24f;
        private const int PathMaxPoints = 4000;
        private static readonly Color PathMarkerFrameColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color PathUnderColor = new Color(0.02f, 0.02f, 0.05f, 0.85f);
        private static readonly Color PathEndMarkBgColor = new Color(0.04f, 0.04f, 0.08f, 1f);
        private static readonly Color PathEndMarkTextColor = new Color(1f, 1f, 1f, 1f);

        private RectTransform _pathRoot;
        private UiPolyline _pathUnderLine;
        private UiPolyline _pathLine;
        private RectTransform _pathStartMark;
        private RectTransform _pathGoalMark;
        private CanvasGroup _pathStartMarkGroup;
        private CanvasGroup _pathGoalMarkGroup;
        private RectTransform _pathMarker;
        private Image _pathMarkerFill;
        private TextMeshProUGUI _pathMarkerId;
        private MeetingPath _ownPath;
        private int _ownParticipantId;
        private string _ownColorHex;
        private bool _pathDirty;
        private float _pathOpenedAt;
        private MapProjectionInputs _pathProjInputs;
        private readonly List<ReplayTrailPoint> _pathPointScratch = new List<ReplayTrailPoint>();
        private readonly List<Vector2> _pathScratch = new List<Vector2>();

        public bool Exists => _root != null;

        public bool IsOpen => _open;

        public bool ConsumeMapToolBlocked()
        {
            if (!_mapToolBlocked) return false;
            _mapToolBlocked = false;
            return true;
        }

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            RectTransform rootRect = UiKit.CreateStretched(layerRoot, "WW_MeetingMapOverlay");
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

            _pathRoot = UiKit.CreateRect(panelRect, "PathRoot", Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            BuildPathVisuals(_pathRoot);
            _pathRoot.gameObject.SetActive(false);

            _mapPanel.SetActive(false);

            RectTransform toggleRoot = _toggle.Build(rootRect, "ToggleButton", ToggleStyle,
                "map_overlay_icon", Texts.Format(TextId.MapOverlayToggleLabelFormat, "M"));
            UiKit.PinToCorner(toggleRoot, UiKit.BottomLeft, ToggleButtonMargin);
        }

        public void Tick(MeetingClientState state, long nowUnixMs, KeyCode key, float orthoSize, int resolutionPreset,
            bool gridEnabled)
        {
            if (_root == null) return;

            bool meetingActive = state != null && state.MeetingActive && state.VotingUiReady(nowUnixMs);

            if (_prevMeetingActive && !meetingActive)
            {
                ForceClose();
                ClearOwnPath();
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
                    if (WasToggleRequested(key)) _mapToolBlocked = true;
                    return;
                }

                _toggle.SetHover(_toggle.IsPointerOverOpaqueIcon());

                if (WasToggleRequested(key))
                {
                    if (_open) CloseInternal();
                    else OpenInternal(orthoSize, resolutionPreset);
                }

                if (_open)
                {
                    Texture tex = _rt.Tick(orthoSize);
                    if (_mapImage != null) _mapImage.texture = tex != null ? tex : ResolvePlaceholder();
                    UpdateGrid(gridEnabled);
                    UpdateOwnPath();
                }
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void SetOwnPath(MeetingPath path, int participantId, string colorHex)
        {
            _ownPath = path;
            _ownParticipantId = participantId;
            _ownColorHex = colorHex;
            _pathDirty = true;
        }

        public void ClearOwnPath()
        {
            _ownPath = null;
            _pathDirty = true;
            if (_pathRoot != null) _pathRoot.gameObject.SetActive(false);
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
            _mapToolBlocked = false;
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
            _pathRoot = null;
            _pathUnderLine = null;
            _pathLine = null;
            _pathStartMark = null;
            _pathGoalMark = null;
            _pathStartMarkGroup = null;
            _pathGoalMarkGroup = null;
            _pathMarker = null;
            _pathMarkerFill = null;
            _pathMarkerId = null;
            _ownPath = null;
            _pathDirty = false;
        }

        private void OpenInternal(float orthoSizeConfig, int resolutionPreset)
        {
            _open = true;
            if (_mapPanel != null) _mapPanel.SetActive(true);
            _pathOpenedAt = Time.unscaledTime;
            _pathDirty = true;

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
            if (_pathRoot != null) _pathRoot.gameObject.SetActive(false);
            WLog.Line("mapoverlay_close", secret: false);
        }

        private bool TryReadMapView(out int levelWidth, out int levelHeight, out MapProjectionInputs inputs)
        {
            inputs = default;
            Camera cam = _rt.Camera;
            if (cam == null || !MapGeometry.TryReadLevelGrid(
                    out levelWidth, out levelHeight, out float mapScale, out Vector3 origin))
            {
                levelWidth = levelHeight = 0;
                return false;
            }
            Vector3 camPos = cam.transform.position;
            inputs.MapScale = mapScale;
            inputs.OriginX = origin.x;
            inputs.OriginZ = origin.z;
            inputs.CamX = camPos.x;
            inputs.CamZ = camPos.z;
            inputs.Ortho = cam.orthographicSize;
            inputs.Aspect = Mathf.Max(0.01f, cam.aspect);
            return true;
        }

        private void BuildPathVisuals(RectTransform root)
        {
            _pathUnderLine = CreatePathLine(root, "UnderLine", PathUnderThickness, PathUnderColor);
            _pathLine = CreatePathLine(root, "Line", PathThickness, Color.white);

            _pathStartMark = CreateEndMark(root, "Start", "S");
            _pathGoalMark = CreateEndMark(root, "Goal", "G");
            _pathStartMarkGroup = _pathStartMark.gameObject.AddComponent<CanvasGroup>();
            _pathGoalMarkGroup = _pathGoalMark.gameObject.AddComponent<CanvasGroup>();

            UiKit.DiamondMarker marker = UiKit.CreateDiamondMarker(root, "Marker",
                PathMarkerFrameSize, PathMarkerFillSize, PathMarkerIdFontSize, PathMarkerFrameColor);
            _pathMarker = marker.Rect;
            _pathMarkerFill = marker.Fill;
            _pathMarkerId = marker.Id;
        }

        private static UiPolyline CreatePathLine(RectTransform root, string name, float thickness, Color color)
        {
            RectTransform lineRect = UiKit.CreateRect(root, name, Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            UiPolyline line = lineRect.gameObject.AddComponent<UiPolyline>();
            line.raycastTarget = false;
            line.Thickness = thickness;
            line.color = color;
            return line;
        }

        private static RectTransform CreateEndMark(RectTransform root, string name, string letter)
        {
            RectTransform mark = UiKit.CreateRect(root, name, Vector2.zero, new Vector2(PathEndMarkSize, PathEndMarkSize));
            Image bg = UiKit.CreateImage(mark, "Bg", Vector2.zero, new Vector2(PathEndMarkSize, PathEndMarkSize), PathEndMarkBgColor);
            bg.raycastTarget = false;
            UiKit.CreateText(mark, "Letter", Vector2.zero, new Vector2(PathEndMarkSize, PathEndMarkSize),
                letter, PathEndMarkFontSize, PathEndMarkTextColor, TextAlignmentOptions.Center);
            return mark;
        }

        private void UpdateOwnPath()
        {
            if (_pathRoot == null) return;
            MapProjectionInputs inputs = default;
            MapPanelProjection proj = default;
            bool visible = _ownPath != null
                && TryReadMapView(out _, out _, out inputs)
                && (proj = inputs.ToProjection(PanelWidth, PanelHeight)).Valid;
            if (_pathRoot.gameObject.activeSelf != visible) _pathRoot.gameObject.SetActive(visible);
            if (!visible) return;

            if (_pathDirty || !inputs.SameAs(_pathProjInputs))
            {
                _pathProjInputs = inputs;
                _pathDirty = false;
                LayoutPath(proj);
            }

            double elapsed = Time.unscaledTime - _pathOpenedAt;
            double t = _ownPath.PathTimeAt(elapsed);
            UpdatePathTrail(proj, t);
            _ownPath.PositionAt(t, out float x, out float z);
            _pathMarker.anchoredPosition = new Vector2(proj.PanelX(x), proj.PanelY(z));
            float blink = MeetingPath.BlinkBrightness(elapsed);
            _pathStartMarkGroup.alpha = blink;
            _pathGoalMarkGroup.alpha = blink;
        }

        private void LayoutPath(MapPanelProjection proj)
        {
            ReplayMarkerPalette.ColorFor(_ownParticipantId, _ownColorHex, out float r, out float g, out float b);
            _pathLine.color = new Color(r, g, b, 0.9f);
            _pathMarkerFill.color = new Color(r, g, b, 1f);
            _pathMarkerId.color = UiKit.MarkerIdInk(r, g, b);
            string idText = _ownParticipantId > 0 ? _ownParticipantId.ToString() : "?";
            if (_pathMarkerId.text != idText) _pathMarkerId.text = idText;

            _pathStartMark.anchoredPosition = new Vector2(
                proj.PanelX(_ownPath.Start.X), proj.PanelY(_ownPath.Start.Z));
            _pathGoalMark.anchoredPosition = new Vector2(
                proj.PanelX(_ownPath.Goal.X), proj.PanelY(_ownPath.Goal.Z));
        }

        private void UpdatePathTrail(MapPanelProjection proj, double pathT)
        {
            _pathPointScratch.Clear();
            _ownPath.TrailInto(pathT, PathMaxPoints, _pathPointScratch);
            _pathScratch.Clear();
            for (int i = 0; i < _pathPointScratch.Count; i++)
            {
                ReplayTrailPoint p = _pathPointScratch[i];
                _pathScratch.Add(new Vector2(proj.PanelX(p.X), proj.PanelY(p.Z)));
            }
            _pathUnderLine.SetPoints(_pathScratch);
            _pathLine.SetPoints(_pathScratch);
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
                if (!TryReadMapView(out int levelWidth, out int levelHeight, out MapProjectionInputs view))
                {
                    return null;
                }

                EnsureGridModuleRange(levelWidth, levelHeight);
                MapGridCellRange? range = _gridModuleRange ?? MapGridMath.RangeFromBounds(
                    levelWidth, levelHeight,
                    view.MapScale, view.OriginX, view.OriginZ,
                    _rt.BoundsCenterXZ.x - _rt.BoundsHalfW, _rt.BoundsCenterXZ.x + _rt.BoundsHalfW,
                    _rt.BoundsCenterXZ.y - _rt.BoundsHalfH, _rt.BoundsCenterXZ.y + _rt.BoundsHalfH);
                if (range == null) return null;

                return MapGridMath.Compute(
                    levelWidth, range.Value,
                    view.MapScale, view.OriginX, view.OriginZ,
                    view.CamX, view.CamZ,
                    view.Ortho, view.Aspect,
                    PanelWidth, PanelHeight);
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_grid_error", secret: false, ("err", e.Message));
                return null;
            }
        }

        private void EnsureGridModuleRange(int levelWidth, int levelHeight)
        {
            if (_gridModuleRangeResolved) return;
            _gridModuleRangeResolved = true;
            try
            {
                List<(float X, float Z)> centers = MapGeometry.ModuleCentersXZ();
                _gridModuleRange = MapGridMath.RangeFromModuleCenters(centers, levelWidth, levelHeight);
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

        private bool WasToggleRequested(KeyCode key)
        {
            bool keyPressed = key != KeyCode.None && NoTextInputsActive() && Input.GetKeyDown(key);
            return keyPressed || _toggle.WasClicked();
        }

        private static bool _textInputErrorLogged;

        private static bool NoTextInputsActive()
        {
            try
            {
                return Inputs.NoTextInputsActive;
            }
            catch (Exception e)
            {
                if (!_textInputErrorLogged)
                {
                    _textInputErrorLogged = true;
                    WLog.Line("mapoverlay_text_input_error", secret: false, ("err", e.Message));
                }
                return true;
            }
        }

        private static string FormatKey(KeyCode key)
        {
            return key == KeyCode.None ? "-" : key.ToString();
        }
    }
}
