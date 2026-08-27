using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;
using Werewolf.Vanilla;

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
                if (_modIntegrityFeature.TryHandleInbound(msg, SessionClock.NowMs())) return;

                if (MessageCodes.IsMasterInbound(msg.Code))
                {
                    HandleMasterInbound(msg);
                    return;
                }

                object[] p = msg.Payload;
                switch (msg.Code)
                {
                    case MessageCodes.AssignRole:
                        _roundMirror.LocalRole = (Role)(byte)p[0];
                        WLog.Line("recv_role", secret: true, ("role", _roundMirror.LocalRole));
                        _roleRevealFeature.TryStartRoleReveal();
                        break;
                    case MessageCodes.RevealSelfRole:
                        RecvRevealSelfRole(p);
                        break;
                    case MessageCodes.RevealTeammates:
                        RecvRevealTeammates(p);
                        break;
                    case MessageCodes.PlayerDied:
                        RecvPlayerDied(p);
                        break;
                    case MessageCodes.ResultDigest:
                        _resultFlowFeature.ApplyResultDigest(p);
                        break;
                    case MessageCodes.ReplayLossLedger:
                        ReplaySampler.ApplyHostLedger(p);
                        break;
                    case MessageCodes.GameOver:
                        RecvGameOver(p);
                        break;
                    case MessageCodes.GameStart:
                        RecvGameStart(p);
                        break;
                    case MessageCodes.PhaseChanged:
                        RecvPhaseChanged(p);
                        break;

                    case MessageCodes.StartMeeting:
                        _meetingClientFeature.HandleStartMeeting(p);
                        break;
                    case MessageCodes.MeetingCancelled:
                        _meetingClientFeature.HandleMeetingCancelled((byte)p[0]);
                        break;
                    case MessageCodes.ScatterGroups:
                        _scatterFeature.HandleScatterGroups(p);
                        break;
                    case MessageCodes.ScatterGuardWindow:
                        _scatterFeature.HandleScatterGuardWindow((int)p[0]);
                        break;
                    case MessageCodes.VoteProgress:
                        _meetingClientFeature.HandleVoteProgress(p);
                        break;
                    case MessageCodes.MeetingResult:
                        _meetingClientFeature.HandleMeetingResult(p);
                        break;
                    case MessageCodes.ConveneDenied:
                        _meetingClientFeature.HandleConveneDenied((byte)p[0]);
                        break;

                    case MessageCodes.BeaconAudit:
                        _meetingClientFeature.HandleBeaconAudit((byte)p[0]);
                        WLog.Line("recv_beacon_audit", secret: false, ("uses", (byte)p[0]));
                        break;
                    case MessageCodes.SyncPerkGauge:
                        _rolesClientFeature.HandleGaugeSync(p);
                        break;
                    case MessageCodes.RoleState:
                        _rolesClientFeature.HandleRoleState((byte)p[0], (int[])p[1], (long)p[2]);
                        break;
                    case MessageCodes.CurseCandidates:
                        _rolesClientFeature.HandleCurseCandidates((int[])p[0]);
                        break;

                    case MessageCodes.CosmeticGrant:
                        HandleCosmeticGrant(p);
                        break;

                    case MessageCodes.BomberState:
                        _bomberFeature.ApplyBomberState((int)p[0], (byte)p[1], (byte)p[2], (long)p[3], (long)p[4]);
                        break;
                    case MessageCodes.BombDetonation:
                        _bomberFeature.ApplyBombDetonation((int)p[0], (long)p[1]);
                        break;

                    case MessageCodes.CheckmateReveal:
                        _winCeremonyFeature.HandleCheckmateReveal(p);
                        break;

                    case MessageCodes.EradicationReveal:
                        _winCeremonyFeature.HandleEradicationReveal(p);
                        break;
                }
            }
            catch (Exception e)
            {
                WLog.Line("recv_error", secret: false, ("code", (int)msg.Code), ("err", e.Message));
            }
        }

        private void RecvRevealSelfRole(object[] p)
        {
            bool initialRevealDone = _roleRevealFeature.RevealStarted;
            _roundMirror.LocalRole = (Role)(byte)p[0];
            WLog.Line("recv_selfrole", secret: true, ("role", _roundMirror.LocalRole));
            _roleRevealFeature.TryStartRoleReveal();
            if (initialRevealDone) _roleRevealFeature.TryStartAwakeningReveal();
        }

        private void RecvRevealTeammates(object[] p)
        {
            _roundMirror.KnownWerewolves = (int[])p[0];
            {
                byte[] rr = p.Length > 1 ? p[1] as byte[] : null;
                _roundMirror.KnownTeammateRoles = (rr != null && rr.Length == _roundMirror.KnownWerewolves.Length) ? rr : null;
            }
            WLog.Line("recv_teammates", secret: true, ("count", _roundMirror.KnownWerewolves.Length));
            _roleRevealFeature.TryStartRoleReveal();
            if (_roundMirror.LocalRole == Role.BlackCat)
                MaybeShowTutorial(TutorialId.InformantUnlockedAsBlackCat);
        }

        private void RecvPlayerDied(object[] p)
        {
            int deadActor = (int)p[0];
            var deadCause = (DeathCause)(byte)p[1];
            _roundMirror.DeathMirror[deadActor] = deadCause;
            ReplaySampler.NoteDeath(deadActor, deadCause.ToString());
            _meetingClient.ApplyPlayerDied(deadActor, deadCause);
            if (deadActor == LocalActor)
            {
                RolesClient.ForceWolfModeOff();
                RolesClient.ForceValuableRecordOff();
                _rolesClientFeature.ResetForLocalDeath();
                MaybeShowTutorial(TutorialId.FirstDeath);
            }
            WLog.Line("recv_died", secret: false,
                ("actor", deadActor), ("cause", deadCause));
        }

        private void RecvGameOver(object[] p)
        {
            _clientPhase = GamePhase.GameOver;
            ReplaySampler.NoteGameOver((byte)p[0], (int[])p[1], (byte[])p[2], _resultFlowFeature.MatchWinReason());
            {
                var announcedAtGameOver = new List<int>();
                _meetingClient.ApplyPhase(GamePhase.GameOver, announcedAtGameOver);
                ReplaySampler.NoteDeathsAnnounced(announcedAtGameOver);
            }
            _meeting?.AbortForGameOver();
            _rolesClientFeature.ResetRoles("gameover");
            WLog.Line("recv_gameover", secret: false, ("team", (Team)(byte)p[0]));
            _resultFlowFeature.BeginResultCountdown();
            ShowResultScreen((byte)p[0], (int[])p[1], (byte[])p[2]);
        }

        private void RecvGameStart(object[] p)
        {
            ClearCosmeticGrantState("game_start");
            _resultFlowFeature.ClearClientDigest();
            CrownRoster.Clear();
            _chatFeature.ResetForNewMatch();
            Patches.WipeGuardPatch.ResetLogThrottle();
            _voiceDriver?.SanitizeAtRoundStart();
            _roundMirror.RoundEndUnixMs = (long)p[0];
            _roundMirror.WerewolfCount = (byte)p[2];
            _roundMirror.GameStartUnixMs = SessionClock.NowMs();
            _resultFlowFeature.FreezeResultAutoReturnConfig();
            if (!Session.IsHost) _lobbySettingsFeature.NoteMatchBlob(LobbyBlobMirror);
            _roundMirror.RevealDelaySec = (int)p[4];
            _cfgMirror.CatPossible = (byte)p[3] != 0;
            _cfgMirror.DebugSession = (byte)p[5] != 0;
            IdRoster.Apply((int[])p[6]);
            SessionClock.RequestResync();
            WLog.Line("recv_gamestart", secret: false,
                ("roundEnd", _roundMirror.RoundEndUnixMs),
                ("clock", SessionClock.NowMs()),
                ("serverClock", SessionClock.IsServerBacked ? 1 : 0),
                ("werewolfCount", _roundMirror.WerewolfCount),
                ("catPossible", _cfgMirror.CatPossible),
                ("revealDelaySec", _roundMirror.RevealDelaySec),
                ("debugSession", _cfgMirror.DebugSession ? 1 : 0));
            _roleRevealFeature.TryStartRoleReveal();
        }

        private void RecvPhaseChanged(object[] p)
        {
            GamePhase newPhase = (GamePhase)(byte)p[0];
            if (_clientPhase == GamePhase.Meeting && newPhase == GamePhase.Play)
            {
                _roundMirror.LastMeetingEndUnixMs = SessionClock.NowMs();
            }
            _clientPhase = newPhase;
            ReplaySampler.NotePhase(newPhase.ToString());
            _roundMirror.RoundEndUnixMs = (long)p[2];
            var announcedAtPhase = new List<int>();
            _meetingClient.ApplyPhase(_clientPhase, announcedAtPhase);
            ReplaySampler.NoteDeathsAnnounced(announcedAtPhase);
            if (_clientPhase == GamePhase.GameOver || _clientPhase == GamePhase.Lobby)
            {
                _rolesClientFeature.ResetRoles("phase");
                Debugging.StructuredLog.FlushDeferredSecrets("recv_phase");
            }
            WLog.Line("recv_phase", secret: false,
                ("phase", _clientPhase), ("roundEnd", _roundMirror.RoundEndUnixMs),
                ("clock", SessionClock.NowMs()));
        }

        private void HandleMasterInbound(InboundMessage msg)
        {
            if (!Session.IsHost)
            {
                DropMasterInbound(msg, "not_host");
                return;
            }

            long now = SessionClock.NowMs();
            switch (msg.Code)
            {
                case MessageCodes.RequestMeeting:
                    {
                        if (_meeting == null) { DropMasterInbound(msg, "no_meeting_session"); return; }
                        if (_winCeremonyFeature.IsCheckmateCeremonyStarted)
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
                case MessageCodes.CastVote:
                    if (_meeting == null) { DropMasterInbound(msg, "no_meeting_session"); return; }
                    _meeting.CastVote(msg.SenderActor, (int)msg.Payload[0], now);
                    break;
                case MessageCodes.RoleAction:
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
                departedToLobbyMenu = Session.InLobbyMenu;
                roundDirectorAlive = Extraction.RoundDirectorAlive;
                cooldownKnown = Session.TryReadCosmeticWorldObjectCooldown(out cooldownSeconds);
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
                    for (byte r = 0; r < counts.Length; r++)
                    {
                        for (int c = 0; c < counts[r]; c++)
                        {
                            Extraction.AddCosmeticExtracted(r);
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
