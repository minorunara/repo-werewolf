using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public void DebugRolesGauge(int pct)
        {
            if (!TryGetRolesDebug("gauge", out RolesSession? roles, out _)) return;
            roles.DebugAddGaugePct(pct);
            WLog.Line("cmd_gauge", secret: true,
                ("pct", pct), ("permille", roles.Gauge.DisplayPermille),
                ("unlocked", roles.Gauge.UnlockedFlags));
        }

        public void DebugRolesBeaconCharge(int count)
        {
            if (!TryGetRolesDebug("beacon", out RolesSession? roles, out _)) return;
            roles.DebugChargeBeacon(count);
            WLog.Line("cmd_beacon", secret: true, ("op", "charge"), ("count", count));
        }

        public void DebugRolesBeaconUse()
        {
            if (!TryGetRolesDebug("beacon", out RolesSession? roles, out _)) return;
            WLog.Line("cmd_beacon", secret: true, ("op", "use"), ("actor", LocalActor));
            roles.DebugUseBeacon(LocalActor, SessionClock.NowMs());
        }

        public void DebugRolesPerkUnlock(PerkId perk)
        {
            if (!TryGetRolesDebug("perk", out RolesSession? roles, out _)) return;
            roles.DebugUnlockPerk(perk);
            WLog.Line("cmd_perk", secret: true,
                ("perk", perk), ("unlocked", roles.Gauge.UnlockedFlags));
        }

        public void DebugRolesInformant()
        {
            if (!TryGetRolesDebug("informant", out RolesSession? roles, out _)) return;
            roles.DebugForceInformant();
            WLog.Line("cmd_informant", secret: true,
                ("established", roles.InformantEstablished));
        }

        public void DebugRolesCurse(int? actorNumber)
        {
            if (!TryGetRolesDebug("curse", out RolesSession? roles, out GameSession? session)) return;

            int target = 0;
            bool found = actorNumber.HasValue;
            if (actorNumber.HasValue)
            {
                target = actorNumber.Value;
            }
            else
            {
                foreach (var p in session.Players)
                {
                    if (p.Role == Role.BlackCat && p.Alive)
                    {
                        target = p.ActorNumber;
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                WLog.Line("cmd_rejected", secret: false,
                    ("name", "curse"), ("reason", "no_alive_blackcat"));
                return;
            }

            ExecuteVotedPlayer(target);
            WLog.Line("cmd_curse", secret: true,
                ("target", target),
                ("curseActive", roles.ActiveCurse != null && !roles.ActiveCurse.Resolved));
        }

        public void DebugBomberFillGauge()
        {
            if (!Session.IsHost)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("reason", "not_host"));
                return;
            }
            ProximityGauge? gauge = Runtime.BomberFeature.DebugProximityGauge;
            if (gauge == null)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("reason", "no_proximity"));
                return;
            }
            int localActor = LocalActor;
            int filled = 0;
            var players = _hostRound?.Session.Players;
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (!p.Alive || p.ActorNumber == localActor) continue;
                    gauge.DebugSetFull(p.ActorNumber);
                    filled++;
                }
            }
            WLog.Line("cmd_bomb_gauge", secret: false, ("filled", filled));
        }

        public void DebugBomberPlant(int targetActor)
        {
            if (!TryGetBomberDebug("plant", out BombSession? bomber)) return;
            HandleBombPlant(bomber.BomberActor, targetActor, SessionClock.NowMs());
        }

        public void DebugBomberDetonate()
        {
            if (!TryGetBomberDebug("detonate", out BombSession? bomber)) return;
            HandleBombDetonate(bomber.BomberActor, SessionClock.NowMs());
        }

        public void DebugBomberGrantAmmo(int n)
        {
            if (!TryGetBomberDebug("ammo", out BombSession? bomber)) return;
            bomber.DebugGrantAmmo(n);
            SendBomberStateIfDirty();
            WLog.Line("cmd_bomb_ammo", secret: true, ("granted", n), ("ammo", bomber.Ammo));
        }

        private bool TryGetBomberDebug(
            string sub,
            [NotNullWhen(true)] out BombSession? bomber)
        {
            bomber = _hostRound?.Play?.Bomber;
            if (!Session.IsHost)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("sub", sub), ("reason", "not_host"));
                return false;
            }
            if (bomber == null)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("sub", sub), ("reason", "no_bomb_session"));
                return false;
            }
            if (bomber.BomberActor < 0)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("sub", sub), ("reason", "no_bomber"));
                return false;
            }
            return true;
        }

        public void DebugScatterDiag()
        {
            Runtime.ScatterFeature.DebugScatter.LogDiagnostics();
        }

        public void DebugScatterWarp()
        {
            ExtractionScatter scatter = Runtime.ScatterFeature.DebugScatter;
            Func<int, bool> isAlive = _hostRound != null
                ? IsSessionAliveActor
                : (Func<int, bool>)Players.IsVanillaAlive;
            bool ok = scatter.WarpScatter(isAlive, warpTruckSlot: true, uniformDebug: true);
            WLog.Line("cmd_scatter", secret: false,
                ("result", ok ? "ok" : "failed"),
                ("assignments", scatter.LastAssignments.Count));
        }

        public void DebugToggleSelfEcho()
        {
            bool next = !Runtime.VoiceDriver.IsDebugSelfEchoOn;
            Runtime.VoiceDriver.SetDebugSelfEcho(next);
            WLog.Line("cmd_echo", secret: false, ("enabled", next ? 1 : 0));
        }

        private bool TryGetRolesDebug(
            string name,
            [NotNullWhen(true)] out RolesSession? roles,
            [NotNullWhen(true)] out GameSession? session)
        {
            HostRoundState? round = _hostRound;
            roles = round?.Play?.Roles;
            session = round?.Session;
            if (roles != null && session != null && Session.IsHost)
            {
                return true;
            }
            WLog.Line("cmd_rejected", secret: false,
                ("name", name), ("reason", "no_roles_session"));
            return false;
        }

        public VoteRejectReason HostCastVoteAsActor(int voterActor, int targetActor)
        {
            MeetingSession? meeting = _hostRound?.Play?.Meeting;
            if (meeting == null || !Session.IsHost)
            {
                WLog.Line("vote_proxy_rejected", secret: false,
                    ("voter", voterActor),
                    ("reason", !Session.IsHost
                        ? "not_host" : "no_meeting_session"));
                return VoteRejectReason.NotVotingStage;
            }

            VoteRejectReason reason = meeting.CastVote(voterActor, targetActor, SessionClock.NowMs());
            WLog.Line("vote_proxy", secret: true,
                ("voter", voterActor), ("target", targetActor), ("reason", reason));
            return reason;
        }

        public bool SimulatePlayerLeave(int actorNumber)
        {
            if (_bus is LoopbackNetBus loopback)
            {
                loopback.SimulatePlayerLeft(actorNumber);
                return true;
            }
            WLog.Line("leave_rejected", secret: false,
                ("actor", actorNumber), ("reason", "loopback_only"));
            return false;
        }

        public void DumpMeetingStatus()
        {
            long now = SessionClock.NowMs();

            MeetingSession? meeting = _hostRound?.Play?.Meeting;
            if (meeting != null)
            {
                WLog.Line("meetingstatus_host", secret: false,
                    ("stage", meeting.Stage), ("caller", meeting.CallerActor),
                    ("voted", meeting.VotedCount), ("endUnixMs", meeting.EndUnixMs),
                    ("lastEndUnixMs", meeting.LastMeetingEndUnixMs),
                    ("executed", meeting.Outcome?.ExecutedActor.ToString() ?? "none"));
            }
            else
            {
                WLog.Line("meetingstatus_host", secret: false, ("session", "none"));
            }

            WLog.Line("meetingstatus_client", secret: false,
                ("active", _meetingClient.MeetingActive), ("caller", _meetingClient.CallerActor),
                ("warpDone", _meetingClient.WarpDone(now)),
                ("remainingMs", _meetingClient.RemainingMs(now)),
                ("votedActors", new List<int>(_meetingClient.VotedActors)),
                ("executed", _meetingClient.Result?.ExecutedActor.ToString() ?? "none"));

            foreach (var kv in _meetingClient.Rows)
            {
                WLog.Line("meetingstatus_row", secret: false,
                    ("actor", kv.Key), ("status", kv.Value));
            }
        }

        public int DebugSpawnFakeBodies()
        {
            if (!LocalPlayer.TryGetPositionAndForward(out Vector3 origin, out Vector3 forward))
            {
                WLog.Line("cmd_body", secret: false, ("reason", "no_local_avatar"));
                return -1;
            }

            var actors = new List<int>();
            GameSession? session = _hostRound?.Session;
            if (session != null)
            {
                foreach (var p in session.Players)
                {
                    if (p.IsBot) actors.Add(p.ActorNumber);
                }
            }
            else
            {
                foreach (var p in _reservations.Bots) actors.Add(p.ActorNumber);
            }

            int created = 0;
            for (int i = 0; i < actors.Count; i++)
            {
                Vector3 groundPos = origin + forward * (2f + 1.5f * i);
                if (FakeBodies.SpawnOrMove(actors[i], groundPos)) created++;
            }
            WLog.Line("cmd_body", secret: false, ("bots", actors.Count), ("created", created));
            return created;
        }

        public bool DebugSpawnMoneyBag(int valueDollars)
        {
            bool spawned = SpawnMoneyBagInFront(valueDollars, out _, out string? reason);
            if (spawned) WLog.Line("cmd_spawnbag", secret: false, ("value", valueDollars));
            else WLog.Line("cmd_spawnbag", secret: false, ("reason", reason));
            return spawned;
        }

        internal bool SpawnMoneyBagInFront(int valueDollars,
            [NotNullWhen(true)] out GameObject? bag,
            [NotNullWhen(false)] out string? reason)
        {
            bag = null;
            reason = null;
            if (!Session.IsHost)
            {
                reason = "not_host";
                return false;
            }
            return MoneyBagSpawner.TrySpawnInFrontOfLocal(valueDollars, out bag, out reason);
        }

        internal bool HostPlaceScenarioCorpse(int actorNumber, Vector3 groundPos, FakeBodyLook? look)
        {
            if (_hostRound == null || !Session.IsHost) return false;
            HostRecordDeathByActor(actorNumber, asVote: false);
            if (!FakeBodies.SpawnOrMove(actorNumber, groundPos, look)) FakeBodies.SetLook(actorNumber, look);
            WLog.Line("scenario_corpse_placed", secret: false,
                ("actor", actorNumber), ("pos", TruckWarper.FormatVec(groundPos)));
            return true;
        }

        internal void HostGrantBombAmmo(int count)
        {
            BombSession? bomber = _hostRound?.Play?.Bomber;
            if (bomber == null || !Session.IsHost) return;
            bomber.DebugGrantAmmo(count);
            SendBomberStateIfDirty();
            WLog.Line("scenario_bomb_ammo_granted", secret: false,
                ("count", count), ("ammo", bomber.Ammo));
        }

        internal bool HostConveneAsActor(int actorNumber)
        {
            MeetingSession? meeting = _hostRound?.Play?.Meeting;
            if (meeting == null || !Session.IsHost) return false;
            bool accepted = meeting.TryConveneByHost(actorNumber, SessionClock.NowMs(), ConveneKind.Button,
                withCountdown: true);
            WLog.Line("scenario_convene_as_actor", secret: false,
                ("actor", actorNumber), ("accepted", accepted));
            return accepted;
        }

        internal void HostGrantMeetingRights(int actorNumber, int count)
        {
            MeetingSession? meeting = _hostRound?.Play?.Meeting;
            if (meeting == null || !Session.IsHost) return;
            meeting.GrantRights(actorNumber, count);
            WLog.Line("scenario_meeting_right_granted", secret: false,
                ("actor", actorNumber), ("count", count),
                ("remaining", meeting.RightsRemaining(actorNumber)));
        }

        public int DebugClearFakeBodies()
        {
            int removed = FakeBodies.Clear();
            WLog.Line("cmd_body_clear", secret: false, ("removed", removed));
            return removed;
        }

        public void HostForceExpireTimer()
        {
            GameSession? session = _hostRound?.Session;
            if (session == null || !Session.IsHost) return;
            session.ForceExpireTimer(SessionClock.NowMs());
        }

        public void HostNotifyDisclosure(DisclosureKind kind)
        {
            GameSession? session = _hostRound?.Session;
            if (session == null || !Session.IsHost) return;
            session.NotifyDisclosureCondition(kind);
        }

        public void DumpStatus()
        {
            GameSession? session = _hostRound?.Session;
            if (session == null)
            {
                WLog.Line("status", secret: false,
                    ("session", "none"), ("clientPhase", _clientPhase),
                    ("clientRoundEnd", _roundMirror.RoundEndUnixMs), ("pendingBots", _reservations.Bots.Count));
                return;
            }

            long now = SessionClock.NowMs();
            WLog.Line("status", secret: false,
                ("session", "active"), ("phase", session.Phase),
                ("remainingMs", session.RemainingMs(now)),
                ("players", session.Players.Count),
                ("winner", session.Winner == null ? "none" : session.Winner.WinningTeam.ToString()),
                ("pendingBots", _reservations.Bots.Count), ("mode", BusMode()));

            MeetingSession? meeting = _hostRound?.Play?.Meeting;
            if (meeting != null)
            {
                WLog.Line("status_meeting", secret: false,
                    ("stage", meeting.Stage), ("voted", meeting.VotedCount),
                    ("endUnixMs", meeting.EndUnixMs), ("caller", meeting.CallerActor),
                    ("lastEndUnixMs", meeting.LastMeetingEndUnixMs));
            }

            WLog.Line("status_village", secret: true, ("peaceful", session.Peaceful));
            foreach (var p in session.Players)
            {
                WLog.Line("status_player", secret: true,
                    ("actor", p.ActorNumber), ("name", p.Name), ("bot", p.IsBot),
                    ("role", p.Role), ("alive", p.Alive),
                    ("cause", p.DeathCause?.ToString() ?? "none"),
                    ("meetingRights", meeting?.RightsRemaining(p.ActorNumber) ?? 0));
            }
        }
    }
}
