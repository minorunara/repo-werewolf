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

        private const string DirtFinderCameraName = "Dirt Finder Map Camera";

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

        private bool _open;
        private bool _prevMeetingActive;
        private KeyCode _lastLabelKey = KeyCode.None;
        private Camera _cachedCamera;

        private Vector3 _origCamLocalPosition;
        private Quaternion _origCamLocalRotation;
        private float _originalOrthoSize;
        private bool _cameraOverrideApplied;

        private RenderTexture _overrideRt;
        private RenderTexture _origTargetTexture;
        private bool _targetTextureSaved;

        private bool _boundsResolved;
        private Vector2 _boundsCenterXZ;
        private float _boundsHalfW;
        private float _boundsHalfH;

        private CameraClearFlags _origClearFlags;
        private Color _origBackgroundColor;
        private bool _origClearSaved;

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
                bool toolActive = IsMapToolActive();
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
                    if (Map.Instance != null && !Map.Instance.Active)
                    {
                        Map.Instance.ActiveSet(true);
                    }

                    RefreshMapTexture();
                    ApplyCameraOverride(orthoSize);
                    UpdateGrid(gridEnabled);
                }
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_tick_error", secret: false, ("err", e.Message));
            }
        }

        private static bool IsMapToolActive()
        {
            try
            {
                MapToolController tool = MapToolController.instance;
                if (tool == null) return false;
                return GameRefs.MapToolController_Active(tool);
            }
            catch
            {
                return false;
            }
        }

        public void ForceClose()
        {
            if (_open) CloseInternal();
        }

        public void Destroy()
        {
            if (_open) CloseInternal();
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
            _cachedCamera = null;
            _origCamLocalPosition = Vector3.zero;
            _origCamLocalRotation = Quaternion.identity;
            _originalOrthoSize = 0f;
            _cameraOverrideApplied = false;
            _origClearFlags = default;
            _origBackgroundColor = default;
            _origClearSaved = false;
            _boundsResolved = false;
            _boundsCenterXZ = Vector2.zero;
            _boundsHalfW = 0f;
            _boundsHalfH = 0f;
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

            EnsureCameraResolved();
            if (_cachedCamera != null && !_cameraOverrideApplied)
            {
                Transform camT = _cachedCamera.transform;
                _origCamLocalPosition = camT.localPosition;
                _origCamLocalRotation = camT.localRotation;
                _originalOrthoSize = _cachedCamera.orthographicSize;
                _cameraOverrideApplied = true;
            }
            if (_cachedCamera != null && !_origClearSaved)
            {
                _origClearFlags = _cachedCamera.clearFlags;
                _origBackgroundColor = _cachedCamera.backgroundColor;
                _origClearSaved = true;
                _cachedCamera.clearFlags = CameraClearFlags.SolidColor;
                _cachedCamera.backgroundColor = new Color(0f, 0f, 0f, 1f);
                WLog.Line("mapoverlay_clear_override", secret: false,
                    ("origFlags", _origClearFlags.ToString()),
                    ("origBgA", _origBackgroundColor.a));
            }
            ApplyTargetTextureOverride(resolutionPreset);
            ApplyCameraOverride(orthoSizeConfig);
            RefreshMapTexture();
            WLog.Line("mapoverlay_open", secret: false,
                ("orthoConfig", orthoSizeConfig),
                ("orthoApplied", _cachedCamera != null ? _cachedCamera.orthographicSize : -1f),
                ("rtW", _overrideRt != null ? _overrideRt.width : -1),
                ("rtH", _overrideRt != null ? _overrideRt.height : -1));
        }

        private void ApplyTargetTextureOverride(int resolutionPreset)
        {
            if (_cachedCamera == null) return;
            if (_targetTextureSaved) return;

            int w, h;
            switch (resolutionPreset)
            {
                case 0: w = 1280; h = 768; break;
                case 2: w = 1920; h = 1152; break;
                default: w = 1600; h = 960; break;
            }

            try
            {
                var rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
                rt.name = "WW_MeetingMapOverlay_RT";
                rt.antiAliasing = 1;
                rt.filterMode = FilterMode.Bilinear;
                rt.wrapMode = TextureWrapMode.Clamp;
                if (!rt.Create())
                {
                    UnityEngine.Object.Destroy(rt);
                    WLog.Line("mapoverlay_rt_create_failed", secret: false, ("w", w), ("h", h));
                    return;
                }
                _origTargetTexture = _cachedCamera.targetTexture;
                _cachedCamera.targetTexture = rt;
                _overrideRt = rt;
                _targetTextureSaved = true;
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_rt_override_error", secret: false, ("err", e.Message));
            }
        }

        private void RestoreTargetTexture()
        {
            if (_cachedCamera == null || !_targetTextureSaved) return;
            try
            {
                _cachedCamera.targetTexture = _origTargetTexture;
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_rt_restore_error", secret: false, ("err", e.Message));
            }
            if (_overrideRt != null)
            {
                _overrideRt.Release();
                UnityEngine.Object.Destroy(_overrideRt);
                _overrideRt = null;
            }
            _origTargetTexture = null;
            _targetTextureSaved = false;
        }

        private void CloseInternal()
        {
            _open = false;
            if (_mapPanel != null) _mapPanel.SetActive(false);
            if (_cachedCamera != null && _cameraOverrideApplied)
            {
                Transform camT = _cachedCamera.transform;
                camT.localPosition = _origCamLocalPosition;
                camT.localRotation = _origCamLocalRotation;
                _cachedCamera.orthographicSize = _originalOrthoSize;
            }
            if (_cachedCamera != null && _origClearSaved)
            {
                _cachedCamera.clearFlags = _origClearFlags;
                _cachedCamera.backgroundColor = _origBackgroundColor;
            }
            RestoreTargetTexture();
            _origClearSaved = false;
            _cameraOverrideApplied = false;
            _boundsResolved = false;
            _gridModuleRangeResolved = false;
            _gridModuleRange = null;
            if (_gridRoot != null) _gridRoot.gameObject.SetActive(false);
            WLog.Line("mapoverlay_close", secret: false);
        }

        private void ApplyCameraOverride(float orthoSizeConfig)
        {
            if (_cachedCamera == null) return;
            Transform camT = _cachedCamera.transform;

            camT.rotation = Quaternion.Euler(90f, 0f, 0f);

            TryResolveMiniatureBounds();
            if (_boundsResolved)
            {
                Vector3 cur = camT.position;
                camT.position = new Vector3(_boundsCenterXZ.x, cur.y, _boundsCenterXZ.y);
            }
            else
            {
                Map map = Map.Instance;
                if (map != null && map.OverLayerParent != null)
                {
                    Vector3 center = map.OverLayerParent.position;
                    Vector3 cur = camT.position;
                    camT.position = new Vector3(center.x, cur.y, center.z);
                }
            }

            _cachedCamera.orthographicSize = ComputeOrthoSize(orthoSizeConfig);
        }

        private float ComputeOrthoSize(float fallback)
        {
            if (_cachedCamera == null) return fallback;
            float aspect = Mathf.Max(0.01f, _cachedCamera.aspect);

            if (_boundsResolved)
            {
                float required = Mathf.Max(_boundsHalfH, _boundsHalfW / aspect);
                return required * 1.05f;
            }

            try
            {
                LevelGenerator lg = LevelGenerator.Instance;
                Map m = Map.Instance;
                if (lg == null || m == null) return fallback;
                if (lg.LevelWidth <= 0 || lg.LevelHeight <= 0) return fallback;

                float moduleWidth = LevelGenerator.ModuleWidth * LevelGenerator.TileSize;
                float worldW = (float)lg.LevelWidth * moduleWidth;
                float worldH = (float)lg.LevelHeight * moduleWidth;
                float scale = m.Scale;
                float miniW = worldW * scale;
                float miniH = worldH * scale;
                float required = Mathf.Max(miniH * 0.5f, miniW / (2f * aspect));
                return required * 1.05f;
            }
            catch
            {
                return fallback;
            }
        }

        private void TryResolveMiniatureBounds()
        {
            if (_boundsResolved) return;
            try
            {
                Map map = Map.Instance;
                if (map == null || map.OverLayerParent == null) return;

                bool has = false;
                float minX = 0f, maxX = 0f, minZ = 0f, maxZ = 0f;
                int overCount = AccumulateBoundsXZ(map.OverLayerParent,
                    ref has, ref minX, ref maxX, ref minZ, ref maxZ);
                int layerCount = 0;
                if (map.Layers != null)
                {
                    for (int i = 0; i < map.Layers.Count; i++)
                    {
                        MapLayer layer = map.Layers[i];
                        if (layer == null) continue;
                        layerCount += AccumulateBoundsXZ(layer.transform,
                            ref has, ref minX, ref maxX, ref minZ, ref maxZ);
                    }
                }
                if (!has) return;

                _boundsCenterXZ = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
                _boundsHalfW = (maxX - minX) * 0.5f;
                _boundsHalfH = (maxZ - minZ) * 0.5f;
                _boundsResolved = true;
                WLog.Line("mapoverlay_bounds_resolved", secret: false,
                    ("cx", _boundsCenterXZ.x), ("cz", _boundsCenterXZ.y),
                    ("hw", _boundsHalfW), ("hh", _boundsHalfH),
                    ("rcOver", overCount), ("rcLayers", layerCount));
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_bounds_error", secret: false, ("err", e.Message));
            }
        }

        private static int AccumulateBoundsXZ(Transform root, ref bool has,
            ref float minX, ref float maxX, ref float minZ, ref float maxZ)
        {
            if (root == null) return 0;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null) return 0;
            int counted = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                Bounds b = r.bounds;
                Vector3 lo = b.min;
                Vector3 hi = b.max;
                if (!has)
                {
                    minX = lo.x; maxX = hi.x;
                    minZ = lo.z; maxZ = hi.z;
                    has = true;
                }
                else
                {
                    if (lo.x < minX) minX = lo.x;
                    if (hi.x > maxX) maxX = hi.x;
                    if (lo.z < minZ) minZ = lo.z;
                    if (hi.z > maxZ) maxZ = hi.z;
                }
                counted++;
            }
            return counted;
        }

        private void UpdateGrid(bool show)
        {
            if (_gridRoot == null) return;
            if (!show || !_boundsResolved)
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
                if (lg == null || map == null || map.OverLayerParent == null || _cachedCamera == null) return null;
                Vector3 origin = map.OverLayerParent.position;
                Vector3 cam = _cachedCamera.transform.position;

                EnsureGridModuleRange(lg);
                MapGridCellRange? range = _gridModuleRange ?? MapGridMath.RangeFromBounds(
                    lg.LevelWidth, lg.LevelHeight,
                    map.Scale, origin.x, origin.z,
                    _boundsCenterXZ.x - _boundsHalfW, _boundsCenterXZ.x + _boundsHalfW,
                    _boundsCenterXZ.y - _boundsHalfH, _boundsCenterXZ.y + _boundsHalfH);
                if (range == null) return null;

                return MapGridMath.Compute(
                    lg.LevelWidth, range.Value,
                    map.Scale, origin.x, origin.z,
                    cam.x, cam.z,
                    _cachedCamera.orthographicSize, Mathf.Max(0.01f, _cachedCamera.aspect),
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

        private void EnsureCameraResolved()
        {
            if (_cachedCamera != null) return;
            try
            {
                Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i] != null && cameras[i].name == DirtFinderCameraName)
                    {
                        _cachedCamera = cameras[i];
                        break;
                    }
                }
                if (_cachedCamera == null)
                {
                    WLog.Line("mapoverlay_camera_missing", secret: false);
                }
            }
            catch (Exception e)
            {
                WLog.Line("mapoverlay_camera_error", secret: false, ("err", e.Message));
                _cachedCamera = null;
            }
        }

        private void RefreshMapTexture()
        {
            if (_mapImage == null) return;

            Texture tex = null;
            if (_cachedCamera != null)
            {
                tex = _cachedCamera.activeTexture;
            }
            if (tex == null)
            {
                tex = ResolvePlaceholder();
            }
            _mapImage.texture = tex;
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
