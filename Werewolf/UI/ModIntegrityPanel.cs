#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public enum ModIntegrityPanelFilter
    {
        NeedsReview,
        Match,
        All,
    }

    public sealed class ModIntegrityPanel : IClientPanel
    {
        private sealed class RowSlot
        {
            public RowSlot(RectTransform rect, Image background, TextMeshProUGUI label)
            {
                Rect = rect; Background = background; Label = label;
            }
            public RectTransform Rect { get; }
            public Image Background { get; }
            public TextMeshProUGUI Label { get; }
            public int Actor { get; set; } = -1;
        }

        private const float RowHeight = 44f;
        private const float RowStep = 46f;
        private sealed class RowsView
        {
            public RowsView(RectTransform content, ScrollRect scroll) { Content = content; Scroll = scroll; }
            public RectTransform Content { get; }
            public ScrollRect Scroll { get; }
        }
        private sealed class DetailView
        {
            public DetailView(TextMeshProUGUI text, RectTransform viewport, RectTransform content, ScrollRect scroll)
            { Text = text; Viewport = viewport; Content = content; Scroll = scroll; }
            public TextMeshProUGUI Text { get; }
            public RectTransform Viewport { get; }
            public RectTransform Content { get; }
            public ScrollRect Scroll { get; }
        }
        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, RectTransform panelRect, RowsView rows, DetailView detail,
                RectTransform filterNeedsRect, RectTransform filterMatchRect,
                RectTransform filterAllRect, RectTransform closeRect,
                TextMeshProUGUI filterNeedsLabel, TextMeshProUGUI filterMatchLabel,
                TextMeshProUGUI filterAllLabel, TextMeshProUGUI closeLabel,
                TextMeshProUGUI titleLabel)
            {
                Root = root; PanelRect = panelRect; Rows = rows; Detail = detail;
                FilterNeedsRect = filterNeedsRect; FilterMatchRect = filterMatchRect;
                FilterAllRect = filterAllRect; CloseRect = closeRect;
                FilterNeedsLabel = filterNeedsLabel; FilterMatchLabel = filterMatchLabel;
                FilterAllLabel = filterAllLabel; CloseLabel = closeLabel;
                TitleLabel = titleLabel;
            }
            public GameObject Root { get; }
            public RectTransform PanelRect { get; }
            public RowsView Rows { get; }
            public DetailView Detail { get; }
            public RectTransform FilterNeedsRect { get; }
            public RectTransform FilterMatchRect { get; }
            public RectTransform FilterAllRect { get; }
            public RectTransform CloseRect { get; }
            public TextMeshProUGUI FilterNeedsLabel { get; }
            public TextMeshProUGUI FilterMatchLabel { get; }
            public TextMeshProUGUI FilterAllLabel { get; }
            public TextMeshProUGUI CloseLabel { get; }
            public TextMeshProUGUI TitleLabel { get; }
        }

        private readonly List<RowSlot> _rowPool = new List<RowSlot>(100);
        private readonly List<ModParticipantView> _allRows = new List<ModParticipantView>(100);
        private readonly Dictionary<int, ModParticipantView> _byActor = new Dictionary<int, ModParticipantView>();
        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(ModIntegrityPanel));
        private GameObject? _root => _view?.Root;
        private RectTransform? _panelRect => _view?.PanelRect;
        private RectTransform? _content => _view?.Rows.Content;
        private ScrollRect? _rowScroll => _view?.Rows.Scroll;
        private TextMeshProUGUI? _detailText => _view?.Detail.Text;
        private RectTransform? _detailViewport => _view?.Detail.Viewport;
        private RectTransform? _detailContent => _view?.Detail.Content;
        private ScrollRect? _detailScroll => _view?.Detail.Scroll;
        private RectTransform? _filterNeedsRect => _view?.FilterNeedsRect;
        private RectTransform? _filterMatchRect => _view?.FilterMatchRect;
        private RectTransform? _filterAllRect => _view?.FilterAllRect;
        private RectTransform? _closeRect => _view?.CloseRect;
        private readonly List<ModParticipantView> _visibleRows = new List<ModParticipantView>(100);
        private ModIntegrityPanelFilter _filter;
        private int _selectedActor = -1;
        private int _detailFailedActor = -1;
        private int _lastRenderedDetailActor = -1;
        private int _lastRenderedDetailRevision = -1;

        public string LayerName => WerewolfUIManager.ModIntegrityLayer;
        public bool Exists => GetLiveViewOrNull() != null;
        public bool Visible => _root != null && _root.activeSelf;
        public Action<int>? OnDetailRequested;
        public Action? OnClosed;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            GameObject? partialRoot = null;
            try
            {
            var rootGo = new GameObject("WW_ModIntegrityPanel", typeof(RectTransform));
            partialRoot = rootGo;
            var rootRect = (RectTransform)rootGo.transform;
            rootRect.SetParent(layerRoot, false);
            UiKit.Stretch(rootRect);
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.PanelSortingOrder;
            rootGo.AddComponent<GraphicRaycaster>();

            Image dim = rootGo.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;

            RectTransform panelRect = UiKit.CreateRect(rootRect, "Panel", Vector2.zero, new Vector2(1480f, 850f));
            UiKit.CreateImage(panelRect, "Bg", Vector2.zero, panelRect.sizeDelta, new Color(0.025f, 0.03f, 0.055f, 0.98f));
            TextMeshProUGUI titleLabel = UiKit.CreateText(panelRect, "Title", new Vector2(0f, 392f), new Vector2(900f, 50f),
                Texts.Get(TextId.ModIntegrityPanelTitle), 34f, new Color(1f, 0.9f, 0.58f), TextAlignmentOptions.Center);

            RectTransform filterNeedsRect = BuildButton(panelRect, "FilterNeeds", new Vector2(-600f, 335f), new Vector2(170f, 46f), Texts.Get(TextId.ModIntegrityFilterNeedsReview), out TextMeshProUGUI filterNeedsLabel);
            RectTransform filterMatchRect = BuildButton(panelRect, "FilterMatch", new Vector2(-410f, 335f), new Vector2(150f, 46f), Texts.Get(TextId.ModIntegrityFilterMatch), out TextMeshProUGUI filterMatchLabel);
            RectTransform filterAllRect = BuildButton(panelRect, "FilterAll", new Vector2(-245f, 335f), new Vector2(150f, 46f), Texts.Get(TextId.ModIntegrityFilterAll), out TextMeshProUGUI filterAllLabel);
            RectTransform closeRect = BuildButton(panelRect, "Close", new Vector2(650f, 392f), new Vector2(120f, 44f), Texts.Get(TextId.ModIntegrityButtonClose), out TextMeshProUGUI closeLabel);

            RowsView rows = BuildRowsArea(panelRect);
            DetailView detail = BuildDetailArea(panelRect);

            rootGo.SetActive(false);
            _view = new ViewRefs(rootGo, panelRect, rows, detail, filterNeedsRect,
                filterMatchRect, filterAllRect, closeRect,
                filterNeedsLabel, filterMatchLabel, filterAllLabel, closeLabel,
                titleLabel);
            _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                _view = null;
                _rowPool.Clear();
                if (partialRoot != null) UnityEngine.Object.Destroy(partialRoot);
                throw;
            }
        }

        public void SetRows(IReadOnlyList<ModParticipantView> rows)
        {
            _allRows.Clear();
            _byActor.Clear();
            if (rows != null)
            {
                for (int i = 0; i < rows.Count && _allRows.Count < 100; i++)
                {
                    ModParticipantView row = rows[i];
                    if (row == null || _byActor.ContainsKey(row.Record.Actor)) continue;
                    _allRows.Add(row);
                    _byActor[row.Record.Actor] = row;
                }
            }
            _allRows.Sort(CompareRows);
            if (_selectedActor >= 0 && !_byActor.ContainsKey(_selectedActor)) _selectedActor = -1;
            ApplyFilter();
            RefreshSelectedSummary();
            if (Visible && _byActor.TryGetValue(_selectedActor, out ModParticipantView selected) &&
                selected.Record.Status == ModIntegrityStatus.Difference)
                OnDetailRequested?.Invoke(_selectedActor);
        }

        public void Open(bool preferNeedsReview, int selectedActor = -1)
        {
            if (_root == null) return;
            _filter = preferNeedsReview && HasNeedsReview()
                ? ModIntegrityPanelFilter.NeedsReview
                : ModIntegrityPanelFilter.All;
            _selectedActor = selectedActor >= 0 && _byActor.ContainsKey(selectedActor) ? selectedActor : -1;
            _detailFailedActor = -1;
            _lastRenderedDetailActor = -1;
            _lastRenderedDetailRevision = -1;
            ApplyFilter();
            if (_selectedActor < 0 && _visibleRows.Count > 0) _selectedActor = _visibleRows[0].Record.Actor;
            RefreshSelectedSummary();
            _root.SetActive(true);
            if (_byActor.TryGetValue(_selectedActor, out ModParticipantView selected) &&
                selected.Record.Status == ModIntegrityStatus.Difference)
                OnDetailRequested?.Invoke(_selectedActor);
        }

        public void Close()
        {
            if (_root == null || !_root.activeSelf) return;
            _root.SetActive(false);
            OnClosed?.Invoke();
        }

        public void Tick(ModIntegrityClientState clientState)
        {
            if (!Visible) return;
            UiKit.KeepCursorFree();
            Vector2 mouse = Input.mousePosition;
            bool clicked = Input.GetMouseButtonDown(0);

            if (clicked && Hit(_closeRect!, mouse)) { Close(); return; }
            if (clicked && Hit(_filterNeedsRect!, mouse)) { SetFilter(ModIntegrityPanelFilter.NeedsReview); return; }
            if (clicked && Hit(_filterMatchRect!, mouse)) { SetFilter(ModIntegrityPanelFilter.Match); return; }
            if (clicked && Hit(_filterAllRect!, mouse)) { SetFilter(ModIntegrityPanelFilter.All); return; }

            if (clicked)
            {
                for (int i = 0; i < _visibleRows.Count && i < _rowPool.Count; i++)
                {
                    RowSlot slot = _rowPool[i];
                    if (slot.Rect.gameObject.activeInHierarchy && Hit(slot.Rect, mouse))
                    {
                        SelectActor(slot.Actor);
                        break;
                    }
                }
            }

            RenderDetail(clientState, clicked && Hit(_detailViewport!, mouse));
        }

        public void SetDetailTimedOut(int actor)
        {
            _detailFailedActor = actor;
            _lastRenderedDetailActor = -1;
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null) return;
            view.TitleLabel.text = Texts.Get(TextId.ModIntegrityPanelTitle);
            view.FilterNeedsLabel.text = Texts.Get(TextId.ModIntegrityFilterNeedsReview);
            view.FilterMatchLabel.text = Texts.Get(TextId.ModIntegrityFilterMatch);
            view.FilterAllLabel.text = Texts.Get(TextId.ModIntegrityFilterAll);
            view.CloseLabel.text = Texts.Get(TextId.ModIntegrityButtonClose);

            float scrollPos = _rowScroll != null ? _rowScroll.verticalNormalizedPosition : 1f;
            ApplyFilter();
            if (_rowScroll != null) _rowScroll.verticalNormalizedPosition = scrollPos;
            RefreshSelectedSummary();
            _lastRenderedDetailActor = -1;
            _lastRenderedDetailRevision = -1;
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
            _rowPool.Clear();
            _allRows.Clear();
            _visibleRows.Clear();
            _byActor.Clear();
            _selectedActor = -1;
        }

        private RowsView BuildRowsArea(RectTransform parent)
        {
            RectTransform area = UiKit.CreateRect(parent, "RowsArea", new Vector2(-385f, -25f), new Vector2(680f, 680f));
            UiKit.CreateImage(area, "Bg", Vector2.zero, area.sizeDelta, new Color(0.055f, 0.065f, 0.095f, 0.96f));

            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(area, false);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(14f, 14f);
            scrollRect.offsetMax = new Vector2(-14f, -14f);
            Image scrollImage = scrollGo.AddComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0.001f);
            scrollImage.raycastTarget = true;
            ScrollRect rowScroll = scrollGo.AddComponent<ScrollRect>();
            rowScroll.horizontal = false;
            rowScroll.vertical = true;
            rowScroll.inertia = false;
            rowScroll.movementType = ScrollRect.MovementType.Clamped;
            rowScroll.scrollSensitivity = RowStep;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewport = (RectTransform)viewportGo.transform;
            viewport.SetParent(scrollRect, false);
            UiKit.Stretch(viewport);
            viewportGo.AddComponent<RectMask2D>();
            Image vpImage = viewportGo.AddComponent<Image>();
            vpImage.color = new Color(0f, 0f, 0f, 0.001f);
            vpImage.raycastTarget = true;
            rowScroll.viewport = viewport;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            RectTransform content = (RectTransform)contentGo.transform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 100 * RowStep);
            rowScroll.content = content;

            for (int i = 0; i < 100; i++)
            {
                RectTransform rowRect = UiKit.CreateRect(content, $"Row_{i}", new Vector2(0f, -RowHeight * 0.5f - i * RowStep), new Vector2(630f, RowHeight));
                rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 1f);
                rowRect.pivot = new Vector2(0.5f, 0.5f);
                Image bg = UiKit.CreateImage(rowRect, "Bg", Vector2.zero, rowRect.sizeDelta, new Color(0.1f, 0.11f, 0.15f, 0.96f));
                TextMeshProUGUI label = UiKit.CreateText(rowRect, "Label", Vector2.zero, new Vector2(610f, RowHeight), string.Empty, 20f, Color.white, TextAlignmentOptions.Left);
                label.richText = false;
                rowRect.gameObject.SetActive(false);
                _rowPool.Add(new RowSlot(rowRect, bg, label));
            }
            return new RowsView(content, rowScroll);
        }

        private static DetailView BuildDetailArea(RectTransform parent)
        {
            RectTransform area = UiKit.CreateRect(parent, "DetailArea", new Vector2(370f, -25f), new Vector2(720f, 680f));
            UiKit.CreateImage(area, "Bg", Vector2.zero, area.sizeDelta, new Color(0.045f, 0.05f, 0.08f, 0.98f));

            var scrollGo = new GameObject("DetailScroll", typeof(RectTransform));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(area, false);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(20f, 20f);
            scrollRect.offsetMax = new Vector2(-20f, -20f);
            Image scrollImage = scrollGo.AddComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0.001f);
            scrollImage.raycastTarget = true;
            ScrollRect detailScroll = scrollGo.AddComponent<ScrollRect>();
            detailScroll.horizontal = false;
            detailScroll.vertical = true;
            detailScroll.inertia = false;
            detailScroll.movementType = ScrollRect.MovementType.Clamped;
            detailScroll.scrollSensitivity = 42f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            RectTransform detailViewport = (RectTransform)viewportGo.transform;
            detailViewport.SetParent(scrollRect, false);
            UiKit.Stretch(detailViewport);
            viewportGo.AddComponent<RectMask2D>();
            Image image = viewportGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;
            detailScroll.viewport = detailViewport;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            RectTransform detailContent = (RectTransform)contentGo.transform;
            detailContent.SetParent(detailViewport, false);
            detailContent.anchorMin = new Vector2(0f, 1f);
            detailContent.anchorMax = new Vector2(1f, 1f);
            detailContent.pivot = new Vector2(0.5f, 1f);
            detailContent.anchoredPosition = Vector2.zero;
            detailContent.sizeDelta = new Vector2(0f, 610f);
            detailScroll.content = detailContent;

            TextMeshProUGUI detailText = UiKit.CreateText(detailContent, "Detail", Vector2.zero, new Vector2(650f, 610f), string.Empty, 20f, Color.white, TextAlignmentOptions.TopLeft);
            RectTransform textRect = detailText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            detailText.richText = false;
            detailText.textWrappingMode = TextWrappingModes.Normal;
            detailText.overflowMode = TextOverflowModes.Overflow;
            return new DetailView(detailText, detailViewport, detailContent, detailScroll);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;
            _view = null;
            _rowPool.Clear();
            _allRows.Clear();
            _visibleRows.Clear();
            _byActor.Clear();
            _selectedActor = -1;
            return null;
        }

        private RectTransform BuildButton(Transform parent, string name, Vector2 position, Vector2 size, string text,
            out TextMeshProUGUI label)
        {
            Image bg = UiKit.CreateImage(parent, name, position, size, new Color(0.18f, 0.2f, 0.28f, 0.98f));
            bg.raycastTarget = false;
            label = UiKit.CreateText(bg.rectTransform, "Label", Vector2.zero, size, text, 20f, Color.white, TextAlignmentOptions.Center);
            label.richText = false;
            return bg.rectTransform;
        }

        private void SetFilter(ModIntegrityPanelFilter filter)
        {
            if (_filter == filter) return;
            _filter = filter;
            ApplyFilter();
            bool selectedVisible = false;
            for (int i = 0; i < _visibleRows.Count; i++)
                if (_visibleRows[i].Record.Actor == _selectedActor) { selectedVisible = true; break; }
            if (!selectedVisible) _selectedActor = _visibleRows.Count > 0 ? _visibleRows[0].Record.Actor : -1;
            ApplyFilter();
            RefreshSelectedSummary();
            if (_byActor.TryGetValue(_selectedActor, out ModParticipantView selected) &&
                selected.Record.Status == ModIntegrityStatus.Difference)
                OnDetailRequested?.Invoke(_selectedActor);
        }

        private void ApplyFilter()
        {
            _visibleRows.Clear();
            for (int i = 0; i < _allRows.Count; i++)
            {
                ModParticipantView row = _allRows[i];
                if (MatchesFilter(row.Record.Status)) _visibleRows.Add(row);
            }

            for (int i = 0; i < _rowPool.Count; i++)
            {
                RowSlot slot = _rowPool[i];
                if (i >= _visibleRows.Count)
                {
                    slot.Actor = -1;
                    slot.Rect.gameObject.SetActive(false);
                    continue;
                }
                ModParticipantView row = _visibleRows[i];
                slot.Actor = row.Record.Actor;
                slot.Label.text = FormatRow(row);
                slot.Background.color = RowColor(row.Record.Status, row.Record.Actor == _selectedActor);
                slot.Rect.gameObject.SetActive(true);
            }
            if (_content != null)
                _content.sizeDelta = new Vector2(0f, Math.Max(640f, _visibleRows.Count * RowStep));
            if (_rowScroll != null) _rowScroll.verticalNormalizedPosition = 1f;
        }

        private bool MatchesFilter(ModIntegrityStatus status)
        {
            switch (_filter)
            {
                case ModIntegrityPanelFilter.NeedsReview:
                    return status == ModIntegrityStatus.Pending || status == ModIntegrityStatus.Difference || status == ModIntegrityStatus.Unavailable;
                case ModIntegrityPanelFilter.Match:
                    return status == ModIntegrityStatus.Baseline || status == ModIntegrityStatus.Match;
                default:
                    return true;
            }
        }

        private void SelectActor(int actor)
        {
            _selectedActor = actor;
            _detailFailedActor = -1;
            _lastRenderedDetailActor = -1;
            ApplyFilter();
            RefreshSelectedSummary();
            if (_byActor.TryGetValue(actor, out ModParticipantView row) && row.Record.Status == ModIntegrityStatus.Difference)
                OnDetailRequested?.Invoke(actor);
        }

        private void RefreshSelectedSummary()
        {
            if (_detailText == null) return;
            if (!_byActor.TryGetValue(_selectedActor, out ModParticipantView row))
            {
                SetDetailText(string.Empty);
                return;
            }
            SetDetailText(FormatSummary(row));
        }

        private void RenderDetail(ModIntegrityClientState state, bool detailClicked)
        {
            if (!_byActor.TryGetValue(_selectedActor, out ModParticipantView row)) return;
            if (row.Record.Status != ModIntegrityStatus.Difference)
            {
                if (_lastRenderedDetailActor != _selectedActor)
                {
                    SetDetailText(FormatSummary(row));
                    _lastRenderedDetailActor = _selectedActor;
                }
                return;
            }

            if (state != null && state.TryGetDetail(_selectedActor, out ModIntegrityDetail? detail))
            {
                if (_lastRenderedDetailActor == detail.Actor && _lastRenderedDetailRevision == detail.Revision) return;
                var text = new StringBuilder();
                text.Append(row.DisplayName).Append('\n').Append(FormatStatus(row.Record.Status)).Append("\n\n");
                for (int i = 0; i < detail.Differences.Count; i++)
                {
                    ModDifference difference = detail.Differences[i];
                    switch (difference.Kind)
                    {
                        case ModDifferenceKind.Missing:
                            text.AppendLine(Texts.Format(TextId.ModIntegrityDetailMissingFormat, difference.Name, difference.Guid));
                            break;
                        case ModDifferenceKind.Extra:
                            text.AppendLine(Texts.Format(TextId.ModIntegrityDetailExtraFormat, difference.Name, difference.Guid));
                            break;
                        case ModDifferenceKind.Version:
                            text.AppendLine(Texts.Format(TextId.ModIntegrityDetailVersionFormat, difference.Name, difference.BaselineValue, difference.ParticipantValue));
                            break;
                        case ModDifferenceKind.Content:
                            text.AppendLine(Texts.Format(TextId.ModIntegrityDetailContentFormat, difference.Name, difference.BaselineValue, difference.ParticipantValue));
                            break;
                    }
                }
                SetDetailText(text.ToString());
                _lastRenderedDetailActor = detail.Actor;
                _lastRenderedDetailRevision = detail.Revision;
                return;
            }

            if (_detailFailedActor == _selectedActor)
            {
                SetDetailText(FormatSummary(row) + "\n\n" + Texts.Get(TextId.ModIntegrityDetailFailed));
                if (detailClicked)
                {
                    _detailFailedActor = -1;
                    SetDetailText(FormatSummary(row) + "\n\n" + Texts.Get(TextId.ModIntegrityDetailLoading));
                    OnDetailRequested?.Invoke(_selectedActor);
                }
                return;
            }

            SetDetailText(FormatSummary(row) + "\n\n" + Texts.Get(TextId.ModIntegrityDetailLoading));
        }

        private void SetDetailText(string text)
        {
            if (_detailText == null) return;
            _detailText.text = text ?? string.Empty;
            _detailText.ForceMeshUpdate();
            float height = Math.Max(610f, _detailText.preferredHeight + 24f);
            _detailText.rectTransform.sizeDelta = new Vector2(650f, height);
            if (_detailContent != null) _detailContent.sizeDelta = new Vector2(0f, height);
            if (_detailScroll != null) _detailScroll.verticalNormalizedPosition = 1f;
        }

        private bool HasNeedsReview()
        {
            for (int i = 0; i < _allRows.Count; i++)
            {
                ModIntegrityStatus status = _allRows[i].Record.Status;
                if (status == ModIntegrityStatus.Pending || status == ModIntegrityStatus.Difference || status == ModIntegrityStatus.Unavailable)
                    return true;
            }
            return false;
        }

        private static int CompareRows(ModParticipantView a, ModParticipantView b)
        {
            int rankA = StatusRank(a.Record.Status);
            int rankB = StatusRank(b.Record.Status);
            int compare = rankA.CompareTo(rankB);
            return compare != 0 ? compare : a.Record.Actor.CompareTo(b.Record.Actor);
        }

        private static int StatusRank(ModIntegrityStatus status)
        {
            switch (status)
            {
                case ModIntegrityStatus.Baseline: return 0;
                case ModIntegrityStatus.Unavailable: return 1;
                case ModIntegrityStatus.Difference: return 2;
                case ModIntegrityStatus.Pending: return 3;
                default: return 4;
            }
        }

        private static string FormatRow(ModParticipantView row)
        {
            ModParticipantRecord record = row.Record;
            string icon = StatusIcon(record.Status);
            string status = FormatStatus(record.Status);
            string suffix = record.Status == ModIntegrityStatus.Difference
                ? $"  -{record.Summary.Missing} +{record.Summary.Extra} V{record.Summary.Version} C{record.Summary.Content}"
                : record.Status == ModIntegrityStatus.Unavailable ? "  " + FormatReason(record.UnavailableReason) : string.Empty;
            return $"{icon}  {row.DisplayName}  [{status}]{suffix}";
        }

        private static string FormatSummary(ModParticipantView row)
        {
            ModParticipantRecord record = row.Record;
            string text = row.DisplayName + "\n" + StatusIcon(record.Status) + " " + FormatStatus(record.Status);
            if (record.Status == ModIntegrityStatus.Unavailable) text += "\n" + FormatReason(record.UnavailableReason);
            if (record.Status == ModIntegrityStatus.Difference)
                text += $"\n不足 {record.Summary.Missing} / 追加 {record.Summary.Extra} / Version {record.Summary.Version} / 内容 {record.Summary.Content}";
            return text;
        }

        private static string StatusIcon(ModIntegrityStatus status)
        {
            switch (status)
            {
                case ModIntegrityStatus.Baseline: return "◆";
                case ModIntegrityStatus.Match: return "✓";
                case ModIntegrityStatus.Difference: return "!";
                case ModIntegrityStatus.Unavailable: return "×";
                default: return "?";
            }
        }

        private static string FormatStatus(ModIntegrityStatus status)
        {
            switch (status)
            {
                case ModIntegrityStatus.Baseline: return Texts.Get(TextId.ModIntegrityStatusBaseline);
                case ModIntegrityStatus.Match: return Texts.Get(TextId.ModIntegrityStatusMatch);
                case ModIntegrityStatus.Difference: return Texts.Get(TextId.ModIntegrityStatusDifference);
                case ModIntegrityStatus.Unavailable: return Texts.Get(TextId.ModIntegrityStatusUnavailable);
                default: return Texts.Get(TextId.ModIntegrityStatusPending);
            }
        }

        private static string FormatReason(ModUnavailableReason reason)
        {
            switch (reason)
            {
                case ModUnavailableReason.NoResponse: return Texts.Get(TextId.ModIntegrityReasonNoResponse);
                case ModUnavailableReason.UnsupportedProtocol: return Texts.Get(TextId.ModIntegrityReasonUnsupportedProtocol);
                case ModUnavailableReason.TooLarge: return Texts.Get(TextId.ModIntegrityReasonTooLarge);
                case ModUnavailableReason.ManifestCollectionFailed: return Texts.Get(TextId.ModIntegrityReasonCollectionFailed);
                default: return Texts.Get(TextId.ModIntegrityReasonInvalidPayload);
            }
        }

        private static Color RowColor(ModIntegrityStatus status, bool selected)
        {
            Color baseColor;
            switch (status)
            {
                case ModIntegrityStatus.Baseline: baseColor = new Color(0.18f, 0.18f, 0.28f, 0.98f); break;
                case ModIntegrityStatus.Match: baseColor = new Color(0.06f, 0.22f, 0.13f, 0.98f); break;
                case ModIntegrityStatus.Difference: baseColor = new Color(0.35f, 0.24f, 0.04f, 0.98f); break;
                case ModIntegrityStatus.Unavailable: baseColor = new Color(0.35f, 0.05f, 0.07f, 0.98f); break;
                default: baseColor = new Color(0.15f, 0.16f, 0.2f, 0.98f); break;
            }
            return selected ? Color.Lerp(baseColor, Color.white, 0.24f) : baseColor;
        }

        private static bool Hit(RectTransform rect, Vector2 point)
        {
            return rect != null && rect.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(rect, point, null);
        }
    }
}
