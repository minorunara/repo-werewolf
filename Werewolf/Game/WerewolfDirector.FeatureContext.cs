using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        internal INetBus? ClientBus => _bus;

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

        internal long ClientRoleRevealStartUnixMs => _roundMirror.RoleRevealStartUnixMs;
        internal long ClientRoleRevealEndUnixMs => _roundMirror.RoleRevealEndUnixMs;

        internal bool RoleRevealVisible => Runtime.RoleRevealFeature.CinematicVisible;

        internal int[]? KnownWerewolves => _roundMirror.KnownWerewolves;

        internal byte[]? KnownTeammateRoles => _roundMirror.KnownTeammateRoles;

        internal int ClientWerewolfCount => _roundMirror.WerewolfCount;

        internal bool ClientCatPossible => _cfgMirror.CatPossible;

        internal int ClientRevealDelaySec => _roundMirror.RevealDelaySec;

        internal bool LocalIsVillagerTeam =>
            _roundMirror.LocalRole.HasValue && RoleDistribution.TeamOf(_roundMirror.LocalRole.Value) == Team.Villagers;

        internal bool IsBlackCatCurseEnabledForClient()
        {
            string? blob = ResolveLobbySettingsBlob();
            if (SettingsCatalog.TryDecodeBlob(blob, out var values)
                && values.TryGetValue("BlackCatCurseEnabled", out string raw))
                return raw == "1";
            return true;
        }

        internal void MaybeShowTutorial(TutorialId id) => Runtime.TutorialFeature.MaybeShowTutorial(id);

        internal void FeedScenarioSignal(ScenarioSignal signal) => Runtime.ScenarioFeature.FeedSignal(signal);

        internal bool LocalNearCorpseBody
            => Runtime.MeetingClientFeature.LocalIsNearCorpseBody;

        internal ScenarioShamanSample ShamanSenseSample => new ScenarioShamanSample(
            Runtime.ShamanFeature.SenseArmed,
            Runtime.ShamanFeature.SenseCoolingDown,
            Runtime.ShamanFeature.GhostVisible,
            Runtime.ShamanFeature.StormTier);

        internal ScenarioFlow? ScenarioFlowView => Runtime.ScenarioFeature.Flow;

        internal bool ScenarioGuidePending
        {
            get
            {
                if (!ClientScenarioSession) return false;
                ScenarioFlow? flow = Runtime.ScenarioFeature.Flow;
                return flow != null && !flow.Finished;
            }
        }

        internal bool ScenarioChatLogExplanationPending
        {
            get
            {
                if (!ClientScenarioSession) return false;
                ScenarioFlow? flow = Runtime.ScenarioFeature.Flow;
                return flow != null && flow.ChatLogExplanationPending;
            }
        }

        internal int BomberTargetActorClient => Runtime.BomberFeature.TargetActorClient;

        internal bool ScenarioWrongBombTarget(int targetActor)
            => Runtime.ScenarioFeature.IsWrongBombTarget(targetActor);

        internal bool ScenarioBombAlreadyOnTarget
            => Runtime.ScenarioFeature.BombOnBombTarget();

        internal bool MeetingChatLogOpen => Runtime.ChatFeature.IsMeetingPanelOpen;

        internal bool MeetingMapOpen => Runtime.MeetingClientFeature.MapOverlayOpen;

        internal bool MeetingActiveClient => _meetingClient.MeetingActive;

        internal bool ScenarioGuideContainsPointer(UnityEngine.Vector2 p)
            => Runtime.ScenarioFeature.GuideContainsPointer(p);

        internal bool EnsureUiCreated() => Runtime.UiManager.EnsureCreated(gameObject);

        internal Transform? GetUiLayerRoot(string layerName) => Runtime.UiManager.GetLayerRoot(layerName);

        internal WerewolfUIManager UiManagerView => Runtime.UiManager;

        internal MeetingChatController Chat => Runtime.ChatFeature;

        internal IReadOnlyList<WPlayer>? HostSessionPlayers => _hostRound?.Session.Players;

        internal GameSession? HostSession => _hostRound?.Session;

        internal GameConfig? HostRoundConfig => _hostRound?.Config;

        internal HostPlayState? HostGameplay => _hostRound?.Play;

        internal MeetingSession? HostMeetingSession => _hostRound?.Play?.Meeting;

        internal MeetingGaugePanel GaugePanelView => Runtime.RolesClientFeature.GaugePanel;

        internal CorpseReportHudPanel.Layout CorpseReportHudLayout => Runtime.RolesClientFeature.CorpseReportHudLayout;

        internal void PlayShamanHauntingRipple(ShamanStormTier tier)
            => Runtime.RolesClientFeature.PlayShamanHauntingRipple(tier);

        internal void ClearShamanHauntingRipples()
            => Runtime.RolesClientFeature.ClearShamanHauntingRipples();

        internal bool ClientDebugSession => _cfgMirror.DebugSession;

        internal bool ClientScenarioSession => ScenarioRun.Active;

        internal bool ScenarioSuppressesEnemies => ScenarioRun.EnemiesSuppressed;

        internal bool ScenarioForcesInvincible => ScenarioRun.InvincibilityForced;

        internal bool ScenarioCheckmateArmed
            => Runtime.ScenarioFeature.CheckmateArmed;

        internal bool MeetingButtonNear =>
            Runtime.MeetingClientFeature.MeetingButtonNear;

        internal bool MeetingButtonUsesGrab =>
            Runtime.MeetingClientFeature.MeetingButtonUsesGrab;

        internal bool ScatterGuardArmed(long now) => Runtime.ScatterFeature.IsClientGuardArmed(now);

        internal void PushToast(SessionNotice notice) => Runtime.RolesClientFeature.PushToast(notice);

        internal void PushRawToast(string message, string clipKey = NoticeSfx.DefaultClipKey,
            string logKind = "bomber", bool playSfx = true)
            => Runtime.RolesClientFeature.PushRawToast(message, clipKey, logKind, playSfx);

        public void ShowDeadlineBanner() => Runtime.RolesClientFeature.ShowDeadlineBanner();

        public void ShowDiscussionImpact() => Runtime.RolesClientFeature.ShowDiscussionImpact();

        internal void HideDiscussionImpact() => Runtime.RolesClientFeature.HideDiscussionImpact();

        internal bool LocalValuableDiscoverSuppressed =>
            Runtime.RolesClientFeature.LocalValuableDiscoverSuppressed;

        internal VotePanel VotePanelView => Runtime.VotePanel;

        internal bool MeetingUiActive =>
            Runtime.MeetingClientFeature.MeetingUiActive;

        internal bool WinCeremonyActive => Runtime.WinCeremonyFeature.WinCeremonyActive;

        internal MeetingScatterController ScatterFeature => Runtime.ScatterFeature;

        internal RolesClientController RolesClientFeature => Runtime.RolesClientFeature;

        internal MeetingClientController MeetingFeature => Runtime.MeetingClientFeature;

        internal MovementFreezer MovementFreezerView => Runtime.MovementFreezer;

        internal EnemyFreezer EnemyFreezerView => Runtime.EnemyFreezer;

        internal TruckWarper TruckWarperView => Runtime.TruckWarper;

        internal ManualOverlay ManualOverlayView => _manualOverlay;

        internal void OverrideClientPhaseMirror(GamePhase phase) => _clientPhase = phase;

        internal void PlayStoppableSfx(string clipKey, float volumeScale = 1f)
        {
            EnsureSfxBuilt();
            _sfxPlayer.PlayStoppable(clipKey, volumeScale);
        }

        internal void StopStoppableSfx(string? clipKey = null) => _sfxPlayer.StopStoppable(clipKey);

        internal string ParticipantIdLabel(int actor)
            => ParticipantLabel.Format(IdRoster.IdOf(actor), ResolveDisplayName(actor));

        public void HostRequestCheckmateScan() => Runtime.WinCeremonyFeature.RequestCheckmateScan();

        public bool DebugForceCheckmate() => Runtime.WinCeremonyFeature.DebugForceCheckmate();

        internal ResultScreen ResultScreenView => _resultScreen;

        internal ResultSequence ResultSequenceView => _resultSequence;

        internal string? LobbyBlobMirror => Runtime.LobbySettingsFeature.BlobMirror;

        internal RoomState RoomStateView => _roomState;

        internal string? ResolveLobbySettingsBlob() => Runtime.LobbySettingsFeature.ResolveLobbySettingsBlob();

        internal void HostRecordExtractionDone() => Runtime.ResultFlowFeature.HostRecordExtractionDone();

        internal bool IsChatLogWindowOpenClient => Runtime.ChatFeature.IsWindowOpen;

        public void RecordMeetingChatClient(int speakerActor, string message)
            => Runtime.ChatFeature.RecordChat(speakerActor, message);

        public void RecordReplayChatClient(int speakerActor, string message)
            => Runtime.ChatFeature.RecordReplayChat(speakerActor, message);

        internal bool TryInterceptLobbyStart(LobbyStartPage? lobbyPage) =>
            Runtime.ModIntegrityFeature.TryInterceptLobbyStart(lobbyPage);

        internal bool TryGetModIntegrityStatus(int actor,
            [NotNullWhen(true)] out ModParticipantRecord? record)
            => Runtime.ModIntegrityFeature.TryGetStatus(actor, out record);

        internal int ModIntegrityRevision => Runtime.ModIntegrityFeature.Revision;
    }
}
