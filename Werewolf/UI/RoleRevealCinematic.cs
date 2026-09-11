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
        private const float TitleFontSize = 64f;
        private const float BodyFontSize = 34f;
        private const float SelfIdFontSize = 40f;
        private const float SelfIdPosY = 390f;
        private const string HeadingColorTag = "<color=#FFF2B3>";

        private GameObject _root;
        private CanvasGroup _group;
        private Image _background;
        private RectTransform _cardRect;
        private Image _cardBack;
        private Image _cardFront;
        private CanvasGroup _textGroup;
        private TextMeshProUGUI _selfIdText;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _bodyText;
        private CanvasGroup _bodyGroup;
        private int _shownPage = -1;

        public bool Exists => _root != null;
        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_RoleReveal");
            _root = rect.gameObject;
            _group = _root.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _background = UiKit.CreateBackdrop(rect, "Bg", Color.clear);

            _cardRect = UiKit.CreateRect(rect, "Card", CardEntryPosition, CardSize);
            _cardBack = UiKit.CreateImage(_cardRect, "Back", Vector2.zero, CardSize,
                new Color(0.06f, 0.07f, 0.09f, 1f));
            _cardBack.raycastTarget = false;
            _cardFront = UiKit.CreateImage(_cardRect, "Front", Vector2.zero, CardSize, Color.white);
            _cardFront.raycastTarget = false;
            _cardFront.enabled = false;

            RectTransform textRoot = UiKit.CreateStretched(rect, "Text");
            _textGroup = textRoot.gameObject.AddComponent<CanvasGroup>();
            _textGroup.alpha = 0f;
            _textGroup.blocksRaycasts = false;
            _textGroup.interactable = false;

            _selfIdText = UiKit.CreateText(textRoot, "SelfId", new Vector2(0f, SelfIdPosY),
                new Vector2(1200f, 56f), string.Empty, SelfIdFontSize,
                new Color(0.92f, 0.94f, 1f, 0.9f), TextAlignmentOptions.Center);
            _titleText = UiKit.CreateText(textRoot, "Title", new Vector2(0f, -40f),
                new Vector2(1600f, 100f), string.Empty, TitleFontSize,
                new Color(1f, 0.95f, 0.7f), TextAlignmentOptions.Center);
            _titleText.enableWordWrapping = false;
            _bodyText = UiKit.CreateText(textRoot, "Body", new Vector2(0f, -220f),
                new Vector2(1600f, 400f), string.Empty, BodyFontSize,
                new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center);
            _bodyText.enableWordWrapping = true;
            _bodyGroup = _bodyText.gameObject.AddComponent<CanvasGroup>();
            _bodyGroup.alpha = 0f;
            _bodyGroup.blocksRaycasts = false;
            _bodyGroup.interactable = false;

            _root.SetActive(false);
        }

        private void Populate(RevealContent content)
        {
            _titleText.text = content.Title ?? string.Empty;
            bool hasSelfId = !string.IsNullOrEmpty(content.SelfIdLine);
            _selfIdText.text = hasSelfId ? content.SelfIdLine : string.Empty;
            _selfIdText.gameObject.SetActive(hasSelfId);

            Sprite back = AssetCatalog.GetSprite("role_card_back");
            _cardBack.sprite = back;
            _cardBack.preserveAspect = true;
            _cardBack.color = back != null ? Color.white : new Color(0.06f, 0.07f, 0.09f, 1f);

            Sprite front = AssetCatalog.GetSprite(IconKey(content.Icon));
            _cardFront.sprite = front;
            _cardFront.preserveAspect = true;
            _cardFront.color = Color.white;
            _cardFront.enabled = front != null;
        }

        private void SetPage(RevealPage page)
        {
            if (page?.BodyLines == null)
            {
                _bodyText.text = string.Empty;
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
            _bodyText.text = sb.ToString();
        }

        private static string IconKey(RoleIcon icon)
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
            Vector2 hudOffset, Action onFlip = null, Action onPage = null, Action onStow = null)
        {
            if (_root == null || content?.Pages == null || content.Pages.Length == 0)
            {
                WLog.Line("reveal_skip", secret: false,
                    ("reason", _root == null ? "not_built" : "no_content"));
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

            Populate(content);
            _shownPage = -1;
            _root.SetActive(true);
            long initialElapsed = Math.Max(0, now - revealStartUnixMs);
            bool flipSfx = initialElapsed >= RoleRevealTimeline.FlipMidMs;
            bool pageSfx = initialElapsed >= Preplay.PageBoundaryMs;
            bool stowSfx = initialElapsed >= RoleRevealTimeline.StowStartMs;
            while ((now = SessionClock.NowMs()) < revealEndUnixMs)
            {
                long elapsed = now - revealStartUnixMs;
                if (elapsed < 0) elapsed = 0;
                RoleRevealFrame frame = RoleRevealTimeline.At(elapsed);
                _group.alpha = Session.IsEscMenuOpen ? 0f : 1f;

                int page = Math.Min(frame.PageIndex, content.Pages.Length - 1);
                if (_shownPage != page)
                {
                    _shownPage = page;
                    SetPage(content.Pages[page]);
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

                _background.color = new Color(0f, 0f, 0f, 0.85f * frame.BackdropAlpha);
                _cardBack.gameObject.SetActive(frame.ShowBack);
                _cardFront.gameObject.SetActive(!frame.ShowBack);

                Vector2 resting = Vector2.LerpUnclamped(CardEntryPosition, CardRestPosition, frame.CardDropRatio);
                _cardRect.anchoredPosition = Vector2.LerpUnclamped(resting, HudTarget(hudOffset), frame.StowRatio);
                float uniform = Mathf.LerpUnclamped(1f, HudCardScale, frame.StowRatio);
                _cardRect.localScale = new Vector3(uniform * frame.CardFlipScaleX, uniform, 1f);
                _textGroup.alpha = frame.TextAlpha;
                _bodyGroup.alpha = frame.BodyAlpha;
                yield return null;
            }

            HideNow();
        }

        public IEnumerator Play(RevealContent content)
        {
            long start = SessionClock.NowMs();
            return Play(content, start, start + Preplay.RoleRevealDurationMs,
                Vector2.zero);
        }

        private Vector2 HudTarget(Vector2 offset)
        {
            RectTransform rootRect = (RectTransform)_root.transform;
            float halfWidth = rootRect.rect.width * 0.5f;
            float halfHeight = rootRect.rect.height * 0.5f;
            return new Vector2(
                -halfWidth + HudGeometry.RoleBadge.LeftMargin + offset.x
                    + HudGeometry.RoleBadge.IconSize * 0.5f,
                halfHeight - HudGeometry.RoleBadge.TopMargin - offset.y
                    - HudGeometry.RoleBadge.RowHeight * 0.5f);
        }

        private static void Invoke(Action callback, string logKey)
        {
            try { callback?.Invoke(); }
            catch (Exception e) { WLog.Line(logKey, secret: false, ("err", e.Message)); }
        }

        public void HideNow()
        {
            if (_root == null) return;
            _group.alpha = 1f;
            _root.SetActive(false);
            _shownPage = -1;
        }

        public void Destroy()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _group = null;
            _background = null;
            _cardRect = null;
            _cardBack = null;
            _cardFront = null;
            _textGroup = null;
            _selfIdText = null;
            _titleText = null;
            _bodyText = null;
            _bodyGroup = null;
            _shownPage = -1;
        }
    }
}
