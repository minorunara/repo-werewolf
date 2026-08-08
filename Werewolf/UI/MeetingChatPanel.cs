using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class MeetingChatPanel : IClientPanel
    {
        private const float PanelWidth = 330f;
        private const float PanelHeight = 690f;
        private const float PanelMarginRight = 24f;
        private const float PanelOffsetY = -10f;

        private const float ToggleButtonMargin = 24f;
        private static readonly IconToggleStyle ToggleStyle = new IconToggleStyle(
            iconSize: 96f,
            labelWidth: 320f,
            labelHeight: 32f,
            labelFontSize: 24f,
            labelColor: new Color(0.95f, 0.95f, 1f, 0.95f),
            fallbackText: "LOG",
            fallbackPlateColor: new Color(0.15f, 0.15f, 0.2f, 0.85f),
            fallbackTextColor: new Color(0.9f, 0.9f, 0.95f, 1f));

        private const float HeaderHeight = 44f;
        private const float FooterHeight = 34f;
        private const float ScrollbarWidth = 12f;
        private const float SidePadding = 8f;
        private const float BlockGap = 5f;
        private const float SpeakerGap = 10f;

        private const float VirtualOverscan = 240f;

        private const float TitleFontSize = 26f;
        private const float HintFontSize = 17f;

        private const float ScrollSensitivity = 40f;

        private const float BottomStickSlack = 24f;

        private static readonly Color PanelBgColor = new Color(0.02f, 0.02f, 0.05f, 0.85f);
        private static readonly Color HeaderBgColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        private static readonly Color TitleTextColor = new Color(1f, 0.9f, 0.6f, 1f);
        private static readonly Color HintTextColor = new Color(0.72f, 0.74f, 0.82f, 1f);
        private static readonly Color ScrollbarBgColor = new Color(0.1f, 0.1f, 0.14f, 0.9f);
        private static readonly Color ScrollbarHandleColor = new Color(0.55f, 0.55f, 0.65f, 0.95f);

        private GameObject _root;
        private GameObject _panel;
        private RectTransform _panelRect;
        private readonly IconToggleButton _toggle = new IconToggleButton();
        private KeyCode _labelKey = KeyCode.None;
        private TextMeshProUGUI _footerHint;
        private GameObject _emptyHint;

        private readonly ChatLayout _layout = new ChatLayout(new ChatLayoutMetrics(
            ChatRowFactory.SpeakerRowWidth, ChatRowFactory.SpeakerRowHeight,
            ContentWidth - SidePadding * 2f, ChatRowFactory.VoteRowHeight,
            BlockGap, SpeakerGap));

        private readonly ChatScrollView _view =
            new ChatScrollView(VirtualOverscan, SidePadding, BottomStickSlack);

        private readonly ChatRowFactory _rows = new ChatRowFactory(ContentWidth, SidePadding);

        private Func<ChatLogEntry, ChatBlockSize> _measureFn;
        private Func<int, GameObject> _createBlockFn;

        private int _renderedVersion = int.MinValue;
        private bool _renderedLocalDead;
        private bool _scrollToBottomPending;

        private MeetingChatLog _log;
        private bool _hasRenderState;

        private bool _open = true;

        public bool Exists => _root != null;

        public bool IsOpen => _open;

        public string LayerName => WerewolfUIManager.MeetingLayer;

        private static float ContentWidth => PanelWidth - ScrollbarWidth;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            var rootGo = new GameObject("WW_MeetingChatRoot", typeof(RectTransform));
            var containerRect = (RectTransform)rootGo.transform;
            containerRect.SetParent(layerRoot, false);
            Stretch(containerRect);
            _root = rootGo;

            var panelGo = new GameObject("WW_MeetingChatPanel", typeof(RectTransform));
            var rootRect = (RectTransform)panelGo.transform;
            rootRect.SetParent(containerRect, false);
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-PanelMarginRight, PanelOffsetY);
            rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var subCanvas = panelGo.AddComponent<Canvas>();
            subCanvas.overrideSorting = true;
            subCanvas.sortingOrder = WerewolfUIManager.PanelSortingOrder;
            panelGo.AddComponent<GraphicRaycaster>();
            _panel = panelGo;
            _panelRect = rootRect;

            Image bg = UiKit.CreateImage(rootRect, "Bg", Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelBgColor);
            Stretch(bg.rectTransform);

            float headerY = PanelHeight * 0.5f - HeaderHeight * 0.5f;
            UiKit.CreateImage(rootRect, "HeaderBg", new Vector2(0f, headerY),
                new Vector2(PanelWidth, HeaderHeight), HeaderBgColor);
            UiKit.CreateText(rootRect, "Title", new Vector2(0f, headerY),
                new Vector2(PanelWidth - 16f, HeaderHeight - 8f),
                Texts.Get(TextId.ChatLogTitle), TitleFontSize, TitleTextColor, TextAlignmentOptions.Center);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(rootRect, false);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(0f, FooterHeight);
            scrollRect.offsetMax = new Vector2(0f, -HeaderHeight);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = ScrollSensitivity;
            var scrollBgImg = scrollGo.AddComponent<Image>();
            scrollBgImg.color = new Color(0f, 0f, 0f, 0.001f);
            scrollBgImg.raycastTarget = true;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewportRect = (RectTransform)viewportGo.transform;
            viewportRect.SetParent(scrollRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-ScrollbarWidth, 0f);
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0.001f);
            vpImg.raycastTarget = true;
            viewportGo.AddComponent<RectMask2D>();
            scroll.viewport = viewportRect;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            scroll.content = contentRect;
            _view.Attach(scroll, viewportRect, contentRect);
            _rows.Attach(contentRect);

            BuildScrollbar(scrollRect, scroll);

            _footerHint = UiKit.CreateText(rootRect, "DeadHint",
                new Vector2(0f, -PanelHeight * 0.5f + FooterHeight * 0.5f),
                new Vector2(PanelWidth - 12f, FooterHeight),
                string.Empty, HintFontSize, HintTextColor, TextAlignmentOptions.Center);
            _footerHint.enableWordWrapping = true;

            _rows.Build(containerRect);
            _measureFn = _rows.MeasureBubble;
            _createBlockFn = CreateBlockObject;

            BuildToggleButton(containerRect);
            _panel.SetActive(_open);

            WLog.Line("chat_panel_built", secret: false,
                ("icon", _toggle.HasIcon ? 1 : 0));
        }

        private void BuildToggleButton(RectTransform containerRect)
        {
            RectTransform toggleRoot = _toggle.Build(containerRect, "ToggleButton", ToggleStyle,
                "icon_chat_log", Texts.Format(TextId.ChatLogToggleLabelFormat, "L"));
            Vector2 size = toggleRoot.sizeDelta;
            toggleRoot.anchoredPosition = new Vector2(
                (1920f * 0.5f) - size.x * 0.5f - ToggleButtonMargin,
                -(1080f * 0.5f) + size.y * 0.5f + ToggleButtonMargin);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void BuildScrollbar(RectTransform scrollRect, ScrollRect scroll)
        {
            var sbGo = new GameObject("Scrollbar", typeof(RectTransform));
            var sbRect = (RectTransform)sbGo.transform;
            sbRect.SetParent(scrollRect, false);
            sbRect.anchorMin = new Vector2(1f, 0f);
            sbRect.anchorMax = new Vector2(1f, 1f);
            sbRect.pivot = new Vector2(1f, 0.5f);
            sbRect.anchoredPosition = Vector2.zero;
            sbRect.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            var sbBgImg = sbGo.AddComponent<Image>();
            sbBgImg.color = ScrollbarBgColor;
            sbBgImg.raycastTarget = true;

            var slidingGo = new GameObject("SlidingArea", typeof(RectTransform));
            var slidingRect = (RectTransform)slidingGo.transform;
            slidingRect.SetParent(sbRect, false);
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(2f, 2f);
            slidingRect.offsetMax = new Vector2(-2f, -2f);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            var handleRect = (RectTransform)handleGo.transform;
            handleRect.SetParent(slidingRect, false);
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = ScrollbarHandleColor;
            handleImg.raycastTarget = true;

            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect = handleRect;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        public void Tick(KeyCode key, bool keysFree)
        {
            if (_root == null) return;

            SyncVirtualization();
            _rows.TickAvatarFreeze();

            if (_labelKey != key)
            {
                _labelKey = key;
                _toggle.SetLabel(Texts.Format(TextId.ChatLogToggleLabelFormat, FormatKey(key)));
            }

            bool hover = _toggle.IsPointerOverHitArea();
            _toggle.SetHover(hover);

            bool keyPressed = keysFree && key != KeyCode.None && Input.GetKeyDown(key);
            bool clicked = Input.GetMouseButtonDown(0) && hover;
            if (keyPressed || clicked)
            {
                _open = !_open;
                if (_panel != null) _panel.SetActive(_open);
                if (_open) _scrollToBottomPending = true;
                else ClearLiveBlocks();
                WLog.Line("chat_panel_toggle", secret: false, ("open", _open));
            }
        }

        public bool IsPointerOverPanel(Vector2 screenPoint)
        {
            if (_root == null) return false;
            if (_open && _panelRect != null
                && RectTransformUtility.RectangleContainsScreenPoint(_panelRect, screenPoint, null))
            {
                return true;
            }
            return _toggle.ContainsRect(screenPoint);
        }

        private static string FormatKey(KeyCode key) => key == KeyCode.None ? "-" : key.ToString();

        public void Render(MeetingChatLog log,
                           int localActor, Func<int, PlayerAvatar> resolveAvatar,
                           Func<int, int> resolveId, Func<int, Role?> markedRole, bool localDead)
        {
            if (_root == null || !_view.Attached) return;

            _log = log;
            _rows.SetContext(localActor, resolveAvatar, resolveId, markedRole);
            _hasRenderState = true;

            bool stickToBottom = _open && _view.IsAtBottom();

            if (localDead != _renderedLocalDead)
            {
                _renderedLocalDead = localDead;
                if (_footerHint != null)
                {
                    _footerHint.text = localDead ? Texts.Get(TextId.ChatLogDeadHint) : string.Empty;
                }
            }

            _layout.Sync(log, _measureFn);
            if (!_open) return;

            bool layoutChanged = _layout.Version != _renderedVersion;
            if (layoutChanged)
            {
                _renderedVersion = _layout.Version;
                ApplyContentHeight();
                UpdateEmptyHint();
            }
            if (stickToBottom && layoutChanged) _scrollToBottomPending = true;
            if (_scrollToBottomPending)
            {
                _scrollToBottomPending = false;
                _view.ScrollToBottom();
            }

            SyncVirtualization();
        }

        private void ApplyContentHeight()
        {
            _view.SetContentHeight(_layout.Count > 0
                ? _layout.TotalHeight + SidePadding * 2f
                : ChatRowFactory.SpeakerRowHeight + SidePadding * 2f);
        }

        private void UpdateEmptyHint()
        {
            bool show = _layout.Count == 0;
            if (show == (_emptyHint != null)) return;

            if (show)
            {
                var size = new Vector2(ContentWidth - SidePadding * 2f, ChatRowFactory.SpeakerRowHeight);
                RectTransform block = _rows.CreateBlock("Empty", size, -SidePadding, 0f);
                UiKit.CreateText(block, "Text", Vector2.zero, size,
                    Texts.Get(TextId.ChatLogEmpty), HintFontSize, HintTextColor, TextAlignmentOptions.Center);
                _emptyHint = block.gameObject;
            }
            else
            {
                UnityEngine.Object.Destroy(_emptyHint);
                _emptyHint = null;
            }
        }

        private void SyncVirtualization()
        {
            if (!_open || !_hasRenderState) return;
            _view.Sync(_layout, _createBlockFn);
        }

        private GameObject CreateBlockObject(int index)
        {
            if (_log == null) return null;
            ChatLayoutBlock block = _layout[index];
            long offset = block.EntrySeq - _log.DroppedTotal;
            if (offset < 0L || offset >= _log.Count) return null;

            ChatLogEntry entry = _log.Entries[(int)offset];
            float topY = -(SidePadding + _layout.ContentTop(index));

            return _rows.CreateRow(topY, block, entry, _layout.IsGroupHead(index));
        }

        private void ClearLiveBlocks()
        {
            _view.Clear();
            _rows.ClearPending();
        }

        public void ResetView()
        {
            _renderedVersion = int.MinValue;
            _scrollToBottomPending = true;
            ClearLiveBlocks();
            _view.ResetScrollPosition();
        }

        public void Destroy()
        {
            try
            {
                ClearLiveBlocks();
                if (_emptyHint != null) UnityEngine.Object.Destroy(_emptyHint);
                if (_root != null) UnityEngine.Object.Destroy(_root);
            }
            catch (Exception e)
            {
                WLog.Line("chat_panel_destroy_error", secret: false, ("err", e.Message));
            }
            _layout.Reset();
            _renderedVersion = int.MinValue;
            _emptyHint = null;
            _measureFn = null;
            _createBlockFn = null;
            _log = null;
            _hasRenderState = false;
            _open = true;
            _root = null;
            _panel = null;
            _panelRect = null;
            _toggle.Clear();
            _labelKey = KeyCode.None;
            _footerHint = null;
            _renderedLocalDead = false;
            ResetView();
            _view.Detach();
            _rows.Detach();
        }
    }
}
