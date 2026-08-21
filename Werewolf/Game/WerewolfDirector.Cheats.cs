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

        public void DebugRolesGauge(int pct)
        {
            if (!GuardRolesDebug("gauge")) return;
            _roles.DebugAddGaugePct(pct);
            WLog.Line("cmd_gauge", secret: true,
                ("pct", pct), ("permille", _roles.Gauge.DisplayPermille),
                ("unlocked", _roles.Gauge.UnlockedFlags));
        }

        public void DebugRolesBeaconCharge(int count)
        {
            if (!GuardRolesDebug("beacon")) return;
            _roles.DebugChargeBeacon(count);
            WLog.Line("cmd_beacon", secret: true, ("op", "charge"), ("count", count));
        }

        public void DebugRolesBeaconUse()
        {
            if (!GuardRolesDebug("beacon")) return;
            WLog.Line("cmd_beacon", secret: true, ("op", "use"), ("actor", LocalActor));
            _roles.DebugUseBeacon(LocalActor, NowUnixMs());
        }

        public void DebugRolesPerkUnlock(PerkId perk)
        {
            if (!GuardRolesDebug("perk")) return;
            _roles.DebugUnlockPerk(perk);
            WLog.Line("cmd_perk", secret: true,
                ("perk", perk), ("unlocked", _roles.Gauge.UnlockedFlags));
        }

        public void DebugRolesInformant()
        {
            if (!GuardRolesDebug("informant")) return;
            _roles.DebugForceInformant();
            WLog.Line("cmd_informant", secret: true,
                ("established", _roles.InformantEstablished));
        }

        public void DebugRolesCurse(int? actorNumber)
        {
            if (!GuardRolesDebug("curse")) return;

            int target = 0;
            bool found = actorNumber.HasValue;
            if (found)
            {
                target = actorNumber.Value;
            }
            else
            {
                foreach (var p in _session.Players)
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
                ("curseActive", _roles.ActiveCurse != null && !_roles.ActiveCurse.Resolved));
        }

        public void DebugBomberFillGauge()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("reason", "not_host"));
                return;
            }
            if (_bomberProximity == null)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("reason", "no_proximity"));
                return;
            }
            int localActor = LocalActor;
            int filled = 0;
            var players = _session != null ? _session.Players : null;
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (!p.Alive || p.ActorNumber == localActor) continue;
                    _bomberProximity.DebugSetFull(p.ActorNumber);
                    filled++;
                }
            }
            WLog.Line("cmd_bomb_gauge", secret: false, ("filled", filled));
        }

        public void DebugBomberPlant(int targetActor)
        {
            if (!GuardBomberDebug("plant")) return;
            HandleBombPlant(_bomber.BomberActor, targetActor, NowUnixMs());
        }

        public void DebugBomberDetonate()
        {
            if (!GuardBomberDebug("detonate")) return;
            HandleBombDetonate(_bomber.BomberActor, NowUnixMs());
        }

        public void DebugBomberGrantAmmo(int n)
        {
            if (!GuardBomberDebug("ammo")) return;
            _bomber.DebugGrantAmmo(n);
            SendBomberStateIfDirty();
            WLog.Line("cmd_bomb_ammo", secret: true, ("granted", n), ("ammo", _bomber.Ammo));
        }

        private bool GuardBomberDebug(string sub)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("sub", sub), ("reason", "not_host"));
                return false;
            }
            if (_bomber == null)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("sub", sub), ("reason", "no_bomb_session"));
                return false;
            }
            if (_bomber.BomberActor < 0)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", "bomb"), ("sub", sub), ("reason", "no_bomber"));
                return false;
            }
            return true;
        }

        public void DebugScatterDiag()
        {
            (_extractionScatter ??= new ExtractionScatter()).LogDiagnostics();
        }

        public void DebugScatterWarp()
        {
            ExtractionScatter scatter = _extractionScatter ??= new ExtractionScatter();
            Func<PlayerAvatar, bool> isAlive = _session != null
                ? IsSessionAlive
                : (Func<PlayerAvatar, bool>)VanillaAlive;
            bool ok = scatter.WarpScatter(isAlive, warpTruckSlot: true, uniformDebug: true);
            WLog.Line("cmd_scatter", secret: false,
                ("result", ok ? "ok" : "failed"),
                ("assignments", scatter.LastAssignments.Count));
        }

        private static bool VanillaAlive(PlayerAvatar avatar) =>
            avatar != null && !GameRefs.PlayerAvatar_isDisabled(avatar);

        public void DebugToggleSelfEcho()
        {
            if (_voiceDriver == null)
            {
                WLog.Line("cmd_rejected", secret: false,
                    ("name", "echo"), ("reason", "no_voice_driver"));
                return;
            }
            bool next = !_voiceDriver.IsDebugSelfEchoOn;
            _voiceDriver.SetDebugSelfEcho(next);
            WLog.Line("cmd_echo", secret: false, ("enabled", next ? 1 : 0));
        }

        private bool GuardRolesDebug(string name)
        {
            if (_roles != null && _session != null && SemiFunc.IsMasterClientOrSingleplayer())
            {
                return true;
            }
            WLog.Line("cmd_rejected", secret: false,
                ("name", name), ("reason", "no_roles_session"));
            return false;
        }

        public VoteRejectReason HostCastVoteAsActor(int voterActor, int targetActor)
        {
            if (_meeting == null || !SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("vote_proxy_rejected", secret: false,
                    ("voter", voterActor),
                    ("reason", !SemiFunc.IsMasterClientOrSingleplayer()
                        ? "not_host" : "no_meeting_session"));
                return VoteRejectReason.NotVotingStage;
            }

            VoteRejectReason reason = _meeting.CastVote(voterActor, targetActor, NowUnixMs());
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
            long now = NowUnixMs();

            var meeting = _meeting;
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
            PlayerAvatar local = PlayerAvatar.instance;
            if (local == null || local.transform == null)
            {
                WLog.Line("cmd_body", secret: false, ("reason", "no_local_avatar"));
                return -1;
            }

            var actors = new List<int>();
            if (_session != null)
            {
                foreach (var p in _session.Players)
                {
                    if (p.IsBot) actors.Add(p.ActorNumber);
                }
            }
            else
            {
                foreach (var p in _pendingBots) actors.Add(p.ActorNumber);
            }

            Vector3 origin = local.transform.position;
            Vector3 forward = local.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

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
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                WLog.Line("cmd_spawnbag", secret: false, ("reason", "not_host"));
                return false;
            }
            PlayerAvatar local = PlayerAvatar.instance;
            GameObject prefab = AssetManager.instance != null ? AssetManager.instance.surplusValuableSmall : null;
            if (local == null || local.transform == null || prefab == null)
            {
                WLog.Line("cmd_spawnbag", secret: false, ("reason", "no_avatar_or_prefab"));
                return false;
            }

            Vector3 forward = local.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 pos = local.transform.position + forward * 1.5f + Vector3.up * 1f;

            GameObject bag = SemiFunc.IsMultiplayer()
                ? Photon.Pun.PhotonNetwork.InstantiateRoomObject("Valuables/" + prefab.name, pos, Quaternion.identity, 0)
                : UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
            if (valueDollars > 0)
            {
                ValuableObject valuable = bag.GetComponent<ValuableObject>();
                if (valuable != null) GameRefs.ValuableObject_dollarValueOverride(valuable) = valueDollars;
            }
            WLog.Line("cmd_spawnbag", secret: false, ("value", valueDollars));
            return true;
        }

        public int DebugClearFakeBodies()
        {
            int removed = FakeBodies.Clear();
            WLog.Line("cmd_body_clear", secret: false, ("removed", removed));
            return removed;
        }

        public void HostForceExpireTimer()
        {
            if (_session == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            _session.ForceExpireTimer(NowUnixMs());
        }

        public void HostNotifyDisclosure(DisclosureKind kind)
        {
            if (_session == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            _session.NotifyDisclosureCondition(kind);
        }

        public void DumpStatus()
        {
            var session = _session;
            if (session == null)
            {
                WLog.Line("status", secret: false,
                    ("session", "none"), ("clientPhase", _clientPhase),
                    ("clientRoundEnd", _clientRoundEndUnixMs), ("pendingBots", _pendingBots.Count));
                return;
            }

            long now = NowUnixMs();
            WLog.Line("status", secret: false,
                ("session", "active"), ("phase", session.Phase),
                ("remainingMs", session.RemainingMs(now)),
                ("players", session.Players.Count),
                ("winner", session.Winner == null ? "none" : session.Winner.WinningTeam.ToString()),
                ("pendingBots", _pendingBots.Count), ("mode", BusMode()));

            var meeting = _meeting;
            if (meeting != null)
            {
                WLog.Line("status_meeting", secret: false,
                    ("stage", meeting.Stage), ("voted", meeting.VotedCount),
                    ("endUnixMs", meeting.EndUnixMs), ("caller", meeting.CallerActor),
                    ("lastEndUnixMs", meeting.LastMeetingEndUnixMs));
            }

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
