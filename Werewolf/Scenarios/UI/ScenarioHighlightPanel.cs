using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.UI
{
    public sealed class ScenarioHighlightPanel : IClientPanel
    {
        public string LayerName => "ScenarioGuide";

        private const float EdgeThickness = 5f;

        private const float PulseSeconds = 1.1f;
        private const float PulseMinAlpha = 0.45f;
        private const float PulseMaxAlpha = 1f;

        private const float FadeSeconds = 0.15f;

        private const float MoveLerpPerSecond = 16f;

        private static readonly Color FrameColor = new Color(0.95f, 0.15f, 0.15f, 1f);

        private GameObject _root;
        private RectTransform _rootRect;
        private CanvasGroup _group;
        private RectTransform _frameRect;
        private readonly Image[] _edges = new Image[4];

        private Vector2 _targetCenter;
        private Vector2 _targetSize;
        private Vector2 _shownCenter;
        private Vector2 _shownSize;
        private bool _placed;
        private bool _visible;

        public bool Exists => _root != null;

        public bool Visible => _visible;

        public bool TryGetCanvas(out CanvasBounds canvas) => UiKit.TryMeasureCanvas(_rootRect, out canvas);

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            _rootRect = UiKit.CreateStretched(layerRoot, "WW_ScenarioHighlight");
            var go = _rootRect.gameObject;
            _root = go;

            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.ScenarioHighlightSortingOrder;
            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _frameRect = UiKit.CreateRect(_rootRect, "Frame", Vector2.zero, Vector2.one);
            for (int i = 0; i < _edges.Length; i++)
            {
                _edges[i] = UiKit.CreateImage(_frameRect, "Edge" + i, Vector2.zero, Vector2.one, FrameColor);
                _edges[i].raycastTarget = false;
            }

            _root.SetActive(false);
        }

        public void ShowBox(GuideBox box)
        {
            SetTarget(new Vector2(box.X, box.Y),
                new Vector2(box.Width + ScenarioHighlightLayout.Padding * 2f,
                            box.Height + ScenarioHighlightLayout.Padding * 2f));
        }

        public void ShowScreenRect(Rect screenRect)
        {
            if (_rootRect == null || screenRect.width <= 1f || screenRect.height <= 1f)
            {
                Hide();
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootRect, screenRect.min, null, out Vector2 min)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootRect, screenRect.max, null, out Vector2 max))
            {
                Hide();
                return;
            }

            SetTarget((min + max) * 0.5f,
                new Vector2(Mathf.Abs(max.x - min.x) + ScenarioHighlightLayout.Padding * 2f,
                            Mathf.Abs(max.y - min.y) + ScenarioHighlightLayout.Padding * 2f));
        }

        public void Hide()
        {
            _visible = false;
        }

        public void Tick()
        {
            if (_root == null || !_root.activeSelf) return;

            float delta = Time.deltaTime;
            float pulse = _visible
                ? Mathf.Lerp(PulseMinAlpha, PulseMaxAlpha,
                    Mathf.PingPong(Time.unscaledTime * (2f / PulseSeconds), 1f))
                : 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, pulse, delta / Mathf.Max(FadeSeconds, 0.01f));
            if (!_visible && _group.alpha <= 0f)
            {
                _root.SetActive(false);
                _placed = false;
                return;
            }

            float follow = 1f - Mathf.Exp(-MoveLerpPerSecond * delta);
            _shownCenter = Vector2.Lerp(_shownCenter, _targetCenter, follow);
            _shownSize = Vector2.Lerp(_shownSize, _targetSize, follow);
            Apply();
        }

        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
            _rootRect = null;
            _group = null;
            _frameRect = null;
            for (int i = 0; i < _edges.Length; i++) _edges[i] = null;
            _placed = false;
            _visible = false;
        }

        private void SetTarget(Vector2 center, Vector2 size)
        {
            if (_root == null) return;

            _visible = true;
            _root.SetActive(true);
            _targetCenter = center;
            _targetSize = size;

            if (_placed) return;
            _placed = true;
            _shownCenter = center;
            _shownSize = size;
            Apply();
        }

        private void Apply()
        {
            if (_frameRect == null) return;

            _frameRect.anchoredPosition = _shownCenter;
            _frameRect.sizeDelta = _shownSize;

            float w = _shownSize.x;
            float h = _shownSize.y;
            float t = EdgeThickness;
            float sideH = Mathf.Max(0f, h - t * 2f);

            SetEdge(0, new Vector2(0f, (h - t) * 0.5f), new Vector2(w, t));
            SetEdge(1, new Vector2(0f, -(h - t) * 0.5f), new Vector2(w, t));
            SetEdge(2, new Vector2(-(w - t) * 0.5f, 0f), new Vector2(t, sideH));
            SetEdge(3, new Vector2((w - t) * 0.5f, 0f), new Vector2(t, sideH));
        }

        private void SetEdge(int index, Vector2 pos, Vector2 size)
        {
            RectTransform rect = _edges[index] != null ? _edges[index].rectTransform : null;
            if (rect == null) return;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }
    }
}
