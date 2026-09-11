using System;
using System.Linq;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        public StartResult StartHosted()
        {
            if (Registry == null || !Registry.Available)
            {
                WLog.Line("start_rejected", secret: false, ("reason", "mod_disabled"));
                return StartResult.Rejected(StartRejectReason.NotInLobby);
            }
            if (!Session.IsHost)
            {
                WLog.Line("start_rejected", secret: false, ("reason", "not_host"));
                return StartResult.Rejected(StartRejectReason.NotInLobby);
            }
            if (_session != null || _clientPhase != GamePhase.Lobby)
            {
                WLog.Line("start_rejected", secret: false,
                    ("reason", "round_already_started"), ("phase", ClientPhase));
                return StartResult.Rejected(StartRejectReason.NotInLobby);
            }

            if (ScenarioRun.Active) SeedScenarioRoster();

            Plugin.RefreshGameConfig();

            string roundSettingsBlob = SettingsCatalog.EncodeBlob(Plugin.GameConfig);
            _roomState.PublishSharedSettings(roundSettingsBlob);
            _lobbySettingsFeature.NotePublishedBlob(roundSettingsBlob);
            _lobbySettingsFeature.NoteMatchBlob(roundSettingsBlob);

            long now = SessionClock.NowMs();

            SelectBus();

            if (Session.InRunLevel) PreplayEnemySpawnHold.Begin(now);

            var players = Registry.BuildRealPlayers();
            players.AddRange(_reservations.Bots);

            _session = new GameSession();
            _session.OnSend += SendViaBus;
            _session.OnSessionEvent += HandleSessionEvent;
            foreach (var kv in _reservations.ForcedRoles)
            {
                _session.ReserveForcedRole(kv.Key, kv.Value);
            }
            if (_reservations.ForcedPeaceful) _session.ReserveForcedPeaceful();
            if (ScenarioRun.Active) ReserveScenarioPlayerRole();

            ClearClientState();
            _resultFlowFeature.FreezeAutoReturnForHost();
            _resultSequence.Cancel();

            StartResult result = _session.StartPreplay(Plugin.GameConfig, players, now, new System.Random());
            if (!result.Success)
            {
                _session.OnSend -= SendViaBus;
                _session.OnSessionEvent -= HandleSessionEvent;
                _session = null;
                PreplayEnemySpawnHold.Cancel();
                return result;
            }

            _reservations.ClearRoles();
            _lifecycleGate.MarkStarted();
            HostValuablesDestroyed = 0;

            _roomState.PublishSettings(Plugin.GameConfig, players.Count);
            _cfgMirror.ValuableMapMode = Plugin.GameConfig.ValuableMapMode;
            _cfgMirror.NecroVoiceMode = Plugin.GameConfig.NecroVoiceMode;
            _cfgMirror.ExtraJumpCount = Plugin.GameConfig.ExtraJumpCount;
            _cfgMirror.ConveneSuppressStartSec = Plugin.GameConfig.ConveneSuppressStartSec;
            _cfgMirror.ConveneSuppressAfterSec = Plugin.GameConfig.ConveneSuppressAfterSec;
            _cfgMirror.HealIntervalSec = Plugin.GameConfig.HealIntervalSec;
            _cfgMirror.OutfitChangeAllowed = Plugin.GameConfig.OutfitChangeAllowed;
            _cfgMirror.BombPack = RoomStateKeys.EncodeBomb(Plugin.GameConfig, players.Count);
            _cfgMirror.ShamanPack = RoomStateKeys.EncodeShaman(Plugin.GameConfig);
            _resetArmed = true;
            return result;
        }

        private void InitializeHostPlayFeatures(long playStartUnixMs)
        {
            if (_session == null || _session.Phase != GamePhase.Play || _meeting != null) return;

            _meeting = new MeetingSession(Plugin.GameConfig, _session, _session.Players, playStartUnixMs);
            _meeting.OnSend += SendViaBus;
            _meeting.OnExecutePlayer += ExecuteVotedPlayer;
            _meeting.OnPhaseChangeRequest += HandleMeetingPhaseRequest;
            _meeting.OnMeetingStateChanged += _roomState.PublishMeeting;
            _meeting.OnRightsChanged += _roomState.PublishRights;

            foreach (WPlayer player in _session.Players)
                _roomState.PublishRights(player.ActorNumber, _meeting.RightsRemaining(player.ActorNumber));

            _winCeremonyFeature.BeginHostRound();

            _roles = new RolesSession(Plugin.GameConfig, _session, playStartUnixMs, new System.Random());
            HostValuablesDestroyed = 0;
            _roles.OnSend += SendViaBus;
            _roles.OnInformantEstablished += HandleInformantEstablished;
            _roles.OnInformantEstablished += _resultFlowFeature.HandleInformantDigest;
            _roles.OnPerkUnlocked += _resultFlowFeature.HandlePerkUnlockedDigest;
            _roles.OnCurseSealed += SealBlackCatOnCurseResolved;
            _roles.OnCurseKill += ExecuteCurseKill;
            _roles.OnEnemyIgnoreChanged += ApplyEnemyIgnore;
            _roles.OnBeaconTriggered += TriggerBeacon;
            _meeting.OnMeetingStateChanged += HandleMeetingStateChangedForRolesAndBomber;
            _meeting.OnVotingStarted += HandleVotingStartedForRoles;

            _bomber = new BombSession(Plugin.GameConfig, _session, playStartUnixMs);
            _roles.OnGaugePctChanged += HandleGaugePctChangedForBomber;
            SendBomberStateIfDirty();

            if (Session.LevelGenerated) Patches.ValueTrackPatch.ScanAndFreezeBase();

            if (ScenarioRun.Active) SeedScenarioLoss();

        }

        private void SeedScenarioRoster()
        {
            ScenarioDefinition definition = ScenarioRun.Definition;
            if (definition == null)
            {
                WLog.Line("scenario_roster_skipped", secret: false,
                    ("scenario", ScenarioRun.Scenario), ("reason", "no_definition"));
                return;
            }

            var participants = definition.Cast.Bots;

            _reservations.Bots.Clear();
            _reservations.Bots.AddRange(ScenarioRoster.BuildBots(participants));

            _reservations.ForcedRoles.Clear();
            foreach (var forced in ScenarioRoster.BuildForcedRoles(participants))
            {
                _reservations.ForcedRoles[forced.Key] = forced.Value;
            }

            WLog.Line("scenario_roster_seeded", secret: false,
                ("scenario", ScenarioRun.Scenario), ("bots", _reservations.Bots.Count));
        }

        private void ReserveScenarioPlayerRole()
        {
            ScenarioDefinition definition = ScenarioRun.Definition;
            if (definition == null || _session == null) return;

            int actor = LocalActor;
            _session.ReserveForcedRole(actor, definition.Cast.PlayerRole);
            WLog.Line("scenario_player_role_reserved", secret: false,
                ("actor", actor), ("role", definition.Cast.PlayerRole));
        }

        private void SeedScenarioLoss()
        {
            ScenarioDefinition definition = ScenarioRun.Definition;
            if (_roles == null || definition == null) return;

            int pct = definition.Environment.SeededLossPct;
            if (pct <= 0) return;

            _roles.DebugAddGaugePct(pct);
            WLog.Line("scenario_loss_seeded", secret: false,
                ("pct", pct),
                ("permille", _roles.Gauge.DisplayPermille),
                ("unlocked", _roles.Gauge.UnlockedFlags));
        }

        public int PendingBotCount => _reservations.Bots.Count;

        public void AddPendingBot(WPlayer bot)
        {
            if (bot != null) _reservations.Bots.Add(bot);
        }

        public void ReserveForcedRole(int actorNumber, Role role)
        {
            _reservations.ForcedRoles[actorNumber] = role;
        }

        public void ReserveForcedPeaceful()
        {
            _reservations.ForcedPeaceful = true;
        }

        public void HostRecordDeath(int actor)
        {
            if (_session == null || !Session.IsHost) return;
            bool recorded = _session.RecordDeath(actor, SessionClock.NowMs());
            _meeting?.NotifyPlayerDied(actor);
            if (recorded) _scatterFeature.TryFireScatterGuard(actor);
        }

        public void HostRecordDeathByActor(int actorNumber, bool asVote)
        {
            if (_session == null || !Session.IsHost) return;
            if (asVote) _session.MarkNextDeathAsVote(actorNumber);
            bool recorded = _session.RecordDeath(actorNumber, SessionClock.NowMs());
            _meeting?.NotifyPlayerDied(actorNumber);
            if (recorded) _scatterFeature.TryFireScatterGuard(actorNumber);
        }

        public void HostNotifyExtraction(bool completed, bool failed)
        {
            if (_session == null || !Session.IsHost) return;
            _session.NotifyExtractionOutcome(completed, failed, SessionClock.NowMs());
        }

        public void HostNotifyMailDeparture()
        {
            if (_session == null || !Session.IsHost) return;

            WLog.Line("mail_departure", secret: false);
            _session.NotifyMailDeparture(SessionClock.NowMs());
        }

        private void TickCorpseReportCancel(long now)
        {
            if (_meeting == null || !Session.IsHost) return;
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
            if (_session == null || !Session.IsHost)
            {
                return PhaseChangeResult.Rejected(PhaseChangeRejectReason.InvalidTransition);
            }
            return _session.RequestPhaseChange(target, SessionClock.NowMs());
        }

        private void ExecuteVotedPlayer(int actorNumber)
        {
            try
            {
                long ceremonyMs = _meeting != null ? _meeting.ResultCeremonyDelayMs : 0;
                long holdExtensionMs = 0;
                bool isBlackCat = _roles != null && _meeting != null
                    && _roles.TryStartCurse(actorNumber, SessionClock.NowMs() + ceremonyMs, out holdExtensionMs,
                                            _meeting.VotersFor(actorNumber));
                if (isBlackCat)
                {
                    _meeting.ExtendClosingHold(holdExtensionMs);
                    _scatterFeature.HostMarkScatterAwaitCurse();
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
                PlayerHandle player = actorNumber > 0 ? Players.Resolve(actorNumber) : PlayerHandle.None;
                if (player.IsValid)
                {
                    _session?.MarkNextDeathAsVote(actorNumber);
                    Players.KillByHost(player);
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

            long now = SessionClock.NowMs();
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
            Extraction.ReadExtractedAndGoal(out int extracted, out int haulGoal);
            _roles.OnMeetingStarted(SessionClock.NowMs(), extracted, haulGoal, _winCeremonyFeature.ComputeCheckmateLineDollars());
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
            _scatterFeature.HostScheduleScatterPlanAfterCurse();
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
                PlayerHandle player = actorNumber > 0 ? Players.Resolve(actorNumber) : PlayerHandle.None;
                if (player.IsValid)
                {
                    Players.KillByHost(player);
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
                _beaconEffect.Trigger(Players.Resolve(requesterActor), SessionClock.NowMs(), summonCooldownSec);
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

        internal void SendViaBus(OutboundMessage msg)
        {
            if (_bus == null) return;
            try
            {
                _resultFlowFeature.ObserveForDigest(msg);
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
            bool denyChanged = NoteBombDeny(senderActor, "plant", reason);
            if (reason == BombDenyReason.None)
            {
                WLog.Line("bomb_plant", secret: true, ("actor", senderActor), ("target", targetActor));
            }
            else if (denyChanged)
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
            Vector3? targetPos = null;
            int currentTarget = _bomber.TargetActor;
            if (currentTarget != -1)
            {
                targetPos = Players.Resolve(currentTarget).Position;
                if (targetPos == null && FakeBodies.TryGetPosition(currentTarget, out Vector3 fakePos))
                    targetPos = fakePos;
                if (targetPos != null)
                {
                    float radius = Plugin.GameConfig != null
                        ? Plugin.GameConfig.BomberTruckSafeRadiusMeters : 10f;
                    targetNearTruck = TruckZone.IsNearTruck(targetPos.Value, radius);
                }
            }

            BombDenyReason scenarioDeny = BombDenyReason.None;
            if (_scenarioFeature != null)
            {
                Vector3? bomberPos = Players.Resolve(senderActor).Position;
                float blastRadius = Plugin.GameConfig != null
                    ? Plugin.GameConfig.BomberBlastRadiusMeters : GameConfig.DefaultBomberBlastRadiusMeters;
                scenarioDeny = _scenarioFeature.DeniesDetonation(
                    bomberPos, targetPos, blastRadius, targetNearTruck, currentTarget);
            }

            BombDenyReason reason = _bomber.TryDetonate(
                senderActor, now, meetingLocked, targetNearTruck, out int detonatedTarget,
                scenarioDeny: scenarioDeny);

            bool denyChanged = NoteBombDeny(senderActor, "detonate", reason);
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
            else if (denyChanged)
            {
                WLog.Line("bomb_deny", secret: false,
                    ("actor", senderActor), ("stage", "detonate"), ("reason", reason));
            }

            SendBomberStateIfDirty();
        }

        private bool NoteBombDeny(int actor, string stage, BombDenyReason reason)
        {
            var key = (actor, stage);
            if (_lastBombDeny.TryGetValue(key, out BombDenyReason last) && last == reason) return false;
            _lastBombDeny[key] = reason;
            return true;
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
                        if (e.Phase == GamePhase.Play)
                        {
                            try
                            {
                                InitializeHostPlayFeatures(
                                    _session != null && _session.StartUnixMs > 0
                                        ? _session.StartUnixMs
                                        : SessionClock.NowMs());
                            }
                            finally
                            {
                                PreplayEnemySpawnHold.Release();
                            }
                        }
                        else if (e.Phase == GamePhase.PreplayAborted && Session.IsHost)
                        {
                            CacheHostSessionRosterNames();
                            _resultSequence.Begin(SessionClock.NowMs(),
                                _resultFlowFeature.RoundGameOverAutoReturnSec);
                        }
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
                        _winCeremonyFeature.HostBeginEradicationCeremony();
                        break;

                    case SessionEventKind.WinnerConfirmed:
                        WLog.Line("session_win", secret: false, ("team", e.Winner.WinningTeam));
                        _enemyIgnoreRoster.ClearAll();

                        long winUnixMs = SessionClock.NowMs();
                        if (!CosmeticHandoff.ShouldGrant(_session.StartUnixMs, winUnixMs, ScenarioRun.Active))
                        {
                            WLog.Line("cosmetic_grant_skipped", secret: false,
                                ("reason", ScenarioRun.Active ? "scenario" : "short_match"),
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

                        if (Session.IsHost)
                        {
                            int autoReturnSec = Plugin.GameConfig != null
                                ? Plugin.GameConfig.GameOverAutoReturnSec : 120;
                            _resultSequence.Begin(SessionClock.NowMs(), autoReturnSec);
                        }
                        break;

                    case SessionEventKind.MatchVoided:
                        WLog.Line("session_void", secret: false);
                        _enemyIgnoreRoster.ClearAll();
                        WLog.Line("cosmetic_grant_skipped", secret: false, ("reason", "void_match"));

                        if (Session.IsHost)
                        {
                            int voidAutoReturnSec = Plugin.GameConfig != null
                                ? Plugin.GameConfig.GameOverAutoReturnSec : 120;
                            _resultSequence.Begin(SessionClock.NowMs(), voidAutoReturnSec);
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
