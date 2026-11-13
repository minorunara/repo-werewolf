#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class TutorialBubblePanel : IClientPanel, ITutorialBubble
    {
        public string LayerName => "TutorialBubble";

        private const float TailFraction = 48f / 240f;

        private const float IconSize = 132f;
        private const float IconMargin = 20f;
        private const float BubbleLeft = 44f;
        private const float BubbleBottom = 140f;
        private const float TextMaxWidth = 900f;
        private const float MinTextWidth = 260f;
        private const float PadX = 36f;
        private const float PadTop = 30f;
        private const float HintHeight = 26f;
        private const float BaseFontSize = 32f;
        private const float HintFontSize = 19f;

        private const float SlideInSeconds = 0.3f;
        private const float SlideOutSeconds = 0.22f;
        private const float SlideMargin = 24f;

        private const float BackgroundAlpha = 0.88f;

        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.65f);
        private static readonly Color FallbackBubbleColor = new Color(0.08f, 0.08f, 0.1f, BackgroundAlpha);

        private enum SlideState { Hidden, In, Shown, Out }

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, RectTransform slider, RectTransform bubbleRect,
                TextMeshProUGUI text, TextMeshProUGUI hint)
            {
                Root = root;
                Slider = slider;
                BubbleRect = bubbleRect;
                Text = text;
                Hint = hint;
            }

            public GameObject Root { get; }
            public RectTransform Slider { get; }
            public RectTransform BubbleRect { get; }
            public TextMeshProUGUI Text { get; }
            public TextMeshProUGUI Hint { get; }
            public SlideState State { get; set; } = SlideState.Hidden;
            public float AnimFrom { get; set; }
            public float AnimT { get; set; }
            public float SlideDistance { get; set; }
        }

        private ViewRefs? _view;

        private static bool _escSuppressErrorLogged;
        private static bool _skipInputErrorLogged;

        public bool Exists => GetLiveViewOrNull() != null;

        public bool Visible
        {
            get
            {
                ViewRefs? view = GetLiveViewOrNull();
                return view != null && view.Root.activeSelf
                    && (view.State == SlideState.In || view.State == SlideState.Shown);
            }
        }

        public bool Idle
        {
            get
            {
                ViewRefs? view = GetLiveViewOrNull();
                return view == null || view.State == SlideState.Hidden;
            }
        }

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_TutorialBubble");
                root = rect.gameObject;

                var canvas = root.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = WerewolfUIManager.TutorialBubbleSortingOrder;

                RectTransform slider = UiKit.CreateStretched(rect, "Slider");

                Sprite? taxman = AssetCatalog.GetSprite("img_taxman_death");
                if (taxman != null)
                {
                    RectTransform iconRect = UiKit.CreateRect(
                        slider, "Taxman", Vector2.zero, new Vector2(IconSize, IconSize));
                    UiKit.SetAnchorsBottomLeft(iconRect,
                        new Vector2(IconMargin + IconSize * 0.5f, IconMargin + IconSize * 0.5f),
                        new Vector2(IconSize, IconSize));
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
                    bubbleRect = UiKit.CreateRect(slider, "Bubble", Vector2.zero, Vector2.one);
                    Image bubble = bubbleRect.gameObject.AddComponent<Image>();
                    bubble.sprite = bubbleSprite;
                    bubble.raycastTarget = false;
                    bubble.color = new Color(1f, 1f, 1f, BackgroundAlpha);
                }
                else
                {
                    Image bubble = UiKit.CreateRoundedImage(
                        slider, "Bubble", Vector2.zero, Vector2.one, FallbackBubbleColor);
                    bubbleRect = bubble.rectTransform;
                    WLog.Line("tutorial_bubble_sprite_missing", secret: false, ("key", "img_tutorial_bubble"));
                }

                TextMeshProUGUI text = UiKit.CreateText(bubbleRect, "Text", Vector2.zero, Vector2.one,
                    string.Empty, BaseFontSize, TextColor, TextAlignmentOptions.TopLeft);
                text.textWrappingMode = TextWrappingModes.Normal;

                TextMeshProUGUI hint = UiKit.CreateText(bubbleRect, "SkipHint", Vector2.zero, Vector2.one,
                    string.Empty, HintFontSize, HintColor, TextAlignmentOptions.BottomRight);

                root.SetActive(false);
                _view = new ViewRefs(root, slider, bubbleRect, text, hint);
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

        public void ShowMessage(string message, float fontScale)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || string.IsNullOrEmpty(message)) return;
            view.Root.SetActive(true);

            view.Text.fontSize = BaseFontSize * Mathf.Clamp(fontScale, 0.1f, 2f);
            view.Text.text = message;
            view.Hint.text = BuildSkipHintText();

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

            view.SlideDistance = BubbleLeft + bubbleW + SlideMargin;
            view.AnimFrom = view.State == SlideState.Hidden
                ? -view.SlideDistance
                : view.Slider.anchoredPosition.x;
            view.AnimT = 0f;
            view.State = SlideState.In;
            SetSlideX(view, view.AnimFrom);
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.Root.activeSelf || view.State == SlideState.Out) return;
            view.AnimFrom = view.Slider.anchoredPosition.x;
            view.AnimT = 0f;
            view.State = SlideState.Out;
        }

        public bool Tick()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.Root.activeSelf) return false;

            if (view.State == SlideState.In)
            {
                view.AnimT += Time.deltaTime / SlideInSeconds;
                float t = Mathf.Clamp01(view.AnimT);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                SetSlideX(view, Mathf.Lerp(view.AnimFrom, 0f, eased));
                if (t >= 1f) view.State = SlideState.Shown;
            }
            else if (view.State == SlideState.Out)
            {
                view.AnimT += Time.deltaTime / SlideOutSeconds;
                float t = Mathf.Clamp01(view.AnimT);
                float eased = t * t * t;
                SetSlideX(view, Mathf.Lerp(view.AnimFrom, -view.SlideDistance, eased));
                if (t >= 1f)
                {
                    view.Root.SetActive(false);
                    view.State = SlideState.Hidden;
                }
                return false;
            }

            try
            {
                Session.DisableEscMenu(1f);
            }
            catch (Exception e)
            {
                if (!_escSuppressErrorLogged)
                {
                    _escSuppressErrorLogged = true;
                    WLog.Line("bubble_esc_suppress_error", secret: false, ("err", e.Message));
                }
            }
            try
            {
                return Inputs.NoTextInputsActive && Inputs.Down(GameKey.Back);
            }
            catch (Exception e)
            {
                if (!_skipInputErrorLogged)
                {
                    _skipInputErrorLogged = true;
                    WLog.Line("bubble_skip_input_error", secret: false, ("err", e.Message));
                }
                return false;
            }
        }

        private static void SetSlideX(ViewRefs view, float x)
        {
            Vector2 pos = view.Slider.anchoredPosition;
            pos.x = x;
            view.Slider.anchoredPosition = pos;
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            return null;
        }

        private static string BuildSkipHintText()
        {
            string hint = Texts.Get(TextId.TutorialSkipHint);
            try
            {
                hint = Inputs.ReplaceDisplayTags(hint, "<color=#FF8500><u><b>", "</b></u></color>");
            }
            catch (Exception e)
            {
                WLog.Line("bubble_hint_tags_error", secret: false, ("err", e.Message));
            }
            return hint;
        }
    }
}
