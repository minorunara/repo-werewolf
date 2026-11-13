using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Vanilla;
using UnityEngine;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private FaultIsolatedTickPipeline CreateUpdatePipeline()
        {
            return new FaultIsolatedTickPipeline(
                new FaultIsolatedTickPipeline.Stage("language", TickLanguageStage),
                new FaultIsolatedTickPipeline.Stage("transport", TickTransportStage),
                new FaultIsolatedTickPipeline.Stage("lobby_integrity", TickLobbyIntegrityStage),
                new FaultIsolatedTickPipeline.Stage("warmup_preplay", TickWarmupPreplayStage),
                new FaultIsolatedTickPipeline.Stage("host_sessions", TickHostSessionsStage),
                new FaultIsolatedTickPipeline.Stage("result_and_lobby", TickResultAndLobbyStage),
                new FaultIsolatedTickPipeline.Stage("persistent_ui", TickPersistentUiStage),
                new FaultIsolatedTickPipeline.Stage("meeting_client", TickMeetingClientStage),
                new FaultIsolatedTickPipeline.Stage("scatter_and_enemy", TickScatterAndEnemyStage),
                new FaultIsolatedTickPipeline.Stage("role_clients", TickRoleClientsStage),
                new FaultIsolatedTickPipeline.Stage("voice", TickVoiceStage),
                new FaultIsolatedTickPipeline.Stage("role_reveal_cursor", TickRoleRevealCursorStage),
                new FaultIsolatedTickPipeline.Stage("tutorial_scenario", TickTutorialScenarioStage),
                new FaultIsolatedTickPipeline.Stage("win_presentation", TickWinPresentationStage),
                new FaultIsolatedTickPipeline.Stage("map_icons", TickMapIconsStage),
                new FaultIsolatedTickPipeline.Stage("replay", TickReplayStage),
                new FaultIsolatedTickPipeline.Stage("map_sync", TickMapSyncStage),
                new FaultIsolatedTickPipeline.Stage("lifecycle", TickLifecycleStage));
        }

        private void TickLanguageStage(long now)
        {
            LanguageBinding.Tick();
        }

        private void TickTransportStage(long now)
        {
            EnsureClientBus();
        }

        private void TickLobbyIntegrityStage(long now)
        {
            Runtime.ModIntegrityFeature.Tick(now);
        }

        private void TickWarmupPreplayStage(long now)
        {
            WorldgenItemBindings.TryInitialize();
            WorldgenMapBinding.TryInitialize();
            MeetingButtonAssetResolver.TryWarmup();

            CachePreplayRosterIfNeeded();
            TickAutoStartWait(now);
        }

        private void TickHostSessionsStage(long now)
        {
            HostRoundState? round = _hostRound;
            round?.Session.Tick(now);
            HostPlayState? play = round?.Play;
            play?.Meeting.Tick(now);
            TickCorpseReportCancel(now);
            play?.Roles.Tick(now);
            Runtime.WinCeremonyFeature.TickHost(now);
            _beaconEffect.Tick(now);

            if (Session.IsHost && _resultSequence.TickShouldReturn(now))
                InvokeReturnToLobby();
        }

        private void TickResultAndLobbyStage(long now)
        {
            Runtime.ChatFeature.Tick(now);
            Runtime.ResultFlowFeature.Tick(now);
            Runtime.LobbySettingsFeature.Tick(now);
        }

        private void TickPersistentUiStage(long now)
        {
            TickManualOverlay();
            TickScenarioSelect();
            TickRoomLeaveOnTitle();
            TickStartHold();
        }

        private void TickMeetingClientStage(long now)
        {
            Runtime.MeetingClientFeature.Tick(now);
        }

        private void TickScatterAndEnemyStage(long now)
        {
            Runtime.ScatterFeature.Tick(now);
            Runtime.EnemyFreezer.Tick(
                _hostRound?.Config.MeetingEnemyRespawnScalePct ?? EnemyRespawnScale.MaxPercent,
                Time.deltaTime);
        }

        private void TickRoleClientsStage(long now)
        {
            Runtime.RolesClientFeature.Tick(now);
            Runtime.BomberFeature.Tick(now);
            Runtime.ShamanFeature.Tick(now);
            TickIdBadges();
        }

        private void TickVoiceStage(long now)
        {
            TickVoice();
        }

        private void TickRoleRevealCursorStage(long now)
        {
            Runtime.RoleRevealFeature.Tick(now);
            TickCursorMirror();
        }

        private void TickTutorialScenarioStage(long now)
        {
            Runtime.TutorialFeature.Tick(now);
            Runtime.ScenarioFeature.Tick(now);
        }

        private void TickWinPresentationStage(long now)
        {
            Runtime.MeetingClientFeature.TickDeathRevealGuard();
            Runtime.WinCeremonyFeature.Tick(now);
        }

        private void TickMapIconsStage(long now)
        {
            EnemyMapIcons.Tick(IsRoundActiveClient && LocalRoleClient == Role.Werewolf);
        }

        private void TickReplayStage(long now)
        {
            ReplaySampler.Tick(this);
        }

        private void TickMapSyncStage(long now)
        {
            MapHidePatch.Tick();
            ValuableMapSyncPatch.Tick();
        }

        private void TickLifecycleStage(long now)
        {
            bool inLobby = Session.InMenuLevel || Session.InLobby;
            if (_resetArmed && inLobby)
            {
                ResetToLobby("level_left");
            }
            else if (!inLobby && (_hostRound != null || _clientPhase != GamePhase.Lobby))
            {
                _resetArmed = true;
            }
        }
    }
}
