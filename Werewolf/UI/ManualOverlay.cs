using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ManualOverlay : IClientPanel
    {
        public string LayerName => WerewolfUIManager.ManualLayer;

        private const float PanelWidth = 1100f;
        private const float PanelHeight = 760f;
        private const float TitleFontSize = 40f;
        private const float BodyFontSize = 26f;
        private const float FooterFontSize = 24f;
        private const float HintFontSize = 18f;

        private const float ImageFrameWidth = 640f;
        private const float ImageDefaultHeight = 240f;
        private const float ImageTopY = 300f;
        private const float ImageBodyGap = 20f;

        private const float BodyWidth = 1000f;
        private const float BodyBottomY = -310f;
        private static readonly Vector2 BodyPosNoImage = new Vector2(0f, -5f);
        private static readonly Vector2 BodySizeNoImage = new Vector2(BodyWidth, 610f);

        private const float BlockIconSize = 104f;
        private const float BlockIconTextGap = 6f;
        private const float BlockGap = 16f;
        private const float BlockLineHeightFallback = 36f;

        private const float IconLeftMargin = 40f;
        private const float IconTopMargin = 364f;
        private const float IconLobbyLeftMargin = 20f;
        private const float IconLobbyTopMargin = 20f;
        private static readonly IconToggleStyle ToggleStyle = new IconToggleStyle(
            iconSize: 72f,
            labelWidth: 170f,
            labelHeight: 28f,
            labelFontSize: 22f,
            labelColor: new Color(1f, 0.9f, 0.6f, 1f),
            fallbackText: "？",
            fallbackPlateColor: new Color(0.08f, 0.08f, 0.10f, 0.55f),
            fallbackTextColor: new Color(0.95f, 0.95f, 0.95f, 0.95f));

        private static readonly Color PanelFrameColor = new Color(0.02f, 0.02f, 0.05f, 0.97f);
        private static readonly Color TitleColor = new Color(1f, 0.95f, 0.7f, 1f);
        private static readonly Color BodyColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color FooterColor = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.6f);

        private GameObject _root;
        private GameObject _panel;
        private RectTransform _panelRect;
        private TextMeshProUGUI _titleText;
        private Image _pageImage;
        private RectTransform _bodyRect;
        private TextMeshProUGUI _footerText;
        private TextMeshProUGUI _hintText;

        private RectTransform _iconContainer;
        private readonly IconToggleButton _toggle = new IconToggleButton();

        private bool _open;
        private int _pageIndex;
        private KeyCode _lastLabelKey = KeyCode.None;
        private Vector2 _positionOffset;
        private Vector2 _appliedOffset;
        private bool _offsetApplied;
        private bool _lobbyLayout;

        public bool Exists => _root != null;

        public bool IsOpen => _open;

        public Vector2 PositionOffset
        {
            get => _positionOffset;
            set => _positionOffset = value;
        }

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            RectTransform rootRect = UiKit.CreateRect(layerRoot, "WW_ManualOverlay", Vector2.zero, new Vector2(1920f, 1080f));
            _root = rootRect.gameObject;
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.ManualSortingOrder;

            RectTransform panelRect = UiKit.CreateRect(rootRect, "ManualPanel", Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            _panel = panelRect.gameObject;
            _panelRect = panelRect;

            UiKit.CreateImage(panelRect, "PanelBg", Vector2.zero, new Vector2(PanelWidth + 12f, PanelHeight + 12f), PanelFrameColor);

            _titleText = UiKit.CreateText(panelRect, "Title", new Vector2(0f, 330f),
                new Vector2(1000f, 60f), "", TitleFontSize, TitleColor, TextAlignmentOptions.Center);

            _pageImage = UiKit.CreateImage(panelRect, "PageImage",
                new Vector2(0f, ImageTopY - ImageDefaultHeight * 0.5f),
                new Vector2(ImageFrameWidth, ImageDefaultHeight), Color.white);
            _pageImage.preserveAspect = true;
            _pageImage.enabled = false;

            _bodyRect = UiKit.CreateRect(panelRect, "BodyArea", BodyPosNoImage, BodySizeNoImage);

            _footerText = UiKit.CreateText(panelRect, "Footer", new Vector2(0f, -335f),
                new Vector2(400f, 30f), "", FooterFontSize, FooterColor, TextAlignmentOptions.Center);

            _hintText = UiKit.CreateText(panelRect, "NavHint", new Vector2(0f, -364f),
                new Vector2(1000f, 26f), "", HintFontSize, HintColor, TextAlignmentOptions.Center);

            _panel.SetActive(false);

            _iconContainer = _toggle.Build(rootRect, "ToggleButton", ToggleStyle,
                "manual_icon", Texts.Format(TextId.ManualToggleLabelFormat, "-"));
            _iconContainer.anchorMin = _iconContainer.anchorMax = new Vector2(0f, 1f);
            ApplyIconOffset(force: true);

            WLog.Line("manual_built", secret: false,
                ("pages", ManualCatalog.PageCount),
                ("icon", _toggle.HasIcon ? 1 : 0));
        }

        public void Tick(KeyCode key, bool available, bool suppressed, bool lobbyMenu)
        {
            if (_root == null) return;

            try
            {
                if (_lobbyLayout != lobbyMenu)
                {
                    _lobbyLayout = lobbyMenu;
                    _offsetApplied = false;
                }
                ApplyIconOffset(force: false);

                if (key != _lastLabelKey)
                {
                    _toggle.SetLabel(Texts.Format(TextId.ManualToggleLabelFormat, FormatKey(key)));
                    if (_hintText != null) _hintText.text = BuildNavHintText(key);
                    _lastLabelKey = key;
                }

                bool iconVisible = available && !suppressed;
                if (_iconContainer != null && _iconContainer.gameObject.activeSelf != iconVisible)
                {
                    _iconContainer.gameObject.SetActive(iconVisible);
                }

                if (!available || suppressed)
                {
                    _toggle.SetHover(false);
                    if (_open) CloseInternal();
                    return;
                }

                bool cursorFree = Cursor.lockState != CursorLockMode.Locked;
                _toggle.SetHover(cursorFree && _toggle.IsPointerOverOpaqueIcon());

                bool textFree = NoTextInputsActive();
                bool keyPressed = textFree && key != KeyCode.None && Input.GetKeyDown(key);
                bool clicked = cursorFree && _toggle.WasClicked();
                if (keyPressed || clicked)
                {
                    if (_open) CloseInternal();
                    else OpenInternal();
                    return;
                }

                if (_open)
                {
                    SuppressEscMenu();

                    if (textFree && MenuKeyDown())
                    {
                        CloseInternal();
                        return;
                    }
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    if (textFree && Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        ShowPage(shift
                            ? ManualCatalog.PreviousSectionStart(_pageIndex)
                            : ManualCatalog.ClampIndex(_pageIndex - 1));
                    }
                    if (textFree && Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        ShowPage(shift
                            ? ManualCatalog.NextSectionStart(_pageIndex)
                            : ManualCatalog.ClampIndex(_pageIndex + 1));
                    }
                }
            }
            catch (Exception e)
            {
                WLog.Line("manual_tick_error", secret: false, ("err", e.Message));
            }
        }

        public bool IsPointerOverPanel(Vector2 screenPoint)
        {
            if (!_open || _panelRect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(_panelRect, screenPoint, null);
        }

        public void ForceClose()
        {
            if (_open) CloseInternal();
        }

        public void Destroy()
        {
            _open = false;
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _panel = null;
            _panelRect = null;
            _titleText = null;
            _pageImage = null;
            _bodyRect = null;
            _footerText = null;
            _hintText = null;
            _iconContainer = null;
            _toggle.Clear();
            _lastLabelKey = KeyCode.None;
            _offsetApplied = false;
            _lobbyLayout = false;
        }

        private void OpenInternal()
        {
            _open = true;
            _panel.SetActive(true);
            ShowPage(ManualCatalog.ClampIndex(_pageIndex));
            WLog.Line("manual_open", secret: false, ("page", _pageIndex + 1));
        }

        private void CloseInternal()
        {
            _open = false;
            if (_panel != null) _panel.SetActive(false);
            WLog.Line("manual_close", secret: false);
        }

        private void ShowPage(int index)
        {
            _pageIndex = index;
            ManualPage page = ManualCatalog.Pages[index];

            _titleText.text = Texts.Get(page.TitleId);
            ManualSection section = ManualCatalog.Sections[ManualCatalog.SectionIndexForPage(index)];
            _footerText.text = Texts.Format(TextId.ManualPageFooterFormat,
                Texts.Get(section.TitleId), index + 1, ManualCatalog.PageCount);

            Sprite sprite = page.ImageKey != null ? AssetCatalog.GetSprite(page.ImageKey) : null;
            bool hasImage = sprite != null;
            Vector2 bodySize;
            if (hasImage)
            {
                float imageHeight = page.ImageHeight > 0f ? page.ImageHeight : ImageDefaultHeight;
                float imageWidth = page.ImageWidth > 0f ? page.ImageWidth : ImageFrameWidth;
                _pageImage.sprite = sprite;
                _pageImage.enabled = true;
                _pageImage.rectTransform.sizeDelta = new Vector2(imageWidth, imageHeight);
                _pageImage.rectTransform.anchoredPosition = new Vector2(0f, ImageTopY - imageHeight * 0.5f);
                float bodyTop = ImageTopY - imageHeight - ImageBodyGap;
                bodySize = new Vector2(BodyWidth, bodyTop - BodyBottomY);
                _bodyRect.anchoredPosition = new Vector2(0f, (bodyTop + BodyBottomY) * 0.5f);
                _bodyRect.sizeDelta = bodySize;
            }
            else
            {
                _pageImage.sprite = null;
                _pageImage.enabled = false;
                bodySize = BodySizeNoImage;
                _bodyRect.anchoredPosition = BodyPosNoImage;
                _bodyRect.sizeDelta = bodySize;
            }

            RebuildBlocks(page, bodySize);
        }

        private void RebuildBlocks(ManualPage page, Vector2 areaSize)
        {
            for (int i = _bodyRect.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(_bodyRect.GetChild(i).gameObject);
            }

            float width = areaSize.x;
            float y = 0f;
            foreach (ManualBlock block in page.Blocks)
            {
                if (block.IconKey != null)
                {
                    Sprite icon = AssetCatalog.GetSprite(block.IconKey);
                    if (icon != null)
                    {
                        Image img = UiKit.CreateImage(_bodyRect, "BlockIcon", Vector2.zero,
                            new Vector2(BlockIconSize, BlockIconSize), Color.white);
                        img.sprite = icon;
                        img.preserveAspect = true;
                        PlaceTopLeft(img.rectTransform, areaSize, 0f, y, BlockIconSize, BlockIconSize);
                        y += BlockIconSize + BlockIconTextGap;
                    }
                }

                string body = Texts.Get(block.BodyId);
                TextMeshProUGUI label = UiKit.CreateText(_bodyRect, "BlockText", Vector2.zero,
                    new Vector2(width, BlockLineHeightFallback), body, BodyFontSize, BodyColor,
                    TextAlignmentOptions.TopLeft);
                label.enableWordWrapping = true;
                float height = MeasureTextHeight(label, body, width);
                label.rectTransform.sizeDelta = new Vector2(width, height);
                PlaceTopLeft(label.rectTransform, areaSize, 0f, y, width, height);
                y += height + BlockGap;
            }

            float contentHeight = y - BlockGap;
            if (contentHeight > areaSize.y)
            {
                WLog.Line("manual_page_overflow", secret: false,
                    ("page", _pageIndex + 1), ("height", (int)contentHeight), ("area", (int)areaSize.y));
            }
        }

        private static float MeasureTextHeight(TextMeshProUGUI label, string text, float width)
        {
            float measured = 0f;
            try
            {
                measured = label.GetPreferredValues(text, width, 0f).y;
            }
            catch
            {
                measured = 0f;
            }
            if (measured > 0f) return Mathf.Ceil(measured);

            int lines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') lines++;
            }
            return lines * BlockLineHeightFallback;
        }

        private static void PlaceTopLeft(RectTransform rect, Vector2 areaSize, float x, float y, float w, float h)
        {
            rect.anchoredPosition = new Vector2(
                -areaSize.x * 0.5f + x + w * 0.5f,
                areaSize.y * 0.5f - y - h * 0.5f);
        }

        private void ApplyIconOffset(bool force)
        {
            if (_iconContainer == null) return;
            if (!force && _offsetApplied && _appliedOffset == _positionOffset) return;
            Vector2 size = _iconContainer.sizeDelta;
            float left = _lobbyLayout ? IconLobbyLeftMargin : IconLeftMargin + _positionOffset.x;
            float top = _lobbyLayout ? IconLobbyTopMargin : IconTopMargin + _positionOffset.y;
            _iconContainer.anchoredPosition = new Vector2(
                left + size.x * 0.5f,
                -(top + size.y * 0.5f));
            _appliedOffset = _positionOffset;
            _offsetApplied = true;
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

        private static bool MenuKeyDown()
        {
            try
            {
                return SemiFunc.InputDown(InputKey.Menu);
            }
            catch
            {
                return false;
            }
        }

        private static bool NoTextInputsActive()
        {
            try
            {
                return SemiFunc.NoTextInputsActive();
            }
            catch
            {
                return true;
            }
        }

        private static string BuildNavHintText(KeyCode key)
        {
            string hint = Texts.Format(TextId.ManualNavHint, FormatKey(key));
            try
            {
                InputManager input = InputManager.instance;
                if (input != null)
                {
                    hint = input.InputDisplayReplaceTags(hint, "<color=#FF8500>", "</color>");
                }
            }
            catch { }
            return hint;
        }

        private static string FormatKey(KeyCode key)
        {
            return key == KeyCode.None ? "-" : key.ToString();
        }
    }
}
