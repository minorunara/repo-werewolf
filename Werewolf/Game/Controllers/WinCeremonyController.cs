using System;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class WinCeremonyController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private CheckmateSequence _checkmate;
        private bool _checkmateScanPending;
        private long _checkmateNextScanUnixMs;

        private readonly CheckmateRevealPanel _checkmateReveal = new CheckmateRevealPanel();
        private Coroutine _checkmateRevealCoroutine;

        private bool _winCeremonyActive;

        private const long CheckmateScanIntervalMs = 500;

        private long _checkmateLineNextSyncUnixMs;

        private const long CheckmateLineSyncIntervalMs = 5000;

        private long _eradicationConfirmAtUnixMs;

        private readonly EradicationRevealPanel _eradicationReveal = new EradicationRevealPanel();
        private Coroutine _eradicationRevealCoroutine;

        internal WinCeremonyController(WerewolfDirector director)
        {
            _d = director;
        }

        internal bool WinCeremonyActive => _winCeremonyActive;

        internal bool IsCheckmateCeremonyStarted => _checkmate != null && _checkmate.CeremonyStarted;

        internal void BeginHostRound()
        {
            _checkmate = new CheckmateSequence();
            _checkmateScanPending = false;
            _checkmateNextScanUnixMs = 0;
            _eradicationConfirmAtUnixMs = 0;
        }

        internal void ResetHost()
        {
            _checkmate = null;
            _checkmateScanPending = false;
            _checkmateLineNextSyncUnixMs = 0;
            _eradicationConfirmAtUnixMs = 0;
        }

        internal void RequestCheckmateScan()
        {
            if (_checkmate == null || !Session.IsHost) return;
            _checkmateScanPending = true;
        }

        internal void TickHost(long now)
        {
            TickCheckmateHost(now);
            TickEradicationHost(now);
        }

        private void TickCheckmateHost(long now)
        {
            GameSession session = _d.HostSession;
            if (_checkmate == null || session == null || session.Winner != null) return;
            if (!Session.IsHost) return;

            if (session.WinLocked && !_checkmate.CeremonyStarted) return;

            bool haulSuck = ValueTrackPatch.InHaulSuckWindow(now);

            RolesSession roles = _d.HostRolesSession;
            if (roles != null && !haulSuck && now >= _checkmateLineNextSyncUnixMs && Session.InRunLevel)
            {
                _checkmateLineNextSyncUnixMs = now + CheckmateLineSyncIntervalMs;
                roles.UpdateCheckmateLine(ComputeCheckmateLineDollars());
            }

            if (_checkmateScanPending && !_checkmate.Detected && !haulSuck
                && now >= _checkmateNextScanUnixMs && Session.InRunLevel
                && _d.ScenarioCheckmateArmed)
            {
                _checkmateScanPending = false;
                _checkmateNextScanUnixMs = now + CheckmateScanIntervalMs;
                TryDetectCheckmate();
            }

            bool curseActive = roles != null
                && roles.ActiveCurse != null && !roles.ActiveCurse.Resolved;
            bool meetingCountdown = _d.HostMeetingSession != null
                && _d.HostMeetingSession.Stage == MeetingStage.Countdown;
            switch (_checkmate.Tick(now, session.Phase, curseActive, meetingCountdown))
            {
                case CheckmateAction.StartCeremony:
                    session.LockValueCheckmate();
                    SendCheckmateReveal(now);
                    break;

                case CheckmateAction.ConfirmWin:
                    session.NotifyValueCheckmate(now);
                    break;
            }
        }

        private void TryDetectCheckmate()
        {
            try
            {
                if (!Extraction.TryReadQuota(out int haulGoal, out int points, out int completed)) return;
                int remainingQuota = CheckmateJudge.RemainingQuotaDollars(haulGoal, points, completed);
                if (remainingQuota <= 0) return;

                float obtainable = ValueTrackPatch.ComputeObtainableDollars();
                if (!CheckmateJudge.IsCheckmate(obtainable, remainingQuota)) return;

                _checkmate.NotifyDetected();
                WLog.Line("checkmate_detected", secret: false,
                    ("obtainable", (int)obtainable), ("remainingQuota", remainingQuota),
                    ("phase", _d.HostSession.Phase));
            }
            catch (Exception e)
            {
                WLog.Line("checkmate_scan_error", secret: false, ("err", e.Message));
            }
        }

        private void SendCheckmateReveal(long now)
        {
            RolesSession roles = _d.HostRolesSession;
            if (roles == null)
            {
                WLog.Line("checkmate_send_skip", secret: false, ("reason", "no_roles_session"));
                return;
            }

            Extraction.ReadExtractedAndGoal(out int extracted, out int haulGoal);

            _d.SendViaBus(new OutboundMessage(
                WWCheckmateCodes.CheckmateReveal,
                new object[]
                {
                    roles.BuildMeetingGaugeData(extracted, haulGoal, ComputeCheckmateLineDollars()),
                    now,
                },
                MessageTarget.All, null));
            WLog.Line("checkmate_ceremony_start", secret: false);
        }

        internal int ComputeCheckmateLineDollars()
        {
            try
            {
                RolesSession roles = _d.HostRolesSession;
                if (roles == null || !Session.InRunLevel) return -1;
                if (!Extraction.TryReadQuota(out int haulGoal, out int points, out int completed)) return -1;
                int remaining = CheckmateJudge.RemainingQuotaDollars(haulGoal, points, completed);
                if (remaining <= 0) return -1;

                float obtainable = ValueTrackPatch.ComputeObtainableDollars();
                float line = roles.Gauge.LostDollars + (obtainable - remaining);
                if (line < 0f) line = 0f;
                return (int)(line + 0.5f);
            }
            catch (Exception e)
            {
                WLog.Line("checkmate_line_error", secret: false, ("err", e.Message));
                return -1;
            }
        }

        internal bool DebugForceCheckmate()
        {
            if (_checkmate == null || !Session.IsHost) return false;
            _checkmate.NotifyDetected();
            WLog.Line("checkmate_forced", secret: false);
            return true;
        }

        internal void HostBeginEradicationCeremony()
        {
            _eradicationConfirmAtUnixMs = SessionClock.NowMs() + EradicationCeremony.CeremonyMs;
            WLog.Line("eradication_ceremony_start", secret: false,
                ("confirmAtUnixMs", _eradicationConfirmAtUnixMs));
        }

        private void TickEradicationHost(long now)
        {
            if (_eradicationConfirmAtUnixMs == 0 || now < _eradicationConfirmAtUnixMs) return;
            _eradicationConfirmAtUnixMs = 0;
            _d.HostSession?.ConfirmPendingWin(now);
        }

        internal void HandleCheckmateReveal(object[] p)
        {
            MeetingGaugeSnapshot snapshot = MeetingGaugeSnapshot.FromData((int[])p[0]);
            if (snapshot == null)
            {
                WLog.Line("checkmate_reveal_drop", secret: false, ("reason", "invalid_payload"));
                return;
            }

            _d.EnsurePanelBuilt(_checkmateReveal);
            if (!_checkmateReveal.Exists) return;

            _checkmateReveal.Show(snapshot,
                _d.GaugePanelView.LastRevealedPermille, _d.GaugePanelView.LastRevealedLoss);

            _winCeremonyActive = true;

            if (_checkmateRevealCoroutine != null) _d.StopCoroutine(_checkmateRevealCoroutine);
            _checkmateRevealCoroutine = _d.StartCoroutine(_checkmateReveal.Play(
                onBreak: () => _d.PlaySfx("sfx_gauge_break"),
                onStamp: () => _d.PlaySfx("sfx_death_stamp"),
                onDone: () => _d.FeedScenarioSignal(ScenarioSignal.WinCeremonyDone)));

        }

        internal void HandleEradicationReveal(object[] p)
        {
            EradicationRevealData data = EradicationRevealWire.FromWire(p);
            if (data == null)
            {
                WLog.Line("eradication_reveal_drop", secret: false, ("reason", "invalid_payload"));
                return;
            }

            _winCeremonyActive = true;

            _d.EnsurePanelBuilt(_eradicationReveal);
            if (!_eradicationReveal.Exists) return;

            _eradicationReveal.Show(
                ResolveEradicationVictim(data), Players.Resolve, data.Outcome, data.Vanished);

            if (_eradicationRevealCoroutine != null) _d.StopCoroutine(_eradicationRevealCoroutine);
            _eradicationRevealCoroutine = _d.StartCoroutine(_eradicationReveal.Play(
                onStamp: () => _d.PlaySfx("sfx_death_stamp"),
                onDone: () => _d.FeedScenarioSignal(ScenarioSignal.WinCeremonyDone)));
            WLog.Line("recv_eradication_reveal", secret: false,
                ("actor", data.ActorNumber), ("outcome", data.Outcome), ("vanished", data.Vanished));
        }

        private WPlayer ResolveEradicationVictim(EradicationRevealData data)
        {
            System.Collections.Generic.IReadOnlyList<WPlayer> roster =
                _d.HostSessionPlayers ?? _d.Registry.BuildRealPlayers();
            if (roster != null)
            {
                foreach (WPlayer player in roster)
                {
                    if (player != null && player.ActorNumber == data.ActorNumber) return player;
                }
            }
            return new WPlayer
            {
                ActorNumber = data.ActorNumber,
                Name = string.IsNullOrEmpty(data.Name) ? $"#{data.ActorNumber}" : data.Name,
                IsBot = data.ActorNumber < 0,
            };
        }

        public void Tick(long now)
        {
            if (_checkmateReveal.Visible && _d.ResultScreenView.Visible)
            {
                HideCheckmateReveal();
            }

            _eradicationReveal.Tick();
            if (_eradicationReveal.Visible && _d.ResultScreenView.Visible)
            {
                HideEradicationReveal();
            }
        }

        private void HideCheckmateReveal()
        {
            _winCeremonyActive = false;
            if (_checkmateRevealCoroutine != null)
            {
                _d.StopCoroutine(_checkmateRevealCoroutine);
                _checkmateRevealCoroutine = null;
                _d.FeedScenarioSignal(ScenarioSignal.WinCeremonyDone);
            }
            if (_checkmateReveal.Exists && _checkmateReveal.Visible) _checkmateReveal.HideNow();
        }

        private void HideEradicationReveal()
        {
            _winCeremonyActive = false;
            if (_eradicationRevealCoroutine != null)
            {
                _d.StopCoroutine(_eradicationRevealCoroutine);
                _eradicationRevealCoroutine = null;
                _d.FeedScenarioSignal(ScenarioSignal.WinCeremonyDone);
            }
            if (_eradicationReveal.Exists) _eradicationReveal.HideNow();
        }

        public void Reset()
        {
            HideCheckmateReveal();
            HideEradicationReveal();
        }

        public void DestroyUi()
        {
            _checkmateReveal.Destroy();
            _eradicationReveal.Destroy();
        }
    }
}
