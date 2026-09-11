using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class RolesClientController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private readonly PerkEffects _perkEffects = new PerkEffects();
        private readonly MeetingGaugePanel _gaugePanel = new MeetingGaugePanel();

        private readonly MeetingGaugePanel _playGaugePanel = new MeetingGaugePanel(
            "WW_PlayGaugePanel",
            panelSize: new Vector2(HudGeometry.MiniGauge.PanelWidth, HudGeometry.MiniGauge.PanelHeight),
            pos: new Vector2(-HudGeometry.MiniGauge.RightMargin, HudGeometry.MiniGauge.BottomMargin),
            anchor: new Vector2(1f, 0f), pivot: new Vector2(1f, 0f),
            scale: HudGeometry.MiniGauge.Scale, markerScale: 2.5f);

        private readonly HudPanel _hudPanel = new HudPanel();
        private readonly HudModel _hudModel = new HudModel();

        private readonly DeadlineBannerPanel _deadlineBanner = new DeadlineBannerPanel();

        private readonly DiscussionImpactPanel _discussionImpact = new DiscussionImpactPanel();

        private readonly ToastPanel _toastPanel = new ToastPanel();
        private ToastQueue _toastQueue;

        private readonly WolfStatusPanel _wolfStatusPanel = new WolfStatusPanel();
        private readonly WolfStatusModel _wolfStatusModel = new WolfStatusModel();

        private readonly CorpseReportHudPanel _corpseReportHud = new CorpseReportHudPanel();

        private readonly ValuableRecordHudPanel _valuableRecordHud = new ValuableRecordHudPanel();
        private readonly ValuableRecordHold _valuableRecordHold = new ValuableRecordHold();

        private CurseTargetSource _curseSource;

        internal RolesClientController(WerewolfDirector director)
        {
            _d = director;
            _gaugePanel.OnRevealBreak = () => _d.PlaySfx("sfx_gauge_break");
            _gaugePanel.OnDeliveryReveal = () => _d.PlaySfx("sfx_delivery_register");
        }

        internal MeetingGaugePanel GaugePanel => _gaugePanel;

        internal CorpseReportHudPanel.Layout CorpseReportHudLayout
            => _d.RolesClient != null && _d.RolesClient.PlayGauge != null
                ? CorpseReportHudPanel.Layout.AboveMiniGauge
                : CorpseReportHudPanel.Layout.AtGaugeSlot;

        internal void PlayShamanHauntingRipple(ShamanStormTier tier)
            => _corpseReportHud.PlayHauntingRipple(tier);

        internal void ClearShamanHauntingRipples() => _corpseReportHud.ClearHauntingRipples();

        public void Tick(long now)
        {
            bool matchLive = CombatRules.IsMatchLive(_d.ClientPhaseMirror);
            Role? effectiveRole = matchLive && _d.IsLocalAlive() ? _d.LocalRoleClient : null;
            if (matchLive)
                _perkEffects.Tick(_d.RolesClient, effectiveRole, SendRoleAction, OnWolfModeChangedLocal);
            else
                _perkEffects.ResetEffects();

            bool healEligible = effectiveRole == Role.Werewolf && _d.RolesClient.HealActive
                && _d.ClientPhaseMirror == GamePhase.Play && !_d.MeetingClientView.MeetingActive;
            _perkEffects.TickHeal(healEligible, now, _d.ClientHealIntervalSec);

            if (_curseSource != null && !_d.RolesClient.CurseActive(now))
            {
                DetachCurseSource("expired");
            }

            if (_d.IsRoundActiveClient) EnsureRolesUiBuilt();

            if (_gaugePanel.Exists) _gaugePanel.Tick(_d.RolesClient, _d.MeetingClientView, now);
            if (_playGaugePanel.Exists)
            {
                _playGaugePanel.TickPlay(_d.RolesClient, _d.MeetingClientView,
                    _d.ClientPhase, _d.LocalRoleClient, now);
            }
            if (_hudPanel.Exists)
            {
                var uiBindings = Plugin.Bindings;
                if (uiBindings != null)
                {
                    _hudPanel.PositionOffset = new Vector2(
                        uiBindings.HudOffsetX.Value, uiBindings.HudOffsetY.Value);
                }

                int rights = 0;
                if (_d.MeetingButtonNear)
                {
                    _d.RoomStateView.TryReadRights(_d.LocalActor, out rights);
                }
                var hudInput = new HudInput(
                    phase: _d.RoleRevealVisible ? GamePhase.RoleReveal : _d.ClientPhase,
                    localRole: _d.LocalRoleClient,
                    roundEndUnixMs: _d.ClientRoundEndUnixMs,
                    rolesClient: _d.RolesClient,
                    nearMeetingButton: _d.MeetingButtonNear,
                    rightsRemaining: rights,
                    nowUnixMs: now,
                    meetingActive: _d.MeetingClientView.MeetingActive,
                    debugSession: _d.ClientDebugSession,
                    selfParticipantId: _d.IdRoster.IdOf(_d.LocalActor),
                    scatterGuardActive: _d.ScatterGuardArmed(now),
                    scenarioSession: _d.ClientScenarioSession);
                HudState hudState = _hudPanel.Tick(_hudModel, hudInput);

                if (hudState.BellVolumeScale > 0f)
                {
                    _d.PlaySfx(BellSchedule.ClipKeyFor(hudState.BellMarkSec), hudState.BellVolumeScale);
                }

                if (hudState.BellMarkSec == BellSchedule.AlertThresholdSec) ShowDeadlineBanner();
            }

            if (_deadlineBanner.Exists) _deadlineBanner.Tick();
            if (_discussionImpact.Exists) _discussionImpact.Tick();

            if (_toastPanel.Exists && _toastQueue != null) _toastPanel.Tick(_toastQueue, now);

            bool warpedInMeeting = _d.MeetingClientView.MeetingActive && _d.MeetingClientView.WarpDone(now);
            if (_wolfStatusPanel.Exists)
            {
                WolfStatusState wolfStatus = _wolfStatusModel.Compute(
                    _d.RolesClient, effectiveRole,
                    _d.ClientPhase, warpedInMeeting, now,
                    _d.ClientExtraJumpCount, InfiniteJumpPatch.RefillsThisAirTime,
                    InfiniteJumpPatch.InjectedChargeAvailable);
                _wolfStatusPanel.Tick(wolfStatus,
                    Plugin.WolfModeKey != null ? Plugin.WolfModeKey.Value.ToString() : "?",
                    Plugin.BeaconKey != null ? Plugin.BeaconKey.Value.ToString() : "?");
            }

            GamePhase crPhase = _d.ClientPhase;
            CorpseReportHudMode crMode = CorpseReportHudGate.Compute(
                crPhase, _d.IsLocalAlive(), _d.MeetingClientView.MeetingActive, warpedInMeeting,
                LastRunGate.IsLastRunActive());
            CorpseReportHudPanel.Layout crLayout = CorpseReportHudLayout;
            if (_corpseReportHud.Exists)
            {
                string crKey = Plugin.CorpseReportKey != null
                    ? Plugin.CorpseReportKey.Value.ToString() : "?";
                _corpseReportHud.Tick(crMode, _d.LocalIsNearUnannouncedCorpse,
                    crKey, crLayout);
            }

            TickValuableRecord(crPhase, warpedInMeeting, crLayout);
        }

        private void TickValuableRecord(GamePhase phase, bool warpedInMeeting,
                                        CorpseReportHudPanel.Layout corpseHudLayout)
        {
            bool canOperate = ValuableRecordGate.CanOperate(
                _d.LocalRoleClient, _d.IsLocalAlive(), phase, warpedInMeeting);

            if (!canOperate)
            {
                _valuableRecordHold.Reset();
            }
            else
            {
                KeyCode key = Plugin.CorpseReportKey != null ? Plugin.CorpseReportKey.Value : KeyCode.R;
                bool held = InputGate.KeysFree && Input.GetKey(key);
                if (_valuableRecordHold.Tick(held, _d.CorpseReportConsumedPress, Time.deltaTime)
                    && _d.RolesClient.ToggleValuableRecord(_d.LocalRoleClient))
                {
                    _d.PlaySfx(_d.RolesClient.ValuableRecordOn
                        ? "sfx_valuable_record_on"
                        : "sfx_valuable_record_off");
                }
            }

            if (_valuableRecordHud.Exists)
            {
                _valuableRecordHud.Tick(canOperate, _d.RolesClient.ValuableRecordOn,
                    _valuableRecordHold.Ratio, _valuableRecordHold.IsCharging,
                    Plugin.CorpseReportKey != null ? Plugin.CorpseReportKey.Value.ToString() : "?",
                    corpseHudLayout);
            }
        }

        internal bool LocalValuableDiscoverSuppressed
            => ValuableRecordGate.ShouldSuppressDiscover(
                _d.LocalRoleClient, _d.IsLocalAlive(), _d.IsRoundActiveClient, _d.RolesClient.ValuableRecordOn);

        private void OnWolfModeChangedLocal(bool wolfModeOn)
        {
            if (!wolfModeOn) return;
            _d.PlaySfx("sfx_howl");
        }

        private void EnsureRolesUiBuilt()
        {
            _d.EnsurePanelBuilt(_gaugePanel);
            _d.EnsurePanelBuilt(_playGaugePanel);
            _d.EnsurePanelBuilt(_hudPanel);
            _d.EnsurePanelBuilt(_deadlineBanner);
            _d.EnsurePanelBuilt(_discussionImpact);
            _d.EnsurePanelBuilt(_toastPanel);
            _d.EnsurePanelBuilt(_wolfStatusPanel);
            _d.EnsurePanelBuilt(_corpseReportHud);
            _d.EnsurePanelBuilt(_valuableRecordHud);
        }

        internal void ShowDeadlineBanner()
        {
            _d.EnsurePanelBuilt(_deadlineBanner);
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

        internal void HideDeadlineBanner()
        {
            if (_deadlineBanner.Exists) _deadlineBanner.Hide();
        }

        internal void ShowDiscussionImpact()
        {
            _d.EnsurePanelBuilt(_discussionImpact);
            if (!_discussionImpact.Exists)
            {
                WLog.Line("discussion_impact_skipped", secret: false, ("reason", "not_built"));
                return;
            }
            _discussionImpact.Show(
                Texts.Get(TextId.ImpactDiscussLeft),
                Texts.Get(TextId.ImpactDiscussRight),
                () => _d.PlaySfx(DiscussionImpactClipKey));
        }

        internal void HideDiscussionImpact()
        {
            if (_discussionImpact.Exists) _discussionImpact.Hide();
        }

        private const string DiscussionImpactClipKey = "sfx_discussion_start";

        private const int ToastDurationFallbackSec = 9;

        internal static int ToastDurationSec()
            => Plugin.GameConfig != null && Plugin.GameConfig.ToastDurationSec > 0
                ? Plugin.GameConfig.ToastDurationSec : ToastDurationFallbackSec;

        private void EnsureToastQueue()
        {
            _toastQueue ??= new ToastQueue(ToastDurationSec());
        }

        internal void PushToast(SessionNotice notice)
        {
            if (notice == null) return;
            string message = NoticeCatalog.Format(notice);
            if (string.IsNullOrEmpty(message)) return;

            EnsureToastQueue();
            EnsureRolesUiBuilt();
            _toastQueue.Push(message, SessionClock.NowMs());
            _d.PlaySfx(NoticeSfx.Resolve(notice));
            WLog.Line("toast_push", secret: false, ("kind", notice.Kind));
        }

        internal void PushRawToast(string message, string clipKey = NoticeSfx.DefaultClipKey,
            string logKind = "bomber", bool playSfx = true)
        {
            if (string.IsNullOrEmpty(message)) return;
            EnsureToastQueue();
            EnsureRolesUiBuilt();
            _toastQueue.Push(message, SessionClock.NowMs());
            if (playSfx)
            {
                _d.PlaySfx(clipKey);
            }
            WLog.Line("toast_push_raw", secret: false, ("kind", logKind));
        }

        internal void PushDebugToast(string message)
        {
            EnsureToastQueue();
            EnsureRolesUiBuilt();
            _toastQueue.Push(message, SessionClock.NowMs());
        }

        internal void HandleGaugeSync(object[] p)
        {
            _d.RolesClient.ApplyGaugeSync((int)p[0], (byte)p[1], (byte)p[2], (byte)p[3],
                (long)p[4], (int[])p[5], SessionClock.NowMs());
            WLog.Line("recv_gauge", secret: true,
                ("ratio", (int)p[0]), ("flags", (byte)p[1]),
                ("charges", (byte)p[2]), ("status", (byte)p[3]));
        }

        internal void HandleCurseCandidates(int[] candidates)
        {
            _d.RolesClient.ApplyCurseCandidates(candidates);
            WLog.Line("recv_curse_candidates", secret: true,
                ("count", candidates?.Length ?? -1));
        }

        internal void HandleRoleState(byte subtype, int[] data, long timeUnixMs)
        {
            switch (subtype)
            {
                case RoleStateSubtype.CurseStarted:
                    if (data == null || data.Length < 1) return;
                    int catActor = data[0];
                    WLog.Line("recv_cursestart", secret: false,
                        ("cat", catActor), ("deadline", timeUnixMs));
                    if (_d.MeetingFeature.TryDeferCurseStarted(catActor, timeUnixMs)) break;
                    PresentCurseStarted(catActor, timeUnixMs);
                    break;
                case RoleStateSubtype.CurseResolved:
                    if (data == null || data.Length < 1) return;
                    int victimActor = data[0];
                    PushToast(SessionNotice.ForCurseVictim(_d.ResolveDisplayName(victimActor)));
                    _d.RolesClient.ApplyCurseResolved();
                    DetachCurseSource("resolved");
                    if (_d.VotePanelView.Exists)
                    {
                        _d.VotePanelView.ShowStatusBanner(victimActor != CurseResolution.NoVictim
                            ? Texts.Format(TextId.NoticeCurseVictimFormat, _d.ResolveDisplayName(victimActor))
                            : Texts.Get(TextId.CurseNoVictim));
                        _d.VotePanelView.SetTimeOverride(0);
                    }
                    WLog.Line("recv_curseresolve", secret: false, ("victim", victimActor));
                    break;
                case RoleStateSubtype.MeetingGauge:
                    _d.RolesClient.ApplyMeetingGauge(data);
                    WLog.Line("recv_meetinggauge", secret: false,
                        ("ratio", data != null && data.Length > 0 ? data[0] : -1));
                    break;
            }
        }

        internal void PresentCurseStarted(int catActor, long timeUnixMs)
        {
            _d.RolesClient.ApplyCurseStarted(catActor, timeUnixMs);
            PushToast(SessionNotice.ForBlackCatRevealed(_d.ResolveDisplayName(catActor)));
            _d.PlaySfx("sfx_execution_curse");
            if (catActor == _d.LocalActor)
            {
                AttachCurseSource();
                _d.MaybeShowTutorial(TutorialId.BlackCatSelectedForExecution);
            }
            else
            {
                if (_d.VotePanelView.Exists)
                {
                    _d.VotePanelView.ShowStatusBanner(
                        Texts.Format(TextId.CurseBlackCatRevealedFormat, _d.ResolveDisplayName(catActor)));
                    _d.VotePanelView.SetTimeOverride(timeUnixMs);
                }
                _d.MaybeShowTutorial(TutorialId.BlackCatExecutionRevealed);
            }
        }

        private void AttachCurseSource()
        {
            if (!_d.VotePanelView.Exists)
            {
                WLog.Line("curse_ui_skipped", secret: false, ("reason", "no_vote_panel"));
                return;
            }
            IReadOnlyList<WPlayer> roster = _d.HostSessionPlayers ?? _d.Registry.BuildRealPlayers();
            _curseSource = new CurseTargetSource(_d.RolesClient, _d.MeetingClientView, roster,
                SessionClock.NowMs, SendCurseDesignate);
            _d.VotePanelView.SetSelectionSource(_curseSource);
            _d.VotePanelView.SetTimeOverride(_d.RolesClient.CurseDeadlineUnixMs);
            WLog.Line("curse_ui_attached", secret: false);
        }

        internal void DetachCurseSource(string via)
        {
            if (_curseSource == null) return;
            _curseSource = null;
            _d.VotePanelView?.SetSelectionSource(null);
            _d.VotePanelView?.SetTimeOverride(0);
            WLog.Line("curse_ui_detached", secret: false, ("via", via));
        }

        private void SendCurseDesignate(int targetActor)
            => SendRoleAction(RoleActionSubtype.CurseDesignate, targetActor, 0);

        private void SendRoleAction(byte subtype, int arg, byte flag)
        {
            INetBus bus = _d.ClientBus;
            if (bus == null)
            {
                WLog.Line("roleaction_send_fail", secret: true,
                    ("subtype", subtype), ("reason", "no_bus"));
                return;
            }
            bus.SendToMaster(MessageCodes.RoleAction, new object[] { subtype, arg, flag });
        }

        internal void ResetForLocalDeath()
        {
            _valuableRecordHold.Reset();
            _perkEffects.ResetEffects();
        }

        public void Reset() => ResetRoles("client_reset");

        internal void ResetRoles(string via)
        {
            DetachCurseSource(via);
            _d.RolesClient.Reset();
            _perkEffects.ResetEffects();
            _hudPanel.Hide();
            _wolfStatusPanel.Hide();
            _wolfStatusModel.Reset();
        }

        public void DestroyUi()
        {
            _gaugePanel.Destroy();
            _playGaugePanel.Destroy();
            _hudPanel.Destroy();
            _deadlineBanner.Destroy();
            _discussionImpact.Destroy();
            _toastPanel.Destroy();
            _wolfStatusPanel.Destroy();
            _corpseReportHud.Destroy();
            _valuableRecordHud.Destroy();
            _toastQueue = null;
        }
    }
}
