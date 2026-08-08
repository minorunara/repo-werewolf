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

        private long _busWaitLogAtMs;

        private void EnsureClientBus()
        {
            bool multiRoom = GameManager.instance != null
                && GameManager.instance.gameMode != 0
                && Photon.Pun.PhotonNetwork.InRoom
                && Photon.Pun.PhotonNetwork.CurrentRoom.PlayerCount >= 2;

            if (!multiRoom && _bus == null
                && Photon.Pun.PhotonNetwork.InRoom
                && Photon.Pun.PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                long now = NowUnixMs();
                if (now - _busWaitLogAtMs >= 10000L)
                {
                    _busWaitLogAtMs = now;
                    WLog.Line("client_bus_waiting", secret: false,
                        ("hasGameManager", GameManager.instance != null),
                        ("gameMode", GameManager.instance != null ? GameManager.instance.gameMode : -1),
                        ("players", Photon.Pun.PhotonNetwork.CurrentRoom.PlayerCount));
                }
            }

            if (multiRoom)
            {
                if (_bus == null)
                {
                    var photon = new PhotonRpcBus();
                    photon.Register();
                    _bus = photon;
                    _bus.OnReceived += ApplyInbound;
                    _bus.OnPlayerLeft += HandlePlayerLeft;
                    photon.OnLocalDisconnected += HandleLocalDisconnected;
                    photon.OnRoomPropertiesChanged += HandleRoomPropertiesChanged;
                    WLog.Line("client_bus_created", secret: false,
                        ("localActor", _bus.LocalActorNumber));
                }
            }
            else if (_bus is PhotonRpcBus && _session == null)
            {
                WLog.Line("client_bus_teardown", secret: false, ("reason", "room_left"));
                TeardownBus();
            }

            if (_bus is PhotonRpcBus aliveBus)
            {
                aliveBus.EnsureEndpoint();
            }
        }

        private void SelectBus()
        {
            int roomPlayers = Photon.Pun.PhotonNetwork.CurrentRoom != null
                ? Photon.Pun.PhotonNetwork.CurrentRoom.PlayerCount : 0;
            bool solo = GameManager.instance == null
                || GameManager.instance.gameMode == 0
                || roomPlayers <= 1;
            bool reused = false;
            if (solo)
            {
                TeardownBus();
                int localActor = Photon.Pun.PhotonNetwork.InRoom
                    ? Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber : 1;
                _bus = new LoopbackNetBus(localActor);
                _bus.OnReceived += ApplyInbound;
                _bus.OnPlayerLeft += HandlePlayerLeft;
            }
            else if (_bus is PhotonRpcBus)
            {
                reused = true;
            }
            else
            {
                TeardownBus();
                var photon = new PhotonRpcBus();
                photon.Register();
                _bus = photon;
                _bus.OnReceived += ApplyInbound;
                _bus.OnPlayerLeft += HandlePlayerLeft;
                photon.OnLocalDisconnected += HandleLocalDisconnected;
                photon.OnRoomPropertiesChanged += HandleRoomPropertiesChanged;
            }
            WLog.Line("bus_select", secret: false,
                ("mode", solo ? "loopback" : "photon"), ("roomPlayers", roomPlayers),
                ("reused", reused));
        }

        private void TeardownBus()
        {
            if (_bus == null) return;
            ClientResetPolicy.ApplyRoomLeft(_meetingClient, IdRoster);
            _bus.OnReceived -= ApplyInbound;
            _bus.OnPlayerLeft -= HandlePlayerLeft;
            if (_bus is PhotonRpcBus photon)
            {
                photon.OnLocalDisconnected -= HandleLocalDisconnected;
                photon.OnRoomPropertiesChanged -= HandleRoomPropertiesChanged;
                photon.Unregister();
            }
            _bus = null;
        }

        private void HandlePlayerLeft(int actorNumber)
        {
            try
            {
                bool gameActive = _session != null || _clientPhase != GamePhase.Lobby;
                if (gameActive)
                {
                    PushToast(SessionNotice.ForPlayerDisconnected(ResolveDisplayName(actorNumber)));
                }

                _modManifestAssemblies.Remove(actorNumber);
                _meetingClient.ApplyPlayerLeft(actorNumber);
                _meeting?.NotifyPlayerLeft(actorNumber, NowUnixMs());
                if (_bomber != null)
                {
                    int prevBomber = _bomber.BomberActor;
                    int prevTarget = _bomber.TargetActor;
                    _bomber.OnPlayerDisconnected(actorNumber);
                    if (prevBomber == actorNumber && prevBomber >= 0)
                    {
                        WLog.Line("bomb_invalidate", secret: false,
                            ("reason", "bomber_disconnected"), ("actor", actorNumber));
                    }
                    else if (prevTarget == actorNumber && prevTarget >= 0)
                    {
                        WLog.Line("bomb_invalidate", secret: false,
                            ("reason", "target_disconnected"), ("actor", actorNumber));
                    }
                    SendBomberStateIfDirty();
                }

                _session?.NotifyPlayerLeft(actorNumber, NowUnixMs());
            }
            catch (Exception e)
            {
                WLog.Line("player_left_error", secret: false,
                    ("actor", actorNumber), ("err", e.Message));
            }
        }

        private void HandleLocalDisconnected()
        {
            try
            {
                WLog.Line("local_disconnect_cleanup", secret: false,
                    ("hadSession", _session != null), ("clientPhase", _clientPhase));
                ResetModIntegrity("local_disconnect");
                ClearClientState();
                _clientPhase = GamePhase.Lobby;
                _lastPublishedBlob = null;
                _lobbyBlobMirror = null;
                _lobbySettingsPanel.SetVisibility(panelVisible: false, hintVisible: false);
                _lastPanelBlob = null;
                _lastPanelModeEnabled = false;
                _lobbyPanelUserHidden = false;
                _debugInjectedBlob = null;
                CrownRoster.Clear();
                _autoStartWait.Disarm();
                _resultSequence.Cancel();
            }
            catch (Exception e)
            {
                WLog.Line("local_disconnect_error", secret: false, ("err", e.Message));
            }
        }

        private void HandleRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable changed)
        {
            try
            {
                if (changed == null || changed.Count == 0) return;
                WLog.Line("room_props_changed", secret: false, ("count", changed.Count));

                if (changed.ContainsKey(RoomState.KeyMinimapHide) &&
                    _roomState.TryReadMinimapHide(out bool minimapHide))
                {
                    _clientMinimapHideEnabled = minimapHide;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "minimapHide"), ("value", minimapHide ? 1 : 0));
                }

                if (changed.ContainsKey(RoomState.KeyCatPossible) &&
                    _roomState.TryReadCatPossible(out bool catPossible))
                {
                    _clientCatPossible = catPossible;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "catPossible"), ("value", catPossible ? 1 : 0));
                }

                if (changed.ContainsKey(RoomState.KeyValuableMapMode) &&
                    _roomState.TryReadValuableMapMode(out ValuableMapMode valuableMode))
                {
                    _clientValuableMapMode = valuableMode;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "valuableMapMode"), ("value", (int)valuableMode));
                }

                if (changed.ContainsKey(RoomState.KeyNecroVoiceMode) &&
                    _roomState.TryReadNecroVoiceMode(out NecroVoiceMode necroVoiceMode))
                {
                    _clientNecroVoiceMode = necroVoiceMode;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "necroVoiceMode"), ("value", (int)necroVoiceMode));
                }

                if (changed.ContainsKey(RoomState.KeyExtraJump) &&
                    _roomState.TryReadExtraJump(out int extraJump))
                {
                    _clientExtraJumpCount = extraJump;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "extraJump"), ("value", extraJump));
                }

                if (changed.ContainsKey(RoomState.KeyConveneSuppressStart) &&
                    _roomState.TryReadConveneSuppressStart(out int conveneSupStart))
                {
                    _clientConveneSuppressStartSec = conveneSupStart;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "conveneSupStart"), ("value", conveneSupStart));
                }

                if (changed.ContainsKey(RoomState.KeyConveneSuppressAfter) &&
                    _roomState.TryReadConveneSuppressAfter(out int conveneSupAfter))
                {
                    _clientConveneSuppressAfterSec = conveneSupAfter;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "conveneSupAfter"), ("value", conveneSupAfter));
                }

                if (changed.ContainsKey(RoomState.KeyHealInterval) &&
                    _roomState.TryReadHealInterval(out int healInterval))
                {
                    _clientHealIntervalSec = healInterval;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "healInterval"), ("value", healInterval));
                }

                if (changed.ContainsKey(RoomState.KeyOutfitChange) &&
                    _roomState.TryReadOutfitChange(out bool outfitAllowed))
                {
                    _clientOutfitChangeAllowed = outfitAllowed;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "outfitChange"), ("value", outfitAllowed ? 1 : 0));
                }

                if (changed.ContainsKey(RoomState.KeyBomb) &&
                    _roomState.TryReadBombPack(out int[] bombPack))
                {
                    _clientBombPack = bombPack;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "bomb"),
                        ("bomberPossible", bombPack[RoomStateKeys.BombIndex.BomberPossible]));
                }
                if (changed.ContainsKey(RoomState.KeyShaman) &&
                    _roomState.TryReadShamanPack(out int[] shamanPack))
                {
                    _clientShamanPack = shamanPack;
                    WLog.Line("room_props_applied", secret: false,
                        ("key", "shaman"),
                        ("gazeFullMs", shamanPack[RoomStateKeys.ShamanIndex.GazeFullMs]));
                }

                if (changed.ContainsKey(RoomState.KeyCfgShared) &&
                    _roomState.TryReadSharedSettings(out string sharedBlob))
                {
                    UpdateLobbyBlobMirror(sharedBlob);
                }
            }
            catch (Exception e)
            {
                WLog.Line("room_props_error", secret: false, ("err", e.Message));
            }
        }

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
            _clientRoundEndUnixMs = 0;
            _clientWerewolfCount = 0;
            _deathMirror.Clear();
            ResetMeetingChat();
            _chatRecapLostBaseline = 0;
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

            _resultScreen.Hide();

            ClearClientDigest();

            ResetBomberClient();

            ResetShamanClient();
        }

    }
}
