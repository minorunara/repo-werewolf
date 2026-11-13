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

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, RectTransform rootRect, CanvasGroup group,
                RectTransform frameRect, Image[] edges)
            {
                Root = root;
                RootRect = rootRect;
                Group = group;
                FrameRect = frameRect;
                Edges = edges;
            }

            public GameObject Root { get; }
            public RectTransform RootRect { get; }
            public CanvasGroup Group { get; }
            public RectTransform FrameRect { get; }
            public Image[] Edges { get; }
        }

        private ViewRefs? _view;

        private Vector2 _targetCenter;
        private Vector2 _targetSize;
        private Vector2 _shownCenter;
        private Vector2 _shownSize;
        private bool _placed;
        private bool _visible;

        public bool Exists => GetLiveViewOrNull() != null;

        public bool Visible => GetLiveViewOrNull() != null && _visible;

        public bool TryGetCanvas(out CanvasBounds canvas)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null)
            {
                canvas = default;
                return false;
            }
            return UiKit.TryMeasureCanvas(view.RootRect, out canvas);
        }

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            GameObject? partialRoot = null;
            try
            {
                RectTransform rootRect = UiKit.CreateStretched(layerRoot, "WW_ScenarioHighlight");
                GameObject go = rootRect.gameObject;
                partialRoot = go;

                var canvas = go.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = WerewolfUIManager.ScenarioHighlightSortingOrder;
                CanvasGroup group = go.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                RectTransform frameRect = UiKit.CreateRect(rootRect, "Frame", Vector2.zero, Vector2.one);
                var edges = new Image[4];
                for (int i = 0; i < edges.Length; i++)
                {
                    Image edge = UiKit.CreateImage(frameRect, "Edge" + i, Vector2.zero, Vector2.one, FrameColor);
                    edge.raycastTarget = false;
                    edges[i] = edge;
                }

                go.SetActive(false);
                _view = new ViewRefs(go, rootRect, group, frameRect, edges);
                partialRoot = null;
            }
            catch
            {
                if (partialRoot != null) Object.Destroy(partialRoot);
                throw;
            }
        }

        public void ShowBox(GuideBox box)
        {
            SetTarget(new Vector2(box.X, box.Y),
                new Vector2(box.Width + ScenarioHighlightLayout.Padding * 2f,
                            box.Height + ScenarioHighlightLayout.Padding * 2f));
        }

        public void ShowScreenRect(Rect screenRect)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || screenRect.width <= 1f || screenRect.height <= 1f)
            {
                Hide();
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    view.RootRect, screenRect.min, null, out Vector2 min)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    view.RootRect, screenRect.max, null, out Vector2 max))
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
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.Root.activeSelf) return;

            float delta = Time.deltaTime;
            float pulse = _visible
                ? Mathf.Lerp(PulseMinAlpha, PulseMaxAlpha,
                    Mathf.PingPong(Time.unscaledTime * (2f / PulseSeconds), 1f))
                : 0f;
            CanvasGroup group = view.Group;
            group.alpha = Mathf.MoveTowards(group.alpha, pulse, delta / Mathf.Max(FadeSeconds, 0.01f));
            if (!_visible && group.alpha <= 0f)
            {
                view.Root.SetActive(false);
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
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) Object.Destroy(view.Root);
            ResetViewOwnedState();
        }

        private void SetTarget(Vector2 center, Vector2 size)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            _visible = true;
            view.Root.SetActive(true);
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
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            view.FrameRect.anchoredPosition = _shownCenter;
            view.FrameRect.sizeDelta = _shownSize;

            float w = _shownSize.x;
            float h = _shownSize.y;
            float t = EdgeThickness;
            float sideH = Mathf.Max(0f, h - t * 2f);

            SetEdge(view, 0, new Vector2(0f, (h - t) * 0.5f), new Vector2(w, t));
            SetEdge(view, 1, new Vector2(0f, -(h - t) * 0.5f), new Vector2(w, t));
            SetEdge(view, 2, new Vector2(-(w - t) * 0.5f, 0f), new Vector2(t, sideH));
            SetEdge(view, 3, new Vector2((w - t) * 0.5f, 0f), new Vector2(t, sideH));
        }

        private static void SetEdge(ViewRefs view, int index, Vector2 pos, Vector2 size)
        {
            RectTransform rect = view.Edges[index].rectTransform;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;
            _view = null;
            ResetViewOwnedState();
            return null;
        }

        private void ResetViewOwnedState()
        {
            _targetCenter = default;
            _targetSize = default;
            _shownCenter = default;
            _shownSize = default;
            _placed = false;
            _visible = false;
        }
    }
}
