using System;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ConveneHoldGauge : IClientPanel
    {
        public string LayerName => "Hud";

        private const float RadialSize = 96f;
        private static readonly Color ChargingColor = new Color(1f, 0.85f, 0.15f, 0.65f);

        private GameObject _root;
        private Image _radial;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);
            var go = new GameObject("WW_ConveneHoldGauge", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _radial = UiKit.CreateRadialImage(go.transform, "HoldRadial",
                Vector2.zero, new Vector2(RadialSize, RadialSize), ChargingColor);
            _root = go;
            _root.SetActive(false);
        }

        public void Tick(bool visible, float ratio, Vector3? buttonWorldPos)
        {
            if (_root == null) return;
            try
            {
                Camera cam = Camera.main;
                bool show = visible && buttonWorldPos != null && cam != null;
                Vector3 viewport = default;
                if (show)
                {
                    viewport = cam.WorldToViewportPoint(buttonWorldPos.Value);
                    if (viewport.z <= 0f
                        || viewport.x < 0f || viewport.x > 1f
                        || viewport.y < 0f || viewport.y > 1f)
                    {
                        show = false;
                    }
                }
                if (_root.activeSelf != show) _root.SetActive(show);
                if (!show) return;

                RectTransform canvasRect = (RectTransform)_root.transform;
                _radial.rectTransform.anchoredPosition = new Vector2(
                    (viewport.x - 0.5f) * canvasRect.rect.width,
                    (viewport.y - 0.5f) * canvasRect.rect.height);
                _radial.fillAmount = Mathf.Clamp01(ratio);
            }
            catch (Exception e)
            {
                WLog.Line("convene_hold_gauge_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _radial = null;
        }
    }
}
