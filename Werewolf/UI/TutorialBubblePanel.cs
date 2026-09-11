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

        private GameObject _root;
        private RectTransform _slider;
        private RectTransform _bubbleRect;
        private TextMeshProUGUI _text;
        private TextMeshProUGUI _hint;
        private SlideState _state = SlideState.Hidden;
        private float _animFrom;
        private float _animT;
        private float _slideDistance;

        private static bool _escSuppressErrorLogged;
        private static bool _skipInputErrorLogged;

        public bool Exists => _root != null;

        public bool Visible =>
            _root != null && _root.activeSelf
            && (_state == SlideState.In || _state == SlideState.Shown);

        public bool Idle => _state == SlideState.Hidden;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_TutorialBubble");
            var go = rect.gameObject;
            _root = go;

            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.TutorialBubbleSortingOrder;

            _slider = UiKit.CreateStretched(rect, "Slider");

            Sprite taxman = AssetCatalog.GetSprite("img_taxman_death");
            if (taxman != null)
            {
                RectTransform iconRect = UiKit.CreateRect(
                    _slider, "Taxman", Vector2.zero, new Vector2(IconSize, IconSize));
                UiKit.SetAnchorsBottomLeft(iconRect,
                    new Vector2(IconMargin + IconSize * 0.5f, IconMargin + IconSize * 0.5f),
                    new Vector2(IconSize, IconSize));
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = taxman;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = new Color(1f, 1f, 1f, BackgroundAlpha);
            }

            Sprite bubbleSprite = AssetCatalog.GetSprite("img_tutorial_bubble");
            if (bubbleSprite != null)
            {
                RectTransform bubbleRect = UiKit.CreateRect(_slider, "Bubble", Vector2.zero, Vector2.one);
                Image bubble = bubbleRect.gameObject.AddComponent<Image>();
                bubble.sprite = bubbleSprite;
                bubble.raycastTarget = false;
                bubble.color = new Color(1f, 1f, 1f, BackgroundAlpha);
                _bubbleRect = bubbleRect;
            }
            else
            {
                Image bubble = UiKit.CreateRoundedImage(
                    _slider, "Bubble", Vector2.zero, Vector2.one, FallbackBubbleColor);
                _bubbleRect = bubble.rectTransform;
                WLog.Line("tutorial_bubble_sprite_missing", secret: false, ("key", "img_tutorial_bubble"));
            }

            _text = UiKit.CreateText(_bubbleRect, "Text", Vector2.zero, Vector2.one,
                string.Empty, BaseFontSize, TextColor, TextAlignmentOptions.TopLeft);
            _text.enableWordWrapping = true;

            _hint = UiKit.CreateText(_bubbleRect, "SkipHint", Vector2.zero, Vector2.one,
                string.Empty, HintFontSize, HintColor, TextAlignmentOptions.BottomRight);

            _root.SetActive(false);
        }

        public void ShowMessage(string message, float fontScale)
        {
            if (_root == null || string.IsNullOrEmpty(message)) return;
            _root.SetActive(true);

            _text.fontSize = BaseFontSize * Mathf.Clamp(fontScale, 0.1f, 2f);
            _text.text = message;
            _hint.text = BuildSkipHintText();

            Vector2 size = UiText.PreferredAutoscaledSize(
                _text, TextMaxWidth, float.PositiveInfinity, 5);
            float textW = Mathf.Clamp(size.x, MinTextWidth, TextMaxWidth);
            float bodyH = PadTop + size.y + HintHeight;
            float bubbleW = textW + PadX * 2f;
            float bubbleH = bodyH / (1f - TailFraction);

            UiKit.SetAnchorsBottomLeft(_bubbleRect,
                new Vector2(BubbleLeft + bubbleW * 0.5f, BubbleBottom + bubbleH * 0.5f),
                new Vector2(bubbleW, bubbleH));

            RectTransform textRect = _text.rectTransform;
            textRect.sizeDelta = new Vector2(textW, size.y);
            textRect.anchoredPosition = new Vector2(0f, bubbleH * 0.5f - PadTop - size.y * 0.5f);

            RectTransform hintRect = _hint.rectTransform;
            hintRect.sizeDelta = new Vector2(textW, HintHeight);
            hintRect.anchoredPosition = new Vector2(
                0f, -bubbleH * 0.5f + bubbleH * TailFraction + HintHeight * 0.5f);

            _slideDistance = BubbleLeft + bubbleW + SlideMargin;
            _animFrom = _state == SlideState.Hidden ? -_slideDistance : _slider.anchoredPosition.x;
            _animT = 0f;
            _state = SlideState.In;
            SetSlideX(_animFrom);
        }

        public void Hide()
        {
            if (_root == null || !_root.activeSelf || _state == SlideState.Out) return;
            _animFrom = _slider.anchoredPosition.x;
            _animT = 0f;
            _state = SlideState.Out;
        }

        public bool Tick()
        {
            if (_root == null || !_root.activeSelf) return false;

            if (_state == SlideState.In)
            {
                _animT += Time.deltaTime / SlideInSeconds;
                float t = Mathf.Clamp01(_animT);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                SetSlideX(Mathf.Lerp(_animFrom, 0f, eased));
                if (t >= 1f) _state = SlideState.Shown;
            }
            else if (_state == SlideState.Out)
            {
                _animT += Time.deltaTime / SlideOutSeconds;
                float t = Mathf.Clamp01(_animT);
                float eased = t * t * t;
                SetSlideX(Mathf.Lerp(_animFrom, -_slideDistance, eased));
                if (t >= 1f)
                {
                    _root.SetActive(false);
                    _state = SlideState.Hidden;
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
                return Inputs.NoTextInputsActive && Inputs.Down(GameKey.Menu);
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

        private void SetSlideX(float x)
        {
            Vector2 pos = _slider.anchoredPosition;
            pos.x = x;
            _slider.anchoredPosition = pos;
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _slider = null;
            _bubbleRect = null;
            _text = null;
            _hint = null;
            _state = SlideState.Hidden;
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
