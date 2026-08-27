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
    public sealed partial class WerewolfDirector
    {

        internal MeetingClientState MeetingClientView => _meetingClient;

        internal long GameStartUnixMsClient => _roundMirror.GameStartUnixMs;

        internal long LastMeetingEndUnixMsClient => _roundMirror.LastMeetingEndUnixMs;

        internal INetBus ClientBus => _bus;

        internal void PlaySfx(string clipKey, float volumeScale = 1f)
        {
            EnsureSfxBuilt();
            _sfxPlayer.Play(clipKey, volumeScale);
        }

        internal void PlaySfxLoop(string clipKey, float volume)
        {
            EnsureSfxBuilt();
            _sfxPlayer.PlayLoop(clipKey, volume);
        }

        internal void StopSfxLoop(string clipKey) => _sfxPlayer.StopLoop(clipKey);

        internal void PlayConveneChargeSfx(float ratio)
        {
            EnsureSfxBuilt();
            _sfxPlayer.PlayConveneCharge(ratio);
        }

        internal void StopConveneChargeSfx() => _sfxPlayer.StopConveneCharge();

        internal Vector3? ResolveBodyWorldPos(int actor)
        {
            if (Players.TryGetPosition(actor, out Vector3 avatarPos)) return avatarPos;
            if (FakeBodies.TryGetPosition(actor, out Vector3 pos)) return pos;
            return null;
        }

        internal GamePhase ClientPhaseMirror => _clientPhase;

        internal long ClientRoundEndUnixMs => _roundMirror.RoundEndUnixMs;

        internal bool RoleRevealVisible => _roleRevealFeature.CinematicVisible;

        internal int[] KnownWerewolves => _roundMirror.KnownWerewolves;

        internal byte[] KnownTeammateRoles => _roundMirror.KnownTeammateRoles;

        internal int ClientWerewolfCount => _roundMirror.WerewolfCount;

        internal bool ClientCatPossible => _cfgMirror.CatPossible;

        internal int ClientRevealDelaySec => _roundMirror.RevealDelaySec;

        internal bool LocalIsVillagerTeam =>
            _roundMirror.LocalRole.HasValue && RoleDistribution.TeamOf(_roundMirror.LocalRole.Value) == Team.Villagers;

        internal bool IsBlackCatCurseEnabledForClient()
        {
            string blob = ResolveLobbySettingsBlob();
            if (SettingsCatalog.TryDecodeBlob(blob, out var values)
                && values.TryGetValue("BlackCatCurseEnabled", out string raw))
                return raw == "1";
            return true;
        }

        internal void MaybeShowTutorial(TutorialId id) => _tutorialFeature?.MaybeShowTutorial(id);

        internal void FeedScenarioSignal(ScenarioSignal signal) => _scenarioFeature?.FeedSignal(signal);

        internal bool LocalNearCorpseBody
            => _meetingClientFeature != null && _meetingClientFeature.LocalIsNearCorpseBody;

        internal ScenarioShamanSample ShamanSenseSample
            => _shamanFeature != null
                ? new ScenarioShamanSample(_shamanFeature.SenseArmed, _shamanFeature.SenseCoolingDown,
                    _shamanFeature.GhostVisible, _shamanFeature.StormTier)
                : default;

        internal ScenarioFlow ScenarioFlowView => _scenarioFeature?.Flow;

        internal bool ScenarioGuidePending
        {
            get
            {
                if (!ClientScenarioSession) return false;
                ScenarioFlow flow = _scenarioFeature?.Flow;
                return flow != null && !flow.Finished;
            }
        }

        internal int BomberTargetActorClient => _bomberFeature != null ? _bomberFeature.TargetActorClient : -1;

        internal bool ScenarioWrongBombTarget(int targetActor)
            => _scenarioFeature != null && _scenarioFeature.IsWrongBombTarget(targetActor);

        internal bool ScenarioBombAlreadyOnTarget
            => _scenarioFeature != null && _scenarioFeature.BombOnBombTarget();

        internal bool MeetingChatLogOpen => _chatFeature != null && _chatFeature.IsMeetingPanelOpen;

        internal bool MeetingMapOpen => _meetingClientFeature != null && _meetingClientFeature.MapOverlayOpen;

        internal bool MeetingActiveClient => _meetingClient != null && _meetingClient.MeetingActive;

        internal bool ScenarioGuideContainsPointer(UnityEngine.Vector2 p)
            => _scenarioFeature != null && _scenarioFeature.GuideContainsPointer(p);

        internal bool EnsureUiCreated() => _uiManager != null && _uiManager.EnsureCreated(gameObject);

        internal Transform GetUiLayerRoot(string layerName) => _uiManager.GetLayerRoot(layerName);

        internal WerewolfUIManager UiManagerView => _uiManager;

        internal MeetingChatController Chat => _chatFeature;

        internal IReadOnlyList<WPlayer> HostSessionPlayers => _session?.Players;

        internal GameSession HostSession => _session;

        internal RolesSession HostRolesSession => _roles;

        internal MeetingSession HostMeetingSession => _meeting;

        internal MeetingGaugePanel GaugePanelView => _rolesClientFeature.GaugePanel;

        internal CorpseReportHudPanel.Layout CorpseReportHudLayout => _rolesClientFeature.CorpseReportHudLayout;

        internal void PlayShamanHauntingRipple(ShamanStormTier tier)
            => _rolesClientFeature?.PlayShamanHauntingRipple(tier);

        internal void ClearShamanHauntingRipples()
            => _rolesClientFeature?.ClearShamanHauntingRipples();

        internal bool ClientDebugSession => _cfgMirror.DebugSession;

        internal bool ClientScenarioSession => ScenarioRun.Active;

        internal bool ScenarioSuppressesEnemies => ScenarioRun.EnemiesSuppressed;

        internal bool ScenarioForcesInvincible => ScenarioRun.InvincibilityForced;

        internal bool ScenarioCheckmateArmed
            => _scenarioFeature == null || _scenarioFeature.CheckmateArmed;

        internal bool MeetingButtonNear =>
            _meetingClientFeature != null && _meetingClientFeature.MeetingButtonNear;

        internal bool MeetingButtonUsesGrab =>
            _meetingClientFeature != null && _meetingClientFeature.MeetingButtonUsesGrab;

        internal bool ScatterGuardArmed(long now) => _scatterFeature.IsClientGuardArmed(now);

        internal void PushToast(SessionNotice notice) => _rolesClientFeature?.PushToast(notice);

        internal void PushRawToast(string message, string clipKey = NoticeSfx.DefaultClipKey,
            string logKind = "bomber", bool playSfx = true)
            => _rolesClientFeature?.PushRawToast(message, clipKey, logKind, playSfx);

        public void ShowDeadlineBanner() => _rolesClientFeature?.ShowDeadlineBanner();

        public void ShowDiscussionImpact() => _rolesClientFeature?.ShowDiscussionImpact();

        internal void HideDiscussionImpact() => _rolesClientFeature?.HideDiscussionImpact();

        internal bool LocalValuableDiscoverSuppressed =>
            _rolesClientFeature != null && _rolesClientFeature.LocalValuableDiscoverSuppressed;

        internal VotePanel VotePanelView => _votePanel;

        internal bool MeetingUiActive =>
            _meetingClientFeature != null && _meetingClientFeature.MeetingUiActive;

        internal bool WinCeremonyActive => _winCeremonyFeature.WinCeremonyActive;

        internal MeetingScatterController ScatterFeature => _scatterFeature;

        internal RolesClientController RolesClientFeature => _rolesClientFeature;

        internal MeetingClientController MeetingFeature => _meetingClientFeature;

        internal MovementFreezer MovementFreezerView => _movementFreezer;

        internal EnemyFreezer EnemyFreezerView => _enemyFreezer;

        internal TruckWarper TruckWarperView => _truckWarper;

        internal ManualOverlay ManualOverlayView => _manualOverlay;

        internal void OverrideClientPhaseMirror(GamePhase phase) => _clientPhase = phase;

        internal void PlayStoppableSfx(string clipKey, float volumeScale = 1f)
        {
            EnsureSfxBuilt();
            _sfxPlayer.PlayStoppable(clipKey, volumeScale);
        }

        internal void StopStoppableSfx(string clipKey = null) => _sfxPlayer.StopStoppable(clipKey);

        internal string ParticipantIdLabel(int actor)
            => ParticipantLabel.Format(IdRoster.IdOf(actor), ResolveDisplayName(actor));

        public void HostRequestCheckmateScan() => _winCeremonyFeature?.RequestCheckmateScan();

        public bool DebugForceCheckmate() =>
            _winCeremonyFeature != null && _winCeremonyFeature.DebugForceCheckmate();

        internal ResultScreen ResultScreenView => _resultScreen;

        internal ResultSequence ResultSequenceView => _resultSequence;

        internal string LobbyBlobMirror => _lobbySettingsFeature.BlobMirror;

        internal RoomState RoomStateView => _roomState;

        internal string ResolveLobbySettingsBlob() => _lobbySettingsFeature.ResolveLobbySettingsBlob();

        internal void HostRecordExtractionDone() => _resultFlowFeature?.HostRecordExtractionDone();

        internal bool IsChatLogWindowOpenClient => _chatFeature != null && _chatFeature.IsWindowOpen;

        public void RecordMeetingChatClient(int speakerActor, string message)
            => _chatFeature?.RecordChat(speakerActor, message);

        public void RecordReplayChatClient(int speakerActor, string message)
            => _chatFeature?.RecordReplayChat(speakerActor, message);

        internal bool TryInterceptLobbyStart(LobbyStartPage lobbyPage) =>
            _modIntegrityFeature == null || _modIntegrityFeature.TryInterceptLobbyStart(lobbyPage);

        internal bool TryGetModIntegrityStatus(int actor, out ModParticipantRecord record)
        {
            if (_modIntegrityFeature != null) return _modIntegrityFeature.TryGetStatus(actor, out record);
            record = null;
            return false;
        }

        internal int ModIntegrityRevision => _modIntegrityFeature?.Revision ?? -1;
    }
}
