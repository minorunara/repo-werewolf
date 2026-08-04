using System;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private CheckmateSequence _checkmate;
        private bool _checkmateScanPending;
        private long _checkmateNextScanUnixMs;

        private readonly CheckmateRevealPanel _checkmateReveal = new CheckmateRevealPanel();
        private Coroutine _checkmateRevealCoroutine;

        private bool _checkmateVoiceOpen;

        private const long CheckmateScanIntervalMs = 500;

        private long _checkmateLineNextSyncUnixMs;

        private const long CheckmateLineSyncIntervalMs = 5000;

        public void HostRequestCheckmateScan()
        {
            if (_checkmate == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            _checkmateScanPending = true;
        }

        private void TickCheckmateHost(long now)
        {
            if (_checkmate == null || _session == null || _session.Winner != null) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            bool haulSuck = ValueTrackPatch.InHaulSuckWindow(now);

            if (_roles != null && !haulSuck && now >= _checkmateLineNextSyncUnixMs && SemiFunc.RunIsLevel())
            {
                _checkmateLineNextSyncUnixMs = now + CheckmateLineSyncIntervalMs;
                _roles.UpdateCheckmateLine(HostComputeCheckmateLineDollars());
            }

            if (_checkmateScanPending && !_checkmate.Detected && !haulSuck
                && now >= _checkmateNextScanUnixMs && SemiFunc.RunIsLevel())
            {
                _checkmateScanPending = false;
                _checkmateNextScanUnixMs = now + CheckmateScanIntervalMs;
                TryDetectCheckmate();
            }

            bool curseActive = _roles != null
                && _roles.ActiveCurse != null && !_roles.ActiveCurse.Resolved;
            switch (_checkmate.Tick(now, _session.Phase, curseActive))
            {
                case CheckmateAction.StartCeremony:
                    _session.LockValueCheckmate();
                    SendCheckmateReveal(now);
                    break;

                case CheckmateAction.ConfirmWin:
                    _session.NotifyValueCheckmate(now);
                    break;
            }
        }

        private void TryDetectCheckmate()
        {
            try
            {
                RoundDirector rd = RoundDirector.instance;
                if (rd == null) return;

                int haulGoal = GameRefs.RoundDirector_haulGoal(rd);
                int points = GameRefs.RoundDirector_extractionPoints(rd);
                int completed = GameRefs.RoundDirector_extractionPointsCompleted(rd);
                int remainingQuota = CheckmateJudge.RemainingQuotaDollars(haulGoal, points, completed);
                if (remainingQuota <= 0) return;

                float obtainable = ValueTrackPatch.ComputeObtainableDollars();
                if (!CheckmateJudge.IsCheckmate(obtainable, remainingQuota)) return;

                _checkmate.NotifyDetected();
                WLog.Line("checkmate_detected", secret: false,
                    ("obtainable", (int)obtainable), ("remainingQuota", remainingQuota),
                    ("phase", _session.Phase));
            }
            catch (Exception e)
            {
                WLog.Line("checkmate_scan_error", secret: false, ("err", e.Message));
            }
        }

        private void SendCheckmateReveal(long now)
        {
            if (_roles == null)
            {
                WLog.Line("checkmate_send_skip", secret: false, ("reason", "no_roles_session"));
                return;
            }

            int extracted = -1;
            int haulGoal = -1;
            RoundDirector rd = RoundDirector.instance;
            if (rd != null)
            {
                haulGoal = GameRefs.RoundDirector_haulGoal(rd);
                int points = GameRefs.RoundDirector_extractionPoints(rd);
                int completed = GameRefs.RoundDirector_extractionPointsCompleted(rd);
                extracted = points > 0 ? haulGoal / points * completed : 0;
            }

            SendViaBus(new OutboundMessage(
                WWCheckmateCodes.CheckmateReveal,
                new object[]
                {
                    _roles.BuildMeetingGaugeData(extracted, haulGoal, HostComputeCheckmateLineDollars()),
                    now,
                },
                MessageTarget.All, null));
            WLog.Line("checkmate_ceremony_start", secret: false);
        }

        internal int HostComputeCheckmateLineDollars()
        {
            try
            {
                if (_roles == null || !SemiFunc.RunIsLevel()) return -1;
                RoundDirector rd = RoundDirector.instance;
                if (rd == null) return -1;

                int haulGoal = GameRefs.RoundDirector_haulGoal(rd);
                int points = GameRefs.RoundDirector_extractionPoints(rd);
                int completed = GameRefs.RoundDirector_extractionPointsCompleted(rd);
                int remaining = CheckmateJudge.RemainingQuotaDollars(haulGoal, points, completed);
                if (remaining <= 0) return -1;

                float obtainable = ValueTrackPatch.ComputeObtainableDollars();
                float line = _roles.Gauge.LostDollars + (obtainable - remaining);
                if (line < 0f) line = 0f;
                return (int)(line + 0.5f);
            }
            catch (Exception e)
            {
                WLog.Line("checkmate_line_error", secret: false, ("err", e.Message));
                return -1;
            }
        }

        public bool DebugForceCheckmate()
        {
            if (_checkmate == null || !SemiFunc.IsMasterClientOrSingleplayer()) return false;
            _checkmate.NotifyDetected();
            WLog.Line("checkmate_forced", secret: false);
            return true;
        }

        private void HandleCheckmateReveal(object[] p)
        {
            MeetingGaugeSnapshot snapshot = MeetingGaugeSnapshot.FromData((int[])p[0]);
            if (snapshot == null)
            {
                WLog.Line("checkmate_reveal_drop", secret: false, ("reason", "invalid_payload"));
                return;
            }

            EnsurePanelBuilt(_checkmateReveal);
            if (!_checkmateReveal.Exists) return;

            _checkmateReveal.Show(snapshot,
                _gaugePanel.LastRevealedPermille, _gaugePanel.LastRevealedLoss);

            _checkmateVoiceOpen = true;

            EnsureSfxBuilt();
            if (_checkmateRevealCoroutine != null) StopCoroutine(_checkmateRevealCoroutine);
            _checkmateRevealCoroutine = StartCoroutine(_checkmateReveal.Play(
                onBreak: () => _sfxPlayer.Play("sfx_gauge_break"),
                onStamp: () => _sfxPlayer.Play("sfx_death_stamp")));
        }

        private void TickCheckmateClient()
        {
            if (_checkmateReveal.Visible && _resultScreen.Visible)
            {
                HideCheckmateReveal();
            }
        }

        private void HideCheckmateReveal()
        {
            _checkmateVoiceOpen = false;
            if (_checkmateRevealCoroutine != null)
            {
                StopCoroutine(_checkmateRevealCoroutine);
                _checkmateRevealCoroutine = null;
            }
            if (_checkmateReveal.Exists && _checkmateReveal.Visible) _checkmateReveal.HideNow();
        }
    }
}
