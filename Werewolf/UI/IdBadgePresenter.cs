using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class IdBadgePresenter : IClientPanel
    {
        public string LayerName => "Hud";

        private const float HeadOffsetY = 1.7f;
        private const float BadgeVerticalOffsetPx = 26f;
        private const float FontSize = 30f;
        private const float BaseAlpha = 0.95f;
        private static readonly Color DefaultColor = new Color(1f, 1f, 1f, BaseAlpha);

        private GameObject _root;
        private readonly Dictionary<int, TextMeshProUGUI> _labels = new Dictionary<int, TextMeshProUGUI>();
        private readonly Dictionary<int, int> _labelIds = new Dictionary<int, int>();
        private readonly OverheadIdGate _gate = new OverheadIdGate();

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);
            var go = new GameObject("WW_IdBadgePresenter", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;
            _root.SetActive(false);
        }

        public void Tick(bool visible,
            IReadOnlyDictionary<int, int> idByActor,
            int localActor,
            Func<int, Role?> markedRole,
            Func<int, Vector3?> resolveWorldPos)
        {
            if (_root == null) return;
            try
            {
                if (_root.activeSelf != visible) _root.SetActive(visible);
                if (!visible || idByActor == null || idByActor.Count == 0)
                {
                    HideAll();
                    _gate.Reset();
                    return;
                }

                Camera cam = Camera.main;
                if (cam == null)
                {
                    HideAll();
                    _gate.Reset();
                    return;
                }

                Vector3 camPos = cam.transform.position;
                Vector3 camForward = cam.transform.forward;
                float dt = Time.unscaledDeltaTime;

                foreach (KeyValuePair<int, int> pair in idByActor)
                {
                    int actor = pair.Key;
                    if (actor == localActor) continue;

                    Vector3? body = resolveWorldPos != null ? resolveWorldPos(actor) : null;
                    bool bodyVisible = body != null && OverheadVision.BodyVisibleFromCamera(body.Value, cam);
                    Vector3 anchor = body != null ? body.Value + Vector3.up * HeadOffsetY : Vector3.zero;
                    Vector3 toTarget = anchor - camPos;
                    float alpha = _gate.Tick(actor, bodyVisible,
                        camForward.x, camForward.y, camForward.z,
                        toTarget.x, toTarget.y, toTarget.z, dt);

                    TextMeshProUGUI label = EnsureLabel(actor, pair.Value);
                    if (alpha <= 0f || body == null)
                    {
                        if (label.enabled) label.enabled = false;
                        continue;
                    }

                    if (!OverheadProjection.TryProject(cam, anchor,
                            (RectTransform)_root.transform, out Vector2 uiPos))
                    {
                        if (label.enabled) label.enabled = false;
                        continue;
                    }

                    if (!label.enabled) label.enabled = true;
                    uiPos.y += BadgeVerticalOffsetPx;
                    label.rectTransform.anchoredPosition = uiPos;

                    Role? marked = markedRole != null ? markedRole(actor) : null;
                    Color color = MarkerColors.ForRole(marked, Color.white);
                    color.a = BaseAlpha * alpha;
                    label.color = color;
                }
            }
            catch (Exception e)
            {
                WLog.Line("id_badge_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf) _root.SetActive(false);
            HideAll();
            _gate.Reset();
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _labels.Clear();
            _labelIds.Clear();
            _gate.Reset();
        }

        private TextMeshProUGUI EnsureLabel(int actor, int id)
        {
            if (_labels.TryGetValue(actor, out TextMeshProUGUI cached) && cached != null)
            {
                if (_labelIds.TryGetValue(actor, out int prevId) && prevId != id)
                {
                    cached.text = id.ToString();
                    _labelIds[actor] = id;
                }
                return cached;
            }
            TextMeshProUGUI label = UiKit.CreateText(_root.transform, "IdBadge_" + actor,
                Vector2.zero, new Vector2(120f, 44f), id.ToString(), FontSize,
                DefaultColor, TextAlignmentOptions.Center);
            label.enabled = false;
            _labels[actor] = label;
            _labelIds[actor] = id;
            return label;
        }

        private void HideAll()
        {
            foreach (TextMeshProUGUI label in _labels.Values)
            {
                if (label != null && label.enabled) label.enabled = false;
            }
        }
    }
}
