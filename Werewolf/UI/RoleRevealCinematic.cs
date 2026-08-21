using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class RoleRevealCinematic : IClientPanel
    {
        public string LayerName => "RoleReveal";

        public const float MoonUiWaitTimeoutSec = 10f;

        private static readonly Vector2 IconSize = new Vector2(256f, 256f);
        private const float TitleFontSize = 64f;
        private const float BodyFontSize = 34f;

        private const float SelfIdFontSize = 40f;
        private const float SelfIdPosY = 370f;

        private const float PageCrossfadeSec = 0.3f;

        private const float SkipFadeOutSec = 0.2f;

        private const float SkipHintFontSize = 24f;

        private const string HeadingColorTag = "<color=#FFF2B3>";

        private GameObject _root;
        private CanvasGroup _group;
        private Image _background;
        private Image _iconImage;
        private TextMeshProUGUI _selfIdText;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _bodyText;
        private CanvasGroup _bodyGroup;
        private TextMeshProUGUI _skipHintText;
        private bool _skipRequested;

        public bool Exists => _root != null;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_RoleReveal", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _background = UiKit.CreateImage(rect, "Bg", Vector2.zero,
                new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.85f));
            var bgRect = _background.rectTransform;
            UiKit.Stretch(bgRect);
            _background.raycastTarget = false;

            _selfIdText = UiKit.CreateText(rect, "SelfId", new Vector2(0f, SelfIdPosY),
                new Vector2(1200f, 56f), "", SelfIdFontSize,
                new Color(0.92f, 0.94f, 1f, 0.9f), TextAlignmentOptions.Center);

            _iconImage = UiKit.CreateImage(rect, "Icon", new Vector2(0f, 180f), IconSize, Color.white);
            _iconImage.raycastTarget = false;
            _iconImage.enabled = false;

            _titleText = UiKit.CreateText(rect, "Title", new Vector2(0f, -40f),
                new Vector2(1600f, 100f), "", TitleFontSize,
                new Color(1f, 0.95f, 0.7f), TextAlignmentOptions.Center);
            _titleText.enableWordWrapping = false;

            _bodyText = UiKit.CreateText(rect, "Body", new Vector2(0f, -220f),
                new Vector2(1600f, 400f), "", BodyFontSize,
                new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center);
            _bodyText.enableWordWrapping = true;

            _bodyGroup = _bodyText.gameObject.AddComponent<CanvasGroup>();
            _bodyGroup.alpha = 1f;
            _bodyGroup.blocksRaycasts = false;
            _bodyGroup.interactable = false;

            _skipHintText = UiKit.CreateText(rect, "SkipHint", new Vector2(0f, -470f),
                new Vector2(1200f, 40f), "", SkipHintFontSize,
                new Color(1f, 1f, 1f, 0.65f), TextAlignmentOptions.Center);

            _root.SetActive(false);
            WLog.Line("reveal_built", secret: false);
        }

        private void Populate(RevealContent content)
        {
            if (_root == null || content == null) return;

            _titleText.text = content.Title ?? string.Empty;
            _skipHintText.text = BuildSkipHintText();

            bool hasSelfId = !string.IsNullOrEmpty(content.SelfIdLine);
            _selfIdText.text = hasSelfId ? content.SelfIdLine : string.Empty;
            if (_selfIdText.gameObject.activeSelf != hasSelfId) _selfIdText.gameObject.SetActive(hasSelfId);

            Sprite icon = AssetCatalog.GetSprite(IconKey(content.Icon));
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
        }

        private void SetPage(RevealPage page)
        {
            if (page?.BodyLines == null)
            {
                _bodyText.text = string.Empty;
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var line in page.BodyLines)
            {
                if (sb.Length > 0) sb.Append('\n');
                if (line != null && line.StartsWith("◆"))
                {
                    sb.Append(HeadingColorTag).Append(line).Append("</color>");
                }
                else
                {
                    sb.Append(line);
                }
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

        public IEnumerator Play(RevealContent content)
        {
            if (_root == null || content == null || content.Pages == null || content.Pages.Length == 0)
            {
                WLog.Line("reveal_skip", secret: false, ("reason", _root == null ? "not_built" : "no_content"));
                yield break;
            }

            yield return WaitForMoonUiFinish(MoonUiWaitTimeoutSec);

            _skipRequested = false;
            Populate(content);
            SetPage(content.Pages[0]);
            _group.alpha = 0f;
            _bodyGroup.alpha = 1f;
            _root.SetActive(true);
            WLog.Line("reveal_show", secret: true, ("icon", content.Icon), ("pages", content.Pages.Length));

            yield return DriveWithSkip(UiTween.Fade(_group, 0f, 1f, content.FadeInSec, UiTween.EaseInOut()));

            for (int i = 0; i < content.Pages.Length && !_skipRequested; i++)
            {
                if (i > 0)
                {
                    yield return DriveWithSkip(UiTween.Fade(_bodyGroup, 1f, 0f, PageCrossfadeSec, UiTween.EaseInOut()));
                    if (_skipRequested) break;
                    SetPage(content.Pages[i]);
                    yield return DriveWithSkip(UiTween.Fade(_bodyGroup, 0f, 1f, PageCrossfadeSec, UiTween.EaseInOut()));
                    if (_skipRequested) break;
                }
                yield return DriveWithSkip(UiTween.Hold(content.Pages[i].HoldSec));
            }

            float fadeOutSec = _skipRequested ? SkipFadeOutSec : content.FadeOutSec;
            yield return DriveSuppressOnly(UiTween.Fade(_group, _group.alpha, 0f, fadeOutSec, UiTween.EaseInOut()));

            HideNow();
            WLog.Line("reveal_end", secret: false, ("skipped", _skipRequested));
        }

        private IEnumerator DriveWithSkip(IEnumerator step)
        {
            while (true)
            {
                SuppressEscMenu();
                if (!_skipRequested && SkipInputDown())
                {
                    _skipRequested = true;
                    WLog.Line("reveal_skip_input", secret: false);
                }
                if (_skipRequested) yield break;

                bool moved;
                try
                {
                    moved = step != null && step.MoveNext();
                }
                catch (Exception e)
                {
                    WLog.Line("reveal_tick_error", secret: false, ("err", e.Message));
                    moved = false;
                }
                if (!moved) yield break;
                yield return step.Current;
            }
        }

        private static IEnumerator DriveSuppressOnly(IEnumerator step)
        {
            while (true)
            {
                SuppressEscMenu();

                bool moved;
                try
                {
                    moved = step != null && step.MoveNext();
                }
                catch (Exception e)
                {
                    WLog.Line("reveal_tick_error", secret: false, ("err", e.Message));
                    moved = false;
                }
                if (!moved) yield break;
                yield return step.Current;
            }
        }

        private static void SuppressEscMenu()
        {
            try
            {
                GameDirector director = GameDirector.instance;
                if (director != null) director.SetDisableEscMenu(1f);
            }
            catch { }
        }

        private static bool SkipInputDown()
        {
            try
            {
                return SemiFunc.NoTextInputsActive() && SemiFunc.InputDown(InputKey.Menu);
            }
            catch
            {
                return false;
            }
        }

        private static string BuildSkipHintText()
        {
            string hint = Texts.Get(TextId.RevealSkipHint);
            try
            {
                InputManager input = InputManager.instance;
                if (input != null)
                {
                    hint = input.InputDisplayReplaceTags(hint, "<color=#FF8500><u><b>", "</b></u></color>");
                }
            }
            catch { }
            return hint;
        }

        private static IEnumerator WaitForMoonUiFinish(float timeoutSec)
        {
            MoonUI moon = null;
            try { moon = MoonUI.instance; }
            catch { moon = null; }

            if (moon == null || moon.objectActive == null || !moon.objectActive.activeSelf)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < timeoutSec)
            {
                MoonUI cur = null;
                try { cur = MoonUI.instance; }
                catch { cur = null; }

                if (cur == null || cur.objectActive == null || !cur.objectActive.activeSelf)
                {
                    WLog.Line("reveal_moon_ok", secret: false, ("waitedSec", elapsed));
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            WLog.Line("reveal_moon_timeout", secret: false, ("timeoutSec", timeoutSec));
        }

        public void HideNow()
        {
            if (_root == null) return;
            _group.alpha = 0f;
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
            _background = null;
            _iconImage = null;
            _selfIdText = null;
            _titleText = null;
            _bodyText = null;
            _bodyGroup = null;
            _skipHintText = null;
        }
    }
}
