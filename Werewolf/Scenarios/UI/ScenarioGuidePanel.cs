using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.UI;
using Werewolf.Scenarios.Vanilla;

namespace Werewolf.Scenarios.UI
{
    public sealed class ScenarioGuidePanel : IClientPanel
    {
        public string LayerName => "ScenarioGuide";

        private const float TailFraction = 48f / 240f;

        private const float IconSize = 132f;
        private const float BubbleLeft = 24f;
        private const float BubbleBottom = IconSize - 12f;

        private const float TextMaxWidth = 560f;
        private const float MinTextWidth = 240f;
        private const float PadX = 32f;
        private const float PadTop = 26f;
        private const float HintHeight = 24f;
        private const float BaseFontSize = 30f;
        private const float HintFontSize = 18f;

        private const float MoveLerpPerSecond = 9f;
        private const float FadeSeconds = 0.18f;

        private const float BackgroundAlpha = 0.9f;

        internal const string KeyPrefix = "<color=#FF8500><b>";
        internal const string KeySuffix = "</b></color>";

        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.97f);
        private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.62f);
        private static readonly Color FallbackBubbleColor = new Color(0.08f, 0.08f, 0.1f, BackgroundAlpha);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, RectTransform rootRect, CanvasGroup group,
                RectTransform groupRect, RectTransform bubbleRect,
                TextMeshProUGUI text, TextMeshProUGUI hint)
            {
                Root = root;
                RootRect = rootRect;
                Group = group;
                GroupRect = groupRect;
                BubbleRect = bubbleRect;
                Text = text;
                Hint = hint;
            }

            public GameObject Root { get; }
            public RectTransform RootRect { get; }
            public CanvasGroup Group { get; }
            public RectTransform GroupRect { get; }
            public RectTransform BubbleRect { get; }
            public TextMeshProUGUI Text { get; }
            public TextMeshProUGUI Hint { get; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(ScenarioGuidePanel));

        private string? _shownText;
        private ScenarioAnchor _shownAnchor;
        private ScenarioGuideHint _shownHint;
        private float _shownVisualScale = 1f;
        private ScenarioPlayHud _shownPlayHud;
        private CanvasBounds _shownCanvas;
        private Vector2 _target;
        private float _targetVisualScale = 1f;
        private bool _placed;
        private bool _visible;

        public bool Exists => GetLiveViewOrNull() != null;

        public ScenarioPlayHud PlayHud { get; set; }

        public bool Visible => GetLiveViewOrNull() != null && _visible;

        public string? ShownLine => GetLiveViewOrNull() != null && _visible ? _shownText : null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            GameObject? partialRoot = null;
            try
            {
                RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_ScenarioGuide");
                GameObject go = rect.gameObject;
                partialRoot = go;

                var canvas = go.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = WerewolfUIManager.ScenarioGuideSortingOrder;
                CanvasGroup group = go.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                var groupGo = new GameObject("Group", typeof(RectTransform));
                var groupRect = (RectTransform)groupGo.transform;
                groupRect.SetParent(rect, false);
                groupRect.anchorMin = groupRect.anchorMax = new Vector2(0.5f, 0.5f);
                groupRect.pivot = Vector2.zero;
                groupRect.sizeDelta = new Vector2(IconSize, IconSize);

                Sprite? taxman = AssetCatalog.GetSprite("img_taxman_death");
                if (taxman != null)
                {
                    RectTransform iconRect = UiKit.CreateRect(
                        groupRect, "Taxman", Vector2.zero, new Vector2(IconSize, IconSize));
                    UiKit.SetAnchorsBottomLeft(iconRect,
                        new Vector2(IconSize * 0.5f, IconSize * 0.5f), new Vector2(IconSize, IconSize));
                    Image icon = iconRect.gameObject.AddComponent<Image>();
                    icon.sprite = taxman;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    icon.color = new Color(1f, 1f, 1f, BackgroundAlpha);
                }

                RectTransform bubbleRect;
                Sprite? bubbleSprite = AssetCatalog.GetSprite("img_tutorial_bubble");
                if (bubbleSprite != null)
                {
                    bubbleRect = UiKit.CreateRect(groupRect, "Bubble", Vector2.zero, Vector2.one);
                    Image bubble = bubbleRect.gameObject.AddComponent<Image>();
                    bubble.sprite = bubbleSprite;
                    bubble.raycastTarget = false;
                    bubble.color = new Color(1f, 1f, 1f, BackgroundAlpha);
                }
                else
                {
                    Image bubble = UiKit.CreateRoundedImage(
                        groupRect, "Bubble", Vector2.zero, Vector2.one, FallbackBubbleColor);
                    bubbleRect = bubble.rectTransform;
                    WLog.Line("scenario_guide_sprite_missing", secret: false, ("key", "img_tutorial_bubble"));
                }

                TextMeshProUGUI text = UiKit.CreateText(bubbleRect, "Text", Vector2.zero, Vector2.one,
                    string.Empty, BaseFontSize, TextColor, TextAlignmentOptions.TopLeft);
                text.textWrappingMode = TextWrappingModes.Normal;

                TextMeshProUGUI hint = UiKit.CreateText(bubbleRect, "Hint", Vector2.zero, Vector2.one,
                    string.Empty, HintFontSize, HintColor, TextAlignmentOptions.BottomRight);

                go.SetActive(false);
                _view = new ViewRefs(go, rect, group, groupRect, bubbleRect, text, hint);
                _textRefresh.Subscribe(() => Exists, ApplyTexts);
                partialRoot = null;
            }
            catch
            {
                if (partialRoot != null) Object.Destroy(partialRoot);
                throw;
            }
        }

        public void ShowPage(string message, ScenarioAnchor anchor, float fontScale,
                             ScenarioGuideHint hint, float visualScale = 1f)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || string.IsNullOrEmpty(message)) return;

            if (!UiKit.TryMeasureCanvas(view.RootRect, out CanvasBounds canvas)) return;

            float clampedVisualScale = Mathf.Clamp(visualScale, 0.5f, 2f);
            bool sameContent = _visible && _shownText == message && _shownAnchor == anchor
                               && _shownHint == hint && _shownPlayHud == PlayHud
                               && Mathf.Approximately(_shownVisualScale, clampedVisualScale)
                               && _shownCanvas == canvas;
            _visible = true;
            view.Root.SetActive(true);
            if (sameContent) return;

            _shownText = message;
            _shownAnchor = anchor;
            _shownHint = hint;
            _shownVisualScale = clampedVisualScale;
            _shownPlayHud = PlayHud;
            _shownCanvas = canvas;

            view.Text.fontSize = BaseFontSize * Mathf.Clamp(fontScale, 0.1f, 2f);
            view.Text.text = ReplaceKeyTags(message, KeyPrefix, KeySuffix);
            view.Hint.text = BuildHintText(hint);

            Vector2 size = UiText.PreferredAutoscaledSize(
                view.Text, TextMaxWidth, float.PositiveInfinity, 5);
            float textW = Mathf.Clamp(size.x, MinTextWidth, TextMaxWidth);
            float bodyH = PadTop + size.y + HintHeight;
            float bubbleW = textW + PadX * 2f;
            float bubbleH = bodyH / (1f - TailFraction);

            UiKit.SetAnchorsBottomLeft(view.BubbleRect,
                new Vector2(BubbleLeft + bubbleW * 0.5f, BubbleBottom + bubbleH * 0.5f),
                new Vector2(bubbleW, bubbleH));

            RectTransform textRect = view.Text.rectTransform;
            textRect.sizeDelta = new Vector2(textW, size.y);
            textRect.anchoredPosition = new Vector2(0f, bubbleH * 0.5f - PadTop - size.y * 0.5f);

            RectTransform hintRect = view.Hint.rectTransform;
            hintRect.sizeDelta = new Vector2(textW, HintHeight);
            hintRect.anchoredPosition = new Vector2(
                0f, -bubbleH * 0.5f + bubbleH * TailFraction + HintHeight * 0.5f);

            float groupW = Mathf.Max(IconSize, BubbleLeft + bubbleW);
            float groupH = BubbleBottom + bubbleH;
            view.GroupRect.sizeDelta = new Vector2(groupW, groupH);

            GuidePlacement placement = ScenarioGuideLayout.Place(
                canvas, anchor, groupW * clampedVisualScale, groupH * clampedVisualScale, PlayHud);
            _target = new Vector2(placement.X, placement.Y);
            _targetVisualScale = clampedVisualScale;
            if (!_placed)
            {
                view.GroupRect.anchoredPosition = _target;
                view.GroupRect.localScale = new Vector3(
                    _targetVisualScale, _targetVisualScale, 1f);
                _placed = true;
            }
        }

        public void Hide()
        {
            _visible = false;
        }

        public bool Tick()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.Root.activeSelf) return false;

            float step = Time.deltaTime / Mathf.Max(FadeSeconds, 0.01f);
            view.Group.alpha = Mathf.MoveTowards(view.Group.alpha, _visible ? 1f : 0f, step);
            if (!_visible && view.Group.alpha <= 0f)
            {
                view.Root.SetActive(false);
                return false;
            }

            float moveT = 1f - Mathf.Exp(-MoveLerpPerSecond * Time.deltaTime);
            view.GroupRect.anchoredPosition = Vector2.Lerp(
                view.GroupRect.anchoredPosition, _target,
                moveT);
            float scale = Mathf.Lerp(view.GroupRect.localScale.x, _targetVisualScale, moveT);
            view.GroupRect.localScale = new Vector3(scale, scale, 1f);

            if (!_visible || view.Group.alpha < 1f) return false;
            return Input.GetMouseButtonDown(0) && ContainsPointer(Input.mousePosition);
        }

        public bool ContainsPointer(Vector2 screenPoint)
        {
            ViewRefs? view = GetLiveViewOrNull();
            return view != null && _visible
                && RectTransformUtility.RectangleContainsScreenPoint(view.BubbleRect, screenPoint, null);
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null || !_visible) return;
            view.Hint.text = BuildHintText(_shownHint);
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) Object.Destroy(view.Root);
            ResetViewOwnedState();
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
            _shownText = null;
            _shownAnchor = default;
            _shownHint = ScenarioGuideHint.Advance;
            _shownVisualScale = 1f;
            _shownPlayHud = ScenarioPlayHud.None;
            _shownCanvas = default;
            _target = default;
            _targetVisualScale = 1f;
            _placed = false;
            _visible = false;
        }

        private static string BuildHintText(ScenarioGuideHint hint)
        {
            string raw;
            switch (hint)
            {
                case ScenarioGuideHint.ActionOnly:
                    raw = Texts.Get(TextId.ScenarioAbortHint);
                    break;
                case ScenarioGuideHint.AbortConfirm:
                    raw = Texts.Get(TextId.ScenarioAbortCancelHint) + "　" + Texts.Get(TextId.ScenarioAbortHint);
                    break;
                case ScenarioGuideHint.None:
                    return string.Empty;
                default:
                    raw = Texts.Get(TextId.ScenarioAdvanceHint) + "　" + Texts.Get(TextId.ScenarioAbortHint);
                    break;
            }
            return ReplaceKeyTags(raw, "<color=#FF8500><u><b>", "</b></u></color>");
        }

        private static string ReplaceKeyTags(string text, string prefix, string suffix)
        {
            try
            {
                if (Inputs.TryReplaceDisplayTags(text, prefix, suffix, out string replaced)) return replaced;
                ScenarioVanillaProbe.Failed("guide_key_tags", "no_input_manager");
            }
            catch (System.Exception e)
            {
                ScenarioVanillaProbe.Failed("guide_key_tags", e);
            }
            return text;
        }
    }
}
