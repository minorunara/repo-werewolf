using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class BlackCatAwakeningToast : IClientPanel
    {
        public string LayerName => "BlackCatAwakening";

        private const float CardWidth = 760f;
        private const float CardHeight = 170f;

        private const float TopMargin = 150f;
        private const float TitleFontSize = 40f;
        private const float BodyFontSize = 22f;
        private static readonly Vector2 IconSize = new Vector2(120f, 120f);

        private const float IconLeftMargin = 24f;
        private const float TextColumnGap = 24f;
        private const float TextRightMargin = 16f;

        private const float SlideInSec = 0.4f;
        private const float HoldSec = 5.0f;
        private const float SlideOutSec = 0.4f;

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _cardRect;
        private Image _iconImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _bodyText;

        private float _shownY;
        private float _hiddenY;

        public bool Exists => _root != null;
        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_BlackCatAwakeningToast", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _root = go;

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var cardGo = new GameObject("Card", typeof(RectTransform));
            _cardRect = (RectTransform)cardGo.transform;
            _cardRect.SetParent(rect, false);
            _cardRect.anchorMin = new Vector2(0.5f, 1f);
            _cardRect.anchorMax = new Vector2(0.5f, 1f);
            _cardRect.pivot = new Vector2(0.5f, 1f);
            _cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

            _shownY = -TopMargin;
            _hiddenY = CardHeight + 40f;
            _cardRect.anchoredPosition = new Vector2(0f, _hiddenY);

            float iconCenterX = -CardWidth / 2f + IconLeftMargin + IconSize.x / 2f;
            float textWidth = CardWidth - IconLeftMargin - IconSize.x - TextColumnGap - TextRightMargin;
            float textCenterX = CardWidth / 2f - TextRightMargin - textWidth / 2f;

            _iconImage = UiKit.CreateImage(_cardRect,
                "Icon", new Vector2(iconCenterX, -(CardHeight - IconSize.y) / 2f), IconSize, Color.white);
            _iconImage.raycastTarget = false;
            _iconImage.enabled = false;
            AnchorToCardTop((RectTransform)_iconImage.transform);

            _titleText = UiKit.CreateText(_cardRect, "Title", new Vector2(textCenterX, -16f),
                new Vector2(textWidth, 44f), "", TitleFontSize,
                new Color(1f, 0.95f, 0.7f), TextAlignmentOptions.MidlineLeft);
            _titleText.enableWordWrapping = false;
            AnchorToCardTop((RectTransform)_titleText.transform);

            _bodyText = UiKit.CreateText(_cardRect, "Body", new Vector2(textCenterX, -72f),
                new Vector2(textWidth, 90f), "", BodyFontSize,
                new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.TopLeft);
            _bodyText.enableWordWrapping = true;
            AnchorToCardTop((RectTransform)_bodyText.transform);

            _root.SetActive(false);
            WLog.Line("cat_awaken_toast_built", secret: false);
        }

        public IEnumerator Play(RevealContent content)
        {
            if (_root == null || content == null)
            {
                WLog.Line("cat_awaken_toast_skip", secret: false,
                    ("reason", _root == null ? "not_built" : "no_content"));
                yield break;
            }

            _titleText.text = content.Title ?? string.Empty;
            _bodyText.text = content.Pages != null && content.Pages.Length > 0
                             && content.Pages[0].BodyLines != null
                ? string.Join("\n", content.Pages[0].BodyLines)
                : string.Empty;

            Sprite icon = AssetCatalog.GetSprite("role_blackcat");
            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.preserveAspect = true;
                _iconImage.color = Color.white;
                _iconImage.enabled = true;
            }
            else
            {
                _iconImage.sprite = null;
                _iconImage.enabled = false;
            }

            _group.alpha = 1f;
            _cardRect.anchoredPosition = new Vector2(0f, _hiddenY);
            _root.SetActive(true);
            WLog.Line("cat_awaken_toast_show", secret: true);

            yield return UiTween.Move(_cardRect,
                new Vector2(0f, _hiddenY), new Vector2(0f, _shownY),
                SlideInSec, UiTween.EaseOut());

            yield return UiTween.Hold(HoldSec);

            yield return UiTween.Move(_cardRect,
                new Vector2(0f, _shownY), new Vector2(0f, _hiddenY),
                SlideOutSec, UiTween.EaseIn());

            HideNow();
            WLog.Line("cat_awaken_toast_end", secret: false);
        }

        private static void AnchorToCardTop(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }

        public void HideNow()
        {
            if (_root == null) return;
            if (_cardRect != null) _cardRect.anchoredPosition = new Vector2(0f, _hiddenY);
            _root.SetActive(false);
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _group = null;
            _cardRect = null;
            _iconImage = null;
            _titleText = null;
            _bodyText = null;
        }
    }
}
