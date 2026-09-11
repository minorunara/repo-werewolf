using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class ResultFlowController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private readonly ResultCountdown _resultCountdown = new ResultCountdown();

        private int _lastResultCountdownSecond = -1;

        private readonly VoidMatchHold _voidMatchHold = new VoidMatchHold();
        private readonly VoidMatchPanel _voidMatchPanel = new VoidMatchPanel();

        private bool _resultReturnPending;

        private readonly ResultDigest _matchDigest = new ResultDigest();

        private IReadOnlyList<DigestEntry> _clientDigestEntries;

        private Dictionary<int, Role> _resultRolesByActor;

        private readonly ResultReturnButtonFlow _resultReturnFlow = new ResultReturnButtonFlow();

        private int _roundGameOverAutoReturnSec;

        internal ResultFlowController(WerewolfDirector director)
        {
            _d = director;
        }

        internal IReadOnlyList<DigestEntry> ClientDigestEntries => _clientDigestEntries;

        internal int MatchWinReason()
        {
            var winner = _d.HostSession?.Winner;
            return winner != null ? (int)winner.Reason : ResultDigest.MatchEndReason(_clientDigestEntries);
        }

        internal void ObserveForDigest(OutboundMessage msg)
        {
            try
            {
                if (msg == null || msg.Target != MessageTarget.All) return;
                _matchDigest.Observe(msg.Code, msg.Payload, SessionClock.NowMs(), _d.HostSession?.Winner);
                if (msg.Code == WWEventCodes.GameOver && _d.ClientBus != null)
                {
                    TryRecordFinalBalance();
                    _d.ClientBus.SendToAll(MessageCodes.ResultDigest, _matchDigest.ToWire());
                    WLog.Line("digest_sent", secret: false, ("entries", _matchDigest.Entries.Count));
                    SendReplayLossLedger();
                }
            }
            catch (Exception e)
            {
                WLog.Line("digest_observe_error", secret: false, ("err", e.Message));
            }
        }

        internal void HostRecordExtractionDone()
        {
            try
            {
                if (!Session.IsHost) return;
                if (!Extraction.TryReadQuota(out _, out int points, out int completed)) return;
                _matchDigest.RecordExtractionDone(completed, points, SessionClock.NowMs());
            }
            catch (Exception e)
            {
                WLog.Line("digest_extraction_error", secret: false, ("err", e.Message));
            }
        }

        internal void HandlePerkUnlockedDigest(PerkId perk)
        {
            _matchDigest.RecordPerkUnlocked((byte)perk, SessionClock.NowMs());
            ReplaySampler.NotePerkUnlocked(perk.ToString());
        }

        internal void HandleInformantDigest()
            => _matchDigest.RecordInformant(SessionClock.NowMs());

        private void TryRecordFinalBalance()
        {
            try
            {
                if (!Extraction.TryReadQuota(out int haulGoal, out int points, out int completed)) return;
                if (haulGoal <= 0 || points <= 0) return;

                int remaining = CheckmateJudge.RemainingQuotaDollars(haulGoal, points, completed);
                if (remaining < 0) return;
                int delivered = haulGoal / points * completed;
                int obtainable = (int)(ValueTrackPatch.ComputeObtainableDollars() + 0.5f);
                _matchDigest.RecordFinalBalance(delivered, remaining, obtainable, SessionClock.NowMs());
            }
            catch (Exception e)
            {
                WLog.Line("digest_balance_error", secret: false, ("err", e.Message));
            }
        }

        private void SendReplayLossLedger()
        {
            object[] wire = ReplaySampler.BuildLossLedgerWire();
            if (wire == null) return;
            _d.ClientBus.SendToAll(MessageCodes.ReplayLossLedger, wire);
            WLog.Line("replay_ledger_sent", secret: false,
                ("segments", (int)wire[0]), ("entries", ((int[])wire[1]).Length));
        }

        internal void ApplyResultDigest(object[] payload)
        {
            _clientDigestEntries = ResultDigest.FromWire(payload);
            WLog.Line("recv_digest", secret: false,
                ("entries", _clientDigestEntries != null ? _clientDigestEntries.Count : -1));
        }

        internal void ClearClientDigest()
        {
            _clientDigestEntries = null;
            _resultRolesByActor = null;
        }

        internal void CaptureResultChatContext(int[] actors, byte[] roles)
        {
            var map = new Dictionary<int, Role>(actors.Length);
            for (int i = 0; i < actors.Length && i < roles.Length; i++)
            {
                map[actors[i]] = (Role)roles[i];
            }
            _resultRolesByActor = map;

            _d.Chat.ResetForResultChat();
        }

        private Role? ResultChatMarkedRole(int actor)
            => _resultRolesByActor != null && _resultRolesByActor.TryGetValue(actor, out Role role)
                ? role
                : (Role?)null;

        public void Tick(long now)
        {
            TickResultReturn();
            TickVoidMatch(now);
        }

        private void TickResultReturn()
        {
            _d.Chat.TickResultChat(_d.IsResultChatActiveClient, ResultChatMarkedRole);
            _d.TickReplayViewer();

            ResultScreen resultScreen = _d.ResultScreenView;
            if (!resultScreen.Visible) return;

            resultScreen.Tick(ResultChatPointerBlocksWheel());

            int? remaining = _resultCountdown.RemainingSeconds(SessionClock.NowMs());
            int displaySecond = remaining ?? -1;
            if (displaySecond != _lastResultCountdownSecond)
            {
                _lastResultCountdownSecond = displaySecond;
                resultScreen.SetFooter(BuildResultFooterText(remaining));
            }

            if (!Session.IsHost) return;
            if (!_d.ResultSequenceView.Active) return;

            long now = SessionClock.NowMs();
            TryStartReturnButton(now);
            TickReturnButton(now);

            if (!_resultReturnFlow.ReadyAt(now)) return;

            KeyCode key = Plugin.ResultReturnKey != null ? Plugin.ResultReturnKey.Value : KeyCode.F5;
            if (key == KeyCode.None) return;
            if (!Inputs.NoTextInputsActive) return;
            if (Input.GetKeyDown(key))
            {
                WLog.Line("result_return_requested", secret: false, ("via", "key"));
                _d.ResultSequenceView.RequestReturn();
            }
        }

        private bool ResultChatPointerBlocksWheel()
            => _d.IsResultChatActiveClient
               && _d.Chat.IsPointerOverResultPanel((Vector2)Input.mousePosition);

        private void TickReturnButton(long nowUnixMs)
        {
            ResultScreen resultScreen = _d.ResultScreenView;
            bool onButton = _resultReturnFlow.ReadyAt(nowUnixMs)
                && !ResultChatPointerBlocksWheel()
                && resultScreen.IsPointerOverReturnButton((Vector2)Input.mousePosition);
            ResultReturnButtonEvent ev = _resultReturnFlow.Tick(
                nowUnixMs, Input.GetMouseButtonDown(0), onButton);
            KeyCode key = Plugin.ResultReturnKey != null ? Plugin.ResultReturnKey.Value : KeyCode.F5;
            bool scenario = _d.ClientScenarioSession;
            resultScreen.SetReturnButton(
                visible: true,
                alpha: _resultReturnFlow.AlphaAt(nowUnixMs),
                armed: _resultReturnFlow.Armed,
                hover: onButton,
                keyName: key != KeyCode.None ? key.ToString() : null,
                labelId: scenario ? TextId.ScenarioReturnToSelect : TextId.ResultReturnButtonLabel,
                labelWithKeyFormatId: scenario
                    ? TextId.ScenarioReturnToSelectWithKeyFormat
                    : TextId.ResultReturnButtonWithKeyFormat);
            switch (ev)
            {
                case ResultReturnButtonEvent.Armed:
                    WLog.Line("result_return_armed", secret: false);
                    break;
                case ResultReturnButtonEvent.Disarmed:
                    WLog.Line("result_return_disarmed", secret: false);
                    break;
                case ResultReturnButtonEvent.Confirmed:
                    WLog.Line("result_return_requested", secret: false, ("via", "button"));
                    _d.ResultSequenceView.RequestReturn();
                    break;
            }
        }

        private void TickVoidMatch(long nowUnixMs)
        {
            _d.EnsurePanelBuilt(_voidMatchPanel);
            if (!_voidMatchPanel.Exists) return;

            GameSession session = _d.HostSession;
            bool available =
                Session.IsHost
                && session != null
                && (session.Phase == GamePhase.Play || session.Phase == GamePhase.Meeting)
                && NoTextInputsActiveSafe()
                && ScenarioSessionGate.VoidMatchEnabled(_d.ClientScenarioSession);

            KeyCode key = Plugin.VoidMatchKey != null ? Plugin.VoidMatchKey.Value : KeyCode.F5;
            bool held = available && key != KeyCode.None && Input.GetKey(key);

            bool cancel = _voidMatchHold.Armed && MenuKeyDownSafe();
            if (_voidMatchHold.Armed) SuppressEscMenu();

            VoidMatchHoldEvent result =
                _voidMatchHold.Tick(held, available, cancel, Time.unscaledDeltaTime);

            switch (result)
            {
                case VoidMatchHoldEvent.Armed:
                    WLog.Line("void_match_armed", secret: false, ("phase", session.Phase));
                    break;
                case VoidMatchHoldEvent.Cancelled:
                    WLog.Line("void_match_cancelled", secret: false);
                    break;
                case VoidMatchHoldEvent.Confirmed:
                    WLog.Line("void_match_confirmed", secret: false, ("phase", session.Phase));
                    session.VoidMatch(nowUnixMs);
                    _voidMatchHold.Reset();
                    break;
            }

            _voidMatchPanel.Tick(
                _voidMatchHold.IsCharging,
                _voidMatchHold.Ratio,
                _voidMatchHold.Armed,
                (int)Math.Ceiling(_voidMatchHold.ArmedRemainingSeconds),
                key);
        }

        internal void ResetVoidMatch()
        {
            _voidMatchHold.Reset();
            if (_voidMatchPanel.Exists)
            {
                _voidMatchPanel.Tick(false, 0f, false, 0, KeyCode.None);
            }
        }

        private static bool _escSuppressErrorLogged;
        private static bool _menuKeyErrorLogged;
        private static bool _textInputErrorLogged;

        private static void SuppressEscMenu()
        {
            try
            {
                Session.DisableEscMenu(1f);
            }
            catch (Exception e)
            {
                if (_escSuppressErrorLogged) return;
                _escSuppressErrorLogged = true;
                WLog.Line("result_esc_suppress_error", secret: false, ("err", e.Message));
            }
        }

        private static bool MenuKeyDownSafe()
        {
            try
            {
                return Inputs.Down(GameKey.Menu);
            }
            catch (Exception e)
            {
                if (!_menuKeyErrorLogged)
                {
                    _menuKeyErrorLogged = true;
                    WLog.Line("result_menu_key_error", secret: false, ("err", e.Message));
                }
                return false;
            }
        }

        private static bool NoTextInputsActiveSafe()
        {
            try
            {
                return Inputs.NoTextInputsActive;
            }
            catch (Exception e)
            {
                if (!_textInputErrorLogged)
                {
                    _textInputErrorLogged = true;
                    WLog.Line("result_text_input_error", secret: false, ("err", e.Message));
                }
                return true;
            }
        }

        internal static string BuildResultFooterText(int? remainingSeconds = null)
        {
            if (Session.IsHost)
            {
                return remainingSeconds.HasValue
                    ? Texts.Format(TextId.ResultAutoReturnCountdownFormat, remainingSeconds.Value)
                    : string.Empty;
            }

            string baseText = Texts.Get(TextId.ResultWaitingHost);
            return remainingSeconds.HasValue
                ? Texts.Format(TextId.ResultFooterWithCountdownFormat, baseText, remainingSeconds.Value)
                : baseText;
        }

        internal void FreezeResultAutoReturnConfig()
        {
            if (Session.IsHost)
            {
                _roundGameOverAutoReturnSec = Math.Max(0,
                    Plugin.GameConfig != null ? Plugin.GameConfig.GameOverAutoReturnSec : 0);
                return;
            }

            _roundGameOverAutoReturnSec = 0;
            if (!SettingsCatalog.TryDecodeBlob(_d.LobbyBlobMirror, out var values)) return;
            if (!values.TryGetValue("GameOverAutoReturnSec", out string raw)) return;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds)) return;
            _roundGameOverAutoReturnSec = Math.Max(0, seconds);
        }

        internal void FreezeAutoReturnForHost()
        {
            _roundGameOverAutoReturnSec = Math.Max(0, Plugin.GameConfig.GameOverAutoReturnSec);
        }

        internal int RoundGameOverAutoReturnSec => _roundGameOverAutoReturnSec;

        internal void BeginResultCountdown()
        {
            long now = SessionClock.NowMs();
            _resultCountdown.Begin(now, _roundGameOverAutoReturnSec);
            _resultReturnPending = true;
            TryStartReturnButton(now);
            _lastResultCountdownSecond = -1;
        }

        private void TryStartReturnButton(long now)
        {
            if (!_resultReturnPending || _d.ScenarioGuidePending) return;

            _resultReturnPending = false;
            _resultReturnFlow.Begin(now);
        }

        internal void ClearResultCountdown()
        {
            _resultCountdown.Clear();
            _resultReturnPending = false;
            _resultReturnFlow.Reset();
            _d.ResultScreenView.SetReturnButton(visible: false, alpha: 0f, armed: false, hover: false, keyName: null);
            _roundGameOverAutoReturnSec = 0;
            _lastResultCountdownSecond = -1;
        }

        public void Reset()
        {
            ClearResultCountdown();
            ResetVoidMatch();
            ClearClientDigest();
        }

        public void DestroyUi()
        {
        }
    }
}
