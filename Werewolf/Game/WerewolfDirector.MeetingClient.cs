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

        private readonly ConveneHoldGauge _conveneHoldGauge = new ConveneHoldGauge();

        private long _scatterPlanAtUnixMs;

        private bool _scatterAwaitCurse;

        private const long ScatterPlanNoExecDelayMs = 1500;

        private const long ScatterPlanAfterKillMarginMs = 1000;

        private readonly ScatterGuard _scatterGuard = new ScatterGuard();

        private readonly ScatterGuard _clientScatterGuard = new ScatterGuard();

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
            ResetMeetingChat();
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

            if (GameOverSafety.ShouldInjectInvincibility(ClientPhase, active, _meetingClient.WarpDone(now)))
            {
                TrySetLocalInvincible();
            }

            if (active && !_meetingClient.WarpDone(now))
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
                    PushToast(SessionNotice.ForBeaconAudit(_pendingBeaconAudit));
                    _chatRecapBeaconUses = _pendingBeaconAudit;
                    _pendingBeaconAudit = -1;
                }
                if (!_chatSystemPosted && _meetingClient.VotingUiReady(now))
                {
                    List<List<int>> lastGroups = _lastScatterGroups;
                    _lastScatterGroups = null;
                    int lostSince = ConsumeRecapLostDelta();
                    if (MeetingChatLogEnabled) PostMeetingChatSystemLines(lastGroups, lostSince);
                    _chatSystemPosted = true;
                    _meetingClient.MarkDiscussionOpen();
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

                if (_executionSfxWaitTicks >= 0 && ++_executionSfxWaitTicks >= ExecutionSfxCurseWaitTicks)
                {
                    _executionSfxWaitTicks = -1;
                    EnsureSfxBuilt();
                    _sfxPlayer.Play("sfx_execution");
                    WLog.Line("execution_sfx", secret: false, ("kind", "regular"));
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
                _votePendulumPlayed = false;
                _executionSfxWaitTicks = -1;
                _deathRevealPending = false;
                _pendingBeaconAudit = -1;
                _pendingMeetingTutorial = false;
                HideDeathReveal();
                _sfxPlayer.StopStoppable();
                _movementFreezer.End();
                if (GameOverSafety.ShouldHoldEnemyFreeze(ClientPhase))
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
                if (_meetingUiActive)
                {
                    _scatterPlanAtUnixMs = 0;
                    _scatterAwaitCurse = false;
                    _votePanel.StopScatterReveal();
                    _votePanel.EndMeeting();
                    ResetMeetingChat();
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

        private void TryExecuteMeetingScatter()
        {
            if (_session == null) return;
            Patches.PlayerSpawnPatch.ArmScatterGrace();
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_session.Phase != GamePhase.Play)
            {
                _extractionScatter?.ClearPlan();
                return;
            }
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;
            ExtractionScatter scatter = _extractionScatter ??= new ExtractionScatter();
            if (scatter.HasPlan)
            {
                if (scatter.ExecutePlan(IsSessionAlive))
                {
                    ArmScatterGuard(scatter);
                }
            }
            else if (scatter.WarpScatter(IsSessionAlive, warpTruckSlot: false,
                         botActors: CollectAliveBotActors()))
            {
                SendScatterGroups(scatter);
                ArmScatterGuard(scatter);
            }
        }

        private void ArmScatterGuard(ExtractionScatter scatter)
        {
            int groups = ScatterGroupsWire.CountGroups(scatter.LastAssignments);
            if (groups < 2)
            {
                _scatterGuard.Disarm();
                return;
            }
            int guardSec = Plugin.GameConfig != null ? Plugin.GameConfig.ScatterGuardSec : 0;
            _scatterGuard.Arm(NowUnixMs(), guardSec);
            if (_scatterGuard.ArmedUntilUnixMs != 0)
            {
                SendScatterGuardWindow(guardSec);
                WLog.Line("scatter_guard_armed", secret: false,
                    ("groups", groups), ("untilUnixMs", _scatterGuard.ArmedUntilUnixMs));
            }
        }

        private void SendScatterGuardWindow(int guardSec)
        {
            if (_bus == null) return;
            _bus.SendToAll(MessageCodes.ScatterGuardWindow, new object[] { guardSec });
        }

        private void HandleScatterGuardWindow(int guardSec)
        {
            if (guardSec > 0) _clientScatterGuard.Arm(NowUnixMs(), guardSec);
            else _clientScatterGuard.Disarm();
            ReplaySampler.NoteGuardWindow(guardSec);
            WLog.Line("recv_scatter_guard_window", secret: false, ("sec", guardSec));
        }

        private void TryFireScatterGuard(int victimActor)
        {
            long now = NowUnixMs();
            if (!_scatterGuard.IsArmed(now)) return;
            if (_meeting == null || _session == null || _session.Phase != GamePhase.Play) return;
            if (_checkmate != null && _checkmate.CeremonyStarted) return;
            if (LastRunGate.IsLastRunActive())
            {
                _scatterGuard.Disarm();
                SendScatterGuardWindow(0);
                WLog.Line("scatter_guard_skip", secret: false, ("reason", "last_run"));
                return;
            }
            if (_meeting.TryConveneScatterGuard(victimActor, now))
            {
                _scatterGuard.Disarm();
                WLog.Line("scatter_guard_fired", secret: false, ("victim", victimActor));
            }
        }

        private void TickScatterPlanHost(long now)
        {
            if (_scatterPlanAtUnixMs == 0 || now < _scatterPlanAtUnixMs) return;
            _scatterPlanAtUnixMs = 0;
            if (!SemiFunc.IsMasterClientOrSingleplayer() || _session == null) return;
            if (_session.Phase != GamePhase.Meeting)
            {
                WLog.Line("scatter_plan_skip", secret: false,
                    ("reason", "phase"), ("phase", _session.Phase));
                return;
            }
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;

            ExtractionScatter scatter = _extractionScatter ??= new ExtractionScatter();
            if (!scatter.PlanScatter(IsSessionAlive, warpTruckSlot: false,
                    botActors: CollectAliveBotActors()))
            {
                return;
            }

            object[] wire = scatter.BuildGroupsWire();
            if (wire == null) return;

            _meeting?.EnsureClosingHoldRemaining(now, VotePanel.ScatterRevealHoldRequiredMs);

            if (_bus != null)
            {
                _bus.SendToAll(MessageCodes.ScatterGroups, wire);
                WLog.Line("scatter_groups_sent", secret: false, ("players", ((int[])wire[0]).Length));
            }
        }

        private void HostScheduleScatterPlanFromResult(int executedActor)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer() || _session == null) return;
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;
            if (_scatterAwaitCurse) return;
            long delayMs = executedActor == -1
                ? ScatterPlanNoExecDelayMs
                : MeetingSession.PostResultKillDelaySec * 1000L + ScatterPlanAfterKillMarginMs;
            _scatterPlanAtUnixMs = NowUnixMs() + delayMs;
        }

        private void HostMarkScatterAwaitCurse()
        {
            _scatterAwaitCurse = true;
            _scatterPlanAtUnixMs = 0;
        }

        private void HostScheduleScatterPlanAfterCurse()
        {
            if (!_scatterAwaitCurse) return;
            _scatterAwaitCurse = false;
            _scatterPlanAtUnixMs = NowUnixMs()
                + RolesSession.CurseKillDelaySec * 1000L + ScatterPlanAfterKillMarginMs;
        }

        private List<int> CollectAliveBotActors()
        {
            List<int> aliveBots = null;
            foreach (WPlayer p in _session.Players)
            {
                if (p != null && p.IsBot && p.Alive) (aliveBots ??= new List<int>()).Add(p.ActorNumber);
            }
            return aliveBots;
        }

        private void SendScatterGroups(ExtractionScatter scatter)
        {
            if (_bus == null) return;
            object[] wire = scatter.BuildGroupsWire();
            if (wire == null) return;
            _bus.SendToAll(MessageCodes.ScatterGroups, wire);
            WLog.Line("scatter_groups_sent", secret: false, ("players", ((int[])wire[0]).Length));
        }

        private void HandleScatterGroups(object[] p)
        {
            List<List<int>> groups = ScatterGroupsWire.FromWire(p);
            if (groups == null || groups.Count < 2)
            {
                WLog.Line("recv_scatter_groups_invalid", secret: false);
                return;
            }

            _lastScatterGroups = groups;
            ReplaySampler.NoteScatterGroups(groups);

            bool animated = false;
            if (_meetingUiActive && _meetingClient.MeetingActive && _votePanel.Exists)
            {
                EnsureSfxBuilt();
                animated = _votePanel.StartScatterReveal(groups, NowUnixMs(),
                    volume => _sfxPlayer.PlayLoop("sfx_scatter_shuffle", volume),
                    () => _sfxPlayer.StopLoop("sfx_scatter_shuffle"),
                    () => _sfxPlayer.Play("sfx_scatter_jingle"));
            }

            if (!animated)
            {
                PushScatterToasts(ScatterGroupsText.FormatLines(groups, ScatterMemberLabel));
            }
            WLog.Line("recv_scatter_groups", secret: false,
                ("groups", groups.Count), ("animated", animated));
        }

        private void PushScatterToasts(List<string> lines)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                PushRawToast(lines[i], logKind: "scatter", playSfx: i == 0);
            }
        }

        private string ScatterMemberLabel(int actor)
        {
            int id = IdRoster.IdOf(actor);
            return id > 0 ? Texts.Format(TextId.NoticeScatterMemberFormat, id) : ResolveDisplayName(actor);
        }

        private string ScatterMemberChatLabel(int actor)
            => ParticipantLabel.Format(IdRoster.IdOf(actor), ResolveDisplayName(actor));

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

        private static bool MeetingChatLogEnabled
            => Plugin.Bindings == null || Plugin.Bindings.MeetingChatLog.Value;

        internal bool IsChatLogWindowOpenClient
            => MeetingChatGate.IsOpen(ClientPhase, IsMeetingDiscussionOpenClient) || IsResultChatActiveClient;

        private bool IsMeetingWarpDoneClient => _meetingClient.WarpDone(NowUnixMs());

        private bool IsMeetingDiscussionOpenClient => _meetingClient.DiscussionOpen;

        private Func<int, PlayerAvatar> ChatAvatarResolver
            => _chatDebugAvatarFallback
                ? (Func<int, PlayerAvatar>)ResolveAvatarForChatDebug
                : ResolveAvatar;

        private void ResetMeetingChat()
        {
            _chatLog.Clear();
            _chatUnread.Clear();
            _chatRecapDeaths.Clear();
            _chatRecapBeaconUses = MeetingRecap.Unknown;
            _chatVoteBaselinePending = false;
            _chatSystemPosted = false;
            if (_chatPanel.Exists) _chatPanel.ResetView();
        }

        private int ConsumeRecapLostDelta()
        {
            MeetingGaugeSnapshot gauge = RolesClient != null ? RolesClient.MeetingGauge : null;
            int total = gauge != null ? gauge.LostDollars : MeetingRecap.Unknown;
            int delta = MeetingRecap.LostSince(total, _chatRecapLostBaseline);
            if (total >= 0) _chatRecapLostBaseline = total;
            return delta;
        }

        private void PostMeetingChatSystemLines(List<List<int>> lastGroups, int lostSince)
        {
            try
            {
                string speaker = Texts.Get(TextId.ChatLogSystemName);
                bool emoji = EmojiSprites.Ready;
                string face = _chatRecapDeaths.Count > 0 ? "img_taxman_system" : "img_taxman_nodeath";
                MeetingGaugeSnapshot gauge = RolesClient != null ? RolesClient.MeetingGauge : null;
                var recap = new MeetingRecapData(
                    _chatRecapDeaths,
                    lostSince,
                    gauge != null ? gauge.ExtractedDollars : MeetingRecap.Unknown,
                    gauge != null ? gauge.HaulGoalDollars : MeetingRecap.Unknown,
                    _chatRecapBeaconUses);
                _chatLog.AppendSystem(speaker, null,
                    string.Join("\n", MeetingRecap.BuildLines(recap, emoji).ToArray()), face);

                if (lastGroups != null && lastGroups.Count >= 2)
                {
                    List<string> lines = ScatterGroupsText.FormatLines(
                        lastGroups, ScatterMemberChatLabel, TextId.ChatLogScatterLineFormat);
                    _chatLog.AppendSystem(speaker, ChatEmoji.Get(TextId.ChatLogScatterTitle, emoji),
                        string.Join("\n", lines.ToArray()), face);
                }
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_system_error", secret: false, ("err", e.Message));
            }
        }

        private void RecordMeetingVotesClient(int[] votedActors)
        {
            if (!MeetingChatLogEnabled || votedActors == null) return;
            try
            {
                if (_chatVoteBaselinePending)
                {
                    _chatVoteBaselinePending = false;
                    return;
                }

                string voted = Texts.Get(TextId.ChatLogVoted);
                IReadOnlyCollection<int> known = _meetingClient.VotedActors;
                foreach (int actor in votedActors)
                {
                    if (Contains(known, actor)) continue;
                    _chatLog.AppendVote(actor, ResolveDisplayName(actor), voted);
                }
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_vote_error", secret: false, ("err", e.Message));
            }
        }

        private static bool Contains(IReadOnlyCollection<int> values, int value)
        {
            foreach (int v in values)
            {
                if (v == value) return true;
            }
            return false;
        }

        public void RecordMeetingChatClient(PlayerAvatar speaker, string message)
        {
            if (!MeetingChatLogEnabled || speaker == null) return;
            try
            {
                int actor = Registry != null ? Registry.ResolveActor(speaker) : -1;
                AppendMeetingChatMessageClient(
                    actor, ResolveDisplayName(actor), message, ChatSpeakerKindFor(actor), playSfx: true);
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_record_error", secret: false, ("err", e.Message));
            }
        }

        public void RecordReplayChatClient(PlayerAvatar speaker, string message)
        {
            if (speaker == null) return;
            try
            {
                RecordReplayChatByActor(
                    Registry != null ? Registry.ResolveActor(speaker) : -1, message);
            }
            catch (Exception e)
            {
                WLog.Line("replay_chat_record_error", secret: false, ("err", e.Message));
            }
        }

        private bool RecordReplayChatByActor(int actor, string message)
        {
            bool alive = _meetingClient.GetRowStatus(actor) == RowStatus.Alive;
            if (!ReplayChatGate.ShouldRecord(ClientPhase, IsMeetingDiscussionOpenClient, alive)) return false;
            string text = ReplayChatText.SanitizeForRecord(message);
            if (text.Length == 0) return false;
            ReplaySampler.NoteChat(actor, text);
            return true;
        }

        private ChatSpeaker ChatSpeakerKindFor(int actor)
        {
            if (ClientPhase == GamePhase.GameOver) return ChatSpeaker.Alive;
            return _meetingClient.GetRowStatus(actor) == RowStatus.Alive
                ? ChatSpeaker.Alive
                : ChatSpeaker.Dead;
        }

        private bool AppendMeetingChatMessageClient(
            int actor, string name, string message, ChatSpeaker kind, bool playSfx)
        {
            bool added = _chatLog.Append(actor, name, message, kind);
            if (added && playSfx)
            {
                EnsureSfxBuilt();
                _sfxPlayer.Play(MeetingChatSfxClipKey, MeetingChatSfxVolumeScale);
                _chatUnread.OnMessageAppended(actor, LocalActor, _chatPanel.IsOpen);
            }
            return added;
        }

        private void TickMeetingChat()
        {
            if (!MeetingChatLogEnabled || !_chatPanel.Exists) return;

            _chatPanel.Tick(
                Plugin.MeetingChatLogKey != null ? Plugin.MeetingChatLogKey.Value : KeyCode.L,
                InputGate.KeysFree);

            _chatPanel.Render(_chatLog, LocalActor, ChatAvatarResolver,
                ParticipantIdFor,
                MarkedTeammateRole,
                !IsLocalAlive());

            if (_chatPanel.IsOpen) _chatUnread.Clear();
            _chatPanel.SetUnreadBadge(_chatUnread.HasUnread);
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
