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

        private CurseTargetSource _curseSource;

        private Dictionary<int, string> _displayNameCache;

        private readonly CatAwakenToastGate _catAwakenGate = new CatAwakenToastGate();

        private void TickRolesClient(long now)
        {
            Role? effectiveRole = IsLocalAlive() ? _localRole : null;
            _perkEffects.Tick(RolesClient, effectiveRole, SendRoleAction, OnWolfModeChangedLocal);

            bool healEligible = effectiveRole == Role.Werewolf && RolesClient.HealActive
                && _clientPhase == GamePhase.Play && !_meetingClient.MeetingActive;
            _perkEffects.TickHeal(healEligible, now, ClientHealIntervalSec);

            if (_curseSource != null && !RolesClient.CurseActive(now))
            {
                DetachCurseSource("expired");
            }

            if (IsRoundActiveClient) EnsureRolesUiBuilt();

            if (_gaugePanel.Exists) _gaugePanel.Tick(RolesClient, _meetingClient, now);
            if (_playGaugePanel.Exists)
            {
                _playGaugePanel.TickPlay(RolesClient, _meetingClient,
                    _session != null ? _session.Phase : _clientPhase, _localRole, now);
            }
            if (_hudPanel.Exists)
            {
                var uiBindings = Plugin.Bindings;
                if (uiBindings != null)
                {
                    _hudPanel.PositionOffset = new UnityEngine.Vector2(
                        uiBindings.HudOffsetX.Value, uiBindings.HudOffsetY.Value);
                }

                int rights = 0;
                if (_meetingButton != null && _meetingButton.IsLocalPlayerNear)
                {
                    _roomState.TryReadRights(LocalActor, out rights);
                }
                GamePhase hudPhase = _session != null ? _session.Phase : _clientPhase;
                var hudInput = new HudInput(
                    phase: hudPhase,
                    localRole: _localRole,
                    roundEndUnixMs: _clientRoundEndUnixMs,
                    rolesClient: RolesClient,
                    nearMeetingButton: _meetingButton != null && _meetingButton.IsLocalPlayerNear,
                    rightsRemaining: rights,
                    nowUnixMs: now,
                    meetingActive: _meetingClient.MeetingActive,
                    debugSession: _clientDebugSession,
                    selfParticipantId: IdRoster.IdOf(LocalActor),
                    scatterGuardActive: _clientScatterGuard.IsArmed(now));
                HudState hudState = _hudPanel.Tick(_hudModel, hudInput);

                if (hudState.BellVolumeScale > 0f)
                {
                    EnsureSfxBuilt();
                    _sfxPlayer.Play(BellSchedule.ClipKeyFor(hudState.BellMarkSec), hudState.BellVolumeScale);
                }

                if (hudState.BellMarkSec == BellSchedule.AlertThresholdSec) ShowDeadlineBanner();
            }

            if (_deadlineBanner.Exists) _deadlineBanner.Tick();
            if (_discussionImpact.Exists) _discussionImpact.Tick();

            if (_toastPanel.Exists && _toastQueue != null) _toastPanel.Tick(_toastQueue, now);

            bool warpedInMeeting = _meetingClient.MeetingActive && _meetingClient.WarpDone(now);
            if (_wolfStatusPanel.Exists)
            {
                WolfStatusState wolfStatus = _wolfStatusModel.Compute(
                    RolesClient, effectiveRole,
                    _session != null ? _session.Phase : _clientPhase, warpedInMeeting, now,
                    ClientExtraJumpCount, InfiniteJumpPatch.RefillsThisAirTime,
                    InfiniteJumpPatch.InjectedChargeAvailable);
                _wolfStatusPanel.Tick(wolfStatus,
                    Plugin.WolfModeKey != null ? Plugin.WolfModeKey.Value.ToString() : "?",
                    Plugin.BeaconKey != null ? Plugin.BeaconKey.Value.ToString() : "?");
            }

            GamePhase crPhase = _session != null ? _session.Phase : _clientPhase;
            CorpseReportHudMode crMode = CorpseReportHudGate.Compute(
                crPhase, IsLocalAlive(), _meetingClient.MeetingActive, warpedInMeeting,
                LastRunGate.IsLastRunActive());
            CorpseReportHudPanel.Layout crLayout = RolesClient != null && RolesClient.PlayGauge != null
                ? CorpseReportHudPanel.Layout.AboveMiniGauge
                : CorpseReportHudPanel.Layout.AtGaugeSlot;
            if (_corpseReportHud.Exists)
            {
                string crKey = Plugin.CorpseReportKey != null
                    ? Plugin.CorpseReportKey.Value.ToString() : "?";
                _corpseReportHud.Tick(crMode, LocalIsNearUnannouncedCorpse,
                    crKey, crLayout);
            }

            TickValuableRecord(crPhase, warpedInMeeting, crLayout);
        }

        private void TickValuableRecord(GamePhase phase, bool warpedInMeeting,
                                        CorpseReportHudPanel.Layout corpseHudLayout)
        {
            bool canOperate = ValuableRecordGate.CanOperate(
                _localRole, IsLocalAlive(), phase, warpedInMeeting);

            if (!canOperate)
            {
                _valuableRecordHold.Reset();
            }
            else
            {
                KeyCode key = Plugin.CorpseReportKey != null ? Plugin.CorpseReportKey.Value : KeyCode.R;
                bool held = InputGate.KeysFree && Input.GetKey(key);
                if (_valuableRecordHold.Tick(held, CorpseReportConsumedPress, Time.deltaTime)
                    && RolesClient.ToggleValuableRecord(_localRole))
                {
                    EnsureSfxBuilt();
                    _sfxPlayer.Play(RolesClient.ValuableRecordOn
                        ? "sfx_valuable_record_on"
                        : "sfx_valuable_record_off");
                }
            }

            if (_valuableRecordHud.Exists)
            {
                _valuableRecordHud.Tick(canOperate, RolesClient.ValuableRecordOn,
                    _valuableRecordHold.Ratio, _valuableRecordHold.IsCharging,
                    Plugin.CorpseReportKey != null ? Plugin.CorpseReportKey.Value.ToString() : "?",
                    corpseHudLayout);
            }
        }

        internal bool LocalValuableDiscoverSuppressed
            => ValuableRecordGate.ShouldSuppressDiscover(
                _localRole, IsLocalAlive(), IsRoundActiveClient, RolesClient.ValuableRecordOn);

        private void EnsureSfxBuilt()
        {
            if (_sfxPlayer.Exists) return;
            _sfxPlayer.Build(gameObject);
        }

        private void OnWolfModeChangedLocal(bool wolfModeOn)
        {
            if (!wolfModeOn) return;
            EnsureSfxBuilt();
            _sfxPlayer.Play("sfx_howl");
        }

        private void HideConveneCountdown()
        {
            if (_conveneTweenCoroutine != null)
            {
                StopCoroutine(_conveneTweenCoroutine);
                _conveneTweenCoroutine = null;
            }
            _conveneCountdown.Hide();
        }

        private void TryStartRoleReveal()
        {
            try
            {
                if (_revealStarted) return;
                if (_localRole == null) return;
                if (_clientRoundEndUnixMs == 0) return;

                Role role = _localRole.Value;
                bool isWolfTeam = role == Role.Werewolf || role == Role.Bomber;
                if (isWolfTeam && _knownWerewolves == null && _clientWerewolfCount != 1) return;

                string[] teammateNames;
                if (isWolfTeam && _knownWerewolves != null)
                {
                    var list = new List<string>(_knownWerewolves.Length);
                    int selfActor = LocalActor;
                    for (int i = 0; i < _knownWerewolves.Length; i++)
                    {
                        int a = _knownWerewolves[i];
                        if (a == selfActor) continue;
                        string name = ParticipantLabel.Format(IdRoster.IdOf(a), ResolveDisplayName(a));
                        if (_knownTeammateRoles != null && i < _knownTeammateRoles.Length
                            && (Role)_knownTeammateRoles[i] == Role.Bomber)
                        {
                            name = name + "（" + Texts.Get(TextId.RoleNameBomber) + "）";
                        }
                        list.Add(name);
                    }
                    teammateNames = list.ToArray();
                }
                else
                {
                    teammateNames = Array.Empty<string>();
                }

                RevealContent content = RevealScript.Build(role, teammateNames, _clientCatPossible,
                    ClientValuableMapMode, IsBlackCatCurseEnabledForClient(),
                    IdRoster.IdOf(LocalActor));

                EnsurePanelBuilt(_revealCinematic);
                if (!_revealCinematic.Exists)
                {
                    _revealStarted = true;
                    WLog.Line("reveal_skip", secret: false, ("reason", "not_built"));
                    return;
                }

                if (_revealCoroutine != null)
                {
                    StopCoroutine(_revealCoroutine);
                    _revealCoroutine = null;
                }
                _revealCoroutine = StartCoroutine(_revealCinematic.Play(content));
                _revealStarted = true;
                WLog.Line("reveal_start", secret: true,
                    ("role", role), ("catPossible", _clientCatPossible),
                    ("teammates", teammateNames.Length));
            }
            catch (Exception e)
            {
                WLog.Line("reveal_start_error", secret: false, ("err", e.Message));
            }
        }

        private void TickCatAwakenToast(long now)
        {
            if (_catAwakenGate.ShouldFire(_clientPhase, _clientCatPossible,
                    _gameStartUnixMsClient, _clientRevealDelaySec, now))
            {
                PushToast(SessionNotice.ForCatAwakened());
                WLog.Line("cat_awaken_toast", secret: false,
                    ("delaySec", _clientRevealDelaySec));
                if (LocalIsVillagerTeam) MaybeShowTutorial(TutorialId.VillagerSeesCatAwakened);
                else if (_localRole == Role.Werewolf) MaybeShowTutorial(TutorialId.WerewolfSeesCatAwakened);
            }
        }

        private void TryStartAwakeningReveal()
        {
            try
            {
                if (_awakeningRevealStarted) return;
                if (_localRole != Role.BlackCat) return;

                RevealContent content = RevealScript.BuildBlackCatAwakening(IsBlackCatCurseEnabledForClient());

                if (_revealCoroutine != null)
                {
                    StopCoroutine(_revealCoroutine);
                    _revealCoroutine = null;
                }
                if (_revealCinematic.Exists) _revealCinematic.HideNow();

                EnsurePanelBuilt(_catAwakenToast);
                if (!_catAwakenToast.Exists)
                {
                    _awakeningRevealStarted = true;
                    WLog.Line("awakening_reveal_skip", secret: false, ("reason", "not_built"));
                    return;
                }

                if (_catAwakenToastCoroutine != null)
                {
                    StopCoroutine(_catAwakenToastCoroutine);
                    _catAwakenToastCoroutine = null;
                }
                _catAwakenToastCoroutine = StartCoroutine(PlayBlackCatAwakeningAndTutorial(content));
                _awakeningRevealStarted = true;
                WLog.Line("awakening_reveal_start", secret: true);
            }
            catch (Exception e)
            {
                WLog.Line("awakening_reveal_error", secret: false, ("err", e.Message));
            }
        }

        private IEnumerator PlayBlackCatAwakeningAndTutorial(RevealContent content)
        {
            yield return _catAwakenToast.Play(content);
            _catAwakenToastCoroutine = null;

            if (_localRole == Role.BlackCat)
                MaybeShowTutorial(TutorialId.BlackCatRoleDrawn);
        }

        private void ShowResultScreen(byte winningTeam, int[] actors, byte[] roles)
        {
            try
            {
                if (actors == null || roles == null)
                {
                    WLog.Line("result_screen_skip", secret: false, ("reason", "null_payload"));
                    return;
                }

                List<int> disconnected = null;
                foreach (var pair in _meetingClient.Rows)
                {
                    if (pair.Value != RowStatus.Disconnected) continue;
                    (disconnected ??= new List<int>()).Add(pair.Key);
                }

                IReadOnlyList<ResultRow> rows = ResultModel.Build(
                    winningTeam, actors, roles, _deathMirror, ResolveDisplayName, disconnected);

                SetCrownRosterFromRows(rows);

                EnsurePanelBuilt(_resultScreen);
                if (!_resultScreen.Exists)
                {
                    WLog.Line("result_screen_skip", secret: false, ("reason", "not_built"));
                    return;
                }

                List<string> digestLines = _clientDigestEntries != null
                    ? ResultDigestText.FormatLines(_clientDigestEntries, ResolveDisplayName)
                    : null;
                _resultScreen.Show(winningTeam, rows, ResolveAvatar,
                    digestLines, BuildResultFooterText(), ParticipantIdFor);
                CaptureResultChatContext(actors, roles);
                PlayResultSfx(winningTeam);
            }
            catch (Exception e)
            {
                WLog.Line("result_screen_error", secret: false, ("err", e.Message));
            }
        }

        private void PlayResultSfx(byte winningTeam)
        {
            string clipKey;
            switch ((Team)winningTeam)
            {
                case Team.Werewolves: clipKey = "sfx_result_wolves_win"; break;
                case Team.Villagers:  clipKey = "sfx_result_villagers_win"; break;
                default: return;
            }
            EnsureSfxBuilt();
            _sfxPlayer.Play(clipKey);
        }

        private static void SetCrownRosterFromRows(IReadOnlyList<ResultRow> rows)
        {
            var winners = new List<int>();
            foreach (ResultRow row in rows)
            {
                if (row.IsWinningSide) winners.Add(row.ActorNumber);
            }
            CrownRoster.SetWinners(winners);
            WLog.Line("crown_roster_set", secret: false, ("count", winners.Count));
        }

        private void EnsureRolesUiBuilt()
        {
            EnsurePanelBuilt(_gaugePanel);
            EnsurePanelBuilt(_playGaugePanel);
            EnsurePanelBuilt(_hudPanel);
            EnsurePanelBuilt(_deadlineBanner);
            EnsurePanelBuilt(_discussionImpact);
            EnsurePanelBuilt(_toastPanel);
            EnsurePanelBuilt(_wolfStatusPanel);
            EnsurePanelBuilt(_corpseReportHud);
            EnsurePanelBuilt(_valuableRecordHud);
        }

        public void ShowDeadlineBanner()
        {
            EnsurePanelBuilt(_deadlineBanner);
            if (!_deadlineBanner.Exists)
            {
                WLog.Line("deadline_banner_skipped", secret: false, ("reason", "not_built"));
                return;
            }
            _deadlineBanner.Show(
                Texts.Get(TextId.BannerMeetTheDeadline),
                Texts.Get(TextId.BannerHurryUp));
            WLog.Line("deadline_banner_shown", secret: false);
        }

        public void ShowDiscussionImpact()
        {
            EnsurePanelBuilt(_discussionImpact);
            if (!_discussionImpact.Exists)
            {
                WLog.Line("discussion_impact_skipped", secret: false, ("reason", "not_built"));
                return;
            }
            _discussionImpact.Show(
                Texts.Get(TextId.ImpactDiscussLeft),
                Texts.Get(TextId.ImpactDiscussRight),
                () =>
                {
                    EnsureSfxBuilt();
                    _sfxPlayer.Play(DiscussionImpactClipKey);
                });
        }

        private const string DiscussionImpactClipKey = "sfx_discussion_start";

        private const int ToastDurationFallbackSec = 9;

        private static int ToastDurationSec()
            => Plugin.GameConfig != null && Plugin.GameConfig.ToastDurationSec > 0
                ? Plugin.GameConfig.ToastDurationSec : ToastDurationFallbackSec;

        private void EnsureToastQueue()
        {
            _toastQueue ??= new ToastQueue(ToastDurationSec());
        }

        private void PushToast(SessionNotice notice)
        {
            if (notice == null) return;
            string message = NoticeCatalog.Format(notice);
            if (string.IsNullOrEmpty(message)) return;

            EnsureToastQueue();
            EnsureRolesUiBuilt();
            _toastQueue.Push(message, NowUnixMs());
            EnsureSfxBuilt();
            _sfxPlayer.Play(NoticeSfx.Resolve(notice));
            WLog.Line("toast_push", secret: false, ("kind", notice.Kind));
        }

        private void HandleRoleState(byte subtype, int[] data, long timeUnixMs)
        {
            switch (subtype)
            {
                case RoleStateSubtype.CurseStarted:
                    if (data == null || data.Length < 1) return;
                    int catActor = data[0];
                    WLog.Line("recv_cursestart", secret: false,
                        ("cat", catActor), ("deadline", timeUnixMs));
                    if (_resultCeremonyAtMs > 0)
                    {
                        _pendingCurseCatActor = catActor;
                        _pendingCurseDeadlineMs = timeUnixMs;
                        break;
                    }
                    PresentCurseStarted(catActor, timeUnixMs);
                    break;
                case RoleStateSubtype.CurseResolved:
                    if (data == null || data.Length < 1) return;
                    int victimActor = data[0];
                    PushToast(SessionNotice.ForCurseVictim(ResolveDisplayName(victimActor)));
                    RolesClient.ApplyCurseResolved();
                    DetachCurseSource("resolved");
                    if (_votePanel.Exists)
                    {
                        _votePanel.ShowStatusBanner(victimActor != CurseResolution.NoVictim
                            ? Texts.Format(TextId.NoticeCurseVictimFormat, ResolveDisplayName(victimActor))
                            : Texts.Get(TextId.CurseNoVictim));
                        _votePanel.SetTimeOverride(0);
                    }
                    WLog.Line("recv_curseresolve", secret: false, ("victim", victimActor));
                    break;
                case RoleStateSubtype.MeetingGauge:
                    RolesClient.ApplyMeetingGauge(data);
                    WLog.Line("recv_meetinggauge", secret: false,
                        ("ratio", data != null && data.Length > 0 ? data[0] : -1));
                    break;
            }
        }

        private void PresentCurseStarted(int catActor, long timeUnixMs)
        {
            RolesClient.ApplyCurseStarted(catActor, timeUnixMs);
            PushToast(SessionNotice.ForBlackCatRevealed(ResolveDisplayName(catActor)));
            EnsureSfxBuilt();
            _sfxPlayer.Play("sfx_execution_curse");
            if (catActor == LocalActor)
            {
                AttachCurseSource();
                MaybeShowTutorial(TutorialId.BlackCatSelectedForExecution);
            }
            else
            {
                if (_votePanel.Exists)
                {
                    _votePanel.ShowStatusBanner(
                        Texts.Format(TextId.CurseBlackCatRevealedFormat, ResolveDisplayName(catActor)));
                    _votePanel.SetTimeOverride(timeUnixMs);
                }
                MaybeShowTutorial(TutorialId.BlackCatExecutionRevealed);
            }
        }

        private void AttachCurseSource()
        {
            if (!_votePanel.Exists)
            {
                WLog.Line("curse_ui_skipped", secret: false, ("reason", "no_vote_panel"));
                return;
            }
            IReadOnlyList<WPlayer> roster = _session != null
                ? _session.Players
                : Registry.BuildRealPlayers();
            _curseSource = new CurseTargetSource(RolesClient, _meetingClient, roster,
                NowUnixMs, SendCurseDesignate);
            _votePanel.SetSelectionSource(_curseSource);
            _votePanel.SetTimeOverride(RolesClient.CurseDeadlineUnixMs);
            WLog.Line("curse_ui_attached", secret: false);
        }

        private void DetachCurseSource(string via)
        {
            if (_curseSource == null) return;
            _curseSource = null;
            _votePanel?.SetSelectionSource(null);
            _votePanel?.SetTimeOverride(0);
            WLog.Line("curse_ui_detached", secret: false, ("via", via));
        }

        private void SendCurseDesignate(int targetActor)
            => SendRoleAction(RoleActionSubtype.CurseDesignate, targetActor, 0);

        private void SendRoleAction(byte subtype, int arg, byte flag)
        {
            if (_bus == null)
            {
                WLog.Line("roleaction_send_fail", secret: true,
                    ("subtype", subtype), ("reason", "no_bus"));
                return;
            }
            _bus.SendToMaster(MessageCodes.RoleAction, new object[] { subtype, arg, flag });
        }

        private void ResetRolesClient(string via)
        {
            DetachCurseSource(via);
            RolesClient.Reset();
            _perkEffects.ResetEffects();
            _hudPanel?.Hide();
            _wolfStatusPanel?.Hide();
            _wolfStatusModel.Reset();
        }

        internal string DisplayNameForActor(int actorNumber) => ResolveDisplayName(actorNumber);

        private string ResolveDisplayName(int actorNumber)
        {
            if (_session != null)
            {
                foreach (WPlayer p in _session.Players)
                {
                    if (p != null && p.ActorNumber == actorNumber && !string.IsNullOrEmpty(p.Name))
                    {
                        return p.Name;
                    }
                }
                return "#" + actorNumber;
            }

            if (_displayNameCache != null && _displayNameCache.TryGetValue(actorNumber, out string cached))
            {
                return cached;
            }
            if (_displayNameCache == null) _displayNameCache = new Dictionary<int, string>();
            foreach (WPlayer p in Registry.BuildRealPlayers())
            {
                if (p != null && !string.IsNullOrEmpty(p.Name))
                {
                    _displayNameCache[p.ActorNumber] = p.Name;
                }
            }
            if (_displayNameCache.TryGetValue(actorNumber, out string resolved))
            {
                return resolved;
            }
            return "#" + actorNumber;
        }

    }
}
