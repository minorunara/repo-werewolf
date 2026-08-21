using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private bool _pendingMeetingTutorial;

        private long _nextConveneHoldHintUnixMs;

        private readonly MeetingMapOverlay _meetingMapOverlay = new MeetingMapOverlay();

        private long _clientWarpUnixMs;
        private bool _warpExecuted;
        private bool _meetingCutByCeremony;

        private bool _votePendulumPlayed;

        private Vector3? _warpVerifyTarget;
        private long _warpVerifyDeadlineMs;

        private readonly ConveneHoldGauge _conveneHoldGauge = new ConveneHoldGauge();

        private void HandleStartMeeting(int caller, long warpUnixMs, long endUnixMs,
                                        ConveneKind kind = ConveneKind.Button)
        {
            _clientWarpUnixMs = warpUnixMs;
            _warpExecuted = false;
            _votePendulumPlayed = false;
            _meetingClient.ApplyStartMeeting(caller, warpUnixMs, endUnixMs, kind);
            _scatterPlanAtUnixMs = 0;
            _scatterAwaitCurse = false;
            _extractionScatter?.ClearPlan();
            _scatterGuard.Disarm();
            _clientScatterGuard.Disarm();
            Patches.PlayerSpawnPatch.MeetingActive = true;
            _deathRevealPending = true;
            ResetMeetingChatView();
            WLog.Line("recv_startmeeting", secret: false,
                ("caller", caller), ("warpUnixMs", warpUnixMs), ("endUnixMs", endUnixMs));
            BeginMeetingUi();

            MaybeShowTutorial(TutorialId.MeetingCountdownStarted);
            _pendingMeetingTutorial = true;
        }

        private void TickMeetingClient(long now)
        {
            TryRestoreMeetingFromRoomState();

            bool gameActive = _session != null
                || _clientPhase == GamePhase.Play || _clientPhase == GamePhase.Meeting;
            if (gameActive && SemiFunc.RunIsLevel())
            {
                if (!_meetingButton.Exists && TruckSafetySpawnPoint.instance != null)
                {
                    _meetingButton.Create();
                }
                long buttonNow = now;
                long readyAtUnixMs = ComputeMeetingReadyAt();
                int rightsForPrompt = 0;
                _roomState.TryReadRights(LocalActor, out rightsForPrompt);
                _meetingButton.Tick(LocalCanConvene(), readyAtUnixMs, buttonNow, rightsForPrompt);

                EnsurePanelBuilt(_conveneHoldGauge);
                _conveneHoldGauge.Tick(_meetingButton.HoldCharging, _meetingButton.HoldRatio,
                    _meetingButton.VisualWorldPos);

                TickCorpseReport();
            }
            else
            {
                if (_meetingButton.Exists) _meetingButton.Destroy();
                if (_conveneHoldGauge.Exists) _conveneHoldGauge.Tick(false, 0f, null);
            }

            bool active = _meetingClient.MeetingActive;

            if (GameOverSafety.ShouldInjectInvincibility(
                    ClientPhase, active, _meetingClient.WarpDone(now), _winCeremonyActive))
            {
                TrySetLocalInvincible();
            }

            if (active && !_warpExecuted && (_winCeremonyActive || _meetingCutByCeremony))
            {
                _meetingCutByCeremony = true;
                if (_conveneCountdown.Visible) HideConveneCountdown();
                if (GameOverSafety.ShouldHoldEnemyFreeze(ClientPhase, _winCeremonyActive)
                    && SemiFunc.IsMasterClientOrSingleplayer() && !_enemyFreezer.Active)
                {
                    _enemyFreezer.Begin();
                }
            }
            else if (active && !_meetingClient.WarpDone(now))
            {
                int remainSec = (int)((_clientWarpUnixMs - now + 999) / 1000);
                EnsurePanelBuilt(_conveneCountdown);
                if (_conveneCountdown.Exists)
                {
                    TextId headerFormat = _meetingClient.Kind == ConveneKind.CorpseReport
                        ? TextId.ConveneCountdownCorpseHeaderFormat
                        : _meetingClient.Kind == ConveneKind.ScatterGuard
                            ? TextId.ConveneCountdownScatterGuardHeader
                            : TextId.ConveneCountdownHeaderFormat;
                    System.Collections.IEnumerator secondTween = _conveneCountdown.Tick(
                        ResolveDisplayName(_meetingClient.CallerActor), remainSec, headerFormat);
                    if (secondTween != null)
                    {
                        if (_conveneTweenCoroutine != null) StopCoroutine(_conveneTweenCoroutine);
                        _conveneTweenCoroutine = StartCoroutine(secondTween);
                        EnsureSfxBuilt();
                        _sfxPlayer.Play("sfx_countdown");
                    }
                }
            }
            else if (active)
            {
                if (!_warpExecuted)
                {
                    _warpExecuted = true;
                    ReplaySampler.NoteMeetingWarp();
                    ExecuteWarpMoment();
                    Patches.ValuableMapSyncPatch.RefreshSnapshot("meeting_warp");
                    if (_deathRevealPending)
                    {
                        _deathRevealPending = false;
                        StartDeathReveal();
                    }
                }
                if (_conveneCountdown.Visible) HideConveneCountdown();
                if (_pendingBeaconAudit >= 0 && _meetingClient.VotingUiReady(now))
                {
                    _chatRecapBeaconUses = _pendingBeaconAudit;
                    _pendingBeaconAudit = -1;
                }
                if (!_chatSystemPosted && _meetingClient.VotingUiReady(now))
                {
                    List<List<int>> lastGroups = _lastScatterGroups;
                    _lastScatterGroups = null;
                    int lostSince = ConsumeRecapLostDelta();
                    _chatMeetingNumber++;
                    if (MeetingChatLogEnabled) PostMeetingChatSystemLines(lastGroups, lostSince, _chatMeetingNumber);
                    _chatSystemPosted = true;
                    _meetingClient.MarkDiscussionOpen();
                    ShowDiscussionImpact();
                }
                if (_pendingMeetingTutorial && _meetingClient.VotingUiReady(now))
                {
                    if (LocalIsVillagerTeam) MaybeShowTutorial(TutorialId.FirstMeetingAsVillager);
                    else if (_localRole == Role.Werewolf) MaybeShowTutorial(TutorialId.FirstMeetingAsWerewolf);
                    else if (_localRole == Role.BlackCat) MaybeShowTutorial(TutorialId.FirstMeetingAsBlackCat);
                    _pendingMeetingTutorial = false;
                }
                _movementFreezer.Tick(IsLocalAlive());

                if (SemiFunc.IsMasterClientOrSingleplayer() && _session != null)
                {
                    _truckWarper.TickWatchdog(IsSessionAlive, now);
                }

                TickScatterPlanHost(now);

                if (!_votePendulumPlayed)
                {
                    long remainingMs = _meetingClient.RemainingMs(now);
                    if (remainingMs > 0 && remainingMs <= 10000)
                    {
                        _votePendulumPlayed = true;
                        EnsureSfxBuilt();
                        _sfxPlayer.PlayStoppable("sfx_vote_pendulum");
                        WLog.Line("vote_pendulum_played", secret: false, ("remainingMs", remainingMs));
                    }
                }
                else if (_meetingClient.ClosedEarly(now))
                {
                    _sfxPlayer.StopStoppable();
                }

                if (_resultCeremonyAtMs > 0 && now >= _resultCeremonyAtMs)
                {
                    _resultCeremonyAtMs = 0;
                    FireResultCeremony();
                }
            }
            else
            {
                if (_meetingUiActive && _warpExecuted)
                {
                    EnsureSfxBuilt();
                    _sfxPlayer.Play("sfx_meeting_end");
                    TryExecuteMeetingScatter();
                }
                _warpExecuted = false;
                _meetingCutByCeremony = false;
                _votePendulumPlayed = false;
                _resultCeremonyAtMs = 0;
                _pendingCurseCatActor = -1;
                _deathRevealPending = false;
                _pendingBeaconAudit = -1;
                _pendingMeetingTutorial = false;
                HideDeathReveal();
                _sfxPlayer.StopStoppable();
                _movementFreezer.End();
                if (GameOverSafety.ShouldHoldEnemyFreeze(ClientPhase, _winCeremonyActive))
                {
                    if (SemiFunc.IsMasterClientOrSingleplayer() && !_enemyFreezer.Active)
                    {
                        _enemyFreezer.Begin();
                    }
                }
                else
                {
                    _enemyFreezer.End();
                }
                _truckWarper?.ResetWatchdog();
                Patches.PlayerSpawnPatch.MeetingActive = false;
                if (_conveneCountdown.Visible) HideConveneCountdown();
                if (_discussionImpact.Exists) _discussionImpact.Hide();
                if (_meetingUiActive)
                {
                    _scatterPlanAtUnixMs = 0;
                    _scatterAwaitCurse = false;
                    _votePanel.StopScatterReveal();
                    _votePanel.EndMeeting();
                    ResetMeetingChatView();
                    _meetingUiActive = false;
                }
            }

            if (_uiManager.Exists)
            {
                _uiManager.Tick(_meetingClient, now);
                if (_uiManager.IsLayerVisible(WerewolfUIManager.MeetingLayer))
                {
                    UiKit.KeepCursorFree();
                    _votePanel.Tick(_meetingClient, now);
                    TickMeetingChat();
                }
            }

            if (_meetingMapOverlay.Exists)
            {
                _meetingMapOverlay.Tick(
                    _meetingClient, now,
                    Plugin.MeetingMapKey != null ? Plugin.MeetingMapKey.Value : KeyCode.M,
                    Plugin.Bindings != null ? Plugin.Bindings.MeetingMapOrthoSize.Value : 15f,
                    Plugin.Bindings != null ? Plugin.Bindings.MeetingMapResolution.Value : 1,
                    Plugin.Bindings == null || Plugin.Bindings.MeetingMapGrid.Value);
            }
        }

        private void FireResultCeremony()
        {
            MeetingOutcome result = _meetingClient.Result;
            int executedActor = result != null ? result.ExecutedActor : -1;
            PushToast(executedActor == -1
                ? SessionNotice.ForNoExecution()
                : SessionNotice.ForExecuted(ResolveDisplayName(executedActor)));
            if (_pendingCurseCatActor != -1)
            {
                int catActor = _pendingCurseCatActor;
                long deadline = _pendingCurseDeadlineMs;
                _pendingCurseCatActor = -1;
                PresentCurseStarted(catActor, deadline);
            }
            else if (executedActor != -1)
            {
                EnsureSfxBuilt();
                _sfxPlayer.Play("sfx_execution");
                WLog.Line("execution_sfx", secret: false, ("kind", "regular"));
            }
        }

        private void ShowConveneHoldHint()
        {
            long now = NowUnixMs();
            if (now < _nextConveneHoldHintUnixMs) return;

            PushToast(SessionNotice.ForConveneHoldHint());
            _nextConveneHoldHintUnixMs = now + ToastDurationSec() * 1000L;
        }

        private void ExecuteWarpMoment()
        {
            if (SemiFunc.IsMasterClientOrSingleplayer() && _session != null)
            {
                if (!_truckWarper.WarpAll(IsSessionAlive))
                {
                    WLog.Line("meeting_warp_failed", secret: false, ("reason", "warp_all_false"));
                }
                if (_truckWarper.LocalPlayerWarpTarget.HasValue)
                {
                    _warpVerifyTarget = _truckWarper.LocalPlayerWarpTarget;
                    _warpVerifyDeadlineMs = NowUnixMs() + 3000;
                }
                _enemyFreezer.Begin();
            }
            _movementFreezer.Begin();
        }

        private void TickWarpVerify(long now)
        {
            if (!_warpVerifyTarget.HasValue) return;
            try
            {
                PlayerController pc = PlayerController.instance;
                if (pc == null) return;

                float distance = Vector3.Distance(pc.transform.position, _warpVerifyTarget.Value);
                if (distance < 1f)
                {
                    WLog.Line("warp_verify_reached", secret: false,
                        ("distance", distance), ("elapsedMs", 3000 - (_warpVerifyDeadlineMs - now)));
                    _warpVerifyTarget = null;
                    return;
                }
                if (now >= _warpVerifyDeadlineMs)
                {
                    WLog.Line("warp_verify_timeout", secret: false,
                        ("distance", distance),
                        ("pos", TruckWarper.FormatVec(pc.transform.position)),
                        ("target", TruckWarper.FormatVec(_warpVerifyTarget.Value)),
                        ("isTumbling", TruckWarper.IsTumbling(PlayerAvatar.instance)));
                    _warpVerifyTarget = null;
                }
            }
            catch (Exception e)
            {
                WLog.Line("warp_verify_error", secret: false, ("err", e.Message));
                _warpVerifyTarget = null;
            }
        }

        private void TryRestoreMeetingFromRoomState()
        {
            if (_meetingClient.MeetingActive || _session != null) return;
            if (!SemiFunc.RunIsLevel()) return;
            if (!_roomState.TryReadPhase(out GamePhase phase) || phase != GamePhase.Meeting) return;
            if (!_roomState.TryReadMeeting(out int caller, out long endUnixMs) || caller == -1) return;

            _meetingClient.RestoreFromRoomState(caller, endUnixMs);
            _clientPhase = GamePhase.Meeting;
            _warpExecuted = false;
            _deathRevealPending = false;
            _pendingBeaconAudit = -1;
            _pendingMeetingTutorial = false;
            ResetMeetingChat();
            _chatVoteBaselinePending = true;
            _chatSystemPosted = true;
            Patches.PlayerSpawnPatch.MeetingActive = true;
            WLog.Line("meeting_restored", secret: false,
                ("caller", caller), ("endUnixMs", endUnixMs));
            BeginMeetingUi();
        }

        private void BeginMeetingUi()
        {
            _deadlineBanner.Hide();

            EnsureUiBuilt();
            if (!_votePanel.Exists) return;

            IReadOnlyList<WPlayer> roster = _session != null
                ? _session.Players
                : Registry.BuildRealPlayers();
            _votePanel.BeginMeeting(roster, ResolveAvatar, LocalActor);
            _meetingUiActive = true;
        }

        private void StartDeathReveal()
        {
            EnsurePanelBuilt(_deathReveal);

            IReadOnlyList<WPlayer> roster = _session != null
                ? _session.Players
                : Registry.BuildRealPlayers();
            var dead = new List<WPlayer>();
            if (roster != null)
            {
                foreach (WPlayer player in roster)
                {
                    if (player == null) continue;
                    if (_meetingClient.IsDeadUnannounced(player.ActorNumber)) dead.Add(player);
                }
            }
            var announcedNow = new List<int>();
            _meetingClient.MarkAllDeadAnnounced(announcedNow);
            ReplaySampler.NoteDeathsAnnounced(announcedNow);

            _chatRecapDeaths.Clear();
            foreach (WPlayer player in dead)
            {
                _chatRecapDeaths.Add(ParticipantLabel.Format(
                    IdRoster.IdOf(player.ActorNumber), player.Name ?? $"#{player.ActorNumber}"));
            }

            WLog.Line("death_reveal_start", secret: false,
                ("deadCount", dead.Count), ("panelBuilt", _deathReveal.Exists));
            if (!_deathReveal.Exists) return;

            _deathReveal.Show(dead, ResolveAvatar, _meetingClient.Kind);

            Action onImpact = null;
            if (dead.Count > 0)
            {
                EnsureSfxBuilt();
                onImpact = () => _sfxPlayer.Play("sfx_death_stamp");
            }

            if (_deathRevealCoroutine != null) StopCoroutine(_deathRevealCoroutine);
            _deathRevealCoroutine = StartCoroutine(_deathReveal.Play(onImpact));
        }

        private void HideDeathReveal()
        {
            if (_deathRevealCoroutine != null)
            {
                StopCoroutine(_deathRevealCoroutine);
                _deathRevealCoroutine = null;
            }
            if (_deathReveal.Exists && _deathReveal.Visible) _deathReveal.HideNow();
        }

        private void EnsureUiBuilt()
        {
            EnsurePanelBuilt(_votePanel);
            EnsurePanelBuilt(_meetingMapOverlay);
            if (MeetingChatLogEnabled) EnsurePanelBuilt(_chatPanel);

            if (_votePanel.Exists && _votePanel.IsPointerBlocked == null)
            {
                _votePanel.IsPointerBlocked = p =>
                    (_meetingMapOverlay.Exists && _meetingMapOverlay.IsPointerOverPanel(p))
                    || (_manualOverlay.Exists && _manualOverlay.IsPointerOverPanel(p))
                    || (_chatPanel.Exists && _chatPanel.IsPointerOverPanel(p));
            }
        }

        public void SendConveneRequest() => SendConveneRequest(ConveneKind.Button);

        public void SendConveneRequest(ConveneKind kind)
        {
            if (_bus == null)
            {
                WLog.Line("convene_send_fail", secret: false, ("reason", "no_bus"));
                return;
            }
            _bus.SendToMaster(MessageCodes.RequestMeeting, new object[] { (byte)kind });
        }

        public void SendVote(int targetActor)
        {
            if (_bus == null)
            {
                WLog.Line("vote_send_fail", secret: false, ("reason", "no_bus"));
                return;
            }
            _bus.SendToMaster(MessageCodes.CastVote, new object[] { targetActor });
        }

        private const float CorpseReportDistance = 3.0f;

        internal bool LocalIsNearUnannouncedCorpse { get; private set; }

        internal bool CorpseReportConsumedPress { get; private set; }

        private void TickCorpseReport()
        {
            LocalIsNearUnannouncedCorpse = false;
            CorpseReportConsumedPress = false;

            if (_meetingClient.MeetingActive) return;
            if (!LocalCanConvene()) return;
            PlayerAvatar local = PlayerAvatar.instance;
            if (local == null) return;

            bool lastRunActive = LastRunGate.IsLastRunActive();

            try
            {
                if (!IsUnannouncedCorpseNear(local.transform.position)) return;

                if (lastRunActive)
                {
                    KeyCode lastRunKey = Plugin.CorpseReportKey != null
                        ? Plugin.CorpseReportKey.Value : KeyCode.R;
                    if (InputGate.KeysFree && Input.GetKeyDown(lastRunKey))
                    {
                        CorpseReportConsumedPress = true;
                        WLog.Line("corpse_report_denied_lastrun_local", secret: false);
                        PushToast(SessionNotice.ForConveneDenied(
                            ConveneRejectReason.CorpseReportLastRun));
                    }
                    return;
                }

                LocalIsNearUnannouncedCorpse = true;

                MaybeShowTutorial(TutorialId.CorpseDiscovery);

                KeyCode key = Plugin.CorpseReportKey != null ? Plugin.CorpseReportKey.Value : KeyCode.R;
                if (InputGate.KeysFree && Input.GetKeyDown(key))
                {
                    CorpseReportConsumedPress = true;
                    WLog.Line("corpse_report_pressed", secret: false);
                    SendConveneRequest(ConveneKind.CorpseReport);
                }
            }
            catch (Exception e)
            {
                WLog.Line("corpse_report_tick_error", secret: false, ("err", e.Message));
            }
        }

        private bool IsUnannouncedCorpseNear(Vector3 pos)
        {
            var director = GameDirector.instance;
            if (director == null || director.PlayerList == null) return false;
            if (Registry == null || !Registry.Available) return false;

            foreach (PlayerAvatar avatar in director.PlayerList)
            {
                if (avatar == null) continue;
                int actor = Registry.ResolveActor(avatar);
                if (!_meetingClient.IsDeadUnannounced(actor)) continue;
                if (TruckWarper.TryGetDeathHeadPosition(avatar, out Vector3 headPos)
                    && Vector3.Distance(pos, headPos) <= CorpseReportDistance)
                {
                    return true;
                }
            }

            if (FakeBodies.Any)
            {
                foreach ((int actor, Vector3 bodyPos) in FakeBodies.Snapshot())
                {
                    if (!_meetingClient.IsDeadUnannounced(actor)) continue;
                    if (Vector3.Distance(pos, bodyPos) <= CorpseReportDistance) return true;
                }
            }
            return false;
        }

        private bool LocalCanConvene()
        {
            GamePhase phase = _session != null ? _session.Phase : _clientPhase;
            return phase == GamePhase.Play && IsLocalAlive();
        }

        private long ComputeMeetingReadyAt()
        {
            long fromStart = _gameStartUnixMsClient + ClientConveneSuppressStartSec * 1000L;
            long fromLastEnd = _lastMeetingEndUnixMsClient + ClientConveneSuppressAfterSec * 1000L;
            return fromStart > fromLastEnd ? fromStart : fromLastEnd;
        }

        private int LocalActor => _bus != null ? _bus.LocalActorNumber : 1;

        private bool IsLocalAlive()
            => _meetingClient.GetRowStatus(LocalActor) == RowStatus.Alive;

        private void TrySetLocalInvincible()
        {
            try
            {
                PlayerAvatar local = PlayerAvatar.instance;
                if (local == null) return;
                PlayerHealth ph = local.playerHealth;
                if (ph == null) return;
                ph.InvincibleSet(0.5f);
            }
            catch (Exception e)
            {
                WLog.Line("meeting_invincible_error", secret: false, ("err", e.Message));
            }
        }

        private bool IsSessionAlive(PlayerAvatar avatar)
        {
            if (_session == null || avatar == null) return false;
            int actor = Registry.ResolveActor(avatar);
            foreach (var p in _session.Players)
            {
                if (p.ActorNumber == actor) return p.Alive;
            }
            return false;
        }

    }
}
