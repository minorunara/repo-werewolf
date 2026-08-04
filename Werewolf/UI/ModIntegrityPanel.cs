using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

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
            public RectTransform Rect;
            public Image Background;
            public TextMeshProUGUI Label;
            public int Actor;
        }

        private const float RowHeight = 44f;
        private const float RowStep = 46f;
        private readonly List<RowSlot> _rowPool = new List<RowSlot>(100);
        private readonly List<ModParticipantView> _allRows = new List<ModParticipantView>(100);
        private readonly Dictionary<int, ModParticipantView> _byActor = new Dictionary<int, ModParticipantView>();
        private GameObject _root;
        private RectTransform _panelRect;
        private RectTransform _content;
        private ScrollRect _rowScroll;
        private TextMeshProUGUI _detailText;
        private RectTransform _detailViewport;
        private RectTransform _detailContent;
        private ScrollRect _detailScroll;
        private RectTransform _filterNeedsRect;
        private RectTransform _filterMatchRect;
        private RectTransform _filterAllRect;
        private RectTransform _closeRect;
        private readonly List<ModParticipantView> _visibleRows = new List<ModParticipantView>(100);
        private ModIntegrityPanelFilter _filter;
        private int _selectedActor = -1;
        private int _detailFailedActor = -1;
        private int _lastRenderedDetailActor = -1;
        private int _lastRenderedDetailRevision = -1;

        public string LayerName => WerewolfUIManager.ModIntegrityLayer;
        public bool Exists => _root != null;
        public bool Visible => _root != null && _root.activeSelf;
        public Action<int> OnDetailRequested;
        public Action OnClosed;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var rootGo = new GameObject("WW_ModIntegrityPanel", typeof(RectTransform));
            var rootRect = (RectTransform)rootGo.transform;
            rootRect.SetParent(layerRoot, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.PanelSortingOrder;
            rootGo.AddComponent<GraphicRaycaster>();
            _root = rootGo;

            Image dim = rootGo.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;

            _panelRect = UiKit.CreateRect(rootRect, "Panel", Vector2.zero, new Vector2(1480f, 850f));
            UiKit.CreateImage(_panelRect, "Bg", Vector2.zero, _panelRect.sizeDelta, new Color(0.025f, 0.03f, 0.055f, 0.98f));
            UiKit.CreateText(_panelRect, "Title", new Vector2(0f, 392f), new Vector2(900f, 50f),
                Texts.Get(TextId.ModIntegrityPanelTitle), 34f, new Color(1f, 0.9f, 0.58f), TextAlignmentOptions.Center);

            _filterNeedsRect = BuildButton(_panelRect, "FilterNeeds", new Vector2(-600f, 335f), new Vector2(170f, 46f), Texts.Get(TextId.ModIntegrityFilterNeedsReview));
            _filterMatchRect = BuildButton(_panelRect, "FilterMatch", new Vector2(-410f, 335f), new Vector2(150f, 46f), Texts.Get(TextId.ModIntegrityFilterMatch));
            _filterAllRect = BuildButton(_panelRect, "FilterAll", new Vector2(-245f, 335f), new Vector2(150f, 46f), Texts.Get(TextId.ModIntegrityFilterAll));
            _closeRect = BuildButton(_panelRect, "Close", new Vector2(650f, 392f), new Vector2(120f, 44f), Texts.Get(TextId.ModIntegrityButtonClose));

            BuildRowsArea(_panelRect);
            BuildDetailArea(_panelRect);

            TextMeshProUGUI footer = UiKit.CreateText(_panelRect, "Disclaimer", new Vector2(0f, -398f), new Vector2(1400f, 36f),
                Texts.Get(TextId.ModIntegrityDisclaimer), 18f, new Color(0.74f, 0.74f, 0.8f), TextAlignmentOptions.Center);
            footer.richText = false;
            _root.SetActive(false);
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

            if (clicked && Hit(_closeRect, mouse)) { Close(); return; }
            if (clicked && Hit(_filterNeedsRect, mouse)) { SetFilter(ModIntegrityPanelFilter.NeedsReview); return; }
            if (clicked && Hit(_filterMatchRect, mouse)) { SetFilter(ModIntegrityPanelFilter.Match); return; }
            if (clicked && Hit(_filterAllRect, mouse)) { SetFilter(ModIntegrityPanelFilter.All); return; }

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

            RenderDetail(clientState, clicked && Hit(_detailViewport, mouse));
        }

        public void SetDetailTimedOut(int actor)
        {
            _detailFailedActor = actor;
            _lastRenderedDetailActor = -1;
        }

        public void Destroy()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _panelRect = null;
            _content = null;
            _rowScroll = null;
            _detailText = null;
            _detailViewport = null;
            _detailContent = null;
            _detailScroll = null;
            _rowPool.Clear();
            _allRows.Clear();
            _visibleRows.Clear();
            _byActor.Clear();
            _selectedActor = -1;
        }

        private void BuildRowsArea(RectTransform parent)
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
            _rowScroll = scrollGo.AddComponent<ScrollRect>();
            _rowScroll.horizontal = false;
            _rowScroll.vertical = true;
            _rowScroll.inertia = false;
            _rowScroll.movementType = ScrollRect.MovementType.Clamped;
            _rowScroll.scrollSensitivity = RowStep;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewport = (RectTransform)viewportGo.transform;
            viewport.SetParent(scrollRect, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();
            Image vpImage = viewportGo.AddComponent<Image>();
            vpImage.color = new Color(0f, 0f, 0f, 0.001f);
            vpImage.raycastTarget = true;
            _rowScroll.viewport = viewport;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            _content = (RectTransform)contentGo.transform;
            _content.SetParent(viewport, false);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 100 * RowStep);
            _rowScroll.content = _content;

            for (int i = 0; i < 100; i++)
            {
                RectTransform rowRect = UiKit.CreateRect(_content, $"Row_{i}", new Vector2(0f, -RowHeight * 0.5f - i * RowStep), new Vector2(630f, RowHeight));
                rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 1f);
                rowRect.pivot = new Vector2(0.5f, 0.5f);
                Image bg = UiKit.CreateImage(rowRect, "Bg", Vector2.zero, rowRect.sizeDelta, new Color(0.1f, 0.11f, 0.15f, 0.96f));
                TextMeshProUGUI label = UiKit.CreateText(rowRect, "Label", Vector2.zero, new Vector2(610f, RowHeight), string.Empty, 20f, Color.white, TextAlignmentOptions.Left);
                label.richText = false;
                rowRect.gameObject.SetActive(false);
                _rowPool.Add(new RowSlot { Rect = rowRect, Background = bg, Label = label, Actor = -1 });
            }
        }

        private void BuildDetailArea(RectTransform parent)
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
            _detailScroll = scrollGo.AddComponent<ScrollRect>();
            _detailScroll.horizontal = false;
            _detailScroll.vertical = true;
            _detailScroll.inertia = false;
            _detailScroll.movementType = ScrollRect.MovementType.Clamped;
            _detailScroll.scrollSensitivity = 42f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            _detailViewport = (RectTransform)viewportGo.transform;
            _detailViewport.SetParent(scrollRect, false);
            _detailViewport.anchorMin = Vector2.zero;
            _detailViewport.anchorMax = Vector2.one;
            _detailViewport.offsetMin = Vector2.zero;
            _detailViewport.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();
            Image image = viewportGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;
            _detailScroll.viewport = _detailViewport;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            _detailContent = (RectTransform)contentGo.transform;
            _detailContent.SetParent(_detailViewport, false);
            _detailContent.anchorMin = new Vector2(0f, 1f);
            _detailContent.anchorMax = new Vector2(1f, 1f);
            _detailContent.pivot = new Vector2(0.5f, 1f);
            _detailContent.anchoredPosition = Vector2.zero;
            _detailContent.sizeDelta = new Vector2(0f, 610f);
            _detailScroll.content = _detailContent;

            _detailText = UiKit.CreateText(_detailContent, "Detail", Vector2.zero, new Vector2(650f, 610f), string.Empty, 20f, Color.white, TextAlignmentOptions.TopLeft);
            RectTransform textRect = _detailText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            _detailText.richText = false;
            _detailText.enableWordWrapping = true;
            _detailText.overflowMode = TextOverflowModes.Overflow;
        }

        private RectTransform BuildButton(Transform parent, string name, Vector2 position, Vector2 size, string text)
        {
            Image bg = UiKit.CreateImage(parent, name, position, size, new Color(0.18f, 0.2f, 0.28f, 0.98f));
            bg.raycastTarget = false;
            TextMeshProUGUI label = UiKit.CreateText(bg.rectTransform, "Label", Vector2.zero, size, text, 20f, Color.white, TextAlignmentOptions.Center);
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

            if (state != null && state.TryGetDetail(_selectedActor, out ModIntegrityDetail detail))
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
