using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        public void DebugPlayConveneCountdown(string callerName, int totalSeconds)
        {
            try
            {
                EnsurePanelBuilt(_conveneCountdown);
                if (!_conveneCountdown.Exists)
                {
                    WLog.Line("cmd_fx_countdown_skipped", secret: false, ("reason", "not_built"));
                    return;
                }
                if (totalSeconds < 0) totalSeconds = 0;
                if (_conveneTweenCoroutine != null) StopCoroutine(_conveneTweenCoroutine);
                _conveneTweenCoroutine = StartCoroutine(
                    _conveneCountdown.PlayStandalone(callerName, totalSeconds));
                WLog.Line("cmd_fx_countdown", secret: false,
                    ("caller", callerName ?? ""), ("seconds", totalSeconds));
            }
            catch (Exception e)
            {
                WLog.Line("cmd_fx_countdown_error", secret: false, ("err", e.Message));
            }
        }

        private int DebugRevealSelfId
        {
            get
            {
                int id = IdRoster.IdOf(LocalActor);
                return id > 0 ? id : 7;
            }
        }

        public void DebugPlayReveal(Role role)
        {
            try
            {
                string[] dummyTeammates = role == Role.Werewolf
                    ? new[]
                    {
                        ParticipantLabel.Format(3, "テスト太郎"),
                        ParticipantLabel.Format(12, "テスト次郎"),
                    }
                    : Array.Empty<string>();
                RevealContent content = RevealScript.Build(role, dummyTeammates, blackCatPossible: true,
                    ClientValuableMapMode, IsBlackCatCurseEnabledForClient(),
                    DebugRevealSelfId);

                EnsurePanelBuilt(_revealCinematic);
                if (!_revealCinematic.Exists)
                {
                    WLog.Line("cmd_fx_reveal_skipped", secret: false, ("reason", "not_built"));
                    return;
                }

                if (_revealCoroutine != null)
                {
                    StopCoroutine(_revealCoroutine);
                    _revealCoroutine = null;
                }
                _revealCoroutine = StartCoroutine(_revealCinematic.Play(content));
                WLog.Line("cmd_fx_reveal", secret: false, ("role", role));
            }
            catch (Exception e)
            {
                WLog.Line("cmd_fx_reveal_error", secret: false, ("err", e.Message));
            }
        }

        public void DebugPlayToast(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message)) message = "テスト通知";

                EnsureToastQueue();
                EnsureRolesUiBuilt();
                _toastQueue.Push(message, NowUnixMs());
                WLog.Line("cmd_fx_toast", secret: false, ("message", message));
            }
            catch (Exception e)
            {
                WLog.Line("cmd_fx_toast_error", secret: false, ("err", e.Message));
            }
        }

        public void DebugPlayResult()
        {
            try
            {
                ClearResultCountdown();
                int[] actors = { 1, 2, 3, 4, 5 };
                byte[] roles =
                {
                    (byte)Role.Villager, (byte)Role.Villager, (byte)Role.Werewolf, (byte)Role.BlackCat, (byte)Role.Villager,
                };
                var deathMirror = new Dictionary<int, DeathCause>
                {
                    { 2, DeathCause.Other },
                    { 4, DeathCause.Vote },
                };
                var disconnected = new[] { 5 };

                IReadOnlyList<ResultRow> rows = ResultModel.Build(
                    (byte)Team.Werewolves, actors, roles, deathMirror, a => "デバッグ" + a, disconnected);

                EnsurePanelBuilt(_resultScreen);
                if (!_resultScreen.Exists)
                {
                    WLog.Line("cmd_fx_result_skipped", secret: false, ("reason", "not_built"));
                    return;
                }
                var digest = new List<DigestEntry>
                {
                    new DigestEntry(DigestKind.MatchStart, 0, 0, 0, 0),
                    new DigestEntry(DigestKind.PerkUnlocked, 60, 0,
                        (int)PerkId.InfiniteStamina, 0),
                    new DigestEntry(DigestKind.Death, 95, 2, 0, 0),
                    new DigestEntry(DigestKind.MeetingConvened, 130, 1, 1, 0),
                    new DigestEntry(DigestKind.InformantEstablished, 180, 0, 0, 0),
                    new DigestEntry(DigestKind.Executed, 200, 4, 0, 0),
                    new DigestEntry(DigestKind.CurseStarted, 200, 4, 0, 0),
                    new DigestEntry(DigestKind.CurseFollow, 210, 3, 0, 0),
                    new DigestEntry(DigestKind.ExtractionDone, 250, 0, 1, 4),
                    new DigestEntry(DigestKind.MatchEnd, 300, 0,
                        (int)Team.Werewolves, (int)WinReason.TimerExpired),
                    new DigestEntry(DigestKind.FinalBalance, 300, 9_500, 12_000, 8_000),
                };
                List<string> digestLines = ResultDigestText.FormatLines(digest, a => "デバッグ" + a);
                _resultScreen.Show((byte)Team.Werewolves, rows, ResolveAvatar,
                    digestLines, BuildResultFooterText(), ParticipantIdFor);
                PlayResultSfx((byte)Team.Werewolves);
                SetCrownRosterFromRows(rows);
                WLog.Line("cmd_fx_result", secret: false, ("rows", rows.Count));
            }
            catch (Exception e)
            {
                WLog.Line("cmd_fx_result_error", secret: false, ("err", e.Message));
            }
        }

        public void DebugPlaySfx(string clipKey)
        {
            try
            {
                EnsureSfxBuilt();
                _sfxPlayer.Play(clipKey);
                WLog.Line("cmd_fx_sfx", secret: false, ("key", clipKey), ("canPlay", _sfxPlayer.CanPlay));
            }
            catch (Exception e)
            {
                WLog.Line("cmd_fx_sfx_error", secret: false, ("err", e.Message));
            }
        }

        public void DebugClearFx()
        {
            try
            {
                if (_revealCoroutine != null)
                {
                    StopCoroutine(_revealCoroutine);
                    _revealCoroutine = null;
                }
                if (_revealCinematic.Exists) _revealCinematic.HideNow();

                HideConveneCountdown();

                if (_resultScreen.Exists) _resultScreen.Hide();
                ClearResultCountdown();

                if (_deadlineBanner.Exists) _deadlineBanner.Hide();
                if (_discussionImpact.Exists) _discussionImpact.Hide();

                CrownRoster.Clear();

                WLog.Line("cmd_fx_clear", secret: false);
            }
            catch (Exception e)
            {
                WLog.Line("cmd_fx_clear_error", secret: false, ("err", e.Message));
            }
        }

        public void DebugInjectCfgBlob(string blob)
        {
            _debugInjectedBlob = blob;
            WLog.Line("cmd_cfg_inject", secret: false,
                ("len", blob == null ? 0 : blob.Length));
        }

        public void DebugClearCfgBlob()
        {
            _debugInjectedBlob = null;
            WLog.Line("cmd_cfg_clear", secret: false);
        }

    }
}
