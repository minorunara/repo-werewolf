using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public interface ITargetSelectionSource
    {
        IReadOnlyList<int> TargetActors { get; }

        bool AllowSkip { get; }

        bool CanConfirm(int localActor);

        void Confirm(int localActor, int targetActor);

        int CurrentSelection { get; }
    }

    public sealed class VotePanel : IClientPanel
    {
        public const int SkipTarget = -1;

        private const float GridTopY = 230f;
        private static readonly Vector2 PanelSize = new Vector2(1280f, 790f);
        private static readonly Vector2 PanelPos = new Vector2(0f, -50f);

        private const float SlideStartOffsetY = -1000f;

        private static readonly Color PanelBgColor = new Color(0.02f, 0.02f, 0.05f, 0.85f);
        private static readonly Color HeaderBgColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        private static readonly Color TitleTextColor = new Color(1f, 0.9f, 0.6f, 1f);
        private static readonly Color TimeNormalColor = new Color(0.95f, 0.95f, 1f, 0.95f);
        private static readonly Color TimeUrgentColor = new Color(1f, 0.35f, 0.35f);
        private static readonly Color ResultExecutedColor = new Color(1f, 0.4f, 0.4f);
        private static readonly Color ResultNoneColor = new Color(0.7f, 0.85f, 1f);
        private static readonly Color NeutralButtonEnabledColor = new Color(0.26f, 0.26f, 0.3f, 0.9f);
        private static readonly Color NeutralButtonDisabledColor = new Color(0.15f, 0.15f, 0.17f, 0.6f);
        private static readonly Color NeutralLabelDisabledColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color NeutralButtonHoverColor = new Color(0.42f, 0.42f, 0.48f, 1f);
        private static readonly ButtonPalette NeutralPalette = new ButtonPalette(
            NeutralButtonEnabledColor, NeutralButtonHoverColor, NeutralButtonDisabledColor, NeutralLabelDisabledColor);

        public event Action<int> OnVoteSubmit;

        private readonly List<VoteRow> _rows = new List<VoteRow>();
        private readonly Dictionary<int, VoteRow> _rowsByActor = new Dictionary<int, VoteRow>();

        private GameObject _root;
        private RectTransform _rootRect;
        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _resultBanner;
        private Image _skipButtonBg;
        private TextMeshProUGUI _skipLabel;
        private TextMeshProUGUI _skipCountLabel;
        private Image _pagePrevBg;
        private Image _pageNextBg;
        private TextMeshProUGUI _pageLabel;

        private ITargetSelectionSource _selectionSource;
        private DefaultVoteTargetProvider _defaultProvider;

        private Func<int, Role?> _teamMarkerRole;

        private MeetingClientState _lastState;
        private int _localActor = -1;
        private int _page;
        private bool _pendingSend;
        private bool _resultShown;
        private bool _countsApplied;
        private long _timeOverrideEndUnixMs;

        private const int ArmedNone = int.MinValue;
        private int _armedTarget = ArmedNone;
        private bool _skipHover;
        private bool _skipEnabled;

        private int _myVoteTarget = ArmedNone;

        private bool _slideActive;
        private bool _reorderApplied;

        private const long ScatterIntroMs = 900;
        private const long ScatterShuffleMs = 3200;
        private const long ScatterFadeMs = 700;
        private const long ScatterSwapIntervalMs = 90;
        private const long ScatterSettledReadMs = 4400;

        public const long ScatterRevealHoldRequiredMs =
            ScatterIntroMs + ScatterShuffleMs + ScatterFadeMs + ScatterSettledReadMs;

        private bool _scatterActive;
        private bool _scatterSettled;
        private bool _scatterMusicStopped;
        private long _scatterStartUnixMs;
        private long _scatterNextSwapUnixMs;
        private readonly Dictionary<int, char> _scatterLetterByActor = new Dictionary<int, char>();
        private char[] _scatterLetters = Array.Empty<char>();
        private readonly System.Random _scatterRng = new System.Random();
        private Action<float> _scatterPumpMusic;
        private Action _scatterStopMusic;
        private Action _scatterPlayJingle;
        private GameObject _scatterSloganRoot;

        public bool Exists => _root != null;

        public string LayerName => WerewolfUIManager.MeetingLayer;

        public Func<Vector2, bool> IsPointerBlocked;

        public int CurrentPage => _page;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            RectTransform rect = UiKit.CreateRect(layerRoot, "WW_VotePanel", PanelPos, PanelSize);
            _root = rect.gameObject;
            _rootRect = rect;

            UiKit.CreateImage(rect, "Bg", Vector2.zero, PanelSize, PanelBgColor);

            UiKit.CreateImage(rect, "HeaderBg", new Vector2(0f, 358f), new Vector2(960f, 56f), HeaderBgColor);
            UiKit.CreateText(rect, "Title", new Vector2(0f, 358f), new Vector2(400f, 44f),
                Texts.Get(TextId.VoteMeetingTitle), 34f, TitleTextColor, TextAlignmentOptions.Center);

            _timeText = UiKit.CreateText(rect, "Time", new Vector2(390f, 358f), new Vector2(200f, 44f),
                "--:--", 30f, TimeNormalColor, TextAlignmentOptions.Center);

            _resultBanner = UiKit.CreateText(rect, "Result", new Vector2(0f, 318f), new Vector2(960f, 36f),
                "", 26f, ResultExecutedColor, TextAlignmentOptions.Center);
            _resultBanner.gameObject.SetActive(false);

            _skipButtonBg = UiKit.CreateImage(rect, "SkipButton", new Vector2(0f, -352f),
                new Vector2(220f, 50f), NeutralButtonEnabledColor);
            _skipLabel = UiKit.CreateText(_skipButtonBg.rectTransform, "Label", Vector2.zero,
                new Vector2(220f, 50f), Texts.Get(TextId.VoteSkipLabel), 24f, Color.white, TextAlignmentOptions.Center);
            Sprite skipIcon = AssetCatalog.GetSprite("icon_btn_skip");
            if (skipIcon != null)
            {
                Image icon = UiKit.CreateImage(_skipButtonBg.rectTransform, "Icon",
                    new Vector2(-220f / 2f + 30f / 2f + 8f, 0f),
                    new Vector2(30f, 30f), Color.white);
                icon.sprite = skipIcon;
                icon.preserveAspect = true;
            }
            _skipCountLabel = UiKit.CreateText(rect, "SkipCount", new Vector2(175f, -352f), new Vector2(110f, 50f),
                "", 22f, TitleTextColor, TextAlignmentOptions.MidlineLeft);
            _skipCountLabel.gameObject.SetActive(false);

            _pagePrevBg = UiKit.CreateImage(rect, "PagePrev", new Vector2(-420f, -352f),
                new Vector2(60f, 50f), NeutralButtonEnabledColor);
            UiKit.CreateText(_pagePrevBg.rectTransform, "Label", Vector2.zero, new Vector2(60f, 50f),
                "◀", 24f, Color.white, TextAlignmentOptions.Center);
            _pageNextBg = UiKit.CreateImage(rect, "PageNext", new Vector2(420f, -352f),
                new Vector2(60f, 50f), NeutralButtonEnabledColor);
            UiKit.CreateText(_pageNextBg.rectTransform, "Label", Vector2.zero, new Vector2(60f, 50f),
                "▶", 24f, Color.white, TextAlignmentOptions.Center);
            _pageLabel = UiKit.CreateText(rect, "PageLabel", new Vector2(-340f, -352f), new Vector2(90f, 50f),
                "", 22f, Color.white, TextAlignmentOptions.Center);
            SetPagerVisible(false);

            WLog.Line("vote_panel_built", secret: false);
        }

        public void BeginMeeting(IReadOnlyList<WPlayer> roster, Func<int, PlayerAvatar> resolveAvatar, int localActor)
        {
            if (_root == null) return;
            ClearRows();
            _localActor = localActor;
            _page = 0;
            _pendingSend = false;
            _resultShown = false;
            _countsApplied = false;
            _armedTarget = ArmedNone;
            _skipHover = false;
            _myVoteTarget = ArmedNone;
            _slideActive = true;
            _reorderApplied = false;
            if (_rootRect != null)
            {
                _rootRect.anchoredPosition = new Vector2(PanelPos.x, PanelPos.y + SlideStartOffsetY);
            }
            _resultBanner.gameObject.SetActive(false);
            _skipCountLabel.gameObject.SetActive(false);

            if (_selectionSource == null || _defaultProvider != null)
            {
                _defaultProvider = new DefaultVoteTargetProvider(this);
                _selectionSource = _defaultProvider;
            }

            if (roster != null)
            {
                foreach (WPlayer player in roster)
                {
                    if (player == null) continue;
                    VoteRow row = VoteRow.Build(_root.transform, player, resolveAvatar, VoteRowGrid.RowSize);
                    _rows.Add(row);
                    _rowsByActor[player.ActorNumber] = row;
                }
            }
            LayoutRows();
            WLog.Line("vote_panel_begin", secret: false,
                ("rows", _rows.Count), ("localActor", localActor), ("pages", PageCount));
        }

        public void SetSelectionSource(ITargetSelectionSource source)
        {
            if (source == null)
            {
                _defaultProvider = new DefaultVoteTargetProvider(this);
                _selectionSource = _defaultProvider;
            }
            else
            {
                _selectionSource = source;
                _defaultProvider = null;
            }
        }

        public void SetTeamMarkerProvider(Func<int, Role?> markedRole)
        {
            _teamMarkerRole = markedRole;
            if (markedRole == null)
            {
                foreach (VoteRow row in _rows) row.SetTeamMarker(null);
            }
        }

        public void ShowStatusBanner(string text)
        {
            _resultShown = true;
            if (_resultBanner == null) return;
            _resultBanner.text = text ?? string.Empty;
            _resultBanner.gameObject.SetActive(true);
        }

        public void SetTimeOverride(long endUnixMs)
        {
            _timeOverrideEndUnixMs = endUnixMs;
        }

        public void NotifyVoteProgress()
        {
            if (!_pendingSend || _lastState == null) return;
            if (!ContainsInt(_lastState.VotedActors, _localActor))
            {
                _pendingSend = false;
                WLog.Line("vote_panel_reenabled", secret: false, ("localActor", _localActor));
            }
        }

        public void Tick(MeetingClientState state, long nowUnixMs)
        {
            if (_root == null || state == null) return;
            _lastState = state;
            try
            {
                UpdateSlide(state, nowUnixMs);
                ApplyInitialSortIfNeeded(state);
                UpdateTime(state, nowUnixMs);
                UpdateRows(state);
                UpdateResult(state);
                UpdateScatterReveal(nowUnixMs);
                HandleInput(state);
            }
            catch (Exception e)
            {
                WLog.Line("vote_panel_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void EndMeeting()
        {
            StopScatterReveal();
            ClearRows();
            _pendingSend = false;
            _resultShown = false;
            _countsApplied = false;
            _armedTarget = ArmedNone;
            _skipHover = false;
            _myVoteTarget = ArmedNone;
            _slideActive = false;
            _reorderApplied = false;
            _timeOverrideEndUnixMs = 0;
            _lastState = null;
            if (_resultBanner != null) _resultBanner.gameObject.SetActive(false);
            if (_skipCountLabel != null) _skipCountLabel.gameObject.SetActive(false);
        }

        public void Destroy()
        {
            StopScatterReveal();
            _scatterSloganRoot = null;
            ClearRows();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _lastState = null;
        }

        private int PageCount => _rows.Count == 0 ? 1 : (_rows.Count + VoteRowGrid.RowsPerPage - 1) / VoteRowGrid.RowsPerPage;

        private bool SelectionLocked => _resultShown && _defaultProvider != null;

        private void ClearRows()
        {
            foreach (VoteRow row in _rows) row.Destroy();
            _rows.Clear();
            _rowsByActor.Clear();
        }

        private void LayoutRows()
        {
            int pageCount = PageCount;
            if (_page >= pageCount) _page = pageCount - 1;
            if (_page < 0) _page = 0;

            for (int i = 0; i < _rows.Count; i++)
            {
                VoteRow row = _rows[i];
                RectTransform rect = row.Root;
                if (rect == null) continue;

                int page = i / VoteRowGrid.RowsPerPage;
                bool onPage = page == _page;
                rect.gameObject.SetActive(onPage);
                if (!onPage) continue;

                rect.anchoredPosition = VoteRowGrid.Position(i % VoteRowGrid.RowsPerPage, GridTopY);
            }

            bool multiPage = pageCount > 1;
            SetPagerVisible(multiPage);
            if (multiPage && _pageLabel != null)
            {
                _pageLabel.text = $"{_page + 1}/{pageCount}";
            }
        }

        private void SetPagerVisible(bool visible)
        {
            if (_pagePrevBg != null) _pagePrevBg.gameObject.SetActive(visible);
            if (_pageNextBg != null) _pageNextBg.gameObject.SetActive(visible);
            if (_pageLabel != null) _pageLabel.gameObject.SetActive(visible);
        }

        private void UpdateSlide(MeetingClientState state, long nowUnixMs)
        {
            if (!_slideActive || _rootRect == null) return;
            float eased = (float)state.GaugeMoveProgress(nowUnixMs);
            if (eased >= 1f) { eased = 1f; _slideActive = false; }
            float startY = PanelPos.y + SlideStartOffsetY;
            float y = startY + (PanelPos.y - startY) * eased;
            _rootRect.anchoredPosition = new Vector2(PanelPos.x, y);
        }

        private void ApplyInitialSortIfNeeded(MeetingClientState state)
        {
            if (_reorderApplied) return;
            _reorderApplied = true;
            if (_rows.Count <= 1) return;
            _rows.Sort((left, right) => state.CompareRowOrder(left.ActorNumber, right.ActorNumber));
            LayoutRows();
        }

        private void UpdateTime(MeetingClientState state, long nowUnixMs)
        {
            if (_timeText == null) return;
            long remainMs = _timeOverrideEndUnixMs > 0
                ? Math.Max(0L, _timeOverrideEndUnixMs - nowUnixMs)
                : state.RemainingMs(nowUnixMs);
            long totalSec = (remainMs + 999) / 1000;
            _timeText.text = $"{totalSec / 60:00}:{totalSec % 60:00}";
            _timeText.color = totalSec <= 10 ? TimeUrgentColor : TimeNormalColor;
        }

        private void UpdateRows(MeetingClientState state)
        {
            bool canConfirm = _selectionSource != null && _selectionSource.CanConfirm(_localActor);
            IReadOnlyList<int> targets = _selectionSource != null ? _selectionSource.TargetActors : null;
            int selectedActor = _selectionSource != null ? _selectionSource.CurrentSelection : SkipTarget;

            if (_armedTarget != ArmedNone)
            {
                bool armedValid = _defaultProvider != null && canConfirm && !SelectionLocked &&
                    (_armedTarget == SkipTarget
                        ? _selectionSource.AllowSkip
                        : ContainsActor(targets, _armedTarget));
                if (!armedValid) _armedTarget = ArmedNone;
            }

            bool myVoteVisible = _myVoteTarget != ArmedNone &&
                (_pendingSend || ContainsInt(state.VotedActors, _localActor));

            foreach (VoteRow row in _rows)
            {
                RowStatus status = state.GetRowStatus(row.ActorNumber);
                row.SetStatus(status);
                row.SetVoted(ContainsInt(state.VotedActors, row.ActorNumber));

                bool isTarget = status == RowStatus.Alive && !SelectionLocked && ContainsActor(targets, row.ActorNumber);
                row.SetVoteButtonVisible(isTarget);
                if (isTarget) row.SetVoteButtonEnabled(canConfirm);
                row.SetSelected(isTarget && row.ActorNumber == selectedActor);
                row.SetArmed(isTarget && row.ActorNumber == _armedTarget);
                row.SetMyVoteMarker(!_scatterActive && myVoteVisible && row.ActorNumber == _myVoteTarget);
                row.SetTeamMarker(_teamMarkerRole != null ? _teamMarkerRole(row.ActorNumber) : null);
                row.SetHostMarker(!_scatterActive && state.CallerActor == row.ActorNumber);
                row.Tick();
            }

            _skipEnabled = !SelectionLocked && canConfirm && _selectionSource != null && _selectionSource.AllowSkip;
            RefreshSkipVisual();
        }

        private void RefreshSkipVisual()
        {
            if (_skipButtonBg == null) return;
            bool armed = _skipEnabled && _defaultProvider != null && _armedTarget == SkipTarget;
            ButtonVisual.Resolve(NeutralPalette,
                armed: armed, hover: _skipHover, selected: false, enabled: _skipEnabled,
                out Color bg, out Color label);
            _skipButtonBg.color = bg;
            if (_skipLabel != null)
            {
                _skipLabel.text = armed ? Texts.Get(TextId.VoteConfirmLabel) : Texts.Get(TextId.VoteSkipLabel);
                _skipLabel.color = label;
            }
        }

        private static bool ContainsInt(IReadOnlyCollection<int> set, int value)
        {
            if (set == null) return false;
            foreach (int v in set)
            {
                if (v == value) return true;
            }
            return false;
        }

        private static bool ContainsActor(IReadOnlyList<int> list, int actor)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == actor) return true;
            }
            return false;
        }

        private void UpdateResult(MeetingClientState state)
        {
            if (state.Result == null) return;
            MeetingOutcome outcome = state.Result;

            int skipVotes = 0;
            if (outcome.TargetActors != null && outcome.VoteCounts != null)
            {
                for (int i = 0; i < outcome.TargetActors.Length && i < outcome.VoteCounts.Length; i++)
                {
                    if (outcome.TargetActors[i] == SkipTarget)
                    {
                        skipVotes = outcome.VoteCounts[i];
                        break;
                    }
                }
            }

            if (!_countsApplied)
            {
                _countsApplied = true;
                foreach (VoteRow row in _rows) row.SetVoteCount(0);
                if (outcome.TargetActors != null && outcome.VoteCounts != null)
                {
                    for (int i = 0; i < outcome.TargetActors.Length && i < outcome.VoteCounts.Length; i++)
                    {
                        int target = outcome.TargetActors[i];
                        if (target == SkipTarget) continue;
                        if (_rowsByActor.TryGetValue(target, out VoteRow row))
                        {
                            row.SetVoteCount(outcome.VoteCounts[i]);
                        }
                    }
                }
                if (_skipCountLabel != null)
                {
                    _skipCountLabel.text = Texts.Format(TextId.VoteCountFormat, skipVotes);
                    _skipCountLabel.gameObject.SetActive(true);
                }
            }

            if (_resultShown) return;
            _resultShown = true;

            string banner;
            if (outcome.ExecutedActor != SkipTarget)
            {
                string name = _rowsByActor.TryGetValue(outcome.ExecutedActor, out VoteRow executed)
                    ? executed.PlayerName
                    : $"#{outcome.ExecutedActor}";
                banner = Texts.Format(TextId.VoteExecutedFormat, name);
                _resultBanner.color = ResultExecutedColor;
            }
            else
            {
                banner = Texts.Get(TextId.VoteNoExecution);
                _resultBanner.color = ResultNoneColor;
            }
            if (skipVotes > 0) banner += Texts.Format(TextId.VoteSkipSuffixFormat, skipVotes);
            _resultBanner.text = banner;
            _resultBanner.gameObject.SetActive(true);

            WLog.Line("vote_panel_result", secret: false,
                ("executed", outcome.ExecutedActor), ("skipVotes", skipVotes));
        }

        public bool StartScatterReveal(List<List<int>> groups, long nowUnixMs,
            Action<float> pumpMusic, Action stopMusic, Action playJingle)
        {
            if (_root == null || _rows.Count == 0) return false;
            if (groups == null || groups.Count < 2) return false;

            _scatterLetterByActor.Clear();
            var letters = new List<char>(groups.Count);
            for (int g = 0; g < groups.Count; g++)
            {
                char letter = (char)('A' + g);
                letters.Add(letter);
                foreach (int actor in groups[g]) _scatterLetterByActor[actor] = letter;
            }

            bool anyRow = false;
            foreach (VoteRow row in _rows)
            {
                if (_scatterLetterByActor.ContainsKey(row.ActorNumber)) { anyRow = true; break; }
            }
            if (!anyRow)
            {
                _scatterLetterByActor.Clear();
                return false;
            }

            _scatterLetters = letters.ToArray();
            _scatterActive = true;
            _scatterSettled = false;
            _scatterMusicStopped = false;
            _scatterStartUnixMs = nowUnixMs;
            _scatterNextSwapUnixMs = 0;
            _scatterPumpMusic = pumpMusic;
            _scatterStopMusic = stopMusic;
            _scatterPlayJingle = playJingle;

            EnsureScatterSloganBuilt();
            if (_scatterSloganRoot != null) _scatterSloganRoot.SetActive(true);

            string unknown = Texts.Get(TextId.VoteScatterBadgeUnknown);
            foreach (VoteRow row in _rows)
            {
                if (_scatterLetterByActor.ContainsKey(row.ActorNumber))
                {
                    row.SetScatterBadge(unknown, settled: false);
                }
            }

            WLog.Line("vote_panel_scatter_reveal", secret: false,
                ("groups", groups.Count), ("actors", _scatterLetterByActor.Count));
            return true;
        }

        private void UpdateScatterReveal(long nowUnixMs)
        {
            if (!_scatterActive) return;
            long elapsed = nowUnixMs - _scatterStartUnixMs;

            if (elapsed < ScatterIntroMs)
            {
                _scatterPumpMusic?.Invoke(1f);
                return;
            }

            if (elapsed < ScatterIntroMs + ScatterShuffleMs)
            {
                _scatterPumpMusic?.Invoke(1f);
                if (nowUnixMs < _scatterNextSwapUnixMs) return;
                _scatterNextSwapUnixMs = nowUnixMs + ScatterSwapIntervalMs;
                foreach (VoteRow row in _rows)
                {
                    if (!_scatterLetterByActor.ContainsKey(row.ActorNumber)) continue;
                    char spin = _scatterLetters[_scatterRng.Next(_scatterLetters.Length)];
                    row.SetScatterBadge(Texts.Format(TextId.VoteScatterBadgeFormat, spin), settled: false);
                }
                return;
            }

            if (!_scatterSettled)
            {
                _scatterSettled = true;
                foreach (VoteRow row in _rows)
                {
                    if (_scatterLetterByActor.TryGetValue(row.ActorNumber, out char letter))
                    {
                        row.SetScatterBadge(Texts.Format(TextId.VoteScatterBadgeFormat, letter), settled: true);
                    }
                }
                _scatterPlayJingle?.Invoke();
                WLog.Line("vote_panel_scatter_settled", secret: false);
            }

            if (_scatterMusicStopped) return;
            long fadeElapsed = elapsed - ScatterIntroMs - ScatterShuffleMs;
            if (fadeElapsed >= ScatterFadeMs)
            {
                _scatterMusicStopped = true;
                _scatterStopMusic?.Invoke();
            }
            else
            {
                _scatterPumpMusic?.Invoke(1f - fadeElapsed / (float)ScatterFadeMs);
            }
        }

        public void StopScatterReveal()
        {
            if (_scatterActive && !_scatterMusicStopped) _scatterStopMusic?.Invoke();
            if (_scatterActive)
            {
                foreach (VoteRow row in _rows) row.SetScatterBadge(null, settled: false);
            }
            _scatterActive = false;
            _scatterSettled = false;
            _scatterMusicStopped = false;
            _scatterLetterByActor.Clear();
            _scatterPumpMusic = null;
            _scatterStopMusic = null;
            _scatterPlayJingle = null;
            if (_scatterSloganRoot != null) _scatterSloganRoot.SetActive(false);
        }

        private void EnsureScatterSloganBuilt()
        {
            if (_scatterSloganRoot != null || _rootRect == null) return;
            RectTransform slogan = UiKit.CreateRect(_rootRect, "ScatterSlogan",
                new Vector2(0f, 284f), new Vector2(520f, 32f));
            _scatterSloganRoot = slogan.gameObject;
            string text = Texts.Get(TextId.NoticeScatterSlogan);
            TextMeshProUGUI label = UiKit.CreateText(slogan, "Label", Vector2.zero,
                new Vector2(520f, 32f), text, 24f, TitleTextColor, TextAlignmentOptions.Center);
            Sprite wink = AssetCatalog.GetSprite("img_taxman_wink")
                ?? AssetCatalog.GetSprite("img_taxman_nodeath");
            if (wink != null)
            {
                const float iconSize = 26f;
                const float gap = 4f;
                float textWidth = label.GetPreferredValues(text).x;
                label.rectTransform.anchoredPosition = new Vector2(-(gap + iconSize) / 2f, 0f);
                Image icon = UiKit.CreateImage(slogan, "Icon",
                    new Vector2(textWidth / 2f + gap / 2f, 0f),
                    new Vector2(iconSize, iconSize), Color.white);
                icon.sprite = wink;
                icon.preserveAspect = true;
            }
            _scatterSloganRoot.SetActive(false);
        }

        private void HandleInput(MeetingClientState state)
        {
            if (_slideActive) return;
            bool canConfirm = !SelectionLocked && _selectionSource != null && _selectionSource.CanConfirm(_localActor);
            bool pagerActive = PageCount > 1;
            if (!canConfirm && !pagerActive) return;

            Vector2 mouse = Input.mousePosition;
            bool clicked = Input.GetMouseButtonDown(0) || SemiFunc.InputDown(InputKey.Confirm);

            bool blocked = IsPointerBlocked != null && IsPointerBlocked(mouse);
            if (blocked) clicked = false;

            bool skipHover = false;
            if (canConfirm)
            {
                foreach (VoteRow row in _rows)
                {
                    if (!row.VoteButtonActive) continue;
                    RectTransform buttonRect = row.VoteButtonRect;
                    if (buttonRect == null || !buttonRect.gameObject.activeInHierarchy) continue;
                    bool hover = !blocked && RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mouse, null);
                    row.SetHover(hover);
                    if (hover && clicked)
                    {
                        HandleTargetClick(row.ActorNumber);
                        return;
                    }
                }

                if (!blocked && _selectionSource.AllowSkip && _skipButtonBg != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(_skipButtonBg.rectTransform, mouse, null))
                {
                    skipHover = true;
                    if (clicked)
                    {
                        HandleTargetClick(SkipTarget);
                        return;
                    }
                }
            }
            if (_skipHover != skipHover)
            {
                _skipHover = skipHover;
                RefreshSkipVisual();
            }

            if (pagerActive && clicked && !blocked)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_pagePrevBg.rectTransform, mouse, null))
                {
                    _page = (_page - 1 + PageCount) % PageCount;
                    LayoutRows();
                    WLog.Line("vote_panel_page", secret: false, ("page", _page));
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(_pageNextBg.rectTransform, mouse, null))
                {
                    _page = (_page + 1) % PageCount;
                    LayoutRows();
                    WLog.Line("vote_panel_page", secret: false, ("page", _page));
                }
            }
        }

        private void HandleTargetClick(int targetActor)
        {
            if (_defaultProvider == null)
            {
                SubmitVote(targetActor);
                return;
            }
            if (_armedTarget != targetActor)
            {
                _armedTarget = targetActor;
                WLog.Line("vote_armed", secret: true, ("target", targetActor));
                return;
            }
            _armedTarget = ArmedNone;
            SubmitVote(targetActor);
        }

        private void SubmitVote(int targetActor)
        {
            try
            {
                _selectionSource.Confirm(_localActor, targetActor);
            }
            catch (Exception e)
            {
                WLog.Line("vote_panel_confirm_error", secret: false, ("err", e.Message));
            }
        }

        private sealed class DefaultVoteTargetProvider : ITargetSelectionSource
        {
            private readonly VotePanel _panel;
            private readonly List<int> _targets = new List<int>();

            internal DefaultVoteTargetProvider(VotePanel panel)
            {
                _panel = panel;
            }

            public IReadOnlyList<int> TargetActors
            {
                get
                {
                    _targets.Clear();
                    MeetingClientState state = _panel._lastState;
                    if (state != null)
                    {
                        foreach (VoteRow row in _panel._rows)
                        {
                            if (state.GetRowStatus(row.ActorNumber) == RowStatus.Alive)
                            {
                                _targets.Add(row.ActorNumber);
                            }
                        }
                    }
                    return _targets;
                }
            }

            public bool AllowSkip => true;

            public int CurrentSelection => SkipTarget;

            public bool CanConfirm(int localActor)
            {
                MeetingClientState state = _panel._lastState;
                if (state == null || localActor < 0) return false;
                if (state.GetRowStatus(localActor) != RowStatus.Alive) return false;
                if (ContainsInt(state.VotedActors, localActor)) return false;
                if (_panel._pendingSend) return false;
                return true;
            }

            public void Confirm(int localActor, int targetActor)
            {
                _panel._pendingSend = true;
                _panel._myVoteTarget = targetActor;
                WLog.Line("vote_submit", secret: true, ("target", targetActor));
                _panel.OnVoteSubmit?.Invoke(targetActor);
            }
        }
    }
}
