using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class VoteTallyRevealView
    {
        private static readonly Vector2 SkipChipsBandLeft = new Vector2(233f, -352f);

        private const int PageTurnVariantCount = 7;
        private const float PageTurnVolumeMin = 0.85f;
        private const float PageTurnVolumeMax = 1f;

        private const int NotRendered = -1;

        private readonly List<VoteRow> _rows;
        private readonly Func<RectTransform> _resolvePanelRoot;
        private readonly Action<string, float> _playSfx;
        private int _lastPageTurnIndex = -1;
        private long _lastPageTurnAtMs;

        private bool _started;
        private bool _fastForwarded;
        private long _startUnixMs;
        private long _stepMs;
        private int _maxCount;
        private int _skipFinal;
        private int _skipLanded;
        private readonly Dictionary<int, int> _finalByActor = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _landedByActor = new Dictionary<int, int>();
        private TextMeshProUGUI _skipCountLabel;
        private TallyChipStrip _skipChips;

        public VoteTallyRevealView(List<VoteRow> rows, Func<RectTransform> resolvePanelRoot,
            Action<string, float> playSfx)
        {
            _rows = rows;
            _resolvePanelRoot = resolvePanelRoot;
            _playSfx = playSfx;
        }

        public void Start(MeetingOutcome outcome, long nowUnixMs, TextMeshProUGUI skipCountLabel)
        {
            _started = true;
            _fastForwarded = false;
            _startUnixMs = nowUnixMs;
            _skipCountLabel = skipCountLabel;
            _finalByActor.Clear();
            _landedByActor.Clear();
            _skipFinal = 0;
            _skipLanded = NotRendered;
            _maxCount = 0;
            _lastPageTurnAtMs = 0;
            if (outcome != null && outcome.TargetActors != null && outcome.VoteCounts != null)
            {
                for (int i = 0; i < outcome.TargetActors.Length && i < outcome.VoteCounts.Length; i++)
                {
                    int count = outcome.VoteCounts[i];
                    if (outcome.TargetActors[i] == VotePanel.SkipTarget) _skipFinal = count;
                    else _finalByActor[outcome.TargetActors[i]] = count;
                    if (count > _maxCount) _maxCount = count;
                }
            }
            _stepMs = VoteTallyTimeline.StepMs(_maxCount);
            if (_skipCountLabel != null) _skipCountLabel.gameObject.SetActive(true);
            WLog.Line("vote_tally_start", secret: false,
                ("maxCount", _maxCount), ("stepMs", _stepMs), ("skip", _skipFinal));
        }

        public void Tick(long nowUnixMs)
        {
            if (!_started || _fastForwarded) return;
            long elapsed = nowUnixMs - _startUnixMs;

            bool anyLanded = false;

            foreach (VoteRow row in _rows)
            {
                int final = _finalByActor.TryGetValue(row.ActorNumber, out int f) ? f : 0;
                int landed = VoteTallyTimeline.Landed(final, elapsed, _stepMs);
                int prev = _landedByActor.TryGetValue(row.ActorNumber, out int p) ? p : NotRendered;
                if (landed != prev)
                {
                    if (prev >= 0 && landed > prev) anyLanded = true;
                    _landedByActor[row.ActorNumber] = landed;
                    row.SetVoteCount(landed);
                }
                row.SetTallyChips(VoteTallyTimeline.VisibleChips(landed),
                    VoteTallyTimeline.TopChipVisible(final, landed, elapsed, _stepMs), nowUnixMs);
            }

            int skipLanded = VoteTallyTimeline.Landed(_skipFinal, elapsed, _stepMs);
            if (skipLanded != _skipLanded)
            {
                if (_skipLanded >= 0 && skipLanded > _skipLanded) anyLanded = true;
                _skipLanded = skipLanded;
                if (_skipCountLabel != null)
                {
                    _skipCountLabel.text = Texts.Format(TextId.VoteCountFormat, skipLanded);
                }
            }
            EnsureSkipChips();
            _skipChips?.Apply(VoteTallyTimeline.VisibleChips(skipLanded),
                VoteTallyTimeline.TopChipVisible(_skipFinal, skipLanded, elapsed, _stepMs), nowUnixMs);

            if (anyLanded) PlayPageTurn(nowUnixMs);
        }

        private void PlayPageTurn(long nowUnixMs)
        {
            if (_playSfx == null) return;
            if (_lastPageTurnAtMs != 0
                && nowUnixMs - _lastPageTurnAtMs < VoteTallyTimeline.MinChipSfxIntervalMs) return;
            _lastPageTurnAtMs = nowUnixMs;

            int index = UnityEngine.Random.Range(0, PageTurnVariantCount);
            if (index == _lastPageTurnIndex) index = (index + 1) % PageTurnVariantCount;
            _lastPageTurnIndex = index;
            float volume = UnityEngine.Random.Range(PageTurnVolumeMin, PageTurnVolumeMax);
            _playSfx(string.Format("sfx_page_turn_{0:00}", index + 1), volume);
        }

        public bool BannerReady(long nowUnixMs)
        {
            if (!_started) return false;
            if (_fastForwarded) return true;
            return VoteTallyTimeline.BannerReady(_maxCount, nowUnixMs - _startUnixMs);
        }

        public void FastForward()
        {
            if (!_started || _fastForwarded) return;
            _fastForwarded = true;
            foreach (VoteRow row in _rows)
            {
                int final = _finalByActor.TryGetValue(row.ActorNumber, out int f) ? f : 0;
                row.SetVoteCount(final);
                row.SetTallyChips(0, topVisible: false, nowMs: 0);
            }
            if (_skipCountLabel != null)
            {
                _skipCountLabel.text = Texts.Format(TextId.VoteCountFormat, _skipFinal);
            }
            _skipChips?.HideAll();
            WLog.Line("vote_tally_fastforward", secret: false);
        }

        public void Stop()
        {
            _started = false;
            _fastForwarded = false;
            _skipChips?.HideAll();
            _skipCountLabel = null;
            _finalByActor.Clear();
            _landedByActor.Clear();
        }

        public void OnPanelDestroy()
        {
            _skipChips = null;
        }

        private void EnsureSkipChips()
        {
            if (_skipChips != null || _skipFinal <= 0) return;
            RectTransform root = _resolvePanelRoot();
            if (root == null) return;
            _skipChips = TallyChipStrip.Create(root, SkipChipsBandLeft);
        }
    }
}
