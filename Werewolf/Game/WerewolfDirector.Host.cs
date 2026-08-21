using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly LifecycleGate _lifecycleGate = new LifecycleGate();

        public StartResult StartHosted()
        {
            if (Registry == null || !Registry.Available)
            {
                WLog.Line("start_rejected", secret: false, ("reason", "mod_disabled"));
                return StartResult.Rejected(StartRejectReason.NotInLobby);
            }
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("start_rejected", secret: false, ("reason", "not_host"));
                return StartResult.Rejected(StartRejectReason.NotInLobby);
            }

            Plugin.RefreshGameConfig();

            string roundSettingsBlob = SettingsCatalog.EncodeBlob(Plugin.GameConfig);
            _roomState.PublishSharedSettings(roundSettingsBlob);
            _lastPublishedBlob = roundSettingsBlob;
            _roundGameOverAutoReturnSec = Math.Max(0, Plugin.GameConfig.GameOverAutoReturnSec);

            long now = NowUnixMs();

            SelectBus();

            var players = Registry.BuildRealPlayers();
            players.AddRange(_pendingBots);

            _session = new GameSession();
            _session.OnSend += SendViaBus;
            _session.OnSessionEvent += HandleSessionEvent;
            foreach (var kv in _pendingForcedRoles)
            {
                _session.ReserveForcedRole(kv.Key, kv.Value);
            }

            ClearClientState();
            _resultSequence.Cancel();

            StartResult result = _session.Start(Plugin.GameConfig, players, now, new System.Random());
            if (!result.Success)
            {
                _session.OnSend -= SendViaBus;
                _session.OnSessionEvent -= HandleSessionEvent;
                _session = null;
                return result;
            }

            _pendingForcedRoles.Clear();
            _lifecycleGate.MarkStarted();

            _meeting = new MeetingSession(Plugin.GameConfig, _session, _session.Players, now);
            _meeting.OnSend += SendViaBus;
            _meeting.OnExecutePlayer += ExecuteVotedPlayer;
            _meeting.OnPhaseChangeRequest += HandleMeetingPhaseRequest;
            _meeting.OnMeetingStateChanged += _roomState.PublishMeeting;
            _meeting.OnRightsChanged += _roomState.PublishRights;

            _roomState.PublishSettings(Plugin.GameConfig, players.Count);
            _clientValuableMapMode = Plugin.GameConfig.ValuableMapMode;
            _clientNecroVoiceMode = Plugin.GameConfig.NecroVoiceMode;
            _clientExtraJumpCount = Plugin.GameConfig.ExtraJumpCount;
            _clientConveneSuppressStartSec = Plugin.GameConfig.ConveneSuppressStartSec;
            _clientConveneSuppressAfterSec = Plugin.GameConfig.ConveneSuppressAfterSec;
            _clientHealIntervalSec = Plugin.GameConfig.HealIntervalSec;
            _clientOutfitChangeAllowed = Plugin.GameConfig.OutfitChangeAllowed;
            _clientBombPack = RoomStateKeys.EncodeBomb(Plugin.GameConfig, players.Count);
            _clientShamanPack = RoomStateKeys.EncodeShaman(Plugin.GameConfig);
            foreach (var p in players)
            {
                _roomState.PublishRights(p.ActorNumber, _meeting.RightsRemaining(p.ActorNumber));
            }

            _checkmate = new CheckmateSequence();
            _checkmateScanPending = false;
            _checkmateNextScanUnixMs = 0;
            _eradicationConfirmAtUnixMs = 0;

            _roles = new RolesSession(Plugin.GameConfig, _session, now, new System.Random());
            _roles.OnSend += SendViaBus;
            _roles.OnInformantEstablished += HandleInformantEstablished;
            _roles.OnInformantEstablished += HandleInformantDigest;
            _roles.OnPerkUnlocked += HandlePerkUnlockedDigest;
            _roles.OnCurseSealed += SealBlackCatOnCurseResolved;
            _roles.OnCurseKill += ExecuteCurseKill;
            _roles.OnEnemyIgnoreChanged += ApplyEnemyIgnore;
            _roles.OnBeaconTriggered += TriggerBeacon;
            _meeting.OnMeetingStateChanged += HandleMeetingStateChangedForRolesAndBomber;
            _meeting.OnVotingStarted += HandleVotingStartedForRoles;

            _bomber = new BombSession(Plugin.GameConfig, _session, now);
            _roles.OnGaugePctChanged += HandleGaugePctChangedForBomber;
            SendBomberStateIfDirty();

            if (LevelGenerator.Instance != null && LevelGenerator.Instance.Generated)
            {
                Patches.ValueTrackPatch.ScanAndFreezeBase();
            }

            _resetArmed = true;
            WLog.Line("start", secret: false, ("players", players.Count), ("mode", BusMode()));
            return result;
        }

        public void OnLevelGenerated()
        {
            try
            {
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

                if (Plugin.GameConfig == null || !Plugin.GameConfig.WerewolfModeEnabled) return;

                if (_session != null) return;

                bool isRunLevel = SemiFunc.RunIsLevel();
                GamePhase phase = HostPhase;
                if (!_lifecycleGate.ShouldAutoStart(true, isRunLevel, phase))
                {
                    WLog.Line("autostart_skipped", secret: false,
                        ("isRunLevel", isRunLevel), ("phase", phase));
                    return;
                }

                _autoStartWait.Arm(NowUnixMs());
                WLog.Line("autostart_armed", secret: false,
                    ("phase", phase), ("timeoutSec", AutoStartWaitGate.DefaultTimeoutSec));
            }
            catch (Exception e)
            {
                WLog.Line("autostart_error", secret: false, ("err", e.Message));
            }
        }

        private void TickAutoStartWait(long now)
        {
            if (!_autoStartWait.Armed) return;

            if (_session != null || !SemiFunc.RunIsLevel() || !SemiFunc.IsMasterClientOrSingleplayer())
            {
                _autoStartWait.Disarm();
                return;
            }

            AutoStartFireReason fire = _autoStartWait.ShouldFire(AllPlayersLevelLoadCompleted(), now);
            if (fire == AutoStartFireReason.None) return;

            WLog.Line("autostart_trigger", secret: false,
                ("phase", HostPhase), ("reason", fire == AutoStartFireReason.AllLoaded ? "all_loaded" : "timeout"));
            StartResult result = StartHosted();
            if (!result.Success)
            {
                WLog.Line("autostart_rejected", secret: false, ("reason", result.Reason));
            }
        }

        private bool AllPlayersLevelLoadCompleted()
        {
            try
            {
                if (GameDirector.instance == null ||
                    GameDirector.instance.currentState != GameDirector.gameState.Main)
                {
                    return false;
                }

                List<PlayerAvatar> avatars = SemiFunc.PlayerGetList();
                if (avatars == null || avatars.Count == 0) return false;
                if (SemiFunc.IsMultiplayer())
                {
                    Photon.Realtime.Room room = Photon.Pun.PhotonNetwork.CurrentRoom;
                    if (room != null && avatars.Count < room.PlayerCount) return false;
                }
                foreach (PlayerAvatar avatar in avatars)
                {
                    if (avatar == null) continue;
                    if (!GameRefs.PlayerAvatar_levelAnimationCompleted(avatar)) return false;
                }
                return true;
            }
            catch (Exception e)
            {
                WLog.Line("autostart_probe_error", secret: false, ("err", e.Message));
                return true;
            }
        }

        public int PendingBotCount => _pendingBots.Count;

        public void AddPendingBot(WPlayer bot)
        {
            if (bot != null) _pendingBots.Add(bot);
        }

        public void ReserveForcedRole(int actorNumber, Role role)
        {
            _pendingForcedRoles[actorNumber] = role;
        }

        public void HostRecordDeath(PlayerAvatar avatar)
        {
            if (_session == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            int actor = Registry.ResolveActor(avatar);
            bool recorded = _session.RecordDeath(actor, NowUnixMs());
            _meeting?.NotifyPlayerDied(actor);
            if (recorded) TryFireScatterGuard(actor);
        }

        public void HostRecordDeathByActor(int actorNumber, bool asVote)
        {
            if (_session == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (asVote) _session.MarkNextDeathAsVote(actorNumber);
            bool recorded = _session.RecordDeath(actorNumber, NowUnixMs());
            _meeting?.NotifyPlayerDied(actorNumber);
            if (recorded) TryFireScatterGuard(actorNumber);
        }

        public void HostNotifyExtraction(bool completed, bool failed)
        {
            if (_session == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            _session.NotifyExtractionOutcome(completed, failed, NowUnixMs());
        }

        public void HostNotifyMailDeparture()
        {
            if (_session == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;

            WLog.Line("mail_departure", secret: false);
            _session.NotifyMailDeparture(NowUnixMs());
        }

        private void TickCorpseReportCancel(long now)
        {
            if (_meeting == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!LastRunGate.IsLastRunActive()) return;
            _meeting.TryCancelCorpseReportCountdown(now);
        }

        private bool HostHasUnannouncedCorpse()
        {
            if (_session == null) return false;
            foreach (var p in _session.Players)
            {
                if (_meetingClient.IsDeadUnannounced(p.ActorNumber)) return true;
            }
            return false;
        }

        public PhaseChangeResult HostRequestPhaseChange(GamePhase target)
        {
            if (_session == null || !SemiFunc.IsMasterClientOrSingleplayer())
            {
                return PhaseChangeResult.Rejected(PhaseChangeRejectReason.InvalidTransition);
            }
            return _session.RequestPhaseChange(target, NowUnixMs());
        }

        private void ExecuteVotedPlayer(int actorNumber)
        {
            try
            {
                long ceremonyMs = _meeting != null ? _meeting.ResultCeremonyDelayMs : 0;
                long holdExtensionMs = 0;
                bool isBlackCat = _roles != null && _meeting != null
                    && _roles.TryStartCurse(actorNumber, NowUnixMs() + ceremonyMs, out holdExtensionMs,
                                            _meeting.VotersFor(actorNumber));
                if (isBlackCat)
                {
                    _meeting.ExtendClosingHold(holdExtensionMs);
                    HostMarkScatterAwaitCurse();
                    long catDelayMs = ceremonyMs
                        + (Plugin.GameConfig != null ? Plugin.GameConfig.CurseWaitSec * 1000 : 10_000);
                    StartCoroutine(DelayedExecute(actorNumber, catDelayMs, reason: "blackcat_curse_wait"));
                    WLog.Line("execute_player", secret: false,
                        ("actor", actorNumber), ("via", "deferred"), ("delayMs", catDelayMs), ("kind", "blackcat"));
                    return;
                }

                long killDelayMs = ceremonyMs + MeetingSession.PostResultKillDelaySec * 1000L;
                StartCoroutine(DelayedExecute(actorNumber, killDelayMs, reason: "post_result_pause"));
                WLog.Line("execute_player", secret: false,
                    ("actor", actorNumber), ("via", "deferred"),
                    ("delayMs", killDelayMs), ("kind", "regular"));
            }
            catch (Exception e)
            {
                WLog.Line("execute_player_error", secret: false,
                    ("actor", actorNumber), ("err", e.Message));
            }
        }

        private System.Collections.IEnumerator DelayedExecute(int actorNumber, long delayMs, string reason)
        {
            if (delayMs > 0) yield return new UnityEngine.WaitForSecondsRealtime(delayMs / 1000f);
            if (_session == null)
            {
                WLog.Line("execute_player_skip", secret: false,
                    ("actor", actorNumber), ("reason", "session_gone"), ("via", "delayed"));
                yield break;
            }
            try
            {
                PlayerAvatar avatar = actorNumber > 0 ? ResolveAvatar(actorNumber) : null;
                if (avatar != null)
                {
                    _session?.MarkNextDeathAsVote(actorNumber);
                    avatar.PlayerDeath(-1);
                    WLog.Line("execute_player_kill", secret: false,
                        ("actor", actorNumber), ("via", "avatar"), ("reason", reason));
                }
                else
                {
                    HostRecordDeathByActor(actorNumber, asVote: true);
                    WLog.Line("execute_player_kill", secret: false,
                        ("actor", actorNumber), ("via", "record"), ("reason", reason));
                }
            }
            catch (Exception e)
            {
                WLog.Line("execute_player_error", secret: false,
                    ("actor", actorNumber), ("stage", "delayed_kill"), ("err", e.Message));
            }
        }

        private void HandleMeetingPhaseRequest(GamePhase target)
        {
            PhaseChangeResult result = HostRequestPhaseChange(target);
            if (!result.Success)
            {
                WLog.Line("meeting_phase_rejected", secret: false,
                    ("target", target), ("reason", result.Reason));
            }
        }

        private void HandleMeetingStateChangedForRolesAndBomber(int callerActor, long endUnixMs)
        {
            if (callerActor >= 0)
            {
                _beaconEffect.CancelAll("meeting_convene");
                return;
            }

            long now = NowUnixMs();
            _roles?.OnMeetingEnded(now);
            if (_bomber != null)
            {
                _bomber.OnMeetingEnded(now);
                SendBomberStateIfDirty();
            }
        }

        private void HandleVotingStartedForRoles()
        {
            if (_roles == null) return;
            int extracted = -1;
            int haulGoal = -1;
            RoundDirector rd = RoundDirector.instance;
            if (rd != null)
            {
                haulGoal = GameRefs.RoundDirector_haulGoal(rd);
                int points = GameRefs.RoundDirector_extractionPoints(rd);
                int completed = GameRefs.RoundDirector_extractionPointsCompleted(rd);
                extracted = points > 0 ? haulGoal / points * completed : 0;
            }
            _roles.OnMeetingStarted(NowUnixMs(), extracted, haulGoal, HostComputeCheckmateLineDollars());
        }

        private void HandleInformantEstablished()
        {
            _session?.NotifyDisclosureCondition(DisclosureKind.BlackCatSeesWerewolves);
        }

        private void SealBlackCatOnCurseResolved(int catActor)
        {
            try
            {
                HostRecordDeathByActor(catActor, asVote: true);
                WLog.Line("curse_cat_sealed", secret: false, ("actor", catActor));
            }
            catch (Exception e)
            {
                WLog.Line("curse_cat_seal_error", secret: false,
                    ("actor", catActor), ("err", e.Message));
            }
            HostScheduleScatterPlanAfterCurse();
        }

        private void ExecuteCurseKill(int actorNumber)
        {
            StartCoroutine(DelayedCurseKill(actorNumber, RolesSession.CurseKillDelaySec * 1000));
            WLog.Line("curse_kill_deferred", secret: false,
                ("actor", actorNumber), ("delayMs", RolesSession.CurseKillDelaySec * 1000));
        }

        private System.Collections.IEnumerator DelayedCurseKill(int actorNumber, int delayMs)
        {
            if (delayMs > 0) yield return new UnityEngine.WaitForSecondsRealtime(delayMs / 1000f);
            if (_session == null)
            {
                WLog.Line("curse_kill_skip", secret: false,
                    ("actor", actorNumber), ("reason", "session_gone"));
                yield break;
            }
            try
            {
                PlayerAvatar avatar = actorNumber > 0 ? ResolveAvatar(actorNumber) : null;
                if (avatar != null)
                {
                    avatar.PlayerDeath(-1);
                    WLog.Line("curse_kill", secret: false, ("actor", actorNumber), ("via", "avatar"));
                }
                else
                {
                    HostRecordDeathByActor(actorNumber, asVote: false);
                    WLog.Line("curse_kill", secret: false, ("actor", actorNumber), ("via", "record"));
                }
            }
            catch (Exception e)
            {
                WLog.Line("curse_kill_error", secret: false,
                    ("actor", actorNumber), ("err", e.Message));
            }
        }

        private void ApplyEnemyIgnore(int actorNumber, bool ignored)
        {
            try
            {
                string steamId = ResolveSteamId(actorNumber);
                if (string.IsNullOrEmpty(steamId))
                {
                    WLog.Line("enemy_ignore_skipped", secret: true,
                        ("reason", "no_steam_id"), ("actor", actorNumber));
                    return;
                }
                _enemyIgnoreRoster.SetIgnored(steamId, ignored);
            }
            catch (Exception e)
            {
                WLog.Line("enemy_ignore_error", secret: true,
                    ("actor", actorNumber), ("err", e.Message));
            }
        }

        private void TriggerBeacon(int requesterActor)
        {
            try
            {
                int summonCooldownSec = Plugin.GameConfig != null ? Plugin.GameConfig.BeaconCooldownSec : 60;
                _beaconEffect.Trigger(ResolveAvatar(requesterActor), NowUnixMs(), summonCooldownSec);
            }
            catch (Exception e)
            {
                WLog.Line("beacon_effect_error", secret: true,
                    ("actor", requesterActor), ("err", e.Message));
            }
        }

        private string ResolveSteamId(int actorNumber)
        {
            if (_session == null) return null;
            foreach (var p in _session.Players)
            {
                if (p.ActorNumber == actorNumber) return p.SteamId;
            }
            return null;
        }

        private PlayerAvatar ResolveAvatar(int actorNumber)
        {
            var director = GameDirector.instance;
            if (director == null || director.PlayerList == null) return null;
            foreach (PlayerAvatar avatar in director.PlayerList)
            {
                if (avatar != null && Registry.ResolveActor(avatar) == actorNumber) return avatar;
            }
            return null;
        }

        private void SendViaBus(OutboundMessage msg)
        {
            if (_bus == null) return;
            try
            {
                ObserveForDigest(msg);
                switch (msg.Target)
                {
                    case MessageTarget.All:
                        _bus.SendToAll(msg.Code, msg.Payload);
                        break;
                    case MessageTarget.Actors:
                        _bus.SendToActors(msg.Code, msg.Payload, msg.TargetActors);
                        break;
                    case MessageTarget.Master:
                        _bus.SendToMaster(msg.Code, msg.Payload);
                        break;
                }
            }
            catch (Exception e)
            {
                WLog.Line("send_error", secret: false, ("code", (int)msg.Code), ("err", e.Message));
            }
        }

        private void HandleBombPlant(int senderActor, int targetActor, long now)
        {
            if (_bomber == null) return;
            BombDenyReason reason = _bomber.TryPlant(senderActor, targetActor, now);
            if (reason == BombDenyReason.None)
            {
                WLog.Line("bomb_plant", secret: true, ("actor", senderActor), ("target", targetActor));
            }
            else
            {
                WLog.Line("bomb_deny", secret: false,
                    ("actor", senderActor), ("stage", "plant"), ("reason", reason));
            }
            SendBomberStateIfDirty();
        }

        private void HandleBombDetonate(int senderActor, long now)
        {
            if (_bomber == null) return;

            bool meetingLocked = HostPhase != GamePhase.Play;

            bool targetNearTruck = false;
            int currentTarget = _bomber.TargetActor;
            if (currentTarget != -1)
            {
                PlayerAvatar av = ResolveAvatar(currentTarget);
                Vector3? targetPos = av != null ? (Vector3?)av.transform.position : null;
                if (targetPos == null && FakeBodies.TryGetPosition(currentTarget, out Vector3 fakePos))
                    targetPos = fakePos;
                if (targetPos != null)
                {
                    float radius = Plugin.GameConfig != null
                        ? Plugin.GameConfig.BomberTruckSafeRadiusMeters : 10f;
                    targetNearTruck = TruckZone.IsNearTruck(targetPos.Value, radius);
                }
            }

            BombDenyReason reason = _bomber.TryDetonate(
                senderActor, now, meetingLocked, targetNearTruck, out int detonatedTarget);

            if (reason == BombDenyReason.None && detonatedTarget != -1)
            {
                float warningSec = Plugin.GameConfig != null ? Plugin.GameConfig.BomberWarningSec : 1f;
                long detonateAtUnixMs = now + (long)(warningSec * 1000f);
                SendViaBus(new OutboundMessage(
                    MessageCodes.BombDetonation,
                    new object[] { detonatedTarget, detonateAtUnixMs },
                    MessageTarget.All, null));
                WLog.Line("bomb_detonate", secret: true,
                    ("actor", senderActor), ("target", detonatedTarget), ("atUnixMs", detonateAtUnixMs));
            }
            else if (reason == BombDenyReason.TargetDead)
            {
                WLog.Line("bomb_dud", secret: false, ("actor", senderActor));
            }
            else
            {
                WLog.Line("bomb_deny", secret: false,
                    ("actor", senderActor), ("stage", "detonate"), ("reason", reason));
            }

            SendBomberStateIfDirty();
        }

        private void HandleGaugePctChangedForBomber(float cumulativeGaugePct)
        {
            if (_bomber == null) return;
            _bomber.OnGaugeChanged(cumulativeGaugePct);
            SendBomberStateIfDirty();
        }

        private void HandleBomberPlayerDied(int actorNumber)
        {
            if (_bomber == null) return;
            int prevBomber = _bomber.BomberActor;
            _bomber.OnPlayerDied(actorNumber);
            if (prevBomber == actorNumber && prevBomber >= 0)
            {
                WLog.Line("bomb_invalidate", secret: false,
                    ("reason", "bomber_died"), ("actor", actorNumber));
            }
            SendBomberStateIfDirty();
        }

        private void SendBomberStateIfDirty()
        {
            if (_bomber == null || !_bomber.Dirty) return;
            int bomberActor = _bomber.BomberActor;
            BomberStateSnapshot snap = _bomber.BuildSnapshot();
            if (bomberActor < 0) return;

            SendViaBus(new OutboundMessage(
                MessageCodes.BomberState,
                new object[]
                {
                    snap.TargetActor,
                    snap.Ammo,
                    (byte)snap.LastDeny,
                    snap.PlantReadyUnixMs,
                    snap.DetonateReadyUnixMs,
                },
                MessageTarget.Actors, new[] { bomberActor }));
        }

        private void HandleSessionEvent(SessionEvent e)
        {
            try
            {
                switch (e.Kind)
                {
                    case SessionEventKind.PhaseChanged:
                        _roomState.PublishPhase(e.Phase, e.RoundEndUnixMs);
                        if (e.Phase == GamePhase.GameOver || e.Phase == GamePhase.Lobby)
                        {
                            Debugging.StructuredLog.FlushDeferredSecrets("host_phase");
                        }
                        break;
                    case SessionEventKind.PlayerDied:
                        _roomState.PublishAlive(e.ActorNumber, false);
                        _roles?.OnPlayerDied(e.ActorNumber);
                        HandleBomberPlayerDied(e.ActorNumber);
                        break;
                    case SessionEventKind.WinLocked:
                        WLog.Line("session_win_locked", secret: false,
                            ("team", e.Winner.WinningTeam), ("reason", e.Winner.Reason),
                            ("actor", e.ActorNumber), ("vanished", e.Vanished));
                        HostBeginEradicationCeremony();
                        break;

                    case SessionEventKind.WinnerConfirmed:
                        WLog.Line("session_win", secret: false, ("team", e.Winner.WinningTeam));
                        _enemyIgnoreRoster.ClearAll();

                        long winUnixMs = NowUnixMs();
                        if (!CosmeticHandoff.ShouldGrant(_session.StartUnixMs, winUnixMs))
                        {
                            WLog.Line("cosmetic_grant_skipped", secret: false,
                                ("reason", "short_match"),
                                ("durationMs", winUnixMs - _session.StartUnixMs),
                                ("minMs", CosmeticHandoff.MinMatchDurationMs));
                        }
                        else
                        {
                            CosmeticGrant grant = CosmeticLottery.BuildGrant(
                                _session.Players.Select(p => p.ActorNumber).ToList(), new System.Random());
                            int[] rarityCounts = new int[CoinRarity.Count];
                            foreach (byte rarity in grant.Rarities)
                            {
                                rarityCounts[rarity]++;
                            }
                            WLog.Line("cosmetic_grant_sent", secret: false,
                                ("actors", grant.Actors.Length),
                                ("common", rarityCounts[CoinRarity.Common]),
                                ("uncommon", rarityCounts[CoinRarity.Uncommon]),
                                ("rare", rarityCounts[CoinRarity.Rare]),
                                ("ultraRare", rarityCounts[CoinRarity.UltraRare]));
                            _bus.SendToAll(MessageCodes.CosmeticGrant, CosmeticGrantWire.ToWire(grant));
                        }

                        if (SemiFunc.IsMasterClientOrSingleplayer())
                        {
                            int autoReturnSec = Plugin.GameConfig != null
                                ? Plugin.GameConfig.GameOverAutoReturnSec : 120;
                            _resultSequence.Begin(NowUnixMs(), autoReturnSec);
                        }
                        break;

                    case SessionEventKind.MatchVoided:
                        WLog.Line("session_void", secret: false);
                        _enemyIgnoreRoster.ClearAll();
                        WLog.Line("cosmetic_grant_skipped", secret: false, ("reason", "void_match"));

                        if (SemiFunc.IsMasterClientOrSingleplayer())
                        {
                            int voidAutoReturnSec = Plugin.GameConfig != null
                                ? Plugin.GameConfig.GameOverAutoReturnSec : 120;
                            _resultSequence.Begin(NowUnixMs(), voidAutoReturnSec);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                WLog.Line("session_event_error", secret: false, ("err", ex.Message));
            }
        }

    }
}
