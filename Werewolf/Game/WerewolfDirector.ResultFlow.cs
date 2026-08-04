using System;
using System.Globalization;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private readonly ResultDigest _matchDigest = new ResultDigest();

        private System.Collections.Generic.IReadOnlyList<DigestEntry> _clientDigestEntries;

        private void ObserveForDigest(OutboundMessage msg)
        {
            try
            {
                if (msg == null || msg.Target != MessageTarget.All) return;
                _matchDigest.Observe(msg.Code, msg.Payload, NowUnixMs(), _session?.Winner);
                if (msg.Code == WWEventCodes.GameOver && _bus != null)
                {
                    TryRecordFinalBalance();
                    _bus.SendToAll(EventCodes.ResultDigest, _matchDigest.ToWire());
                    WLog.Line("digest_sent", secret: false, ("entries", _matchDigest.Entries.Count));
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
            => _matchDigest.RecordPerkUnlocked((byte)perk, NowUnixMs());

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

        private void ApplyResultDigest(object[] payload)
        {
            _clientDigestEntries = ResultDigest.FromWire(payload);
            WLog.Line("recv_digest", secret: false,
                ("entries", _clientDigestEntries != null ? _clientDigestEntries.Count : -1));
        }

        private void ClearClientDigest()
        {
            _clientDigestEntries = null;
        }

        private void TickResultReturn()
        {
            if (!_resultScreen.Visible) return;

            _resultScreen.Tick();

            int? remaining = _resultCountdown.RemainingSeconds(NowUnixMs());
            int displaySecond = remaining ?? -1;
            if (displaySecond != _lastResultCountdownSecond)
            {
                _lastResultCountdownSecond = displaySecond;
                _resultScreen.SetFooter(BuildResultFooterText(remaining));
            }

            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!_resultSequence.Active) return;

            KeyCode key = Plugin.ResultReturnKey != null ? Plugin.ResultReturnKey.Value : KeyCode.F5;
            if (key == KeyCode.None) return;
            if (!SemiFunc.NoTextInputsActive()) return;
            if (Input.GetKeyDown(key))
            {
                WLog.Line("result_return_requested", secret: false);
                _resultSequence.RequestReturn();
            }
        }

        private static string BuildResultFooterText(int? remainingSeconds = null)
        {
            string baseText;
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                string keyName = Plugin.ResultReturnKey != null
                    ? Plugin.ResultReturnKey.Value.ToString() : KeyCode.F5.ToString();
                baseText = Texts.Format(TextId.ResultReturnPromptFormat, keyName);
            }
            else
            {
                baseText = Texts.Get(TextId.ResultWaitingHost);
            }

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

        private void BeginResultCountdown()
        {
            _resultCountdown.Begin(NowUnixMs(), _roundGameOverAutoReturnSec);
            _lastResultCountdownSecond = -1;
        }

        private void ClearResultCountdown()
        {
            _resultCountdown.Clear();
            _roundGameOverAutoReturnSec = 0;
            _lastResultCountdownSecond = -1;
        }
    }
}
