#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class ConveneHoldGauge : IClientPanel
    {
        public string LayerName => "Hud";

        private const float RadialSize = 96f;
        private static readonly Color ChargingColor = new Color(1f, 0.85f, 0.15f, 0.65f);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, Image radial)
            {
                Root = root;
                Radial = radial;
            }

            public GameObject Root { get; }
            public Image Radial { get; }
        }

        private ViewRefs? _view;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);
                root = new GameObject("WW_ConveneHoldGauge", typeof(RectTransform));
                var rect = (RectTransform)root.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);
                Image radial = UiKit.CreateRadialImage(root.transform, "HoldRadial",
                    Vector2.zero, new Vector2(RadialSize, RadialSize), ChargingColor);

                root.SetActive(false);
                _view = new ViewRefs(root, radial);
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

        public void Tick(bool visible, float ratio, Vector3? buttonWorldPos)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            try
            {
                Camera? cam = Camera.main;
                bool show = false;
                Vector3 viewport = default;
                if (visible && buttonWorldPos.HasValue && cam != null)
                {
                    viewport = cam.WorldToViewportPoint(buttonWorldPos.Value);
                    show = viewport.z > 0f
                        && viewport.x >= 0f && viewport.x <= 1f
                        && viewport.y >= 0f && viewport.y <= 1f;
                }
                if (view.Root.activeSelf != show) view.Root.SetActive(show);
                if (!show) return;

                RectTransform canvasRect = (RectTransform)view.Root.transform;
                view.Radial.rectTransform.anchoredPosition = new Vector2(
                    (viewport.x - 0.5f) * canvasRect.rect.width,
                    (viewport.y - 0.5f) * canvasRect.rect.height);
                view.Radial.fillAmount = Mathf.Clamp01(ratio);
            }
            catch (Exception e)
            {
                WLog.Line("convene_hold_gauge_tick_error", secret: false, ("err", e.Message));
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            return null;
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }
    }
}
