#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class RoleRevealCinematic : IClientPanel
    {
        public string LayerName => "RoleReveal";

        private static readonly Vector2 CardSize = new Vector2(320f, 320f);
        private static readonly Vector2 CardRestPosition = new Vector2(0f, 180f);
        private static readonly Vector2 CardEntryPosition = new Vector2(0f, 780f);
        private const float HudCardScale = 128f / 320f;
        private const float CardLiftOffsetY = 16f;
        private const float TitleFontSize = 64f;
        private const float BodyFontSize = 34f;
        private const float SelfIdFontSize = 40f;
        private const float SelfIdPosY = 390f;
        private const string HeadingColorTag = "<color=#FFF2B3>";

        private sealed class ViewRefs
        {
            public ViewRefs(
                GameObject root,
                CanvasGroup group,
                Image background,
                RectTransform cardRect,
                Image cardBack,
                Image cardFront,
                CanvasGroup textGroup,
                TextMeshProUGUI selfIdText,
                TextMeshProUGUI titleText,
                TextMeshProUGUI bodyText,
                CanvasGroup bodyGroup)
            {
                Root = root;
                Group = group;
                Background = background;
                CardRect = cardRect;
                CardBack = cardBack;
                CardFront = cardFront;
                TextGroup = textGroup;
                SelfIdText = selfIdText;
                TitleText = titleText;
                BodyText = bodyText;
                BodyGroup = bodyGroup;
            }

            public GameObject Root { get; }
            public CanvasGroup Group { get; }
            public Image Background { get; }
            public RectTransform CardRect { get; }
            public Image CardBack { get; }
            public Image CardFront { get; }
            public CanvasGroup TextGroup { get; }
            public TextMeshProUGUI SelfIdText { get; }
            public TextMeshProUGUI TitleText { get; }
            public TextMeshProUGUI BodyText { get; }
            public CanvasGroup BodyGroup { get; }
        }

        private ViewRefs? _view;

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
                RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_RoleReveal");
                root = rect.gameObject;
                root.SetActive(false);

                CanvasGroup group = root.AddComponent<CanvasGroup>();
                group.alpha = 1f;
                group.blocksRaycasts = false;
                group.interactable = false;

                Image background = UiKit.CreateBackdrop(rect, "Bg", Color.clear);

                RectTransform cardRect = UiKit.CreateRect(rect, "Card", CardEntryPosition, CardSize);
                Image cardBack = UiKit.CreateImage(cardRect, "Back", Vector2.zero, CardSize,
                    new Color(0.06f, 0.07f, 0.09f, 1f));
                cardBack.raycastTarget = false;
                Image cardFront = UiKit.CreateImage(cardRect, "Front", Vector2.zero, CardSize, Color.white);
                cardFront.raycastTarget = false;
                cardFront.enabled = false;

                RectTransform textRoot = UiKit.CreateStretched(rect, "Text");
                CanvasGroup textGroup = textRoot.gameObject.AddComponent<CanvasGroup>();
                textGroup.alpha = 0f;
                textGroup.blocksRaycasts = false;
                textGroup.interactable = false;

                TextMeshProUGUI selfIdText = UiKit.CreateText(textRoot, "SelfId",
                    new Vector2(0f, SelfIdPosY), new Vector2(1200f, 56f), string.Empty,
                    SelfIdFontSize, new Color(0.92f, 0.94f, 1f, 0.9f),
                    TextAlignmentOptions.Center);
                TextMeshProUGUI titleText = UiKit.CreateText(textRoot, "Title", new Vector2(0f, -40f),
                    new Vector2(1600f, 100f), string.Empty, TitleFontSize,
                    new Color(1f, 0.95f, 0.7f), TextAlignmentOptions.Center);
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                TextMeshProUGUI bodyText = UiKit.CreateText(textRoot, "Body", new Vector2(0f, -220f),
                    new Vector2(1600f, 400f), string.Empty, BodyFontSize,
                    new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center);
                bodyText.textWrappingMode = TextWrappingModes.Normal;
                CanvasGroup bodyGroup = bodyText.gameObject.AddComponent<CanvasGroup>();
                bodyGroup.alpha = 0f;
                bodyGroup.blocksRaycasts = false;
                bodyGroup.interactable = false;

                var built = new ViewRefs(root, group, background, cardRect, cardBack, cardFront,
                    textGroup, selfIdText, titleText, bodyText, bodyGroup);
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);
                _view = built;
            }
            catch
            {
                _view = null;
                if (root != null) UnityEngine.Object.Destroy(root);
                throw;
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            return null;
        }

        private static void Populate(ViewRefs view, RevealContent content)
        {
            view.TitleText.text = content.Title ?? string.Empty;
            bool hasSelfId = !string.IsNullOrEmpty(content.SelfIdLine);
            view.SelfIdText.text = hasSelfId ? content.SelfIdLine : string.Empty;
            view.SelfIdText.gameObject.SetActive(hasSelfId);

            Sprite? back = AssetCatalog.GetSprite("role_card_back");
            view.CardBack.sprite = back;
            view.CardBack.preserveAspect = true;
            view.CardBack.color = back != null ? Color.white : new Color(0.06f, 0.07f, 0.09f, 1f);

            Sprite? front = AssetCatalog.GetSprite(IconKey(content.Icon));
            view.CardFront.sprite = front;
            view.CardFront.preserveAspect = true;
            view.CardFront.color = Color.white;
            view.CardFront.enabled = front != null;
        }

        private static void SetPage(ViewRefs view, RevealPage page)
        {
            if (page?.BodyLines == null)
            {
                view.BodyText.text = string.Empty;
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (string line in page.BodyLines)
            {
                if (sb.Length > 0) sb.Append('\n');
                if (line != null && line.StartsWith("◆"))
                    sb.Append(HeadingColorTag).Append(line).Append("</color>");
                else
                    sb.Append(line);
            }
            view.BodyText.text = sb.ToString();
        }

        private static string? IconKey(RoleIcon icon)
        {
            switch (icon)
            {
                case RoleIcon.Villager: return "role_villager";
                case RoleIcon.Werewolf: return "role_werewolf";
                case RoleIcon.BlackCat: return "role_blackcat";
                case RoleIcon.Bomber: return "role_bomber";
                case RoleIcon.Shaman: return "role_shaman";
                default: return null;
            }
        }

        public IEnumerator Play(RevealContent content, long revealStartUnixMs, long revealEndUnixMs,
            Vector2 hudOffset, Action? onFlip = null, Action? onPage = null, Action? onStow = null)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || content?.Pages == null || content.Pages.Length == 0)
            {
                WLog.Line("reveal_skip", secret: false,
                    ("reason", view == null ? "not_built" : "no_content"));
                yield break;
            }

            long now = SessionClock.NowMs();
            if (revealStartUnixMs <= 0) revealStartUnixMs = now;
            if (revealEndUnixMs <= revealStartUnixMs)
                revealEndUnixMs = revealStartUnixMs + Preplay.RoleRevealDurationMs;
            if (now >= revealEndUnixMs)
            {
                WLog.Line("reveal_skip", secret: false, ("reason", "late"));
                yield break;
            }

            Populate(view, content);
            int shownPage = -1;
            view.Root.SetActive(true);
            long initialElapsed = Math.Max(0, now - revealStartUnixMs);
            bool flipSfx = initialElapsed >= RoleRevealTimeline.FlipMidMs;
            bool pageSfx = initialElapsed >= Preplay.PageBoundaryMs;
            bool stowSfx = initialElapsed >= RoleRevealTimeline.StowStartMs;
            while ((now = SessionClock.NowMs()) < revealEndUnixMs)
            {
                if (!ReferenceEquals(_view, view) || view.Root == null) yield break;

                long elapsed = now - revealStartUnixMs;
                if (elapsed < 0) elapsed = 0;
                RoleRevealFrame frame = RoleRevealTimeline.At(elapsed);
                view.Group.alpha = Session.IsEscMenuOpen ? 0f : 1f;

                int page = Math.Min(frame.PageIndex, content.Pages.Length - 1);
                if (shownPage != page)
                {
                    shownPage = page;
                    SetPage(view, content.Pages[page]);
                }

                if (!flipSfx && elapsed >= RoleRevealTimeline.FlipMidMs)
                {
                    flipSfx = true;
                    Invoke(onFlip, "reveal_flip_callback_error");
                }
                if (!pageSfx && elapsed >= Preplay.PageBoundaryMs)
                {
                    pageSfx = true;
                    Invoke(onPage, "reveal_page_callback_error");
                }
                if (!stowSfx && elapsed >= RoleRevealTimeline.StowStartMs)
                {
                    stowSfx = true;
                    Invoke(onStow, "reveal_stow_callback_error");
                }

                view.Background.color = new Color(0f, 0f, 0f, 0.85f * frame.BackdropAlpha);
                view.CardBack.gameObject.SetActive(frame.ShowBack);
                view.CardFront.gameObject.SetActive(!frame.ShowBack);

                Vector2 resting = Vector2.LerpUnclamped(CardEntryPosition, CardRestPosition, frame.CardDropRatio);
                float liftRatio = Mathf.InverseLerp(
                    1f, RoleRevealTimeline.FlipLiftPeakScale, frame.CardLiftScale);
                Vector2 lifted = resting + Vector2.up * (CardLiftOffsetY * liftRatio);
                view.CardRect.anchoredPosition = Vector2.LerpUnclamped(
                    lifted, HudTarget(view, hudOffset), frame.StowRatio);
                float uniform = Mathf.LerpUnclamped(1f, HudCardScale, frame.StowRatio)
                    * frame.CardLiftScale;
                view.CardRect.localScale = new Vector3(uniform * frame.CardFlipScaleX, uniform, 1f);
                view.TextGroup.alpha = frame.TextAlpha;
                view.BodyGroup.alpha = frame.BodyAlpha;
                yield return null;
            }

            HideNow(view);
        }

        public IEnumerator Play(RevealContent content)
        {
            long start = SessionClock.NowMs();
            return Play(content, start, start + Preplay.RoleRevealDurationMs,
                Vector2.zero);
        }

        private static Vector2 HudTarget(ViewRefs view, Vector2 offset)
        {
            RectTransform rootRect = (RectTransform)view.Root.transform;
            float halfWidth = rootRect.rect.width * 0.5f;
            float halfHeight = rootRect.rect.height * 0.5f;
            return new Vector2(
                -halfWidth + HudGeometry.RoleBadge.LeftMargin + offset.x
                    + HudGeometry.RoleBadge.IconSize * 0.5f,
                halfHeight - HudGeometry.RoleBadge.TopMargin - offset.y
                    - HudGeometry.RoleBadge.RowHeight * 0.5f);
        }

        private static void Invoke(Action? callback, string logKey)
        {
            try { callback?.Invoke(); }
            catch (Exception e) { WLog.Line(logKey, secret: false, ("err", e.Message)); }
        }

        public void HideNow()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            HideNow(view);
        }

        private void HideNow(ViewRefs view)
        {
            if (!ReferenceEquals(_view, view) || view.Root == null) return;
            view.Group.alpha = 1f;
            view.Root.SetActive(false);
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }
    }
}
