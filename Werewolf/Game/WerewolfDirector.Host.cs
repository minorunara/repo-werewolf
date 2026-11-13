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
            if (!Session.IsHost)
            {
                WLog.Line("start_rejected", secret: false, ("reason", "not_host"));
                return StartResult.Rejected(StartRejectReason.NotInLobby);
            }
            if (_hostRound != null || _clientPhase != GamePhase.Lobby)
            {
                WLog.Line("start_rejected", secret: false,
                    ("reason", "round_already_started"), ("phase", ClientPhase));
                return StartResult.Rejected(StartRejectReason.NotInLobby);
            }

            if (ScenarioRun.Active) SeedScenarioRoster();

            Plugin.RefreshGameConfig();
            GameConfig? config = Plugin.GameConfig;
            if (config == null)
            {
                WLog.Line("start_rejected", secret: false, ("reason", "no_config"));
                return StartResult.Rejected(StartRejectReason.InvalidConfig);
            }

            string roundSettingsBlob = SettingsCatalog.EncodeBlob(config);
            _roomState.PublishSharedSettings(roundSettingsBlob);
            Runtime.LobbySettingsFeature.NotePublishedBlob(roundSettingsBlob);
            Runtime.LobbySettingsFeature.NoteMatchBlob(roundSettingsBlob);

            long now = SessionClock.NowMs();

            SelectBus();

            if (Session.InRunLevel) PreplayEnemySpawnHold.Begin(now);

            var players = Registry.BuildRealPlayers();
            players.AddRange(_reservations.Bots);

            var session = new GameSession();
            _hostRound = new HostRoundState(config, session);
            session.OnSend += SendViaBus;
            session.OnSessionEvent += HandleSessionEvent;
            foreach (var kv in _reservations.ForcedRoles)
            {
                session.ReserveForcedRole(kv.Key, kv.Value);
            }
            if (_reservations.ForcedPeaceful) session.ReserveForcedPeaceful();
            if (ScenarioRun.Active) ReserveScenarioPlayerRole();

            StartResult result;
            try
            {
                ClearClientState();
                Runtime.ResultFlowFeature.FreezeAutoReturnForHost(config);
                _resultSequence.Cancel();

                result = session.StartPreplay(config, players, now, new System.Random());
            }
            catch
            {
                DetachHostRoundState();
                PreplayEnemySpawnHold.Cancel();
                throw;
            }
            if (!result.Success)
            {
                DetachHostRoundState();
                PreplayEnemySpawnHold.Cancel();
                return result;
            }

            _reservations.ClearRoles();
            _lifecycleGate.MarkStarted();
            HostValuablesDestroyed = 0;

            _roomState.PublishSettings(config, players.Count);
            _cfgMirror.ValuableMapMode = config.ValuableMapMode;
            _cfgMirror.NecroVoiceMode = config.NecroVoiceMode;
            _cfgMirror.ExtraJumpCount = config.ExtraJumpCount;
            _cfgMirror.ConveneSuppressStartSec = config.ConveneSuppressStartSec;
            _cfgMirror.ConveneSuppressAfterSec = config.ConveneSuppressAfterSec;
            _cfgMirror.HealIntervalSec = config.HealIntervalSec;
            _cfgMirror.OutfitChangeAllowed = config.OutfitChangeAllowed;
            _cfgMirror.BombPack = RoomStateKeys.EncodeBomb(config, players.Count);
            _cfgMirror.ShamanPack = RoomStateKeys.EncodeShaman(config);
            _resetArmed = true;
            return result;
        }

        private void InitializeHostPlayFeatures(long playStartUnixMs)
        {
            HostRoundState? round = _hostRound;
            if (round == null || round.Session.Phase != GamePhase.Play || round.Play != null) return;

            HostPlayState play;
            try
            {
                play = round.BeginPlay(playStartUnixMs, new System.Random(), WireHostPlayState);
            }
            catch (Exception e)
            {
                WLog.Line("host_play_init_error", secret: false, ("err", e.Message));
                if (round.Session.Phase == GamePhase.Play && round.Play == null)
                    round.Session.VoidMatch(SessionClock.NowMs());
                return;
            }

            HostValuablesDestroyed = 0;
            RunHostInitializationStep("win_ceremony", Runtime.WinCeremonyFeature.BeginHostRound);
            PublishInitialMeetingRights(round, play);
            RunHostInitializationStep("bomber_sync", SendBomberStateIfDirty);

            if (Session.LevelGenerated)
                RunHostInitializationStep("gauge_base", Patches.ValueTrackPatch.ScanAndFreezeBase);

            if (ScenarioRun.Active)
                RunHostInitializationStep("scenario_loss", SeedScenarioLoss);

        }

        private static void RunHostInitializationStep(string step, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                WLog.Line("host_init_step_error", secret: false,
                    ("step", step), ("err", e.Message));
            }
        }

        private void PublishInitialMeetingRights(HostRoundState round, HostPlayState play)
        {
            foreach (WPlayer player in round.Session.Players)
            {
                int actor = player.ActorNumber;
                RunHostInitializationStep("meeting_rights_" + actor,
                    () => _roomState.PublishRights(actor, play.Meeting.RightsRemaining(actor)));
            }
        }

        private void WireHostPlayState(HostPlayState play)
        {
            MeetingSession meeting = play.Meeting;
            RolesSession roles = play.Roles;

            meeting.OnSend += SendViaBus;
            meeting.OnExecutePlayer += ExecuteVotedPlayer;
            meeting.OnPhaseChangeRequest += HandleMeetingPhaseRequest;
            meeting.OnMeetingStateChanged += _roomState.PublishMeeting;
            meeting.OnMeetingStateChanged += HandleMeetingStateChangedForRolesAndBomber;
            meeting.OnVotingStarted += HandleVotingStartedForRoles;
            meeting.OnRightsChanged += _roomState.PublishRights;

            roles.OnSend += SendViaBus;
            roles.OnInformantEstablished += HandleInformantEstablished;
            roles.OnInformantEstablished += Runtime.ResultFlowFeature.HandleInformantDigest;
            roles.OnPerkUnlocked += Runtime.ResultFlowFeature.HandlePerkUnlockedDigest;
            roles.OnCurseSealed += SealBlackCatOnCurseResolved;
            roles.OnCurseKill += ExecuteCurseKill;
            roles.OnEnemyIgnoreChanged += ApplyEnemyIgnore;
            roles.OnBeaconTriggered += TriggerBeacon;
            roles.OnGaugePctChanged += HandleGaugePctChangedForBomber;
        }

        private void UnwireHostPlayState(HostPlayState play)
        {
            MeetingSession meeting = play.Meeting;
            RolesSession roles = play.Roles;

            meeting.OnSend -= SendViaBus;
            meeting.OnExecutePlayer -= ExecuteVotedPlayer;
            meeting.OnPhaseChangeRequest -= HandleMeetingPhaseRequest;
            meeting.OnMeetingStateChanged -= _roomState.PublishMeeting;
            meeting.OnMeetingStateChanged -= HandleMeetingStateChangedForRolesAndBomber;
            meeting.OnVotingStarted -= HandleVotingStartedForRoles;
            meeting.OnRightsChanged -= _roomState.PublishRights;

            roles.OnSend -= SendViaBus;
            roles.OnInformantEstablished -= HandleInformantEstablished;
            roles.OnInformantEstablished -= Runtime.ResultFlowFeature.HandleInformantDigest;
            roles.OnPerkUnlocked -= Runtime.ResultFlowFeature.HandlePerkUnlockedDigest;
            roles.OnCurseSealed -= SealBlackCatOnCurseResolved;
            roles.OnCurseKill -= ExecuteCurseKill;
            roles.OnEnemyIgnoreChanged -= ApplyEnemyIgnore;
            roles.OnBeaconTriggered -= TriggerBeacon;
            roles.OnGaugePctChanged -= HandleGaugePctChangedForBomber;
        }

        private void DetachHostRoundState()
        {
            HostRoundState? round = _hostRound;
            _hostRound = null;
            if (round == null) return;

            if (round.Play != null) UnwireHostPlayState(round.Play);
            round.Session.OnSend -= SendViaBus;
            round.Session.OnSessionEvent -= HandleSessionEvent;
        }

        private void SeedScenarioRoster()
        {
            ScenarioDefinition? definition = ScenarioRun.Definition;
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
            ScenarioDefinition? definition = ScenarioRun.Definition;
            GameSession? session = _hostRound?.Session;
            if (definition == null || session == null) return;

            int actor = LocalActor;
            session.ReserveForcedRole(actor, definition.Cast.PlayerRole);
            WLog.Line("scenario_player_role_reserved", secret: false,
                ("actor", actor), ("role", definition.Cast.PlayerRole));
        }

        private void SeedScenarioLoss()
        {
            ScenarioDefinition? definition = ScenarioRun.Definition;
            RolesSession? roles = _hostRound?.Play?.Roles;
            if (roles == null || definition == null) return;

            int pct = definition.Environment.SeededLossPct;
            if (pct <= 0) return;

            roles.DebugAddGaugePct(pct);
            WLog.Line("scenario_loss_seeded", secret: false,
                ("pct", pct),
                ("permille", roles.Gauge.DisplayPermille),
                ("unlocked", roles.Gauge.UnlockedFlags));
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
            HostRoundState? round = _hostRound;
            if (round == null || !Session.IsHost) return;
            bool recorded = round.Session.RecordDeath(actor, SessionClock.NowMs());
            round.Play?.Meeting.NotifyPlayerDied(actor);
            if (recorded) Runtime.ScatterFeature.TryFireScatterGuard(actor);
        }

        public void HostRecordDeathByActor(int actorNumber, bool asVote)
        {
            HostRoundState? round = _hostRound;
            if (round == null || !Session.IsHost) return;
            if (asVote) round.Session.MarkNextDeathAsVote(actorNumber);
            bool recorded = round.Session.RecordDeath(actorNumber, SessionClock.NowMs());
            round.Play?.Meeting.NotifyPlayerDied(actorNumber);
            if (recorded) Runtime.ScatterFeature.TryFireScatterGuard(actorNumber);
        }

        public void HostNotifyExtraction(bool completed, bool failed)
        {
            GameSession? session = _hostRound?.Session;
            if (session == null || !Session.IsHost) return;
            session.NotifyExtractionOutcome(completed, failed, SessionClock.NowMs());
        }

        public void HostNotifyMailDeparture()
        {
            GameSession? session = _hostRound?.Session;
            if (session == null || !Session.IsHost) return;

            WLog.Line("mail_departure", secret: false);
            session.NotifyMailDeparture(SessionClock.NowMs());
        }

        private void TickCorpseReportCancel(long now)
        {
            MeetingSession? meeting = _hostRound?.Play?.Meeting;
            if (meeting == null || !Session.IsHost) return;
            if (!LastRunGate.IsLastRunActive()) return;
            meeting.TryCancelCorpseReportCountdown(now);
        }

        private bool HostHasUnannouncedCorpse()
        {
            GameSession? session = _hostRound?.Session;
            if (session == null) return false;
            foreach (var p in session.Players)
            {
                if (_meetingClient.IsDeadUnannounced(p.ActorNumber)) return true;
            }
            return false;
        }

        public PhaseChangeResult HostRequestPhaseChange(GamePhase target)
        {
            GameSession? session = _hostRound?.Session;
            if (session == null || !Session.IsHost)
            {
                return PhaseChangeResult.Rejected(PhaseChangeRejectReason.InvalidTransition);
            }
            return session.RequestPhaseChange(target, SessionClock.NowMs());
        }

        private void ExecuteVotedPlayer(int actorNumber)
        {
            try
            {
                HostRoundState? round = _hostRound;
                HostPlayState? play = round?.Play;
                long ceremonyMs = play?.Meeting.ResultCeremonyDelayMs ?? 0;
                long holdExtensionMs = 0;
                bool isBlackCat = play != null
                    && play.Roles.TryStartCurse(actorNumber, SessionClock.NowMs() + ceremonyMs, out holdExtensionMs,
                                                play.Meeting.VotersFor(actorNumber));
                if (isBlackCat && play != null)
                {
                    play.Meeting.ExtendClosingHold(holdExtensionMs);
                    Runtime.ScatterFeature.HostMarkScatterAwaitCurse();
                    long catDelayMs = ceremonyMs
                        + (round != null ? round.Config.CurseWaitSec * 1000 : 10_000);
                    StartCoroutine(DelayedExecute(
                        actorNumber, catDelayMs, reason: "blackcat_curse_wait", expectedRound: round));
                    WLog.Line("execute_player", secret: false,
                        ("actor", actorNumber), ("via", "deferred"), ("delayMs", catDelayMs), ("kind", "blackcat"));
                    return;
                }

                long killDelayMs = ceremonyMs + MeetingSession.PostResultKillDelaySec * 1000L;
                StartCoroutine(DelayedExecute(
                    actorNumber, killDelayMs, reason: "post_result_pause", expectedRound: round));
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

        private System.Collections.IEnumerator DelayedExecute(
            int actorNumber,
            long delayMs,
            string reason,
            HostRoundState? expectedRound)
        {
            if (delayMs > 0) yield return new UnityEngine.WaitForSecondsRealtime(delayMs / 1000f);
            if (expectedRound == null || !ReferenceEquals(_hostRound, expectedRound))
            {
                WLog.Line("execute_player_skip", secret: false,
                    ("actor", actorNumber), ("reason", "round_changed"), ("via", "delayed"));
                yield break;
            }
            try
            {
                PlayerHandle player = actorNumber > 0 ? Players.Resolve(actorNumber) : PlayerHandle.None;
                if (player.IsValid)
                {
                    expectedRound.Session.MarkNextDeathAsVote(actorNumber);
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
            HostPlayState? play = _hostRound?.Play;
            if (play == null) return;
            play.Roles.OnMeetingEnded(now);
            play.Bomber.OnMeetingEnded(now);
            SendBomberStateIfDirty();
        }

        private void HandleVotingStartedForRoles()
        {
            RolesSession? roles = _hostRound?.Play?.Roles;
            if (roles == null) return;
            Extraction.ReadExtractedAndGoal(out int extracted, out int haulGoal);
            roles.OnMeetingStarted(SessionClock.NowMs(), extracted, haulGoal, Runtime.WinCeremonyFeature.ComputeCheckmateLineDollars());
        }

        private void HandleInformantEstablished()
        {
            _hostRound?.Session.NotifyDisclosureCondition(DisclosureKind.BlackCatSeesWerewolves);
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
            Runtime.ScatterFeature.HostScheduleScatterPlanAfterCurse();
        }

        private void ExecuteCurseKill(int actorNumber)
        {
            HostRoundState? round = _hostRound;
            StartCoroutine(DelayedCurseKill(
                actorNumber, RolesSession.CurseKillDelaySec * 1000, expectedRound: round));
            WLog.Line("curse_kill_deferred", secret: false,
                ("actor", actorNumber), ("delayMs", RolesSession.CurseKillDelaySec * 1000));
        }

        private System.Collections.IEnumerator DelayedCurseKill(
            int actorNumber,
            int delayMs,
            HostRoundState? expectedRound)
        {
            if (delayMs > 0) yield return new UnityEngine.WaitForSecondsRealtime(delayMs / 1000f);
            if (expectedRound == null || !ReferenceEquals(_hostRound, expectedRound))
            {
                WLog.Line("curse_kill_skip", secret: false,
                    ("actor", actorNumber), ("reason", "round_changed"));
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
                string? steamId = ResolveSteamId(actorNumber);
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
                int summonCooldownSec = _hostRound?.Config.BeaconCooldownSec ?? 60;
                _beaconEffect.Trigger(Players.Resolve(requesterActor), SessionClock.NowMs(), summonCooldownSec);
            }
            catch (Exception e)
            {
                WLog.Line("beacon_effect_error", secret: true,
                    ("actor", requesterActor), ("err", e.Message));
            }
        }

        private string? ResolveSteamId(int actorNumber)
        {
            GameSession? session = _hostRound?.Session;
            if (session == null) return null;
            foreach (var p in session.Players)
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
                Runtime.ResultFlowFeature.ObserveForDigest(msg);
                switch (msg.Destination)
                {
                    case MessageDestination.AllDestination:
                        _bus.SendToAll(msg.Code, msg.Payload);
                        break;
                    case MessageDestination.ActorsDestination actors:
                        _bus.SendToActors(msg.Code, msg.Payload, actors.Actors);
                        break;
                    case MessageDestination.MasterDestination:
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
            BombSession? bomber = _hostRound?.Play?.Bomber;
            if (bomber == null) return;
            BombDenyReason reason = bomber.TryPlant(senderActor, targetActor, now);
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
            HostRoundState? round = _hostRound;
            BombSession? bomber = round?.Play?.Bomber;
            if (round == null || bomber == null) return;

            bool meetingLocked = HostPhase != GamePhase.Play;

            bool targetNearTruck = false;
            Vector3? targetPos = null;
            int currentTarget = bomber.TargetActor;
            if (currentTarget != -1)
            {
                targetPos = Players.Resolve(currentTarget).Position;
                if (targetPos == null && FakeBodies.TryGetPosition(currentTarget, out Vector3 fakePos))
                    targetPos = fakePos;
                if (targetPos != null)
                {
                    float radius = round.Config.BomberTruckSafeRadiusMeters;
                    targetNearTruck = TruckZone.IsNearTruck(targetPos.Value, radius);
                }
            }

            Vector3? bomberPos = Players.Resolve(senderActor).Position;
            float blastRadius = round.Config.BomberBlastRadiusMeters;
            BombDenyReason scenarioDeny = Runtime.ScenarioFeature.DeniesDetonation(
                bomberPos, targetPos, blastRadius, targetNearTruck, currentTarget);

            BombDenyReason reason = bomber.TryDetonate(
                senderActor, now, meetingLocked, targetNearTruck, out int detonatedTarget,
                scenarioDeny: scenarioDeny);

            bool denyChanged = NoteBombDeny(senderActor, "detonate", reason);
            if (reason == BombDenyReason.None && detonatedTarget != -1)
            {
                float warningSec = round.Config.BomberWarningSec;
                long detonateAtUnixMs = now + (long)(warningSec * 1000f);
                SendViaBus(OutboundMessage.ToAll(
                    MessageCodes.BombDetonation,
                    new object[] { detonatedTarget, detonateAtUnixMs }));
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
            BombSession? bomber = _hostRound?.Play?.Bomber;
            if (bomber == null) return;
            bomber.OnGaugeChanged(cumulativeGaugePct);
            SendBomberStateIfDirty();
        }

        private void HandleBomberPlayerDied(int actorNumber)
        {
            BombSession? bomber = _hostRound?.Play?.Bomber;
            if (bomber == null) return;
            int prevBomber = bomber.BomberActor;
            bomber.OnPlayerDied(actorNumber);
            if (prevBomber == actorNumber && prevBomber >= 0)
            {
                WLog.Line("bomb_invalidate", secret: false,
                    ("reason", "bomber_died"), ("actor", actorNumber));
            }
            SendBomberStateIfDirty();
        }

        private void SendBomberStateIfDirty()
        {
            BombSession? bomber = _hostRound?.Play?.Bomber;
            if (bomber == null || !bomber.Dirty) return;
            int bomberActor = bomber.BomberActor;
            BomberStateSnapshot snap = bomber.BuildSnapshot();
            if (bomberActor < 0) return;

            SendViaBus(OutboundMessage.ToActors(
                MessageCodes.BomberState,
                new object[]
                {
                    snap.TargetActor,
                    snap.Ammo,
                    (byte)snap.LastDeny,
                    snap.PlantReadyUnixMs,
                    snap.DetonateReadyUnixMs,
                },
                new[] { bomberActor }));
        }

        private void HandleSessionEvent(SessionEvent e)
        {
            try
            {
                switch (e.Kind)
                {
                    case SessionEventKind.PhaseChanged:
                        RunHostInitializationStep("publish_phase_" + e.Phase,
                            () => _roomState.PublishPhase(e.Phase, e.RoundEndUnixMs));
                        if (e.Phase == GamePhase.Play)
                        {
                            try
                            {
                                GameSession? phaseSession = _hostRound?.Session;
                                InitializeHostPlayFeatures(
                                    phaseSession != null && phaseSession.StartUnixMs > 0
                                        ? phaseSession.StartUnixMs
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
                                Runtime.ResultFlowFeature.RoundGameOverAutoReturnSec);
                        }
                        if (e.Phase == GamePhase.GameOver || e.Phase == GamePhase.Lobby)
                        {
                            Debugging.StructuredLog.FlushDeferredSecrets("host_phase");
                        }
                        break;
                    case SessionEventKind.PlayerDied:
                        _roomState.PublishAlive(e.ActorNumber, false);
                        _hostRound?.Play?.Roles.OnPlayerDied(e.ActorNumber);
                        HandleBomberPlayerDied(e.ActorNumber);
                        break;
                    case SessionEventKind.WinLocked:
                        WLog.Line("session_win_locked", secret: false,
                            ("team", e.Winner!.WinningTeam), ("reason", e.Winner.Reason),
                            ("actor", e.ActorNumber), ("vanished", e.Vanished));
                        Runtime.WinCeremonyFeature.HostBeginEradicationCeremony();
                        break;

                    case SessionEventKind.WinnerConfirmed:
                        WLog.Line("session_win", secret: false, ("team", e.Winner!.WinningTeam));
                        _enemyIgnoreRoster.ClearAll();

                        long winUnixMs = SessionClock.NowMs();
                        GameSession? session = _hostRound?.Session;
                        if (session == null) break;
                        if (!CosmeticHandoff.ShouldGrant(session.StartUnixMs, winUnixMs, ScenarioRun.Active))
                        {
                            WLog.Line("cosmetic_grant_skipped", secret: false,
                                ("reason", ScenarioRun.Active ? "scenario" : "short_match"),
                                ("durationMs", winUnixMs - session.StartUnixMs),
                                ("minMs", CosmeticHandoff.MinMatchDurationMs));
                        }
                        else
                        {
                            CosmeticGrant grant = CosmeticLottery.BuildGrant(
                                session.Players.Select(p => p.ActorNumber).ToList(), new System.Random());
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
                            _bus?.SendToAll(MessageCodes.CosmeticGrant, CosmeticGrantWire.ToWire(grant));
                        }

                        if (Session.IsHost)
                        {
                            _resultSequence.Begin(SessionClock.NowMs(),
                                Runtime.ResultFlowFeature.RoundGameOverAutoReturnSec);
                        }
                        break;

                    case SessionEventKind.MatchVoided:
                        WLog.Line("session_void", secret: false);
                        _enemyIgnoreRoster.ClearAll();
                        WLog.Line("cosmetic_grant_skipped", secret: false, ("reason", "void_match"));

                        if (Session.IsHost)
                        {
                            _resultSequence.Begin(SessionClock.NowMs(),
                                Runtime.ResultFlowFeature.RoundGameOverAutoReturnSec);
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
