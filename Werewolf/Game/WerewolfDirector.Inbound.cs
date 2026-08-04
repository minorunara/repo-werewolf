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

        private bool _cosmeticGrantApplied;

        private int[] _pendingCoinCounts;

        private void ApplyInbound(InboundMessage msg)
        {
            try
            {
                if (TryHandleModIntegrityInbound(msg, NowUnixMs())) return;

                if (EventCodes.IsMasterInbound(msg.Code))
                {
                    HandleMasterInbound(msg);
                    return;
                }

                object[] p = msg.Payload;
                switch (msg.Code)
                {
                    case EventCodes.AssignRole:
                        _localRole = (Role)(byte)p[0];
                        WLog.Line("recv_role", secret: true, ("role", _localRole));
                        TryStartRoleReveal();
                        break;
                    case EventCodes.RevealSelfRole:
                        {
                            bool initialRevealDone = _revealStarted;
                            _localRole = (Role)(byte)p[0];
                            WLog.Line("recv_selfrole", secret: true, ("role", _localRole));
                            TryStartRoleReveal();
                            if (initialRevealDone) TryStartAwakeningReveal();
                        }
                        break;
                    case EventCodes.RevealTeammates:
                        _knownWerewolves = (int[])p[0];
                        {
                            byte[] rr = p.Length > 1 ? p[1] as byte[] : null;
                            _knownTeammateRoles = (rr != null && rr.Length == _knownWerewolves.Length) ? rr : null;
                        }
                        WLog.Line("recv_teammates", secret: true, ("count", _knownWerewolves.Length));
                        TryStartRoleReveal();
                        if (_localRole == Role.BlackCat)
                            MaybeShowTutorial(TutorialId.InformantUnlockedAsBlackCat);
                        break;
                    case EventCodes.PlayerDied:
                        int deadActor = (int)p[0];
                        var deadCause = (DeathCause)(byte)p[1];
                        _deathMirror[deadActor] = deadCause;
                        _meetingClient.ApplyPlayerDied(deadActor, deadCause);
                        if (deadActor == LocalActor)
                        {
                            RolesClient.ForceWolfModeOff();
                            RolesClient.ForceValuableRecordOff();
                            _valuableRecordHold.Reset();
                            _perkEffects.ResetEffects();
                            MaybeShowTutorial(TutorialId.FirstDeath);
                        }
                        WLog.Line("recv_died", secret: false,
                            ("actor", deadActor), ("cause", deadCause));
                        break;
                    case EventCodes.ResultDigest:
                        ApplyResultDigest(p);
                        break;
                    case EventCodes.GameOver:
                        _clientPhase = GamePhase.GameOver;
                        _meetingClient.ApplyPhase(GamePhase.GameOver);
                        _meeting?.AbortForGameOver();
                        ResetRolesClient("gameover");
                        WLog.Line("recv_gameover", secret: false, ("team", (Team)(byte)p[0]));
                        BeginResultCountdown();
                        ShowResultScreen((byte)p[0], (int[])p[1], (byte[])p[2]);
                        break;
                    case EventCodes.GameStart:
                        ClearCosmeticGrantState("game_start");
                        ClearClientDigest();
                        CrownRoster.Clear();
                        Patches.WipeGuardPatch.ResetLogThrottle();
                        _voiceDriver?.SanitizeAtRoundStart();
                        _clientRoundEndUnixMs = (long)p[0];
                        _clientWerewolfCount = (byte)p[2];
                        _gameStartUnixMsClient = NowUnixMs();
                        FreezeResultAutoReturnConfig();
                        _clientRevealDelaySec = (int)p[4];
                        _clientCatPossible = (byte)p[3] != 0;
                        _clientDebugSession = (byte)p[5] != 0;
                        WLog.Line("recv_gamestart", secret: false,
                            ("roundEnd", _clientRoundEndUnixMs),
                            ("werewolfCount", _clientWerewolfCount),
                            ("catPossible", _clientCatPossible),
                            ("revealDelaySec", _clientRevealDelaySec),
                            ("debugSession", _clientDebugSession ? 1 : 0));
                        TryStartRoleReveal();
                        break;
                    case EventCodes.PhaseChanged:
                        {
                            GamePhase newPhase = (GamePhase)(byte)p[0];
                            if (_clientPhase == GamePhase.Meeting && newPhase == GamePhase.Play)
                            {
                                _lastMeetingEndUnixMsClient = NowUnixMs();
                            }
                            _clientPhase = newPhase;
                            _clientRoundEndUnixMs = (long)p[2];
                            _meetingClient.ApplyPhase(_clientPhase);
                            if (_clientPhase == GamePhase.GameOver || _clientPhase == GamePhase.Lobby)
                            {
                                ResetRolesClient("phase");
                                Debugging.StructuredLog.FlushDeferredSecrets("recv_phase");
                            }
                            WLog.Line("recv_phase", secret: false,
                                ("phase", _clientPhase), ("roundEnd", _clientRoundEndUnixMs));
                        }
                        break;

                    case EventCodes.StartMeeting:
                        {
                            ConveneKind startKind = (byte)p[3] == 1
                                ? ConveneKind.CorpseReport : ConveneKind.Button;
                            HandleStartMeeting((int)p[0], (long)p[1], (long)p[2], startKind);
                            string startCaller = ResolveDisplayName((int)p[0]);
                            PushToast(startKind == ConveneKind.CorpseReport
                                ? SessionNotice.ForCorpseReportStarted(startCaller)
                                : SessionNotice.ForConveneStarted(startCaller));
                        }
                        break;
                    case EventCodes.MeetingCancelled:
                        _meetingClient.ApplyCancelled();
                        PushToast(SessionNotice.ForMeetingCancelled());
                        WLog.Line("recv_meeting_cancelled", secret: false, ("reason", (byte)p[0]));
                        break;
                    case EventCodes.VoteProgress:
                        int[] votedActors = (int[])p[0];
                        bool voteAdded = votedActors.Length > _meetingClient.VotedActors.Count;
                        RecordMeetingVotesClient(votedActors);
                        _meetingClient.ApplyVoteProgress(votedActors, (long)p[1]);
                        _votePanel?.NotifyVoteProgress();
                        if (voteAdded)
                        {
                            EnsureSfxBuilt();
                            _sfxPlayer.Play("sfx_vote_cast");
                        }
                        WLog.Line("recv_voteprogress", secret: false,
                            ("voted", votedActors.Length), ("endUnixMs", (long)p[1]));
                        break;
                    case EventCodes.MeetingResult:
                        int executedActor = (int)p[0];
                        _meetingClient.ApplyResult(new MeetingOutcome
                        {
                            ExecutedActor = executedActor,
                            TargetActors = (int[])p[1],
                            VoteCounts = (int[])p[2],
                        });
                        WLog.Line("recv_meetingresult", secret: false, ("executed", executedActor));
                        PushToast(executedActor == -1
                            ? SessionNotice.ForNoExecution()
                            : SessionNotice.ForExecuted(ResolveDisplayName(executedActor)));
                        if (executedActor != -1) _executionSfxWaitTicks = 0;
                        break;
                    case EventCodes.ConveneDenied:
                        var denyReason = ConveneDeniedWire.FromWire((byte)p[0]);
                        PushToast(SessionNotice.ForConveneDenied(denyReason));
                        WLog.Line("recv_convenedenied", secret: false,
                            ("wire", (byte)p[0]), ("reason", denyReason));
                        break;

                    case EventCodes.BeaconAudit:
                        _pendingBeaconAudit = (byte)p[0];
                        WLog.Line("recv_beacon_audit", secret: false, ("uses", (byte)p[0]));
                        break;
                    case EventCodes.SyncPerkGauge:
                        RolesClient.ApplyGaugeSync((int)p[0], (byte)p[1], (byte)p[2], (byte)p[3], (long)p[4], (int[])p[5], NowUnixMs());
                        WLog.Line("recv_gauge", secret: true,
                            ("ratio", (int)p[0]), ("flags", (byte)p[1]),
                            ("charges", (byte)p[2]), ("status", (byte)p[3]));
                        break;
                    case EventCodes.RoleState:
                        HandleRoleState((byte)p[0], (int[])p[1], (long)p[2]);
                        break;
                    case EventCodes.CurseCandidates:
                        RolesClient.ApplyCurseCandidates((int[])p[0]);
                        WLog.Line("recv_curse_candidates", secret: true,
                            ("count", ((int[])p[0])?.Length ?? -1));
                        break;

                    case EventCodes.CosmeticGrant:
                        HandleCosmeticGrant(p);
                        break;

                    case EventCodes.BomberState:
                        ApplyBomberState((int)p[0], (byte)p[1], (byte)p[2], (long)p[3], (long)p[4]);
                        break;
                    case EventCodes.BombDetonation:
                        ApplyBombDetonation((int)p[0], (long)p[1]);
                        break;

                    case EventCodes.CheckmateReveal:
                        HandleCheckmateReveal(p);
                        break;
                }
            }
            catch (Exception e)
            {
                WLog.Line("recv_error", secret: false, ("code", (int)msg.Code), ("err", e.Message));
            }
        }

        private void HandleMasterInbound(InboundMessage msg)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                DropMasterInbound(msg, "not_host");
                return;
            }

            long now = NowUnixMs();
            switch (msg.Code)
            {
                case EventCodes.RequestMeeting:
                    {
                        if (_meeting == null) { DropMasterInbound(msg, "no_meeting_session"); return; }
                        if (_checkmate != null && _checkmate.CeremonyStarted)
                        {
                            DropMasterInbound(msg, "checkmate_ceremony");
                            return;
                        }
                        ConveneKind kind = msg.Payload.Length > 0 && (byte)msg.Payload[0] == 1
                            ? ConveneKind.CorpseReport : ConveneKind.Button;
                        bool lastRun = kind == ConveneKind.CorpseReport
                            && LastRunGate.IsLastRunActive();
                        bool corpse = kind != ConveneKind.CorpseReport || HostHasUnannouncedCorpse();
                        _meeting.TryConvene(msg.SenderActor, now, kind, lastRun, corpse);
                    }
                    break;
                case EventCodes.CastVote:
                    if (_meeting == null) { DropMasterInbound(msg, "no_meeting_session"); return; }
                    _meeting.CastVote(msg.SenderActor, (int)msg.Payload[0], now);
                    break;
                case EventCodes.RoleAction:
                    {
                        byte subtype = (byte)msg.Payload[0];
                        int arg = (int)msg.Payload[1];
                        if (subtype == BombRoleActionSubtype.Plant)
                        {
                            if (_bomber == null) { DropMasterInbound(msg, "no_bomb_session"); return; }
                            HandleBombPlant(msg.SenderActor, arg, now);
                            return;
                        }
                        if (subtype == BombRoleActionSubtype.Detonate)
                        {
                            if (_bomber == null) { DropMasterInbound(msg, "no_bomb_session"); return; }
                            HandleBombDetonate(msg.SenderActor, now);
                            return;
                        }
                        if (_roles == null) { DropMasterInbound(msg, "no_roles_session"); return; }
                        _roles.HandleRoleAction(msg.SenderActor, subtype, arg, (byte)msg.Payload[2], now);
                    }
                    break;
            }
        }

        private static void DropMasterInbound(InboundMessage msg, string reason)
        {
            WLog.Line("recv_master_dropped", secret: false,
                ("code", (int)msg.Code), ("sender", msg.SenderActor), ("reason", reason));
        }

        private void HandleCosmeticGrant(object[] p)
        {
            if (_cosmeticGrantApplied)
            {
                WLog.Line("cosmetic_grant_drop", secret: false, ("reason", "already_applied"));
                return;
            }

            if (!CosmeticGrantWire.TryFromWire(p, out CosmeticGrant grant))
            {
                WLog.Line("cosmetic_grant_drop", secret: false, ("reason", "invalid_payload"));
                return;
            }

            _cosmeticGrantApplied = true;

            int localActor = _bus.LocalActorNumber;
            if (!grant.TryGetCounts(localActor, out int[] counts))
            {
                WLog.Line("cosmetic_grant_absent", secret: false, ("actor", localActor));
                return;
            }

            if (counts == null || counts.Length != CoinRarity.Count)
            {
                WLog.Line("cosmetic_grant_apply_failed", secret: false,
                    ("actor", localActor), ("counts", counts ?? Array.Empty<int>()),
                    ("err", "invalid_counts"));
                return;
            }

            _pendingCoinCounts = counts;
            int totalCoins = 0;
            for (int i = 0; i < counts.Length; i++) totalCoins += counts[i];
            WLog.Line("cosmetic_grant_pending", secret: false,
                ("actor", localActor), ("coins", totalCoins));
        }

        private void ResolveCosmeticPending(string trigger)
        {
            int[] counts = _pendingCoinCounts;
            if (counts == null) return;
            _pendingCoinCounts = null;

            int totalCoins = 0;
            for (int i = 0; i < counts.Length; i++) totalCoins += counts[i];
            if (totalCoins <= 0) return;

            bool departedToLobbyMenu = false;
            bool roundDirectorAlive = false;
            bool cooldownKnown = false;
            float cooldownSeconds = float.MaxValue;
            try
            {
                departedToLobbyMenu = SemiFunc.RunIsLobbyMenu();
                roundDirectorAlive = RoundDirector.instance != null;
                var rm = RunManager.instance;
                if (rm != null && GameRefs.RunManager_cosmeticWorldObjectCooldown != null)
                {
                    cooldownSeconds = GameRefs.RunManager_cosmeticWorldObjectCooldown(rm);
                    cooldownKnown = true;
                }
            }
            catch (Exception e)
            {
                WLog.Line("cosmetic_handoff_probe_error", secret: false, ("err", e.Message));
            }

            var route = CosmeticHandoff.Decide(departedToLobbyMenu, roundDirectorAlive,
                cooldownKnown, cooldownSeconds, out string fallbackReason);

            if (route == CosmeticHandoff.Route.Inject)
            {
                int injected = 0;
                try
                {
                    var rd = RoundDirector.instance;
                    for (byte r = 0; r < counts.Length; r++)
                    {
                        for (int c = 0; c < counts[r]; c++)
                        {
                            rd.CosmeticWorldObjectExtracted((SemiFunc.Rarity)r);
                            injected++;
                        }
                    }
                    WLog.Line("cosmetic_handoff", secret: false,
                        ("route", "inject"), ("trigger", trigger),
                        ("coins", injected), ("cooldown", cooldownSeconds));
                    return;
                }
                catch (Exception e)
                {
                    WLog.Line("cosmetic_handoff_inject_error", secret: false,
                        ("err", e.Message), ("injected", injected));
                    counts = CosmeticHandoff.SubtractLeading(counts, injected);
                    fallbackReason = "inject_error";
                }
            }

            var run = CosmeticCoinApplier.BeginRun(counts);
            if (run != null)
            {
                run.FlushRemaining();
                run.Finish();
            }
            WLog.Line("cosmetic_handoff", secret: false,
                ("route", "direct"), ("trigger", trigger), ("reason", fallbackReason));
        }

        private void ClearCosmeticGrantState(string trigger = "round_boundary")
        {
            ResolveCosmeticPending(trigger);
            _cosmeticGrantApplied = false;
        }

    }
}
