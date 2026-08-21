using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private long _scatterPlanAtUnixMs;

        private bool _scatterAwaitCurse;

        private const long ScatterPlanNoExecDelayMs = 1500;

        private const long ScatterPlanAfterKillMarginMs = 1000;

        private readonly ScatterGuard _scatterGuard = new ScatterGuard();

        private readonly ScatterGuard _clientScatterGuard = new ScatterGuard();

        private void TryExecuteMeetingScatter()
        {
            if (_session == null) return;
            Patches.PlayerSpawnPatch.ArmScatterGrace();
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_session.Phase != GamePhase.Play)
            {
                _extractionScatter?.ClearPlan();
                return;
            }
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;
            ExtractionScatter scatter = _extractionScatter ??= new ExtractionScatter();
            if (scatter.HasPlan)
            {
                if (scatter.ExecutePlan(IsSessionAlive))
                {
                    ArmScatterGuard(scatter);
                }
            }
            else if (scatter.WarpScatter(IsSessionAlive, warpTruckSlot: false,
                         botActors: CollectAliveBotActors()))
            {
                SendScatterGroups(scatter);
                ArmScatterGuard(scatter);
            }
        }

        private void ArmScatterGuard(ExtractionScatter scatter)
        {
            int groups = ScatterGroupsWire.CountGroups(scatter.LastAssignments);
            if (groups < 2)
            {
                _scatterGuard.Disarm();
                return;
            }
            int guardSec = Plugin.GameConfig != null ? Plugin.GameConfig.ScatterGuardSec : 0;
            _scatterGuard.Arm(NowUnixMs(), guardSec);
            if (_scatterGuard.ArmedUntilUnixMs != 0)
            {
                SendScatterGuardWindow(guardSec);
                WLog.Line("scatter_guard_armed", secret: false,
                    ("groups", groups), ("untilUnixMs", _scatterGuard.ArmedUntilUnixMs));
            }
        }

        private void SendScatterGuardWindow(int guardSec)
        {
            if (_bus == null) return;
            _bus.SendToAll(MessageCodes.ScatterGuardWindow, new object[] { guardSec });
        }

        private void HandleScatterGuardWindow(int guardSec)
        {
            if (guardSec > 0) _clientScatterGuard.Arm(NowUnixMs(), guardSec);
            else _clientScatterGuard.Disarm();
            ReplaySampler.NoteGuardWindow(guardSec);
            WLog.Line("recv_scatter_guard_window", secret: false, ("sec", guardSec));
        }

        private void TryFireScatterGuard(int victimActor)
        {
            long now = NowUnixMs();
            if (!_scatterGuard.IsArmed(now)) return;
            if (_meeting == null || _session == null || _session.Phase != GamePhase.Play) return;
            if (_session.WinLocked) return;
            if (LastRunGate.IsLastRunActive())
            {
                _scatterGuard.Disarm();
                SendScatterGuardWindow(0);
                WLog.Line("scatter_guard_skip", secret: false, ("reason", "last_run"));
                return;
            }
            if (_meeting.TryConveneScatterGuard(victimActor, now))
            {
                _scatterGuard.Disarm();
                WLog.Line("scatter_guard_fired", secret: false, ("victim", victimActor));
            }
        }

        private void TickScatterPlanHost(long now)
        {
            if (_scatterPlanAtUnixMs == 0 || now < _scatterPlanAtUnixMs) return;
            _scatterPlanAtUnixMs = 0;
            if (!SemiFunc.IsMasterClientOrSingleplayer() || _session == null) return;
            if (_session.WinLocked) return;
            if (_session.Phase != GamePhase.Meeting)
            {
                WLog.Line("scatter_plan_skip", secret: false,
                    ("reason", "phase"), ("phase", _session.Phase));
                return;
            }
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;

            ExtractionScatter scatter = _extractionScatter ??= new ExtractionScatter();
            if (!scatter.PlanScatter(IsSessionAlive, warpTruckSlot: false,
                    botActors: CollectAliveBotActors()))
            {
                return;
            }

            object[] wire = scatter.BuildGroupsWire();
            if (wire == null) return;

            _meeting?.EnsureClosingHoldRemaining(now, VotePanel.ScatterRevealHoldRequiredMs);

            if (_bus != null)
            {
                _bus.SendToAll(MessageCodes.ScatterGroups, wire);
                WLog.Line("scatter_groups_sent", secret: false, ("players", ((int[])wire[0]).Length));
            }
        }

        private void HostScheduleScatterPlanFromResult(int executedActor)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer() || _session == null) return;
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;
            if (_scatterAwaitCurse) return;
            long ceremonyMs = _meeting != null ? _meeting.ResultCeremonyDelayMs : 0;
            long delayMs = ceremonyMs + (executedActor == -1
                ? ScatterPlanNoExecDelayMs
                : MeetingSession.PostResultKillDelaySec * 1000L + ScatterPlanAfterKillMarginMs);
            _scatterPlanAtUnixMs = NowUnixMs() + delayMs;
        }

        private void HostMarkScatterAwaitCurse()
        {
            _scatterAwaitCurse = true;
            _scatterPlanAtUnixMs = 0;
        }

        private void HostScheduleScatterPlanAfterCurse()
        {
            if (!_scatterAwaitCurse) return;
            _scatterAwaitCurse = false;
            _scatterPlanAtUnixMs = NowUnixMs()
                + RolesSession.CurseKillDelaySec * 1000L + ScatterPlanAfterKillMarginMs;
        }

        private List<int> CollectAliveBotActors()
        {
            List<int> aliveBots = null;
            foreach (WPlayer p in _session.Players)
            {
                if (p != null && p.IsBot && p.Alive) (aliveBots ??= new List<int>()).Add(p.ActorNumber);
            }
            return aliveBots;
        }

        private void SendScatterGroups(ExtractionScatter scatter)
        {
            if (_bus == null) return;
            object[] wire = scatter.BuildGroupsWire();
            if (wire == null) return;
            _bus.SendToAll(MessageCodes.ScatterGroups, wire);
            WLog.Line("scatter_groups_sent", secret: false, ("players", ((int[])wire[0]).Length));
        }

        private void HandleScatterGroups(object[] p)
        {
            List<List<int>> groups = ScatterGroupsWire.FromWire(p);
            if (groups == null || groups.Count < 2)
            {
                WLog.Line("recv_scatter_groups_invalid", secret: false);
                return;
            }

            _lastScatterGroups = groups;
            ReplaySampler.NoteScatterGroups(groups);

            bool animated = false;
            if (_meetingUiActive && _meetingClient.MeetingActive && _votePanel.Exists)
            {
                EnsureSfxBuilt();
                animated = _votePanel.StartScatterReveal(groups, NowUnixMs(),
                    volume => _sfxPlayer.PlayLoop("sfx_scatter_shuffle", volume),
                    () => _sfxPlayer.StopLoop("sfx_scatter_shuffle"),
                    () => _sfxPlayer.Play("sfx_scatter_jingle"));
            }

            if (!animated)
            {
                PushScatterToasts(ScatterGroupsText.FormatLines(groups, ScatterMemberLabel));
            }
            WLog.Line("recv_scatter_groups", secret: false,
                ("groups", groups.Count), ("animated", animated));
        }

        private void PushScatterToasts(List<string> lines)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                PushRawToast(lines[i], logKind: "scatter", playSfx: i == 0);
            }
        }

        private string ScatterMemberLabel(int actor)
        {
            int id = IdRoster.IdOf(actor);
            return id > 0 ? Texts.Format(TextId.NoticeScatterMemberFormat, id) : ResolveDisplayName(actor);
        }

        private string ScatterMemberChatLabel(int actor)
            => ParticipantLabel.Format(IdRoster.IdOf(actor), ResolveDisplayName(actor));

    }
}
