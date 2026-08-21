using System;
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

        private void InvokeReturnToLobby()
        {
            try
            {
                var rm = RunManager.instance;
                if (rm == null)
                {
                    WLog.Line("return_lobby_skipped", secret: false, ("reason", "no_run_manager"));
                    return;
                }
                WLog.Line("return_lobby", secret: false, ("via", "result_sequence"));
                rm.ChangeLevel(true, false, RunManager.ChangeLevelType.LobbyMenu);
                rm.AllPlayersDeadSet(false);
            }
            catch (Exception e)
            {
                WLog.Line("return_lobby_error", secret: false, ("err", e.Message));
            }
        }

        private void ResetToLobby(string reason)
        {
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
                _roles.OnInformantEstablished -= HandleInformantDigest;
                _roles.OnPerkUnlocked -= HandlePerkUnlockedDigest;
                _roles.OnCurseKill -= ExecuteCurseKill;
                _roles.OnEnemyIgnoreChanged -= ApplyEnemyIgnore;
                _roles.OnBeaconTriggered -= TriggerBeacon;
                _roles.OnGaugePctChanged -= HandleGaugePctChangedForBomber;
                _roles = null;
            }
            _bomber = null;
            _checkmate = null;
            _checkmateScanPending = false;
            _checkmateLineNextSyncUnixMs = 0;
            _eradicationConfirmAtUnixMs = 0;
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
            _pendingBots.Clear();
            _pendingForcedRoles.Clear();
            ClearClientState();
            _clientPhase = GamePhase.Lobby;
            _resetArmed = false;
            _autoStartWait.Disarm();
            _resultSequence.Cancel();

            _lifecycleGate.ResetForNextRound();

            ClearCosmeticGrantState(reason);

            Patches.WipeGuardPatch.ResetLogThrottle();

            _meetingButton?.Destroy();
            _movementFreezer?.End();
            _enemyFreezer?.End();
            Patches.PlayerSpawnPatch.MeetingActive = false;
            _meetingUiActive = false;

            foreach (IClientPanel panel in _roundPanels) panel.Destroy();
            _toastQueue = null;
            _sfxPlayer.Destroy();

            WLog.Phase(GamePhase.GameOver, GamePhase.Lobby, reason);
            Debugging.StructuredLog.FlushDeferredSecrets("session_reset");
        }

        private void ClearClientState()
        {
            ClearResultCountdown();
            ResetVoidMatch();
            _nextConveneHoldHintUnixMs = 0;
            _localRole = null;
            _knownWerewolves = null;
            _knownTeammateRoles = null;
            IdRoster.Reset();
            ReplaySampler.ResetAll();
            _clientRoundEndUnixMs = 0;
            _clientWerewolfCount = 0;
            _deathMirror.Clear();
            ResetMeetingChat();
            _chatRecapLostBaseline = 0;
            _chatMeetingNumber = 0;
            _lastScatterGroups = null;
            _displayNameCache = null;
            _clientMinimapHideEnabled = false;
            _clientCatPossible = false;
            _clientDebugSession = false;
            _clientValuableMapMode = ValuableMapMode.MeetingSync;
            _clientNecroVoiceMode = NecroVoiceMode.NonWerewolfDead;
            _clientExtraJumpCount = null;
            _clientConveneSuppressStartSec = null;
            _clientConveneSuppressAfterSec = null;
            _clientHealIntervalSec = null;
            _clientOutfitChangeAllowed = null;

            _clientBombPack = null;
            _clientShamanPack = null;
            _voiceDriver?.ForceRestore("client_reset");
            _voiceDriver?.SanitizeAtRoundStart();
            _meetingClient.Reset();
            _clientWarpUnixMs = 0;
            _warpExecuted = false;
            _meetingCutByCeremony = false;
            _votePendulumPlayed = false;
            _gameStartUnixMsClient = 0;
            _lastMeetingEndUnixMsClient = 0;
            _clientRevealDelaySec = 0;
            _catAwakenGate.Reset();
            _tutorialPresenter.Cancel();
            _wasRoleRevealVisible = false;
            _prevBeaconCharges = 0;
            _truckWarper?.ResetWatchdog();
            _scatterPlanAtUnixMs = 0;
            _scatterAwaitCurse = false;
            _extractionScatter?.ClearPlan();
            _scatterGuard.Disarm();
            _clientScatterGuard.Disarm();
            _votePanel?.StopScatterReveal();
            ResetRolesClient("client_reset");

            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }
            _revealCinematic.HideNow();
            if (_catAwakenToastCoroutine != null)
            {
                StopCoroutine(_catAwakenToastCoroutine);
                _catAwakenToastCoroutine = null;
            }
            _catAwakenToast.HideNow();
            _revealStarted = false;
            _awakeningRevealStarted = false;

            HideConveneCountdown();

            _deathRevealPending = false;
            HideDeathReveal();

            HideCheckmateReveal();

            HideEradicationReveal();

            _resultScreen.Hide();

            ClearClientDigest();

            ResetBomberClient();

            ResetShamanClient();
        }

    }
}
