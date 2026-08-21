using System;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal sealed class MapRtView
    {
        private const string DirtFinderCameraName = "Dirt Finder Map Camera";

        private readonly string _srcTag;

        private Camera _cachedCamera;

        private Vector3 _origCamLocalPosition;
        private Quaternion _origCamLocalRotation;
        private float _originalOrthoSize;
        private bool _cameraOverrideApplied;

        private CameraClearFlags _origClearFlags;
        private Color _origBackgroundColor;
        private bool _origClearSaved;

        private RenderTexture _overrideRt;
        private RenderTexture _origTargetTexture;
        private bool _targetTextureSaved;

        private bool _boundsResolved;
        private Vector2 _boundsCenterXZ;
        private float _boundsHalfW;
        private float _boundsHalfH;

        public MapRtView(string srcTag)
        {
            _srcTag = srcTag ?? "?";
        }

        public bool IsOpen { get; private set; }

        public Camera Camera => _cachedCamera;

        public bool BoundsResolved => _boundsResolved;
        public Vector2 BoundsCenterXZ => _boundsCenterXZ;
        public float BoundsHalfW => _boundsHalfW;
        public float BoundsHalfH => _boundsHalfH;

        public void Open(float orthoSizeConfig, int resolutionPreset)
        {
            if (IsOpen) return;
            IsOpen = true;

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
            }
            ApplyTargetTextureOverride(resolutionPreset);
            ApplyCameraOverride(orthoSizeConfig);
            WLog.Line("maprt_open", secret: false,
                ("src", _srcTag),
                ("orthoConfig", orthoSizeConfig),
                ("orthoApplied", _cachedCamera != null ? _cachedCamera.orthographicSize : -1f),
                ("rtW", _overrideRt != null ? _overrideRt.width : -1),
                ("rtH", _overrideRt != null ? _overrideRt.height : -1));
        }

        public Texture Tick(float orthoSizeConfig)
        {
            if (!IsOpen) return null;

            if (Map.Instance != null && !Map.Instance.Active)
            {
                Map.Instance.ActiveSet(true);
            }

            ApplyCameraOverride(orthoSizeConfig);

            return _cachedCamera != null ? _cachedCamera.activeTexture : null;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            try
            {
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
            }
            catch (Exception e)
            {
                WLog.Line("maprt_close_error", secret: false, ("src", _srcTag), ("err", e.Message));
            }
            RestoreTargetTexture();
            _origClearSaved = false;
            _cameraOverrideApplied = false;
            _boundsResolved = false;
            WLog.Line("maprt_close", secret: false, ("src", _srcTag));
        }

        public void Reset()
        {
            Close();
            _cachedCamera = null;
            _origCamLocalPosition = Vector3.zero;
            _origCamLocalRotation = Quaternion.identity;
            _originalOrthoSize = 0f;
            _origClearFlags = default;
            _origBackgroundColor = default;
            _boundsCenterXZ = Vector2.zero;
            _boundsHalfW = 0f;
            _boundsHalfH = 0f;
        }

        public static bool IsMapToolActive()
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
                rt.name = "WW_MapRtView_RT";
                rt.antiAliasing = 1;
                rt.filterMode = FilterMode.Bilinear;
                rt.wrapMode = TextureWrapMode.Clamp;
                if (!rt.Create())
                {
                    UnityEngine.Object.Destroy(rt);
                    WLog.Line("maprt_rt_create_failed", secret: false,
                        ("src", _srcTag), ("w", w), ("h", h));
                    return;
                }
                _origTargetTexture = _cachedCamera.targetTexture;
                _cachedCamera.targetTexture = rt;
                _overrideRt = rt;
                _targetTextureSaved = true;
            }
            catch (Exception e)
            {
                WLog.Line("maprt_rt_override_error", secret: false, ("src", _srcTag), ("err", e.Message));
            }
        }

        private void RestoreTargetTexture()
        {
            if (_cachedCamera == null || !_targetTextureSaved)
            {
                ReleaseOverrideRt();
                _targetTextureSaved = false;
                return;
            }
            try
            {
                _cachedCamera.targetTexture = _origTargetTexture;
            }
            catch (Exception e)
            {
                WLog.Line("maprt_rt_restore_error", secret: false, ("src", _srcTag), ("err", e.Message));
            }
            ReleaseOverrideRt();
            _origTargetTexture = null;
            _targetTextureSaved = false;
        }

        private void ReleaseOverrideRt()
        {
            if (_overrideRt == null) return;
            _overrideRt.Release();
            UnityEngine.Object.Destroy(_overrideRt);
            _overrideRt = null;
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
                WLog.Line("maprt_bounds_resolved", secret: false,
                    ("src", _srcTag),
                    ("cx", _boundsCenterXZ.x), ("cz", _boundsCenterXZ.y),
                    ("hw", _boundsHalfW), ("hh", _boundsHalfH),
                    ("rcOver", overCount), ("rcLayers", layerCount));
            }
            catch (Exception e)
            {
                WLog.Line("maprt_bounds_error", secret: false, ("src", _srcTag), ("err", e.Message));
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
                    WLog.Line("maprt_camera_missing", secret: false, ("src", _srcTag));
                }
            }
            catch (Exception e)
            {
                WLog.Line("maprt_camera_error", secret: false, ("src", _srcTag), ("err", e.Message));
                _cachedCamera = null;
            }
        }
    }
}
