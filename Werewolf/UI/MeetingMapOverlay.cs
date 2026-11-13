#nullable enable
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

        private sealed class PathView
        {
            public PathView(RectTransform root, UiPolyline underLine, UiPolyline line,
                RectTransform startMark, RectTransform goalMark, CanvasGroup startMarkGroup,
                CanvasGroup goalMarkGroup, RectTransform marker, Image markerFill,
                TextMeshProUGUI markerId)
            {
                Root = root;
                UnderLine = underLine;
                Line = line;
                StartMark = startMark;
                GoalMark = goalMark;
                StartMarkGroup = startMarkGroup;
                GoalMarkGroup = goalMarkGroup;
                Marker = marker;
                MarkerFill = markerFill;
                MarkerId = markerId;
            }

            public RectTransform Root { get; }
            public UiPolyline UnderLine { get; }
            public UiPolyline Line { get; }
            public RectTransform StartMark { get; }
            public RectTransform GoalMark { get; }
            public CanvasGroup StartMarkGroup { get; }
            public CanvasGroup GoalMarkGroup { get; }
            public RectTransform Marker { get; }
            public Image MarkerFill { get; }
            public TextMeshProUGUI MarkerId { get; }
        }

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, GameObject mapPanel, RectTransform mapPanelRect,
                RawImage mapImage, RectTransform gridRoot, PathView path)
            {
                Root = root;
                MapPanel = mapPanel;
                MapPanelRect = mapPanelRect;
                MapImage = mapImage;
                GridRoot = gridRoot;
                Path = path;
            }

            public GameObject Root { get; }
            public GameObject MapPanel { get; }
            public RectTransform MapPanelRect { get; }
            public RawImage MapImage { get; }
            public RectTransform GridRoot { get; }
            public PathView Path { get; }
            public List<RectTransform> GridVLines { get; } = new List<RectTransform>();
            public List<RectTransform> GridHLines { get; } = new List<RectTransform>();
            public List<TextMeshProUGUI> GridColLabels { get; } = new List<TextMeshProUGUI>();
            public List<TextMeshProUGUI> GridRowLabels { get; } = new List<TextMeshProUGUI>();
        }

        private sealed class OwnPathState
        {
            public OwnPathState(MeetingPath path, int participantId, string? colorHex)
            {
                Path = path;
                ParticipantId = participantId;
                ColorHex = colorHex;
            }

            public MeetingPath Path { get; }
            public int ParticipantId { get; }
            public string? ColorHex { get; }
        }

        private ViewRefs? _view;
        private readonly IconToggleButton _toggle = new IconToggleButton();
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(MeetingMapOverlay));

        private readonly MapRtView _rt = new MapRtView("meeting");

        private bool _mapToolBlocked;
        private bool _prevMeetingActive;
        private KeyCode _lastLabelKey = KeyCode.None;

        private Texture? _placeholderTex;
        private bool _placeholderResolved;

        private const float GridLineThickness = 2f;
        private const float GridLabelFontSize = 26f;
        private const float GridLabelWidth = 48f;
        private const float GridLabelHeight = 30f;
        private static readonly Color GridLineColor = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color GridLabelColor = new Color(1f, 1f, 1f, 0.92f);

        private bool _gridModuleRangeResolved;
        private MapGridCellRange? _gridModuleRange;

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

        private OwnPathState? _ownPath;
        private bool _pathDirty;
        private float _pathOpenedAt;
        private MapProjectionInputs _pathProjInputs;
        private readonly List<ReplayTrailPoint> _pathPointScratch = new List<ReplayTrailPoint>();
        private readonly List<Vector2> _pathScratch = new List<Vector2>();

        public bool Exists => GetLiveViewOrNull() != null;

        public bool IsOpen => _rt.IsOpen;

        public bool ConsumeMapToolBlocked()
        {
            if (!_mapToolBlocked) return false;
            _mapToolBlocked = false;
            return true;
        }

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                RectTransform rootRect = UiKit.CreateStretched(layerRoot, "WW_MeetingMapOverlay");
                root = rootRect.gameObject;

            RectTransform panelRect = UiKit.CreateRect(rootRect, "MapPanel", Vector2.zero, new Vector2(PanelWidth, PanelHeight));
                GameObject mapPanel = panelRect.gameObject;

            UiKit.CreateImage(panelRect, "PanelBg", Vector2.zero, new Vector2(PanelWidth + 12f, PanelHeight + 12f), PanelFrameColor);

            var mapGo = new GameObject("MapImage", typeof(RectTransform));
            var mapRect = (RectTransform)mapGo.transform;
            mapRect.SetParent(panelRect, false);
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.anchoredPosition = Vector2.zero;
            mapRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                RawImage mapImage = mapGo.AddComponent<RawImage>();
                mapImage.color = MapTint;
                mapImage.raycastTarget = false;

                RectTransform gridRoot = UiKit.CreateRect(panelRect, "GridRoot", Vector2.zero, new Vector2(PanelWidth, PanelHeight));
                gridRoot.gameObject.SetActive(false);

                RectTransform pathRoot = UiKit.CreateRect(panelRect, "PathRoot", Vector2.zero, new Vector2(PanelWidth, PanelHeight));
                PathView path = BuildPathVisuals(pathRoot);
                pathRoot.gameObject.SetActive(false);

                mapPanel.SetActive(false);

                RectTransform toggleRoot = _toggle.Build(rootRect, "ToggleButton", ToggleStyle,
                    "map_overlay_icon", Texts.Format(TextId.MapOverlayToggleLabelFormat, "M"));
                UiKit.PinToCorner(toggleRoot, UiKit.BottomLeft, ToggleButtonMargin);
                _view = new ViewRefs(root, mapPanel, panelRect, mapImage, gridRoot, path);
                _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                _view = null;
                _toggle.Clear();
                if (root != null)
                {
                    root.SetActive(false);
                    UnityEngine.Object.Destroy(root);
                }
                throw;
            }
        }

        public void Tick(MeetingClientState state, long nowUnixMs, KeyCode key, float orthoSize, int resolutionPreset,
            bool gridEnabled)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

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
                    if (_rt.IsOpen) CloseInternal();
                    if (WasToggleRequested(key)) _mapToolBlocked = true;
                    return;
                }

                _toggle.SetHover(_toggle.IsPointerOverOpaqueIcon());

                if (WasToggleRequested(key))
                {
                    if (_rt.IsOpen) CloseInternal();
                    else OpenInternal(orthoSize, resolutionPreset);
                }

                if (_rt.IsOpen)
                {
                    Texture? tex = _rt.Tick(orthoSize);
                    view.MapImage.texture = tex != null ? tex : ResolvePlaceholder();
                    UpdateGrid(gridEnabled);
                    UpdateOwnPath();
                }
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void SetOwnPath(MeetingPath? path, int participantId, string? colorHex)
        {
            _ownPath = path != null ? new OwnPathState(path, participantId, colorHex) : null;
            _pathDirty = true;
        }

        public void ClearOwnPath()
        {
            _ownPath = null;
            _pathDirty = true;
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) view.Path.Root.gameObject.SetActive(false);
        }

        public void ForceClose()
        {
            if (_rt.IsOpen) CloseInternal();
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            if (_rt.IsOpen) CloseInternal(view);
            _rt.Reset();
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
            _toggle.Clear();
            _mapToolBlocked = false;
            _prevMeetingActive = false;
            _lastLabelKey = KeyCode.None;
            _placeholderTex = null;
            _placeholderResolved = false;
            _gridModuleRangeResolved = false;
            _gridModuleRange = null;
            _ownPath = null;
            _pathDirty = false;
        }

        private void OpenInternal(float orthoSizeConfig, int resolutionPreset)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            _pathOpenedAt = Time.unscaledTime;
            _pathDirty = true;

            try
            {
                _rt.Open(orthoSizeConfig, resolutionPreset);
                Texture? tex = _rt.Tick(orthoSizeConfig);
                view.MapImage.texture = tex != null ? tex : ResolvePlaceholder();
                view.MapPanel.SetActive(true);
            }
            catch
            {
                _rt.Close();
                view.MapPanel.SetActive(false);
                throw;
            }
            WLog.Line("mapoverlay_open", secret: false,
                ("orthoConfig", orthoSizeConfig),
                ("orthoApplied", _rt.Camera != null ? _rt.Camera.orthographicSize : -1f));
        }

        private void CloseInternal()
            => CloseInternal(GetLiveViewOrNull());

        private void CloseInternal(ViewRefs? view)
        {
            if (view != null) view.MapPanel.SetActive(false);
            _rt.Close();
            _gridModuleRangeResolved = false;
            _gridModuleRange = null;
            if (view != null)
            {
                view.GridRoot.gameObject.SetActive(false);
                view.Path.Root.gameObject.SetActive(false);
            }
            WLog.Line("mapoverlay_close", secret: false);
        }

        private bool TryReadMapView(out int levelWidth, out int levelHeight, out MapProjectionInputs inputs)
        {
            inputs = default;
            Camera? cam = _rt.Camera;
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

        private static PathView BuildPathVisuals(RectTransform root)
        {
            UiPolyline underLine = CreatePathLine(root, "UnderLine", PathUnderThickness, PathUnderColor);
            UiPolyline line = CreatePathLine(root, "Line", PathThickness, Color.white);

            RectTransform startMark = CreateEndMark(root, "Start", "S");
            RectTransform goalMark = CreateEndMark(root, "Goal", "G");
            CanvasGroup startMarkGroup = startMark.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup goalMarkGroup = goalMark.gameObject.AddComponent<CanvasGroup>();

            UiKit.DiamondMarker marker = UiKit.CreateDiamondMarker(root, "Marker",
                PathMarkerFrameSize, PathMarkerFillSize, PathMarkerIdFontSize, PathMarkerFrameColor);
            return new PathView(root, underLine, line, startMark, goalMark,
                startMarkGroup, goalMarkGroup, marker.Rect, marker.Fill, marker.Id);
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
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            OwnPathState? ownPath = _ownPath;
            MapProjectionInputs inputs = default;
            MapPanelProjection proj = default;
            bool visible = ownPath != null
                && TryReadMapView(out _, out _, out inputs)
                && (proj = inputs.ToProjection(PanelWidth, PanelHeight)).Valid;
            if (view.Path.Root.gameObject.activeSelf != visible) view.Path.Root.gameObject.SetActive(visible);
            if (!visible) return;
            if (ownPath == null) return;

            if (_pathDirty || !inputs.SameAs(_pathProjInputs))
            {
                _pathProjInputs = inputs;
                _pathDirty = false;
                LayoutPath(view.Path, ownPath, proj);
            }

            double elapsed = Time.unscaledTime - _pathOpenedAt;
            double t = ownPath.Path.PathTimeAt(elapsed);
            UpdatePathTrail(view.Path, ownPath.Path, proj, t);
            ownPath.Path.PositionAt(t, out float x, out float z);
            view.Path.Marker.anchoredPosition = new Vector2(proj.PanelX(x), proj.PanelY(z));
            float blink = MeetingPath.BlinkBrightness(elapsed);
            view.Path.StartMarkGroup.alpha = blink;
            view.Path.GoalMarkGroup.alpha = blink;
        }

        private static void LayoutPath(PathView view, OwnPathState ownPath, MapPanelProjection proj)
        {
            ReplayMarkerPalette.ColorFor(ownPath.ParticipantId, ownPath.ColorHex, out float r, out float g, out float b);
            view.Line.color = new Color(r, g, b, 0.9f);
            view.MarkerFill.color = new Color(r, g, b, 1f);
            view.MarkerId.color = UiKit.MarkerIdInk(r, g, b);
            string idText = ownPath.ParticipantId > 0 ? ownPath.ParticipantId.ToString() : "?";
            if (view.MarkerId.text != idText) view.MarkerId.text = idText;

            view.StartMark.anchoredPosition = new Vector2(
                proj.PanelX(ownPath.Path.Start.X), proj.PanelY(ownPath.Path.Start.Z));
            view.GoalMark.anchoredPosition = new Vector2(
                proj.PanelX(ownPath.Path.Goal.X), proj.PanelY(ownPath.Path.Goal.Z));
        }

        private void UpdatePathTrail(PathView view, MeetingPath path, MapPanelProjection proj, double pathT)
        {
            _pathPointScratch.Clear();
            path.TrailInto(pathT, PathMaxPoints, _pathPointScratch);
            _pathScratch.Clear();
            for (int i = 0; i < _pathPointScratch.Count; i++)
            {
                ReplayTrailPoint p = _pathPointScratch[i];
                _pathScratch.Add(new Vector2(proj.PanelX(p.X), proj.PanelY(p.Z)));
            }
            view.UnderLine.SetPoints(_pathScratch);
            view.Line.SetPoints(_pathScratch);
        }

        private void UpdateGrid(bool show)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (!show || !_rt.BoundsResolved)
            {
                view.GridRoot.gameObject.SetActive(false);
                return;
            }
            MapGridLayout? layout = ComputeGridLayout();
            if (layout == null)
            {
                view.GridRoot.gameObject.SetActive(false);
                return;
            }
            view.GridRoot.gameObject.SetActive(true);
            ApplyGridLayout(view, layout);
        }

        private MapGridLayout? ComputeGridLayout()
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

        private void ApplyGridLayout(ViewRefs view, MapGridLayout g)
        {
            float rectW = g.RectRight - g.RectLeft;
            float rectH = g.RectTop - g.RectBottom;
            float centerX = (g.RectLeft + g.RectRight) * 0.5f;
            float centerY = (g.RectBottom + g.RectTop) * 0.5f;

            EnsureGridLines(view, view.GridVLines, g.VerticalLineX.Length, "GridV");
            for (int i = 0; i < g.VerticalLineX.Length; i++)
            {
                RectTransform line = view.GridVLines[i];
                line.anchoredPosition = new Vector2(g.VerticalLineX[i], centerY);
                line.sizeDelta = new Vector2(GridLineThickness, rectH);
            }
            EnsureGridLines(view, view.GridHLines, g.HorizontalLineY.Length, "GridH");
            for (int i = 0; i < g.HorizontalLineY.Length; i++)
            {
                RectTransform line = view.GridHLines[i];
                line.anchoredPosition = new Vector2(centerX, g.HorizontalLineY[i]);
                line.sizeDelta = new Vector2(rectW, GridLineThickness);
            }

            EnsureGridLabels(view, view.GridColLabels, g.ColumnLabels.Length, "GridColLabel");
            for (int i = 0; i < g.ColumnLabels.Length; i++)
            {
                TextMeshProUGUI label = view.GridColLabels[i];
                if (label.text != g.ColumnLabels[i]) label.text = g.ColumnLabels[i];
                label.rectTransform.anchoredPosition = new Vector2(g.ColumnLabelX[i], g.ColumnLabelY);
            }
            EnsureGridLabels(view, view.GridRowLabels, g.RowLabels.Length, "GridRowLabel");
            for (int i = 0; i < g.RowLabels.Length; i++)
            {
                TextMeshProUGUI label = view.GridRowLabels[i];
                if (label.text != g.RowLabels[i]) label.text = g.RowLabels[i];
                label.rectTransform.anchoredPosition = new Vector2(g.RowLabelX, g.RowLabelY[i]);
            }
        }

        private static void EnsureGridLines(ViewRefs view, List<RectTransform> pool, int needed, string namePrefix)
        {
            while (pool.Count < needed)
            {
                Image img = UiKit.CreateImage(view.GridRoot, namePrefix + pool.Count, Vector2.zero,
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

        private static void EnsureGridLabels(ViewRefs view, List<TextMeshProUGUI> pool, int needed, string namePrefix)
        {
            while (pool.Count < needed)
            {
                TextMeshProUGUI label = UiKit.CreateText(view.GridRoot, namePrefix + pool.Count, Vector2.zero,
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
            ViewRefs? view = GetLiveViewOrNull();
            if (!_rt.IsOpen || view == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(view.MapPanelRect, screenPoint, null);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            _rt.Reset();
            _toggle.Clear();
            _mapToolBlocked = false;
            _prevMeetingActive = false;
            _lastLabelKey = KeyCode.None;
            _placeholderTex = null;
            _placeholderResolved = false;
            _gridModuleRangeResolved = false;
            _gridModuleRange = null;
            _pathDirty = true;
            return null;
        }

        private void ApplyTexts()
        {
            string keyLabel = _lastLabelKey == KeyCode.None ? "M" : FormatKey(_lastLabelKey);
            _toggle.SetLabel(Texts.Format(TextId.MapOverlayToggleLabelFormat, keyLabel));
        }

        private Texture? ResolvePlaceholder()
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
