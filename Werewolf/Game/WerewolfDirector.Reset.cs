using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private void InvokeReturnToLobby()
        {
            try
            {
                if (!Session.HasRunManager)
                {
                    WLog.Line("return_lobby_skipped", secret: false, ("reason", "no_run_manager"));
                    return;
                }
                if (ScenarioSessionGate.ReturnsToTitle(ScenarioRun.Active))
                {
                    Session.AllPlayersDeadSet(false);
                    ScenarioRun.ExitToTitle("result_sequence", completed: true);
                    return;
                }

                WLog.Line("return_lobby", secret: false, ("via", "result_sequence"));
                Session.ReturnToLobbyMenu();
                Session.AllPlayersDeadSet(false);
            }
            catch (Exception e)
            {
                WLog.Line("return_lobby_error", secret: false, ("err", e.Message));
            }
        }

        private void ResetToLobby(string reason)
        {
            ScenarioRun.Clear();

            if (_session != null)
            {
                WorldgenApplier.RestoreOnLobbyReturn();
            }

            FakeBodies.Clear();

            if (_meeting != null)
            {
                _meeting.OnSend -= SendViaBus;
                _meeting.OnExecutePlayer -= ExecuteVotedPlayer;
                _meeting.OnPhaseChangeRequest -= HandleMeetingPhaseRequest;
                _meeting.OnMeetingStateChanged -= _roomState.PublishMeeting;
                _meeting.OnMeetingStateChanged -= HandleMeetingStateChangedForRolesAndBomber;
                _meeting.OnVotingStarted -= HandleVotingStartedForRoles;
                _meeting.OnRightsChanged -= _roomState.PublishRights;
                _meeting = null;
            }
            if (_roles != null)
            {
                _roles.OnSend -= SendViaBus;
                _roles.OnInformantEstablished -= HandleInformantEstablished;
                _roles.OnInformantEstablished -= _resultFlowFeature.HandleInformantDigest;
                _roles.OnPerkUnlocked -= _resultFlowFeature.HandlePerkUnlockedDigest;
                _roles.OnCurseKill -= ExecuteCurseKill;
                _roles.OnEnemyIgnoreChanged -= ApplyEnemyIgnore;
                _roles.OnBeaconTriggered -= TriggerBeacon;
                _roles.OnGaugePctChanged -= HandleGaugePctChangedForBomber;
                _roles = null;
            }
            _bomber = null;
            _lastBombDeny.Clear();
            _winCeremonyFeature.ResetHost();
            _enemyIgnoreRoster.ClearAll();
            _beaconEffect.CancelAll("reset");
            _beaconEffect.ResetSummonGate();
            if (_session != null)
            {
                _session.OnSend -= SendViaBus;
                _session.OnSessionEvent -= HandleSessionEvent;
                _session = null;
            }
            TeardownBus();
            _reservations.Clear();
            ClearClientState();
            _clientPhase = GamePhase.Lobby;
            _resetArmed = false;
            _autoStartWait.Disarm();
            _resultSequence.Cancel();

            _lifecycleGate.ResetForNextRound();

            _lobbySettingsFeature.ResetForLobbyReturn();

            ClearCosmeticGrantState(reason);

            Patches.WipeGuardPatch.ResetLogThrottle();

            _movementFreezer?.End();
            _enemyFreezer?.End();
            Patches.PlayerSpawnPatch.MeetingActive = false;

            foreach (IClientPanel panel in _roundPanels) panel.Destroy();
            _bomberFeature?.DestroyUi();
            _shamanFeature?.DestroyUi();
            _tutorialFeature?.DestroyUi();
            _scenarioFeature?.DestroyUi();
            _chatFeature?.DestroyUi();
            _resultFlowFeature?.DestroyUi();
            _roleRevealFeature?.DestroyUi();
            _winCeremonyFeature?.DestroyUi();
            _scatterFeature?.DestroyUi();
            _rolesClientFeature?.DestroyUi();
            _meetingClientFeature?.DestroyUi();
            _sfxPlayer.Destroy();

            WLog.Phase(GamePhase.GameOver, GamePhase.Lobby, reason);
            Debugging.StructuredLog.FlushDeferredSecrets("session_reset");
        }

        private void ClearClientState()
        {
            _resultFlowFeature.Reset();
            _roundMirror.Reset();
            _cfgMirror.Reset();
            IdRoster.Reset();
            ReplaySampler.ResetAll();
            _chatFeature.Reset();
            _voiceDriver?.ForceRestore("client_reset");
            _voiceDriver?.SanitizeAtRoundStart();
            _meetingClient.Reset();
            _meetingClientFeature.Reset();
            _tutorialFeature.Reset();
            _scenarioFeature.Reset();
            _truckWarper?.ResetWatchdog();
            _scatterFeature.Reset();
            _votePanel?.StopScatterReveal();
            _rolesClientFeature.Reset();

            _roleRevealFeature.Reset();

            _winCeremonyFeature.Reset();

            _resultScreen.Hide();

            _bomberFeature.Reset();

            _shamanFeature.Reset();
        }

    }
}
