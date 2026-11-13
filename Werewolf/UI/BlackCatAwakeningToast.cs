#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

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

        private ViewRefs? _view;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, CanvasGroup group, RectTransform cardRect,
                Image icon, TextMeshProUGUI title, TextMeshProUGUI body,
                float shownY, float hiddenY)
            {
                Root = root;
                Group = group;
                CardRect = cardRect;
                Icon = icon;
                Title = title;
                Body = body;
                ShownY = shownY;
                HiddenY = hiddenY;
            }

            public GameObject Root { get; }
            public CanvasGroup Group { get; }
            public RectTransform CardRect { get; }
            public Image Icon { get; }
            public TextMeshProUGUI Title { get; }
            public TextMeshProUGUI Body { get; }
            public float ShownY { get; }
            public float HiddenY { get; }
            public int RunGeneration { get; set; }
        }

        public bool Exists => GetLiveViewOrNull() != null;
        public bool Visible
        {
            get
            {
                ViewRefs? view = GetLiveViewOrNull();
                return view != null && view.Root.activeSelf;
            }
        }

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                root = new GameObject("WW_BlackCatAwakeningToast", typeof(RectTransform));
                var rect = (RectTransform)root.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);

                CanvasGroup group = root.AddComponent<CanvasGroup>();
                group.alpha = 1f;
                group.blocksRaycasts = false;
                group.interactable = false;

                var cardGo = new GameObject("Card", typeof(RectTransform));
                var cardRect = (RectTransform)cardGo.transform;
                cardRect.SetParent(rect, false);
                cardRect.anchorMin = new Vector2(0.5f, 1f);
                cardRect.anchorMax = new Vector2(0.5f, 1f);
                cardRect.pivot = new Vector2(0.5f, 1f);
                cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

                float shownY = -TopMargin;
                float hiddenY = CardHeight + 40f;
                cardRect.anchoredPosition = new Vector2(0f, hiddenY);

                float iconCenterX = -CardWidth / 2f + IconLeftMargin + IconSize.x / 2f;
                float textWidth = CardWidth - IconLeftMargin - IconSize.x - TextColumnGap - TextRightMargin;
                float textCenterX = CardWidth / 2f - TextRightMargin - textWidth / 2f;

                Image icon = UiKit.CreateImage(cardRect,
                    "Icon", new Vector2(iconCenterX, -(CardHeight - IconSize.y) / 2f), IconSize, Color.white);
                icon.raycastTarget = false;
                icon.enabled = false;
                AnchorToCardTop((RectTransform)icon.transform);

                TextMeshProUGUI title = UiKit.CreateText(cardRect, "Title", new Vector2(textCenterX, -16f),
                    new Vector2(textWidth, 44f), "", TitleFontSize,
                    new Color(1f, 0.95f, 0.7f), TextAlignmentOptions.MidlineLeft);
                title.textWrappingMode = TextWrappingModes.NoWrap;
                AnchorToCardTop((RectTransform)title.transform);

                TextMeshProUGUI body = UiKit.CreateText(cardRect, "Body", new Vector2(textCenterX, -72f),
                    new Vector2(textWidth, 90f), "", BodyFontSize,
                    new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.TopLeft);
                body.textWrappingMode = TextWrappingModes.Normal;
                AnchorToCardTop((RectTransform)body.transform);

                root.SetActive(false);
                _view = new ViewRefs(root, group, cardRect, icon, title, body, shownY, hiddenY);
                WLog.Line("cat_awaken_toast_built", secret: false);
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

        public IEnumerator Play(RevealContent content)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || content == null)
            {
                WLog.Line("cat_awaken_toast_skip", secret: false,
                    ("reason", view == null ? "not_built" : "no_content"));
                yield break;
            }

            int generation = NextRun(view);
            view.Title.text = content.Title ?? string.Empty;
            view.Body.text = content.Pages != null && content.Pages.Length > 0
                             && content.Pages[0].BodyLines != null
                ? string.Join("\n", content.Pages[0].BodyLines)
                : string.Empty;

            Sprite? icon = AssetCatalog.GetSprite("role_blackcat");
            if (icon != null)
            {
                view.Icon.sprite = icon;
                view.Icon.preserveAspect = true;
                view.Icon.color = Color.white;
                view.Icon.enabled = true;
            }
            else
            {
                view.Icon.sprite = null;
                view.Icon.enabled = false;
            }

            view.Group.alpha = 1f;
            view.CardRect.anchoredPosition = new Vector2(0f, view.HiddenY);
            view.Root.SetActive(true);
            WLog.Line("cat_awaken_toast_show", secret: true);

            yield return RunGuarded(view, generation, UiTween.Move(view.CardRect,
                new Vector2(0f, view.HiddenY), new Vector2(0f, view.ShownY),
                SlideInSec, UiTween.EaseOut()));
            if (!IsCurrentRun(view, generation)) yield break;

            yield return RunGuarded(view, generation, UiTween.Hold(HoldSec));
            if (!IsCurrentRun(view, generation)) yield break;

            yield return RunGuarded(view, generation, UiTween.Move(view.CardRect,
                new Vector2(0f, view.ShownY), new Vector2(0f, view.HiddenY),
                SlideOutSec, UiTween.EaseIn()));
            if (!IsCurrentRun(view, generation)) yield break;

            HideView(view);
            WLog.Line("cat_awaken_toast_end", secret: false);
        }

        private IEnumerator RunGuarded(ViewRefs view, int generation, IEnumerator routine)
        {
            while (IsCurrentRun(view, generation))
            {
                bool moved;
                bool failed = false;
                try
                {
                    moved = routine.MoveNext();
                }
                catch (Exception e)
                {
                    WLog.Line("cat_awaken_toast_tween_error", secret: false, ("err", e.Message));
                    if (IsCurrentRun(view, generation)) HideView(view);
                    moved = false;
                    failed = true;
                }

                if (failed || !moved || !IsCurrentRun(view, generation)) yield break;
                yield return routine.Current;
            }
        }

        private static void AnchorToCardTop(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }

        public void HideNow()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) HideView(view);
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view == null) return;

            InvalidateRun(view);
            if (view.Root != null)
            {
                view.Root.SetActive(false);
                UnityEngine.Object.Destroy(view.Root);
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null || view.Root != null) return view;

            _view = null;
            InvalidateRun(view);
            return null;
        }

        private int NextRun(ViewRefs view)
        {
            unchecked { view.RunGeneration++; }
            return view.RunGeneration;
        }

        private static void InvalidateRun(ViewRefs view)
        {
            unchecked { view.RunGeneration++; }
        }

        private bool IsCurrentRun(ViewRefs view, int generation)
        {
            return ReferenceEquals(_view, view)
                && view.Root != null
                && view.RunGeneration == generation;
        }

        private static void HideView(ViewRefs view)
        {
            InvalidateRun(view);
            if (view.CardRect != null)
                view.CardRect.anchoredPosition = new Vector2(0f, view.HiddenY);
            if (view.Root != null) view.Root.SetActive(false);
        }
    }
}
