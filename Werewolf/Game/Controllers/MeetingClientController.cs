using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class MeetingClientController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private bool _pendingMeetingTutorial;

        private readonly ToastRepeatGate _conveneHoldHintGate = new ToastRepeatGate();

        private readonly MeetingMapOverlay _meetingMapOverlay = new MeetingMapOverlay();

        private readonly ToastRepeatGate _mapToolBlockedHintGate = new ToastRepeatGate();

        private readonly ToastRepeatGate _corpseReportLastRunGate = new ToastRepeatGate();

        private long _clientWarpUnixMs;
        private bool _warpExecuted;
        private bool _meetingCutByCeremony;

        private bool _votePendulumPlayed;

        private readonly ConveneHoldGauge _conveneHoldGauge = new ConveneHoldGauge();

        private readonly MeetingButton _meetingButton;

        private readonly DeathRevealPanel _deathReveal = new DeathRevealPanel();
        private Coroutine _deathRevealCoroutine;
        private bool _deathRevealPending;

        private int _pendingBeaconAudit = -1;

        private readonly ConveneCountdown _conveneCountdown = new ConveneCountdown();
        private Coroutine _conveneTweenCoroutine;

        private long _resultCeremonyAtMs;
        private int _pendingCurseCatActor = -1;
        private long _pendingCurseDeadlineMs;

        private bool _meetingUiActive;

        internal MeetingClientController(WerewolfDirector director)
        {
            _d = director;
            _meetingButton = new MeetingButton();
            _meetingButton.OnConvene = director.SendConveneRequest;
            _meetingButton.OnIncompleteHold = ShowConveneHoldHint;
        }

        internal bool MeetingUiActive => _meetingUiActive;

        internal bool MeetingButtonNear => _meetingButton.IsLocalPlayerNear;

        internal bool MeetingButtonUsesGrab => _meetingButton.UsesGrabInteract;

        internal bool MapOverlayOpen => _meetingMapOverlay.Exists && _meetingMapOverlay.IsOpen;

        internal bool LocalIsNearUnannouncedCorpse { get; private set; }

        internal bool LocalIsNearCorpseBody { get; private set; }

        internal bool CorpseReportConsumedPress { get; private set; }

        internal void HandleStartMeeting(object[] p)
        {
            byte kindByte = (byte)p[3];
            ConveneKind startKind = kindByte == 1 ? ConveneKind.CorpseReport
                : kindByte == 2 ? ConveneKind.ScatterGuard
                : ConveneKind.Button;
            ApplyStartMeeting((int)p[0], (long)p[1], (long)p[2], startKind);
            ReplaySampler.NoteMeetingStart((int)p[0], startKind.ToString());
            string startCaller = _d.ResolveDisplayName((int)p[0]);
            _d.PushToast(startKind == ConveneKind.CorpseReport
                ? SessionNotice.ForCorpseReportStarted(startCaller)
                : startKind == ConveneKind.ScatterGuard
                    ? SessionNotice.ForScatterGuardTripped()
                    : SessionNotice.ForConveneStarted(startCaller));
            _d.FeedScenarioSignal(ScenarioSignal.ConveneAccepted);
        }

        internal void HandleMeetingCancelled(byte reason)
        {
            _d.MeetingClientView.ApplyCancelled();
            ReplaySampler.NoteMeetingCancelled(reason);
            _d.PushToast(SessionNotice.ForMeetingCancelled());
            WLog.Line("recv_meeting_cancelled", secret: false, ("reason", reason));
        }

        internal void HandleVoteProgress(object[] p)
        {
            int[] votedActors = (int[])p[0];
            bool voteAdded = votedActors.Length > _d.MeetingClientView.VotedActors.Count;
            _d.Chat.RecordVoteProgress(votedActors);
            _d.MeetingClientView.ApplyVoteProgress(votedActors, (long)p[1]);
            _d.VotePanelView?.NotifyVoteProgress();
            if (voteAdded) _d.PlaySfx("sfx_vote_cast");
            if (System.Array.IndexOf(votedActors, _d.LocalActor) >= 0)
            {
                _d.FeedScenarioSignal(ScenarioSignal.VoteAccepted);
            }
            WLog.Line("recv_voteprogress", secret: false,
                ("voted", votedActors.Length), ("endUnixMs", (long)p[1]));
        }

        internal void HandleMeetingResult(object[] p)
        {
            int executedActor = (int)p[0];
            _d.MeetingClientView.ApplyResult(new MeetingOutcome
            {
                ExecutedActor = executedActor,
                TargetActors = (int[])p[1],
                VoteCounts = (int[])p[2],
            });
            WLog.Line("recv_meetingresult", secret: false, ("executed", executedActor));
            ReplaySampler.NoteMeetingResult(executedActor);
            _resultCeremonyAtMs = SessionClock.NowMs()
                + VoteTallyTimeline.CeremonyDelayMs((int[])p[2]);
            _d.ScatterFeature.HostScheduleScatterPlanFromResult(executedActor);
        }

        internal void HandleConveneDenied(byte wire)
        {
            var denyReason = ConveneDeniedWire.FromWire(wire);
            _d.PushToast(SessionNotice.ForConveneDenied(denyReason));
            WLog.Line("recv_convenedenied", secret: false,
                ("wire", wire), ("reason", denyReason));
        }

        private void ApplyStartMeeting(int caller, long warpUnixMs, long endUnixMs,
                                       ConveneKind kind = ConveneKind.Button)
        {
            _clientWarpUnixMs = warpUnixMs;
            _warpExecuted = false;
            _votePendulumPlayed = false;
            _d.MeetingClientView.ApplyStartMeeting(caller, warpUnixMs, endUnixMs, kind);
            _d.ScatterFeature.OnMeetingStarted();
            Patches.PlayerSpawnPatch.MeetingActive = true;
            _deathRevealPending = true;
            _d.Chat.ResetMeetingView();
            WLog.Line("recv_startmeeting", secret: false,
                ("caller", caller), ("warpUnixMs", warpUnixMs), ("endUnixMs", endUnixMs));
            BeginMeetingUi();

            _d.MaybeShowTutorial(TutorialId.MeetingCountdownStarted);
            _pendingMeetingTutorial = true;
        }

        internal void HandleBeaconAudit(int uses) => _pendingBeaconAudit = uses;

        internal bool TryDeferCurseStarted(int catActor, long deadlineUnixMs)
        {
            if (_resultCeremonyAtMs <= 0) return false;
            _pendingCurseCatActor = catActor;
            _pendingCurseDeadlineMs = deadlineUnixMs;
            return true;
        }

        public void Tick(long now) => TickMeetingClient(now);

        private void TickMeetingClient(long now)
        {
            TryRestoreMeetingFromRoomState();

            bool gameActive = _d.IsRoundActiveClient;
            if (gameActive && Session.InRunLevel)
            {
                if (!_meetingButton.Exists && MeetingButton.AnchorExists)
                {
                    _meetingButton.Create();
                }
                long buttonNow = now;
                long readyAtUnixMs = ComputeMeetingReadyAt();
                int rightsForPrompt = 0;
                MeetingSession hostMeeting = _d.HostMeetingSession;
                if (hostMeeting != null) rightsForPrompt = hostMeeting.RightsRemaining(_d.LocalActor);
                else _d.RoomStateView.TryReadRights(_d.LocalActor, out rightsForPrompt);
                _meetingButton.Tick(LocalCanConvene(), readyAtUnixMs, buttonNow, rightsForPrompt);

                _d.EnsurePanelBuilt(_conveneHoldGauge);
                _conveneHoldGauge.Tick(_meetingButton.HoldCharging, _meetingButton.HoldRatio,
                    _meetingButton.VisualWorldPos);
                if (_meetingButton.HoldCharging)
                {
                    _d.PlayConveneChargeSfx(_meetingButton.HoldRatio);
                }
                else
                {
                    _d.StopConveneChargeSfx();
                }

                TickCorpseReport();
            }
            else
            {
                if (_meetingButton.Exists) _meetingButton.Destroy();
                if (_conveneHoldGauge.Exists) _conveneHoldGauge.Tick(false, 0f, null);
                _d.StopConveneChargeSfx();
            }

            MeetingClientState meetingClient = _d.MeetingClientView;
            bool active = meetingClient.MeetingActive;

            if (GameOverSafety.ShouldInjectInvincibility(
                    _d.ClientPhase, active, meetingClient.WarpDone(now), _d.WinCeremonyActive,
                    _d.ScenarioForcesInvincible))

            {
                TrySetLocalInvincible();
            }

            if (active && !_warpExecuted && (_d.WinCeremonyActive || _meetingCutByCeremony))
            {
                _meetingCutByCeremony = true;
                if (_conveneCountdown.Visible) HideConveneCountdown();
                if (GameOverSafety.ShouldHoldEnemyFreeze(
                        _d.ClientPhase, _d.WinCeremonyActive, _d.ScenarioSuppressesEnemies)
                    && Session.IsHost && !_d.EnemyFreezerView.Active)
                {
                    _d.EnemyFreezerView.Begin();
                }
            }
            else if (active && !meetingClient.WarpDone(now))
            {
                int remainSec = (int)((_clientWarpUnixMs - now + 999) / 1000);
                _d.EnsurePanelBuilt(_conveneCountdown);
                if (_conveneCountdown.Exists)
                {
                    TextId headerFormat = meetingClient.Kind == ConveneKind.CorpseReport
                        ? TextId.ConveneCountdownCorpseHeaderFormat
                        : meetingClient.Kind == ConveneKind.ScatterGuard
                            ? TextId.ConveneCountdownScatterGuardHeader
                            : TextId.ConveneCountdownHeaderFormat;
                    System.Collections.IEnumerator secondTween = _conveneCountdown.Tick(
                        _d.ParticipantIdLabel(meetingClient.CallerActor), remainSec, headerFormat);
                    if (secondTween != null)
                    {
                        if (_conveneTweenCoroutine != null) _d.StopCoroutine(_conveneTweenCoroutine);
                        _conveneTweenCoroutine = _d.StartCoroutine(secondTween);
                        _d.PlaySfx("sfx_countdown");
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
                    RefreshMeetingMapOwnPath();
                    Patches.ValuableMapSyncPatch.RefreshSnapshot("meeting_warp");
                    if (_deathRevealPending)
                    {
                        _deathRevealPending = false;
                        StartDeathReveal();
                    }
                }
                if (_conveneCountdown.Visible) HideConveneCountdown();
                if (_pendingBeaconAudit >= 0 && meetingClient.VotingUiReady(now))
                {
                    _d.Chat.NoteBeaconAudit(_pendingBeaconAudit);
                    _pendingBeaconAudit = -1;
                }
                if (!_d.Chat.MeetingOpeningPosted && meetingClient.VotingUiReady(now))
                {
                    List<List<int>> lastGroups = _d.ScatterFeature.ConsumeLastScatterGroups();
                    _d.Chat.PostMeetingOpening(lastGroups);
                    meetingClient.MarkDiscussionOpen();
                    _d.FeedScenarioSignal(ScenarioSignal.MeetingOpened);
                    _d.ShowDiscussionImpact();
                }
                if (_pendingMeetingTutorial && meetingClient.VotingUiReady(now))
                {
                    if (_d.LocalIsVillagerTeam) _d.MaybeShowTutorial(TutorialId.FirstMeetingAsVillager);
                    else if (_d.LocalRoleClient == Role.Werewolf) _d.MaybeShowTutorial(TutorialId.FirstMeetingAsWerewolf);
                    else if (_d.LocalRoleClient == Role.BlackCat) _d.MaybeShowTutorial(TutorialId.FirstMeetingAsBlackCat);
                    _pendingMeetingTutorial = false;
                }
                _d.MovementFreezerView.Tick(_d.IsLocalAlive());

                if (Session.IsHost && _d.HostSession != null)
                {
                    _d.TruckWarperView.TickWatchdog(_d.IsSessionAliveActor, now);
                }

                if (!_votePendulumPlayed)
                {
                    long remainingMs = meetingClient.RemainingMs(now);
                    if (remainingMs > 0 && remainingMs <= 10000)
                    {
                        _votePendulumPlayed = true;
                        _d.PlayStoppableSfx("sfx_vote_pendulum");
                        WLog.Line("vote_pendulum_played", secret: false, ("remainingMs", remainingMs));
                    }
                }
                else if (meetingClient.ClosedEarly(now))
                {
                    _d.StopStoppableSfx("sfx_vote_pendulum");
                }

                if (_resultCeremonyAtMs > 0 && now >= _resultCeremonyAtMs)
                {
                    _resultCeremonyAtMs = 0;
                    FireResultCeremony();
                    _d.FeedScenarioSignal(ScenarioSignal.TallyRevealed);
                }
            }
            else
            {
                if (_meetingUiActive && _warpExecuted)
                {
                    _d.PlaySfx("sfx_meeting_end");
                    _d.ScatterFeature.TryExecuteMeetingScatter();
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
                _d.StopStoppableSfx("sfx_vote_pendulum");
                _d.MovementFreezerView.End();
                if (GameOverSafety.ShouldHoldEnemyFreeze(
                        _d.ClientPhase, _d.WinCeremonyActive, _d.ScenarioSuppressesEnemies))
                {
                    if (Session.IsHost && !_d.EnemyFreezerView.Active)
                    {
                        _d.EnemyFreezerView.Begin();
                    }
                }
                else
                {
                    _d.EnemyFreezerView.End();
                }
                _d.TruckWarperView?.ResetWatchdog();
                Patches.PlayerSpawnPatch.MeetingActive = false;
                if (_conveneCountdown.Visible) HideConveneCountdown();
                _d.HideDiscussionImpact();
                if (_meetingUiActive)
                {
                    _d.ScatterFeature.CancelPlan();
                    _d.VotePanelView.StopScatterReveal();
                    _d.VotePanelView.EndMeeting();
                    _d.Chat.ResetMeetingView();
                    _meetingUiActive = false;
                    _d.FeedScenarioSignal(ScenarioSignal.MeetingReleased);
                }
            }

            if (_d.UiManagerView.Exists)
            {
                _d.UiManagerView.Tick(meetingClient, now);
                if (_d.UiManagerView.IsLayerVisible(WerewolfUIManager.MeetingLayer))
                {
                    UiKit.KeepCursorFree();
                    _d.VotePanelView.HideTimeLimit = _d.ClientScenarioSession;
                    _d.VotePanelView.Tick(meetingClient, now);
                    _d.Chat.TickMeetingPanel();
                }
            }

            if (_meetingMapOverlay.Exists)
            {
                _meetingMapOverlay.Tick(
                    meetingClient, now,
                    Plugin.MeetingMapKey != null ? Plugin.MeetingMapKey.Value : KeyCode.M,
                    Plugin.Bindings != null ? Plugin.Bindings.MeetingMapOrthoSize.Value : 15f,
                    Plugin.Bindings != null ? Plugin.Bindings.MeetingMapResolution.Value : 1,
                    ScenarioSessionGate.MapGridEnabled(
                        Plugin.Bindings == null || Plugin.Bindings.MeetingMapGrid.Value,
                        ScenarioRun.Active));

                if (_meetingMapOverlay.ConsumeMapToolBlocked()) ShowMapToolBlockedHint(now);
            }
        }

        private void FireResultCeremony()
        {
            MeetingOutcome result = _d.MeetingClientView.Result;
            int executedActor = result != null ? result.ExecutedActor : -1;
            _d.PushToast(executedActor == -1
                ? SessionNotice.ForNoExecution()
                : SessionNotice.ForExecuted(_d.ResolveDisplayName(executedActor)));
            if (_pendingCurseCatActor != -1)
            {
                int catActor = _pendingCurseCatActor;
                long deadline = _pendingCurseDeadlineMs;
                _pendingCurseCatActor = -1;
                _d.RolesClientFeature.PresentCurseStarted(catActor, deadline);
            }
            else if (executedActor != -1)
            {
                _d.PlaySfx("sfx_execution");
                WLog.Line("execution_sfx", secret: false, ("kind", "regular"));
            }
        }

        private void ShowConveneHoldHint()
        {
            if (!_conveneHoldHintGate.TryPass(SessionClock.NowMs(),
                    RolesClientController.ToastDurationSec())) return;

            _d.PushToast(SessionNotice.ForConveneHoldHint());
        }

        private void ShowMapToolBlockedHint(long now)
        {
            if (!_mapToolBlockedHintGate.TryPass(now, RolesClientController.ToastDurationSec())) return;

            _d.PushRawToast(Texts.Get(TextId.MapOverlayMapToolBlocked), logKind: "map_overlay");
        }

        private void RefreshMeetingMapOwnPath()
        {
            try
            {
                bool alive = _d.IsLocalAlive();
                MeetingPath path = alive ? MeetingPath.FromRecorder(ReplaySampler.Recorder, _d.LocalActor) : null;
                _meetingMapOverlay.SetOwnPath(path, _d.IdRoster.IdOf(_d.LocalActor), PlayerHeadColor.HexOfLocal());
                WLog.Line("meeting_path", secret: false,
                    ("alive", alive ? 1 : 0),
                    ("points", path != null ? path.Count : 0),
                    ("durationSec", path != null ? path.DurationSec : 0.0));
            }
            catch (Exception e)
            {
                _meetingMapOverlay.ClearOwnPath();
                WLog.Line("meeting_path_error", secret: false, ("err", e.Message));
            }
        }

        private void ExecuteWarpMoment()
        {
            if (Session.IsHost && _d.HostSession != null)
            {
                TruckWarper truckWarper = _d.TruckWarperView;
                if (!truckWarper.WarpAll(_d.IsSessionAliveActor))
                {
                    WLog.Line("meeting_warp_failed", secret: false, ("reason", "warp_all_false"));
                }
                _d.EnemyFreezerView.Begin();
            }
            _d.MovementFreezerView.Begin();
        }

        private void TryRestoreMeetingFromRoomState()
        {
            if (_d.MeetingClientView.MeetingActive || _d.HostSession != null) return;
            if (!Session.InRunLevel) return;
            if (!_d.RoomStateView.TryReadPhase(out GamePhase phase) || phase != GamePhase.Meeting) return;
            if (!_d.RoomStateView.TryReadMeeting(out int caller, out long endUnixMs) || caller == -1) return;

            _d.MeetingClientView.RestoreFromRoomState(caller, endUnixMs);
            _d.OverrideClientPhaseMirror(GamePhase.Meeting);
            _warpExecuted = false;
            _deathRevealPending = false;
            _pendingBeaconAudit = -1;
            _pendingMeetingTutorial = false;
            _d.Chat.ResetForRestoredMeeting();
            Patches.PlayerSpawnPatch.MeetingActive = true;
            WLog.Line("meeting_restored", secret: false,
                ("caller", caller), ("endUnixMs", endUnixMs));
            BeginMeetingUi();
        }

        private void BeginMeetingUi()
        {
            _d.RolesClientFeature.HideDeadlineBanner();

            EnsureUiBuilt();
            VotePanel votePanel = _d.VotePanelView;
            if (!votePanel.Exists) return;

            IReadOnlyList<WPlayer> roster = _d.HostSession != null
                ? _d.HostSessionPlayers
                : _d.Registry.BuildRealPlayers();
            votePanel.BeginMeeting(roster, Players.Resolve, _d.LocalActor);
            _meetingUiActive = true;
        }

        private void StartDeathReveal()
        {
            _d.EnsurePanelBuilt(_deathReveal);

            MeetingClientState meetingClient = _d.MeetingClientView;
            IReadOnlyList<WPlayer> roster = _d.HostSession != null
                ? _d.HostSessionPlayers
                : _d.Registry.BuildRealPlayers();
            var dead = new List<WPlayer>();
            if (roster != null)
            {
                foreach (WPlayer player in roster)
                {
                    if (player == null) continue;
                    if (meetingClient.IsDeadUnannounced(player.ActorNumber)) dead.Add(player);
                }
            }
            var announcedNow = new List<int>();
            meetingClient.MarkAllDeadAnnounced(announcedNow);
            ReplaySampler.NoteDeathsAnnounced(announcedNow);

            var recapDeaths = new List<string>(dead.Count);
            foreach (WPlayer player in dead)
            {
                recapDeaths.Add(_d.ParticipantIdLabel(player.ActorNumber));
            }
            _d.Chat.SetRecapDeaths(recapDeaths);

            WLog.Line("death_reveal_start", secret: false,
                ("deadCount", dead.Count), ("panelBuilt", _deathReveal.Exists));
            if (!_deathReveal.Exists) return;

            _deathReveal.Show(dead, Players.Resolve, meetingClient.Kind);

            Action onImpact = null;
            if (dead.Count > 0)
            {
                onImpact = () => _d.PlaySfx("sfx_death_stamp");
            }

            if (_deathRevealCoroutine != null) _d.StopCoroutine(_deathRevealCoroutine);
            _deathRevealCoroutine = _d.StartCoroutine(_deathReveal.Play(onImpact));
        }

        private void HideDeathReveal()
        {
            if (_deathRevealCoroutine != null)
            {
                _d.StopCoroutine(_deathRevealCoroutine);
                _deathRevealCoroutine = null;
            }
            if (_deathReveal.Exists && _deathReveal.Visible) _deathReveal.HideNow();
        }

        internal void TickDeathRevealGuard() => _deathReveal.Tick();

        internal void HideConveneCountdown()
        {
            if (_conveneTweenCoroutine != null)
            {
                _d.StopCoroutine(_conveneTweenCoroutine);
                _conveneTweenCoroutine = null;
            }
            _conveneCountdown.Hide();
        }

        internal void DebugPlayConveneCountdown(string callerLabel, int totalSeconds)
        {
            try
            {
                _d.EnsurePanelBuilt(_conveneCountdown);
                if (!_conveneCountdown.Exists)
                {
                    WLog.Line("cmd_fx_countdown_skipped", secret: false, ("reason", "not_built"));
                    return;
                }
                if (totalSeconds < 0) totalSeconds = 0;
                if (_conveneTweenCoroutine != null) _d.StopCoroutine(_conveneTweenCoroutine);
                _conveneTweenCoroutine = _d.StartCoroutine(
                    _conveneCountdown.PlayStandalone(callerLabel, totalSeconds));
                WLog.Line("cmd_fx_countdown", secret: false,
                    ("caller", callerLabel ?? ""), ("seconds", totalSeconds));
            }
            catch (Exception e)
            {
                WLog.Line("cmd_fx_countdown_error", secret: false, ("err", e.Message));
            }
        }

        private void EnsureUiBuilt()
        {
            VotePanel votePanel = _d.VotePanelView;
            _d.EnsurePanelBuilt(votePanel);
            _d.EnsurePanelBuilt(_meetingMapOverlay);
            _d.Chat.EnsureMeetingPanelBuilt();

            if (votePanel.Exists && votePanel.IsPointerBlocked == null)
            {
                votePanel.IsPointerBlocked = p =>
                    (_meetingMapOverlay.Exists && _meetingMapOverlay.IsPointerOverPanel(p))
                    || (_d.ManualOverlayView.Exists && _d.ManualOverlayView.IsPointerOverPanel(p))
                    || _d.Chat.IsPointerOverMeetingPanel(p)
                    || _d.ScenarioGuideContainsPointer(p);
            }
        }

        private void TickCorpseReport()
        {
            LocalIsNearUnannouncedCorpse = false;
            LocalIsNearCorpseBody = false;
            CorpseReportConsumedPress = false;

            if (_d.MeetingClientView.MeetingActive) return;
            if (!LocalCanConvene()) return;
            if (!LocalPlayer.TryGetPosition(out Vector3 localPos)) return;

            bool lastRunActive = LastRunGate.IsLastRunActive();

            try
            {
                if (!IsUnannouncedCorpseNear(localPos)) return;

                LocalIsNearCorpseBody = true;

                if (lastRunActive)
                {
                    KeyCode lastRunKey = Plugin.CorpseReportKey != null
                        ? Plugin.CorpseReportKey.Value : KeyCode.R;
                    if (InputGate.KeysFree && Input.GetKeyDown(lastRunKey))
                    {
                        CorpseReportConsumedPress = true;
                        WLog.Line("corpse_report_denied_lastrun_local", secret: false);
                        if (_corpseReportLastRunGate.TryPass(SessionClock.NowMs(),
                                RolesClientController.ToastDurationSec()))
                        {
                            _d.PushToast(SessionNotice.ForConveneDenied(
                                ConveneRejectReason.CorpseReportLastRun));
                        }
                        _d.FeedScenarioSignal(ScenarioSignal.CorpseReportDenied);
                    }
                    return;
                }

                LocalIsNearUnannouncedCorpse = true;

                _d.MaybeShowTutorial(TutorialId.CorpseDiscovery);

                KeyCode key = Plugin.CorpseReportKey != null ? Plugin.CorpseReportKey.Value : KeyCode.R;
                if (InputGate.KeysFree && Input.GetKeyDown(key))
                {
                    CorpseReportConsumedPress = true;
                    WLog.Line("corpse_report_pressed", secret: false);
                    _d.SendConveneRequest(ConveneKind.CorpseReport);
                }
            }
            catch (Exception e)
            {
                WLog.Line("corpse_report_tick_error", secret: false, ("err", e.Message));
            }
        }

        private bool IsUnannouncedCorpseNear(Vector3 pos)
        {
            PlayerRegistry registry = _d.Registry;
            if (registry == null || !registry.Available) return false;

            MeetingClientState meetingClient = _d.MeetingClientView;
            foreach (PlayerHandle player in Players.All())
            {
                if (!meetingClient.IsDeadUnannounced(player.Actor)) continue;
                if (Players.TryGetDeathHeadPosition(player, out Vector3 headPos)
                    && CorpseReportProximity.IsWithin(Vector3.Distance(pos, headPos)))
                {
                    return true;
                }
            }

            if (FakeBodies.Any)
            {
                foreach ((int actor, Vector3 bodyPos) in FakeBodies.Snapshot())
                {
                    if (!meetingClient.IsDeadUnannounced(actor)) continue;
                    if (CorpseReportProximity.IsWithin(Vector3.Distance(pos, bodyPos))) return true;
                }
            }
            return false;
        }

        private bool LocalCanConvene()
            => _d.ClientPhase == GamePhase.Play && _d.IsLocalAlive();

        private long ComputeMeetingReadyAt()
        {
            long fromStart = _d.GameStartUnixMsClient + _d.ClientConveneSuppressStartSec * 1000L;
            long fromLastEnd = _d.LastMeetingEndUnixMsClient + _d.ClientConveneSuppressAfterSec * 1000L;
            return fromStart > fromLastEnd ? fromStart : fromLastEnd;
        }

        private void TrySetLocalInvincible()
        {
            try
            {
                LocalPlayer.SetInvincible(0.5f);
            }
            catch (Exception e)
            {
                WLog.Line("meeting_invincible_error", secret: false, ("err", e.Message));
            }
        }

        public void Reset()
        {
            _conveneHoldHintGate.Reset();
            _mapToolBlockedHintGate.Reset();
            _corpseReportLastRunGate.Reset();
            _clientWarpUnixMs = 0;
            _warpExecuted = false;
            _meetingCutByCeremony = false;
            _votePendulumPlayed = false;

            _resultCeremonyAtMs = 0;
            _pendingCurseCatActor = -1;
            _pendingCurseDeadlineMs = 0;
            _pendingBeaconAudit = -1;
            _pendingMeetingTutorial = false;

            HideConveneCountdown();
            _d.StopConveneChargeSfx();

            _deathRevealPending = false;
            HideDeathReveal();
        }

        public void DestroyUi()
        {
            _meetingButton.Destroy();
            _d.StopConveneChargeSfx();
            _conveneCountdown.Destroy();
            _deathReveal.Destroy();
            _conveneHoldGauge.Destroy();
            _meetingUiActive = false;
        }
    }
}
