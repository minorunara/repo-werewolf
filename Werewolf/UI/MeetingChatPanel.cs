using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public enum ChatPanelSurface : byte
    {
        Meeting = 0,

        Result = 1,
    }

    public sealed class MeetingChatPanel : IClientPanel
    {
        private readonly ChatPanelSurface _surface;

        public MeetingChatPanel() : this(ChatPanelSurface.Meeting) { }

        public MeetingChatPanel(ChatPanelSurface surface)
        {
            _surface = surface;
        }

        private const float PanelWidth = HudGeometry.ChatPanel.Width;
        private const float PanelHeight = HudGeometry.ChatPanel.Height;
        private const float PanelMarginRight = HudGeometry.ChatPanel.MarginRight;
        private const float PanelOffsetY = HudGeometry.ChatPanel.OffsetY;

        private const float ToggleButtonMargin = HudGeometry.ChatToggle.Margin;
        private static readonly IconToggleStyle ToggleStyle = new IconToggleStyle(
            iconSize: HudGeometry.ChatToggle.IconSize,
            labelWidth: HudGeometry.ChatToggle.LabelWidth,
            labelHeight: HudGeometry.ChatToggle.LabelHeight,
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

        private const float MinVisibleLineHeight = 6f;

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

        private readonly ChatSectionNav _nav = new ChatSectionNav();

        private readonly ChatSpeakerFilter _speakerFilter = new ChatSpeakerFilter();

        private readonly Vector3[] _cornerScratch = new Vector3[4];

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

        public string LayerName => _surface == ChatPanelSurface.Result
            ? ResultScreen.Layer
            : WerewolfUIManager.MeetingLayer;

        private TextId TitleTextId => _surface == ChatPanelSurface.Result
            ? TextId.ResultChatTitle
            : TextId.ChatLogTitle;

        private TextId ToggleLabelFormatId => _surface == ChatPanelSurface.Result
            ? TextId.ResultChatToggleLabelFormat
            : TextId.ChatLogToggleLabelFormat;

        private static float ContentWidth => PanelWidth - ScrollbarWidth;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            RectTransform containerRect = UiKit.CreateStretched(layerRoot, "WW_MeetingChatRoot");
            _root = containerRect.gameObject;

            RectTransform rootRect = UiKit.CreateAnchoredRect(containerRect, "WW_MeetingChatPanel",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-PanelMarginRight, PanelOffsetY), new Vector2(PanelWidth, PanelHeight));
            var panelGo = rootRect.gameObject;

            var subCanvas = panelGo.AddComponent<Canvas>();
            if (_surface == ChatPanelSurface.Meeting)
            {
                subCanvas.overrideSorting = true;
                subCanvas.sortingOrder = WerewolfUIManager.PanelSortingOrder;
            }
            panelGo.AddComponent<GraphicRaycaster>();
            _panel = panelGo;
            _panelRect = rootRect;

            UiKit.CreateBackdrop(rootRect, "Bg", PanelBgColor);

            float headerY = PanelHeight * 0.5f - HeaderHeight * 0.5f;
            UiKit.CreateImage(rootRect, "HeaderBg", new Vector2(0f, headerY),
                new Vector2(PanelWidth, HeaderHeight), HeaderBgColor);
            TextMeshProUGUI title = UiKit.CreateText(rootRect, "Title", new Vector2(0f, headerY),
                new Vector2(PanelWidth - 16f, HeaderHeight - 8f),
                Texts.Get(TitleTextId), TitleFontSize, TitleTextColor, TextAlignmentOptions.Center);

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

            RectTransform scrollbar = BuildScrollbar(scrollRect, scroll);
            _nav.Build(rootRect, headerY, PanelWidth, scrollbar);
            _speakerFilter.Attach(title, viewportRect);

            _footerHint = UiKit.CreateText(rootRect, "DeadHint",
                new Vector2(0f, -PanelHeight * 0.5f + FooterHeight * 0.5f),
                new Vector2(PanelWidth - 12f, FooterHeight),
                _surface == ChatPanelSurface.Result ? Texts.Get(TextId.ResultChatHint) : string.Empty,
                HintFontSize, HintTextColor, TextAlignmentOptions.Center);
            _footerHint.enableWordWrapping = true;

            _rows.Build(containerRect);
            _measureFn = _rows.MeasureBubble;
            _createBlockFn = CreateBlockObject;

            BuildToggleButton(containerRect);
            _panel.SetActive(_open);
        }

        public void EnsureTopSibling()
        {
            if (_root != null) _root.transform.SetAsLastSibling();
        }

        public void SetVisible(bool visible)
        {
            if (_root == null || _root.activeSelf == visible) return;
            _root.SetActive(visible);
            if (visible) _scrollToBottomPending = true;
            else
            {
                ClearSpeakerFilter();
                ClearLiveBlocks();
            }
        }

        private void BuildToggleButton(RectTransform containerRect)
        {
            RectTransform toggleRoot = _toggle.Build(containerRect, "ToggleButton", ToggleStyle,
                "icon_chat_log", Texts.Format(ToggleLabelFormatId, "L"));
            UiKit.PinToCorner(toggleRoot, UiKit.BottomRight, ToggleButtonMargin);
        }

        private static RectTransform BuildScrollbar(RectTransform scrollRect, ScrollRect scroll)
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
            UiKit.Stretch(handleRect);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = ScrollbarHandleColor;
            handleImg.raycastTarget = true;

            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect = handleRect;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return sbRect;
        }

        public void Tick(KeyCode key, bool keysFree)
        {
            if (_root == null) return;

            SyncVirtualization();
            _rows.TickAvatarFreeze();

            if (_open && _hasRenderState && _log != null)
            {
                _nav.Tick(_layout, _log.SectionSeqs, _view);
            }

            if (_open && _speakerFilter.Tick())
            {
                _layout.SetFilter(_speakerFilter.Predicate);
                _scrollToBottomPending = true;
            }

            if (_labelKey != key)
            {
                _labelKey = key;
                _toggle.SetLabel(Texts.Format(ToggleLabelFormatId, FormatKey(key)));
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
                else
                {
                    ClearSpeakerFilter();
                    ClearLiveBlocks();
                }
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

        public bool TryGetRecapLineScreenRect(int lineIndex, out Rect screenRect)
        {
            screenRect = default;
            if (_root == null || !_open || !_hasRenderState || _log == null || !_view.Attached) return false;
            if (lineIndex < 0) return false;

            IReadOnlyList<long> sections = _log.SectionSeqs;
            if (sections == null || sections.Count == 0) return false;
            long seq = sections[sections.Count - 1];

            GameObject row = null;
            for (int i = _layout.Count - 1; i >= 0; i--)
            {
                ChatLayoutBlock block = _layout[i];
                if (block.EntrySeq != seq || block.Kind != ChatBlockKind.Bubble) continue;
                row = _view.Live(i);
                break;
            }
            if (row == null) return false;

            var label = row.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null || !TryGetLineWorldRect(label, lineIndex, out Rect world)) return false;

            RectTransform viewport = _view.Viewport;
            if (viewport != null)
            {
                viewport.GetWorldCorners(_cornerScratch);
                float minY = Mathf.Max(world.yMin, _cornerScratch[0].y);
                float maxY = Mathf.Min(world.yMax, _cornerScratch[1].y);
                if (maxY - minY < MinVisibleLineHeight) return false;
                world = Rect.MinMaxRect(world.xMin, minY, world.xMax, maxY);
            }

            screenRect = world;
            return true;
        }

        private static bool TryGetLineWorldRect(TextMeshProUGUI label, int lineIndex, out Rect world)
        {
            world = default;
            TMP_TextInfo info = label.textInfo;
            if (info == null || info.characterCount <= 0) return false;

            string source = label.text;
            if (string.IsNullOrEmpty(source)) return false;

            int start = 0;
            for (int i = 0; i < lineIndex; i++)
            {
                int br = source.IndexOf('\n', start);
                if (br < 0) return false;
                start = br + 1;
            }
            int end = source.IndexOf('\n', start);
            if (end < 0) end = source.Length;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            int count = Mathf.Min(info.characterCount, info.characterInfo.Length);
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (ch.index < start || ch.index >= end || ch.character == '\n') continue;
                minX = Mathf.Min(minX, ch.bottomLeft.x);
                maxX = Mathf.Max(maxX, ch.topRight.x);
                minY = Mathf.Min(minY, ch.descender);
                maxY = Mathf.Max(maxY, ch.ascender);
            }
            if (minX > maxX || minY > maxY) return false;

            Transform t = label.rectTransform;
            Vector3 wMin = t.TransformPoint(new Vector3(minX, minY, 0f));
            Vector3 wMax = t.TransformPoint(new Vector3(maxX, maxY, 0f));
            world = Rect.MinMaxRect(
                Mathf.Min(wMin.x, wMax.x), Mathf.Min(wMin.y, wMax.y),
                Mathf.Max(wMin.x, wMax.x), Mathf.Max(wMin.y, wMax.y));
            return true;
        }

        public void SetUnreadBadge(bool visible)
        {
            if (_root == null) return;
            _toggle.SetBadgeVisible(visible);
        }

        private static string FormatKey(KeyCode key) => key == KeyCode.None ? "-" : key.ToString();

        public void Render(MeetingChatLog log,
                           int localActor, Func<int, PlayerHandle> resolveAvatar,
                           Func<int, int> resolveId, Func<int, Role?> markedRole, bool localDead)
        {
            if (_root == null || !_view.Attached) return;

            _log = log;
            _rows.SetContext(localActor, resolveAvatar, resolveId, markedRole);
            _hasRenderState = true;

            bool stickToBottom = _open && _view.IsAtBottom();

            if (_surface == ChatPanelSurface.Meeting && localDead != _renderedLocalDead)
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
            _nav.SyncMarks(_layout, log.SectionSeqs, SidePadding,
                _layout.Count > 0 ? _layout.TotalHeight + SidePadding * 2f : 0f);
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

            GameObject row = _rows.CreateRow(topY, block, entry, _layout.IsGroupHead(index));
            if (row != null && block.Kind == ChatBlockKind.Speaker && entry.Kind == ChatEntryKind.Message)
            {
                _speakerFilter.RegisterTarget((RectTransform)row.transform, entry.Actor);
            }
            return row;
        }

        private void ClearLiveBlocks()
        {
            _view.Clear();
            _rows.ClearPending();
            _speakerFilter.ClearTargets();
        }

        private void ClearSpeakerFilter()
        {
            _speakerFilter.Deactivate();
            _layout.SetFilter(null);
        }

        public void ResetView()
        {
            ClearSpeakerFilter();
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
            _nav.Clear();
            _speakerFilter.Clear();
            _labelKey = KeyCode.None;
            _footerHint = null;
            _renderedLocalDead = false;
            ResetView();
            _view.Detach();
            _rows.Detach();
        }
    }
}
