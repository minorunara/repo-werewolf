using System;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.UI;

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
            public Look(Color color, Func<Sprite> icon, float offsetY, bool hideWhileVisible)
            {
                Color = color;
                Icon = icon;
                OffsetY = offsetY;
                HideWhileVisible = hideWhileVisible;
            }

            public Color Color { get; }

            public Func<Sprite> Icon { get; }

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

        private GameObject _root;
        private Image _ring;
        private Image _icon;
        private Image _arrow;

        private ScenarioTargetMarkKind _kind;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_ScenarioTargetMark", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);

            _ring = UiKit.CreateImage(go.transform, "TargetRing",
                Vector2.zero, new Vector2(RingSize, RingSize), Color.white);
            _ring.sprite = UiKit.RingSprite();

            _icon = UiKit.CreateImage(go.transform, "TargetIcon",
                Vector2.zero, new Vector2(IconSize, IconSize), Color.white);

            _arrow = UiKit.CreateImage(go.transform, "TargetArrow",
                Vector2.zero, new Vector2(ArrowSize, ArrowSize), Color.white);
            _arrow.sprite = UiKit.TriangleUpSprite();

            _root = go;
            _root.SetActive(false);
            _kind = ScenarioTargetMarkKind.None;
            ApplyKind(ScenarioTargetMarkKind.BombTarget);
        }

        public void Tick(ScenarioTargetMarkKind kind, Vector3? worldPos)
        {
            if (_root == null) return;
            try
            {
                Camera cam = Camera.main;
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
                var canvasRect = (RectTransform)_root.transform;
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
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _ring = null;
            _icon = null;
            _arrow = null;
        }

        private void SetShown(bool shown)
        {
            if (_root.activeSelf != shown) _root.SetActive(shown);
        }

        private void ApplyKind(ScenarioTargetMarkKind kind)
        {
            if (_kind == kind) return;
            _kind = kind;

            Look look = LookOf(kind);
            _ring.color = look.Color;
            _arrow.color = look.Color;
            _icon.sprite = look.Icon();
        }

        private void PlaceMark(Vector2 uiPos, float alpha)
        {
            _ring.rectTransform.anchoredPosition = uiPos;
            _icon.rectTransform.anchoredPosition = uiPos;
            _ring.enabled = true;
            _icon.enabled = _icon.sprite != null;
            _arrow.enabled = false;
            SetAlpha(_ring, alpha);
            SetAlpha(_icon, alpha);
        }

        private void PlaceArrow(Vector2 direction, RectTransform canvasRect, float alpha)
        {
            _ring.enabled = false;
            _icon.enabled = false;
            _arrow.enabled = true;

            float halfW = Mathf.Max(1f, canvasRect.rect.width * 0.5f - EdgeMargin);
            float halfH = Mathf.Max(1f, canvasRect.rect.height * 0.5f - EdgeMargin);
            float scale = Mathf.Min(
                halfW / Mathf.Max(Mathf.Abs(direction.x), 0.0001f),
                halfH / Mathf.Max(Mathf.Abs(direction.y), 0.0001f));

            _arrow.rectTransform.anchoredPosition = direction * scale;
            _arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            SetAlpha(_arrow, alpha);
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
    }
}
