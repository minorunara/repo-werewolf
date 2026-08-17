using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private readonly ResultDigest _matchDigest = new ResultDigest();

        private System.Collections.Generic.IReadOnlyList<DigestEntry> _clientDigestEntries;

        private Dictionary<int, Role> _resultRolesByActor;

        private void ObserveForDigest(OutboundMessage msg)
        {
            try
            {
                if (msg == null || msg.Target != MessageTarget.All) return;
                _matchDigest.Observe(msg.Code, msg.Payload, NowUnixMs(), _session?.Winner);
                if (msg.Code == WWEventCodes.GameOver && _bus != null)
                {
                    TryRecordFinalBalance();
                    _bus.SendToAll(MessageCodes.ResultDigest, _matchDigest.ToWire());
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
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
                RoundDirector rd = RoundDirector.instance;
                if (rd == null) return;
                _matchDigest.RecordExtractionDone(
                    GameRefs.RoundDirector_extractionPointsCompleted(rd),
                    GameRefs.RoundDirector_extractionPoints(rd),
                    NowUnixMs());
            }
            catch (Exception e)
            {
                WLog.Line("digest_extraction_error", secret: false, ("err", e.Message));
            }
        }

        private void HandlePerkUnlockedDigest(PerkId perk)
        {
            _matchDigest.RecordPerkUnlocked((byte)perk, NowUnixMs());
            ReplaySampler.NotePerkUnlocked(perk.ToString());
        }

        private void HandleInformantDigest()
            => _matchDigest.RecordInformant(NowUnixMs());

        private void TryRecordFinalBalance()
        {
            try
            {
                RoundDirector rd = RoundDirector.instance;
                if (rd == null) return;
                int haulGoal = GameRefs.RoundDirector_haulGoal(rd);
                int points = GameRefs.RoundDirector_extractionPoints(rd);
                int completed = GameRefs.RoundDirector_extractionPointsCompleted(rd);
                if (haulGoal <= 0 || points <= 0) return;

                int remaining = CheckmateJudge.RemainingQuotaDollars(haulGoal, points, completed);
                if (remaining < 0) return;
                int delivered = haulGoal / points * completed;
                int obtainable = (int)(ValueTrackPatch.ComputeObtainableDollars() + 0.5f);
                _matchDigest.RecordFinalBalance(delivered, remaining, obtainable, NowUnixMs());
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
            _bus.SendToAll(MessageCodes.ReplayLossLedger, wire);
            WLog.Line("replay_ledger_sent", secret: false,
                ("segments", (int)wire[0]), ("entries", ((int[])wire[1]).Length));
        }

        private void ApplyResultDigest(object[] payload)
        {
            _clientDigestEntries = ResultDigest.FromWire(payload);
            WLog.Line("recv_digest", secret: false,
                ("entries", _clientDigestEntries != null ? _clientDigestEntries.Count : -1));
        }

        private void ClearClientDigest()
        {
            _clientDigestEntries = null;
            _resultRolesByActor = null;
        }

        private void TickResultReturn()
        {
            TickResultChat();
            TickReplayViewer();

            if (!_resultScreen.Visible) return;

            _resultScreen.Tick(ResultChatPointerBlocksWheel());

            int? remaining = _resultCountdown.RemainingSeconds(NowUnixMs());
            int displaySecond = remaining ?? -1;
            if (displaySecond != _lastResultCountdownSecond)
            {
                _lastResultCountdownSecond = displaySecond;
                _resultScreen.SetFooter(BuildResultFooterText(remaining));
            }

            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!_resultSequence.Active) return;

            long now = NowUnixMs();
            TickReturnButton(now);

            if (now < _resultReturnArmedAtUnixMs) return;

            KeyCode key = Plugin.ResultReturnKey != null ? Plugin.ResultReturnKey.Value : KeyCode.F5;
            if (key == KeyCode.None) return;
            if (!SemiFunc.NoTextInputsActive()) return;
            if (Input.GetKeyDown(key))
            {
                WLog.Line("result_return_requested", secret: false, ("via", "key"));
                _resultSequence.RequestReturn();
            }
        }

        private void TickReturnButton(long nowUnixMs)
        {
            bool onButton = _resultReturnFlow.ReadyAt(nowUnixMs)
                && !ResultChatPointerBlocksWheel()
                && _resultScreen.IsPointerOverReturnButton((Vector2)Input.mousePosition);
            ResultReturnButtonEvent ev = _resultReturnFlow.Tick(
                nowUnixMs, Input.GetMouseButtonDown(0), onButton);
            KeyCode key = Plugin.ResultReturnKey != null ? Plugin.ResultReturnKey.Value : KeyCode.F5;
            _resultScreen.SetReturnButton(
                visible: true,
                alpha: _resultReturnFlow.AlphaAt(nowUnixMs),
                armed: _resultReturnFlow.Armed,
                hover: onButton,
                keyName: key != KeyCode.None ? key.ToString() : null);
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
                    _resultSequence.RequestReturn();
                    break;
            }
        }

        private void TickVoidMatch(long nowUnixMs)
        {
            EnsurePanelBuilt(_voidMatchPanel);
            if (!_voidMatchPanel.Exists) return;

            bool available =
                SemiFunc.IsMasterClientOrSingleplayer()
                && _session != null
                && (_session.Phase == GamePhase.Play || _session.Phase == GamePhase.Meeting)
                && NoTextInputsActiveSafe();

            KeyCode key = Plugin.VoidMatchKey != null ? Plugin.VoidMatchKey.Value : KeyCode.F5;
            bool held = available && key != KeyCode.None && Input.GetKey(key);

            bool cancel = _voidMatchHold.Armed && MenuKeyDownSafe();
            if (_voidMatchHold.Armed) SuppressEscMenu();

            VoidMatchHoldEvent result =
                _voidMatchHold.Tick(held, available, cancel, Time.unscaledDeltaTime);

            switch (result)
            {
                case VoidMatchHoldEvent.Armed:
                    WLog.Line("void_match_armed", secret: false, ("phase", _session.Phase));
                    break;
                case VoidMatchHoldEvent.Cancelled:
                    WLog.Line("void_match_cancelled", secret: false);
                    break;
                case VoidMatchHoldEvent.Confirmed:
                    WLog.Line("void_match_confirmed", secret: false, ("phase", _session.Phase));
                    _session.VoidMatch(nowUnixMs);
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

        internal bool IsResultChatActiveClient
            => ResultChatGate.IsOpen(ClientPhase, _resultScreen.Visible, MeetingChatLogEnabled);

        private void TickResultChat()
        {
            bool active = IsResultChatActiveClient;
            if (active) EnsurePanelBuilt(_resultChatPanel);
            if (_resultChatPanel.Exists) _resultChatPanel.SetVisible(active);
            if (!active || !_resultChatPanel.Exists) return;

            UiKit.KeepCursorFree();
            _resultChatPanel.Tick(
                Plugin.MeetingChatLogKey != null ? Plugin.MeetingChatLogKey.Value : KeyCode.L,
                InputGate.KeysFree);
            _resultChatPanel.Render(_chatLog, LocalActor, ChatAvatarResolver,
                ParticipantIdFor, ResultChatMarkedRole, localDead: false);

            if (_resultChatPanel.IsOpen) _chatUnread.Clear();
            _resultChatPanel.SetUnreadBadge(_chatUnread.HasUnread);
        }

        private bool ResultChatPointerBlocksWheel()
            => IsResultChatActiveClient && _resultChatPanel.Exists
               && _resultChatPanel.IsPointerOverPanel((Vector2)Input.mousePosition);

        private void TickReplayViewer()
        {
            bool window = _resultScreen.Visible || _replayViewer.DemoActive;
            if (window && !_replayViewer.Exists)
            {
                EnsurePanelBuilt(_replayViewer);
                if (_resultChatPanel.Exists) _resultChatPanel.EnsureTopSibling();
            }
            if (!_replayViewer.Exists) return;

            _replayViewer.Tick(
                _resultScreen.Visible,
                ReplaySampler.Recorder,
                Plugin.Bindings != null ? Plugin.Bindings.MeetingMapOrthoSize.Value : 15f,
                Plugin.Bindings != null ? Plugin.Bindings.MeetingMapResolution.Value : 1,
                Plugin.ReplayViewerKey != null ? Plugin.ReplayViewerKey.Value : KeyCode.R,
                InputGate.KeysFree,
                p => IsResultChatActiveClient && _resultChatPanel.Exists
                    && _resultChatPanel.IsPointerOverPanel(p),
                ReplaySampler.ExportForUser);
        }

        internal void DebugReplayDemo(int count)
        {
            EnsurePanelBuilt(_replayViewer);
            if (_resultChatPanel.Exists) _resultChatPanel.EnsureTopSibling();
            if (_replayViewer.Exists) _replayViewer.SetDemo(count);
        }

        private void CaptureResultChatContext(int[] actors, byte[] roles)
        {
            var map = new Dictionary<int, Role>(actors.Length);
            for (int i = 0; i < actors.Length && i < roles.Length; i++)
            {
                map[actors[i]] = (Role)roles[i];
            }
            _resultRolesByActor = map;

            _chatLog.Clear();
            _chatUnread.Clear();
            if (_resultChatPanel.Exists) _resultChatPanel.ResetView();
        }

        private Role? ResultChatMarkedRole(int actor)
            => _resultRolesByActor != null && _resultRolesByActor.TryGetValue(actor, out Role role)
                ? role
                : (Role?)null;

        private void ResetVoidMatch()
        {
            _voidMatchHold.Reset();
            if (_voidMatchPanel.Exists)
            {
                _voidMatchPanel.Tick(false, 0f, false, 0, KeyCode.None);
            }
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

        private static bool MenuKeyDownSafe()
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

        private static bool NoTextInputsActiveSafe()
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

        private static string BuildResultFooterText(int? remainingSeconds = null)
        {
            if (SemiFunc.IsMasterClientOrSingleplayer())
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

        private void FreezeResultAutoReturnConfig()
        {
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                _roundGameOverAutoReturnSec = Math.Max(0,
                    Plugin.GameConfig != null ? Plugin.GameConfig.GameOverAutoReturnSec : 0);
                return;
            }

            _roundGameOverAutoReturnSec = 0;
            if (!SettingsCatalog.TryDecodeBlob(_lobbyBlobMirror, out var values)) return;
            if (!values.TryGetValue("GameOverAutoReturnSec", out string raw)) return;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds)) return;
            _roundGameOverAutoReturnSec = Math.Max(0, seconds);
        }

        private readonly ResultReturnButtonFlow _resultReturnFlow = new ResultReturnButtonFlow();

        private void BeginResultCountdown()
        {
            long now = NowUnixMs();
            _resultCountdown.Begin(now, _roundGameOverAutoReturnSec);
            _resultReturnFlow.Begin(now);
            _resultReturnArmedAtUnixMs = now + ResultReturnButtonFlow.ArmDelayMs;
            _lastResultCountdownSecond = -1;
        }

        private void ClearResultCountdown()
        {
            _resultCountdown.Clear();
            _resultReturnFlow.Reset();
            _resultScreen.SetReturnButton(visible: false, alpha: 0f, armed: false, hover: false, keyName: null);
            _roundGameOverAutoReturnSec = 0;
            _resultReturnArmedAtUnixMs = 0L;
            _lastResultCountdownSecond = -1;
        }
    }
}
