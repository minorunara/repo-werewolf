#nullable enable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Vanilla;

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

        private sealed class Badge
        {
            public Badge(TextMeshProUGUI label, int id)
            {
                Label = label;
                Id = id;
            }

            public TextMeshProUGUI Label { get; }
            public int Id { get; set; }
        }

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root)
            {
                Root = root;
                Badges = new Dictionary<int, Badge>();
                Gate = new OverheadIdGate();
            }

            public GameObject Root { get; }
            public Dictionary<int, Badge> Badges { get; }
            public OverheadIdGate Gate { get; }
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
                root = new GameObject("WW_IdBadgePresenter", typeof(RectTransform));
                var rect = (RectTransform)root.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);
                root.SetActive(false);
                _view = new ViewRefs(root);
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

        public void Tick(bool visible,
            IReadOnlyDictionary<int, int> idByActor,
            int localActor,
            Func<int, Role?> markedRole,
            Func<int, Vector3?> resolveWorldPos)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            try
            {
                if (view.Root.activeSelf != visible) view.Root.SetActive(visible);
                if (!visible || idByActor == null || idByActor.Count == 0)
                {
                    HideAll(view);
                    view.Gate.Reset();
                    return;
                }

                Camera cam = Camera.main;
                if (cam == null)
                {
                    HideAll(view);
                    view.Gate.Reset();
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
                    float alpha = view.Gate.Tick(actor, bodyVisible,
                        camForward.x, camForward.y, camForward.z,
                        toTarget.x, toTarget.y, toTarget.z, dt);

                    TextMeshProUGUI label = EnsureLabel(view, actor, pair.Value);
                    if (alpha <= 0f || body == null)
                    {
                        if (label.enabled) label.enabled = false;
                        continue;
                    }

                    if (!OverheadProjection.TryProject(cam, anchor,
                            (RectTransform)view.Root.transform, out Vector2 uiPos))
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
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (view.Root.activeSelf) view.Root.SetActive(false);
            HideAll(view);
            view.Gate.Reset();
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view == null) return;
            ResetViewState(view);
            if (view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private static TextMeshProUGUI EnsureLabel(ViewRefs view, int actor, int id)
        {
            if (view.Badges.TryGetValue(actor, out Badge cached) && cached.Label != null)
            {
                if (cached.Id != id)
                {
                    cached.Label.text = id.ToString();
                    cached.Id = id;
                }
                return cached.Label;
            }
            TextMeshProUGUI label = UiKit.CreateText(view.Root.transform, "IdBadge_" + actor,
                Vector2.zero, new Vector2(120f, 44f), id.ToString(), FontSize,
                DefaultColor, TextAlignmentOptions.Center);
            label.enabled = false;
            view.Badges[actor] = new Badge(label, id);
            return label;
        }

        private static void HideAll(ViewRefs view)
        {
            foreach (Badge badge in view.Badges.Values)
            {
                if (badge.Label != null && badge.Label.enabled) badge.Label.enabled = false;
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            ResetViewState(view);
            return null;
        }

        private static void ResetViewState(ViewRefs view)
        {
            view.Badges.Clear();
            view.Gate.Reset();
        }
    }
}
