using System;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.UI
{
    public enum ScenarioTargetMarkKind : byte
    {
        None = 0,

        BombTarget = 1,

        Corpse = 2,
    }

    public sealed class ScenarioTargetMarkPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private readonly struct Look
        {
            public Look(Color color, Func<Sprite?> icon, float offsetY, bool hideWhileVisible)
            {
                Color = color;
                Icon = icon;
                OffsetY = offsetY;
                HideWhileVisible = hideWhileVisible;
            }

            public Color Color { get; }

            public Func<Sprite?> Icon { get; }

            public float OffsetY { get; }

            public bool HideWhileVisible { get; }
        }

        private static readonly Look BombTargetLook = new Look(
            MarkerColors.Bomber, AssetCatalog.GetBombIcon, offsetY: 1.7f, hideWhileVisible: true);

        private static readonly Look CorpseLook = new Look(
            new Color(0.72f, 0.86f, 1f, 1f), () => AssetCatalog.GetSprite("icon_status_dead"),
            offsetY: 1.0f, hideWhileVisible: false);

        private static Look LookOf(ScenarioTargetMarkKind kind)
            => kind == ScenarioTargetMarkKind.Corpse ? CorpseLook : BombTargetLook;

        private const float RingSize = 72f;
        private const float IconSize = 34f;
        private const float ArrowSize = 44f;

        private const float EdgeMargin = 72f;

        private const float PulseSeconds = 1.1f;
        private const float PulseMinAlpha = 0.35f;
        private const float PulseMaxAlpha = 0.85f;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, Image ring, Image icon, Image arrow)
            {
                Root = root;
                Ring = ring;
                Icon = icon;
                Arrow = arrow;
            }

            public GameObject Root { get; }
            public Image Ring { get; }
            public Image Icon { get; }
            public Image Arrow { get; }
        }

        private ViewRefs? _view;

        private ScenarioTargetMarkKind _kind;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            GameObject? partialRoot = null;
            try
            {
                var go = new GameObject("WW_ScenarioTargetMark", typeof(RectTransform));
                partialRoot = go;
                var rect = (RectTransform)go.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);

                Image ring = UiKit.CreateImage(go.transform, "TargetRing",
                    Vector2.zero, new Vector2(RingSize, RingSize), Color.white);
                ring.sprite = UiKit.RingSprite();

                Image icon = UiKit.CreateImage(go.transform, "TargetIcon",
                    Vector2.zero, new Vector2(IconSize, IconSize), Color.white);

                Image arrow = UiKit.CreateImage(go.transform, "TargetArrow",
                    Vector2.zero, new Vector2(ArrowSize, ArrowSize), Color.white);
                arrow.sprite = UiKit.TriangleUpSprite();

                go.SetActive(false);
                Look initialLook = LookOf(ScenarioTargetMarkKind.BombTarget);
                ring.color = initialLook.Color;
                arrow.color = initialLook.Color;
                icon.sprite = initialLook.Icon();
                _kind = ScenarioTargetMarkKind.BombTarget;
                _view = new ViewRefs(go, ring, icon, arrow);
                partialRoot = null;
            }
            catch
            {
                if (partialRoot != null) UnityEngine.Object.Destroy(partialRoot);
                _view = null;
                throw;
            }
        }

        public void Tick(ScenarioTargetMarkKind kind, Vector3? worldPos)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            try
            {
                Camera? cam = Camera.main;
                if (kind == ScenarioTargetMarkKind.None || worldPos == null || cam == null)
                {
                    SetShown(false);
                    return;
                }

                Look look = LookOf(kind);
                Vector3 head = worldPos.Value + Vector3.up * look.OffsetY;
                if (look.HideWhileVisible && OverheadVision.BodyVisibleFromCamera(worldPos.Value, cam))
                {
                    SetShown(false);
                    return;
                }

                ApplyKind(kind);
                SetShown(true);
                var canvasRect = (RectTransform)view.Root.transform;
                Vector3 viewport = cam.WorldToViewportPoint(head);
                bool onScreen = viewport.z > 0f
                                && viewport.x >= 0f && viewport.x <= 1f
                                && viewport.y >= 0f && viewport.y <= 1f;

                float alpha = PulseAlpha();
                if (onScreen)
                {
                    Vector2 uiPos = OverheadProjection.ViewportToUi(viewport, canvasRect);
                    PlaceMark(uiPos, alpha);
                }
                else
                {
                    PlaceArrow(EdgeDirection(viewport, canvasRect), canvasRect, alpha);
                }
            }
            catch (Exception e)
            {
                WLog.Line("scenario_target_mark_error", secret: false, ("err", e.Message));
            }
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
            _kind = ScenarioTargetMarkKind.None;
        }

        private void SetShown(bool shown)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null && view.Root.activeSelf != shown) view.Root.SetActive(shown);
        }

        private void ApplyKind(ScenarioTargetMarkKind kind)
        {
            if (_kind == kind) return;
            _kind = kind;

            Look look = LookOf(kind);
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            view.Ring.color = look.Color;
            view.Arrow.color = look.Color;
            view.Icon.sprite = look.Icon();
        }

        private void PlaceMark(Vector2 uiPos, float alpha)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            Image ring = view.Ring;
            Image icon = view.Icon;
            Image arrow = view.Arrow;
            ring.rectTransform.anchoredPosition = uiPos;
            icon.rectTransform.anchoredPosition = uiPos;
            ring.enabled = true;
            icon.enabled = icon.sprite != null;
            arrow.enabled = false;
            SetAlpha(ring, alpha);
            SetAlpha(icon, alpha);
        }

        private void PlaceArrow(Vector2 direction, RectTransform canvasRect, float alpha)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            Image ring = view.Ring;
            Image icon = view.Icon;
            Image arrow = view.Arrow;
            ring.enabled = false;
            icon.enabled = false;
            arrow.enabled = true;

            float halfW = Mathf.Max(1f, canvasRect.rect.width * 0.5f - EdgeMargin);
            float halfH = Mathf.Max(1f, canvasRect.rect.height * 0.5f - EdgeMargin);
            float scale = Mathf.Min(
                halfW / Mathf.Max(Mathf.Abs(direction.x), 0.0001f),
                halfH / Mathf.Max(Mathf.Abs(direction.y), 0.0001f));

            arrow.rectTransform.anchoredPosition = direction * scale;
            arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            SetAlpha(arrow, alpha);
        }

        private static Vector2 EdgeDirection(Vector3 viewport, RectTransform canvasRect)
        {
            Vector2 v = viewport.z > 0f
                ? new Vector2(viewport.x, viewport.y)
                : new Vector2(1f - viewport.x, 1f - viewport.y);
            Vector2 dir = OverheadProjection.ViewportToUi(v, canvasRect);
            return dir.sqrMagnitude < 0.0001f ? Vector2.up : dir.normalized;
        }

        private static float PulseAlpha()
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / PulseSeconds));
            return Mathf.Lerp(PulseMinAlpha, PulseMaxAlpha, wave);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;
            _view = null;
            _kind = ScenarioTargetMarkKind.None;
            return null;
        }
    }
}
