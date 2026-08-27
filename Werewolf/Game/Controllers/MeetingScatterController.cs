using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class MeetingScatterController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private long _scatterPlanAtUnixMs;

        private bool _scatterAwaitCurse;

        private const long ScatterPlanNoExecDelayMs = 1500;

        private const long ScatterPlanAfterKillMarginMs = 1000;

        private ExtractionScatter _extractionScatter;

        private List<List<int>> _lastScatterGroups;

        private readonly ScatterGuard _scatterGuard = new ScatterGuard();

        private readonly ScatterGuard _clientScatterGuard = new ScatterGuard();

        internal MeetingScatterController(WerewolfDirector director)
        {
            _d = director;
        }

        internal bool IsClientGuardArmed(long now) => _clientScatterGuard.IsArmed(now);

        internal ExtractionScatter DebugScatter => _extractionScatter ??= new ExtractionScatter();

        internal List<List<int>> ConsumeLastScatterGroups()
        {
            List<List<int>> groups = _lastScatterGroups;
            _lastScatterGroups = null;
            return groups;
        }

        internal void OnMeetingStarted()
        {
            _scatterPlanAtUnixMs = 0;
            _scatterAwaitCurse = false;
            _extractionScatter?.ClearPlan();
            _scatterGuard.Disarm();
            _clientScatterGuard.Disarm();
        }

        internal void CancelPlan()
        {
            _scatterPlanAtUnixMs = 0;
            _scatterAwaitCurse = false;
        }

        internal void TryExecuteMeetingScatter()
        {
            GameSession session = _d.HostSession;
            if (session == null) return;
            Patches.PlayerSpawnPatch.ArmWarpGrace();
            if (!Session.IsHost) return;
            if (session.Phase != GamePhase.Play)
            {
                _extractionScatter?.ClearPlan();
                return;
            }
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;
            ExtractionScatter scatter = _extractionScatter ??= new ExtractionScatter();
            if (scatter.HasPlan)
            {
                if (scatter.ExecutePlan(_d.IsSessionAliveActor))
                {
                    ArmScatterGuard(scatter);
                }
            }
            else if (scatter.WarpScatter(_d.IsSessionAliveActor, warpTruckSlot: false,
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
            _scatterGuard.Arm(SessionClock.NowMs(), guardSec);
            if (_scatterGuard.ArmedUntilUnixMs != 0)
            {
                SendScatterGuardWindow(guardSec);
                WLog.Line("scatter_guard_armed", secret: false,
                    ("groups", groups), ("untilUnixMs", _scatterGuard.ArmedUntilUnixMs));
            }
        }

        private void SendScatterGuardWindow(int guardSec)
        {
            _d.ClientBus?.SendToAll(MessageCodes.ScatterGuardWindow, new object[] { guardSec });
        }

        internal void HandleScatterGuardWindow(int guardSec)
        {
            if (guardSec > 0) _clientScatterGuard.Arm(SessionClock.NowMs(), guardSec);
            else _clientScatterGuard.Disarm();
            ReplaySampler.NoteGuardWindow(guardSec);
            WLog.Line("recv_scatter_guard_window", secret: false, ("sec", guardSec));
        }

        internal void TryFireScatterGuard(int victimActor)
        {
            long now = SessionClock.NowMs();
            if (!_scatterGuard.IsArmed(now)) return;
            MeetingSession meeting = _d.HostMeetingSession;
            GameSession session = _d.HostSession;
            if (meeting == null || session == null || session.Phase != GamePhase.Play) return;
            if (session.WinLocked) return;
            if (LastRunGate.IsLastRunActive())
            {
                _scatterGuard.Disarm();
                SendScatterGuardWindow(0);
                WLog.Line("scatter_guard_skip", secret: false, ("reason", "last_run"));
                return;
            }
            if (meeting.TryConveneScatterGuard(victimActor, now))
            {
                _scatterGuard.Disarm();
                WLog.Line("scatter_guard_fired", secret: false, ("victim", victimActor));
            }
        }

        public void Tick(long now)
        {
            if (_scatterPlanAtUnixMs == 0 || now < _scatterPlanAtUnixMs) return;
            _scatterPlanAtUnixMs = 0;
            GameSession session = _d.HostSession;
            if (!Session.IsHost || session == null) return;
            if (session.WinLocked) return;
            if (session.Phase != GamePhase.Meeting)
            {
                WLog.Line("scatter_plan_skip", secret: false,
                    ("reason", "phase"), ("phase", session.Phase));
                return;
            }
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;

            ExtractionScatter scatter = _extractionScatter ??= new ExtractionScatter();
            if (!scatter.PlanScatter(_d.IsSessionAliveActor, warpTruckSlot: false,
                    botActors: CollectAliveBotActors()))
            {
                return;
            }

            object[] wire = scatter.BuildGroupsWire();
            if (wire == null) return;

            _d.HostMeetingSession?.EnsureClosingHoldRemaining(now, VotePanel.ScatterRevealHoldRequiredMs);

            if (_d.ClientBus != null)
            {
                _d.ClientBus.SendToAll(MessageCodes.ScatterGroups, wire);
                WLog.Line("scatter_groups_sent", secret: false, ("players", ((int[])wire[0]).Length));
            }
        }

        internal void HostScheduleScatterPlanFromResult(int executedActor)
        {
            if (!Session.IsHost || _d.HostSession == null) return;
            if (Plugin.GameConfig == null || !Plugin.GameConfig.MeetingScatterEnabled) return;
            if (_scatterAwaitCurse) return;
            MeetingSession meeting = _d.HostMeetingSession;
            long ceremonyMs = meeting != null ? meeting.ResultCeremonyDelayMs : 0;
            long delayMs = ceremonyMs + (executedActor == -1
                ? ScatterPlanNoExecDelayMs
                : MeetingSession.PostResultKillDelaySec * 1000L + ScatterPlanAfterKillMarginMs);
            _scatterPlanAtUnixMs = SessionClock.NowMs() + delayMs;
        }

        internal void HostMarkScatterAwaitCurse()
        {
            _scatterAwaitCurse = true;
            _scatterPlanAtUnixMs = 0;
        }

        internal void HostScheduleScatterPlanAfterCurse()
        {
            if (!_scatterAwaitCurse) return;
            _scatterAwaitCurse = false;
            _scatterPlanAtUnixMs = SessionClock.NowMs()
                + RolesSession.CurseKillDelaySec * 1000L + ScatterPlanAfterKillMarginMs;
        }

        private List<int> CollectAliveBotActors()
        {
            List<int> aliveBots = null;
            foreach (WPlayer p in _d.HostSessionPlayers)
            {
                if (p != null && p.IsBot && p.Alive) (aliveBots ??= new List<int>()).Add(p.ActorNumber);
            }
            return aliveBots;
        }

        private void SendScatterGroups(ExtractionScatter scatter)
        {
            if (_d.ClientBus == null) return;
            object[] wire = scatter.BuildGroupsWire();
            if (wire == null) return;
            _d.ClientBus.SendToAll(MessageCodes.ScatterGroups, wire);
            WLog.Line("scatter_groups_sent", secret: false, ("players", ((int[])wire[0]).Length));
        }

        internal void HandleScatterGroups(object[] p)
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
            VotePanel votePanel = _d.VotePanelView;
            if (_d.MeetingUiActive && _d.MeetingClientView.MeetingActive && votePanel.Exists)
            {
                animated = votePanel.StartScatterReveal(groups, SessionClock.NowMs(),
                    volume => _d.PlaySfxLoop("sfx_scatter_shuffle", volume),
                    () => _d.StopSfxLoop("sfx_scatter_shuffle"),
                    () => _d.PlaySfx("sfx_scatter_jingle"));
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
                _d.PushRawToast(lines[i], logKind: "scatter", playSfx: i == 0);
            }
        }

        private string ScatterMemberLabel(int actor)
        {
            int id = _d.IdRoster.IdOf(actor);
            return id > 0 ? Texts.Format(TextId.NoticeScatterMemberFormat, id) : _d.ResolveDisplayName(actor);
        }

        public void Reset()
        {
            _scatterPlanAtUnixMs = 0;
            _scatterAwaitCurse = false;
            _extractionScatter?.ClearPlan();
            _scatterGuard.Disarm();
            _clientScatterGuard.Disarm();
            _lastScatterGroups = null;
        }

        public void DestroyUi()
        {
        }
    }
}
