using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Net;

namespace Werewolf.Debugging
{
    public sealed class SelfTestResult
    {
        public SelfTestResult(string name, bool pass, string detail)
        {
            Name = name;
            Pass = pass;
            Detail = detail;
        }

        public string Name { get; }
        public bool Pass { get; }

        public string Detail { get; }
    }

    public static class SelfTest
    {
        private const long Now = 1_000_000;
        private const int RoundSeconds = 600;
        private const long RoundEnd = Now + RoundSeconds * 1000L;
        private const int DelaySec = 60;

        public static IReadOnlyList<SelfTestResult> RunAll()
        {
            var results = new List<SelfTestResult>
            {
                Run("role_distribution_3", f => ScenarioDistribution(f, 3, teamTotal: 1, wolves: 1, cats: 1)),
                Run("role_distribution_5", f => ScenarioDistribution(f, 5, teamTotal: 2, wolves: 2, cats: 1)),
                Run("role_distribution_7", f => ScenarioDistribution(f, 7, teamTotal: 3, wolves: 3, cats: 1)),
                Run("role_distribution_10", f => ScenarioDistribution(f, 10, teamTotal: 4, wolves: 4, cats: 1)),
                Run("forced_role", ScenarioForcedRole),
                Run("win_priority_simultaneous", ScenarioWinPrioritySimultaneous),
                Run("blackcat_excluded_from_wolf_count", ScenarioBlackCatExcludedFromWolfCount),
                Run("blackcat_death_continues", ScenarioBlackCatDeathContinues),
                Run("blackcat_shares_wolf_win", ScenarioBlackCatSharesWolfWin),
                Run("disclosure_order_and_dedup", ScenarioDisclosureOrderAndDedup),
                Run("meeting_pause_and_resume_extend", ScenarioMeetingPauseAndResumeExtend),
                Run("timer_expiry_wolf_win", ScenarioTimerExpiryWolfWin),
                Run("meeting_full_flow", ScenarioMeetingFullFlow),
                Run("meeting_vote_secrecy", ScenarioMeetingVoteSecrecy),
                Run("meeting_leave_no_execution", ScenarioMeetingLeaveNoExecution),
                Run("meeting_restore_from_room_state", ScenarioMeetingRestoreFromRoomState),
                Run("meeting_vote_reject_reenable", ScenarioMeetingVoteRejectReenable),
                Run("roles_gauge_freeze_and_floor", ScenarioRolesGaugeFreezeAndFloor),
                Run("roles_perk_unlock_order_once", ScenarioRolesPerkUnlockOrderOnce),
                Run("roles_beacon_gate", ScenarioRolesBeaconGate),
                Run("roles_beacon_meeting_end_sync", ScenarioRolesBeaconMeetingEndSync),
                Run("roles_informant_conditions", ScenarioRolesInformantConditions),
                Run("roles_curse_resolution", ScenarioRolesCurseResolution),
                Run("roles_curse_win_judge", ScenarioRolesCurseWinJudge),
                Run("roles_action_validation", ScenarioRolesActionValidation),
                Run("roles_full_curse_flow", ScenarioRolesFullCurseFlow),
                Run("roles_gauge_distribution_targets", ScenarioRolesGaugeDistributionTargets),
                Run("roles_meeting_gauge_snapshot", ScenarioRolesMeetingGaugeSnapshot),
                Run("bomber_full_flow", ScenarioBomberFullFlow),
            };

            int pass = 0;
            foreach (var r in results)
            {
                if (r.Pass) pass++;
            }
            WLog.Line("selftest_summary", secret: false,
                ("total", results.Count), ("pass", pass), ("fail", results.Count - pass));

            return results;
        }

        private static SelfTestResult Run(string name, Action<List<string>> scenario)
        {
            var fails = new List<string>();
            try
            {
                scenario(fails);
            }
            catch (Exception e)
            {
                fails.Add("exception: " + e.Message);
            }

            string detail = fails.Count == 0 ? "" : string.Join("; ", fails);
            var result = new SelfTestResult(name, fails.Count == 0, detail);
            WLog.Line("selftest", secret: false,
                ("name", name), ("result", result.Pass ? "PASS" : "FAIL"), ("detail", detail));
            return result;
        }

        private static void Check(List<string> fails, bool condition, string what)
        {
            if (!condition) fails.Add(what);
        }

        private sealed class Harness
        {
            public readonly GameSession Session = new GameSession();
            public readonly LoopbackNetBus Bus = new LoopbackNetBus(1);
            public readonly List<OutboundMessage> Sent = new List<OutboundMessage>();
            public readonly List<InboundMessage> Received = new List<InboundMessage>();

            public Harness()
            {
                Bus.OnReceived += m => Received.Add(m);
                Session.OnSend += m =>
                {
                    Sent.Add(m);
                    switch (m.Target)
                    {
                        case MessageTarget.All:
                            Bus.SendToAll(m.Code, m.Payload);
                            break;
                        case MessageTarget.Actors:
                            Bus.SendToActors(m.Code, m.Payload, m.TargetActors);
                            break;
                        case MessageTarget.Master:
                            Bus.SendToMaster(m.Code, m.Payload);
                            break;
                    }
                };
            }

            public static List<WPlayer> SelfPlusBots(int total)
            {
                var players = new List<WPlayer> { new WPlayer { ActorNumber = 1, Name = "Self" } };
                for (int i = 1; i < total; i++)
                {
                    players.Add(new WPlayer { ActorNumber = -i, Name = "Bot" + i, IsBot = true });
                }
                return players;
            }

            public void Start(int totalPlayers, int seed, params (int Actor, Role Role)[] forced)
                => Start(totalPlayers, seed, teamTotal: 1, forced);

            public void Start(int totalPlayers, int seed, int teamTotal, params (int Actor, Role Role)[] forced)
            {
                foreach (var (actor, role) in forced)
                {
                    Session.ReserveForcedRole(actor, role);
                }
                var config = new GameConfig
                {
                    WerewolfCount = teamTotal,
                    RoundSeconds = RoundSeconds,
                    BlackCatRevealDelaySec = DelaySec,
                    BlackCatChancePercent = 100,
                    BomberChancePercent = 0,
                };
                var result = Session.Start(config, SelfPlusBots(totalPlayers), Now, new Random(seed));
                if (!result.Success)
                {
                    throw new InvalidOperationException("start rejected: " + result.Reason);
                }
            }

            public int CountSent(byte code)
            {
                int count = 0;
                foreach (var m in Sent)
                {
                    if (m.Code == code) count++;
                }
                return count;
            }

            public WPlayer Player(int actor)
            {
                foreach (var p in Session.Players)
                {
                    if (p.ActorNumber == actor) return p;
                }
                return null;
            }

            public int CountRole(Role role)
            {
                int count = 0;
                foreach (var p in Session.Players)
                {
                    if (p.Role == role) count++;
                }
                return count;
            }
        }

        private static void ScenarioDistribution(List<string> fails, int total, int teamTotal, int wolves, int cats)
        {
            var h = new Harness();
            h.Start(total, seed: total, teamTotal: teamTotal);

            Check(fails, h.CountRole(Role.Werewolf) == wolves,
                $"wolves={h.CountRole(Role.Werewolf)} expected={wolves}");
            Check(fails, h.CountRole(Role.BlackCat) == cats,
                $"cats={h.CountRole(Role.BlackCat)} expected={cats}");
            Check(fails, h.CountSent(WWEventCodes.AssignRole) == total,
                $"assignRoleSent={h.CountSent(WWEventCodes.AssignRole)} expected={total}");
            int selfReceived = 0;
            foreach (var m in h.Received)
            {
                if (m.Code == WWEventCodes.AssignRole) selfReceived++;
            }
            Check(fails, selfReceived == 1, $"selfAssignRoleReceived={selfReceived} expected=1");
        }

        private static void ScenarioForcedRole(List<string> fails)
        {
            var h = new Harness();
            h.Start(5, seed: 42, (-1, Role.Werewolf), (-2, Role.BlackCat));

            Check(fails, h.Player(-1).Role == Role.Werewolf, "actor-1 not werewolf");
            Check(fails, h.Player(-2).Role == Role.BlackCat, "actor-2 not blackcat");
            Check(fails, h.CountRole(Role.Werewolf) == 1 && h.CountRole(Role.BlackCat) == 1,
                "5p distribution not 1W/1C after forcing");
        }

        private static void ScenarioWinPrioritySimultaneous(List<string> fails)
        {
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Role = Role.Werewolf, Alive = false },
                new WPlayer { ActorNumber = 2, Role = Role.Villager, Alive = false },
                new WPlayer { ActorNumber = 3, Role = Role.BlackCat, Alive = false },
            };

            var result = WinJudge.Judge(players);
            Check(fails, result != null, "no winner on simultaneous eradication");
            Check(fails, result != null && result.WinningTeam == Team.Villagers,
                "winner is not villagers");
            Check(fails, result != null && result.Reason == WinReason.WerewolvesEradicated,
                "reason is not WerewolvesEradicated");
        }

        private static void ScenarioBlackCatExcludedFromWolfCount(List<string> fails)
        {
            var h = new Harness();
            h.Start(5, seed: 1, (-1, Role.Werewolf), (-2, Role.BlackCat));

            h.Session.RecordDeath(-1, Now + 1000);

            Check(fails, h.Session.Winner != null, "no winner after last wolf died");
            Check(fails, h.Session.Winner != null && h.Session.Winner.WinningTeam == Team.Villagers,
                "winner is not villagers");
            Check(fails, h.Player(-2).Alive, "blackcat unexpectedly dead");
            Check(fails, h.CountSent(WWEventCodes.GameOver) == 1, "EV_GameOver not sent exactly once");
        }

        private static void ScenarioBlackCatDeathContinues(List<string> fails)
        {
            var h = new Harness();
            h.Start(5, seed: 1, (-1, Role.Werewolf), (-2, Role.BlackCat));

            h.Session.RecordDeath(-2, Now + 1000);

            Check(fails, h.Session.Winner == null, "winner confirmed by blackcat-only death");
            Check(fails, h.Session.Phase == GamePhase.Play, "phase left Play");
            Check(fails, h.CountSent(WWEventCodes.PlayerDied) == 1, "EV_PlayerDied not sent");
            Check(fails, h.CountSent(WWEventCodes.GameOver) == 0, "EV_GameOver sent unexpectedly");
        }

        private static void ScenarioBlackCatSharesWolfWin(List<string> fails)
        {
            var h = new Harness();
            h.Start(5, seed: 1, (-1, Role.Werewolf), (-2, Role.BlackCat));

            h.Session.RecordDeath(1, Now + 1000);
            h.Session.RecordDeath(-3, Now + 2000);
            h.Session.RecordDeath(-4, Now + 3000);

            Check(fails, h.Session.Winner != null, "no winner after villagers eradicated");
            Check(fails, h.Session.Winner != null && h.Session.Winner.WinningTeam == Team.Werewolves,
                "winner is not werewolves");
            Check(fails, h.Player(-2).Alive, "blackcat unexpectedly dead");
            Check(fails, RoleDistribution.TeamOf(Role.BlackCat) == Team.Werewolves,
                "blackcat team mapping is not werewolves");
        }

        private static void ScenarioDisclosureOrderAndDedup(List<string> fails)
        {
            var h = new Harness();
            h.Start(7, seed: 7, (-1, Role.Werewolf), (-2, Role.Werewolf), (-3, Role.BlackCat));

            Check(fails, h.Sent.Count == 11, $"sentCount={h.Sent.Count} expected=11");
            for (int i = 0; i < 7 && i < h.Sent.Count; i++)
            {
                Check(fails, h.Sent[i].Code == WWEventCodes.AssignRole, $"sent[{i}] is not 160");
            }
            if (h.Sent.Count == 11)
            {
                Check(fails, h.Sent[7].Code == WWEventCodes.RevealTeammates, "sent[7] is not 162");
                Check(fails, h.Sent[8].Code == WWEventCodes.RevealTeammates, "sent[8] is not 162");
                Check(fails, h.Sent[9].Code == WWEventCodes.GameStart, "sent[9] is not 170");
                Check(fails, h.Sent[10].Code == WWEventCodes.PhaseChanged, "sent[10] is not 172");
            }

            foreach (var m in h.Sent)
            {
                if (m.Code == WWEventCodes.AssignRole && m.TargetActors[0] == -3)
                {
                    Check(fails, (byte)m.Payload[0] == (byte)Role.Villager,
                        "blackcat initial notice is not Villager");
                }
            }

            h.Session.NotifyDisclosureCondition(DisclosureKind.BlackCatSelfAwareness);
            h.Session.NotifyDisclosureCondition(DisclosureKind.BlackCatSelfAwareness);
            h.Session.Tick(Now + DelaySec * 1000L + 1);
            Check(fails, h.CountSent(WWEventCodes.RevealSelfRole) == 1,
                $"selfRoleSent={h.CountSent(WWEventCodes.RevealSelfRole)} expected=1");

            int teammatesBefore = h.CountSent(WWEventCodes.RevealTeammates);
            h.Session.NotifyDisclosureCondition(DisclosureKind.BlackCatSeesWerewolves);
            h.Session.NotifyDisclosureCondition(DisclosureKind.BlackCatSeesWerewolves);
            Check(fails, h.CountSent(WWEventCodes.RevealTeammates) == teammatesBefore + 1,
                "werewolves-to-blackcat reveal not issued exactly once");
        }

        private static void ScenarioMeetingPauseAndResumeExtend(List<string> fails)
        {
            var h = new Harness();
            h.Start(5, seed: 1, (-1, Role.Werewolf), (-2, Role.BlackCat));

            long pauseAt = Now + 10_000;
            long resumeAt = Now + 710_000;
            long extendedEnd = RoundEnd + (resumeAt - pauseAt);

            Check(fails, h.Session.RequestPhaseChange(GamePhase.Meeting, pauseAt).Success,
                "meeting transition rejected");
            h.Session.Tick(Now + 700_000);
            Check(fails, h.Session.Winner == null, "expired during meeting");

            Check(fails, h.Session.RequestPhaseChange(GamePhase.Play, resumeAt).Success,
                "play transition rejected");

            OutboundMessage lastPhase = null;
            foreach (var m in h.Sent)
            {
                if (m.Code == WWEventCodes.PhaseChanged) lastPhase = m;
            }
            Check(fails, lastPhase != null && (long)lastPhase.Payload[2] == extendedEnd,
                "PhaseChanged payload does not carry extended round end");

            h.Session.Tick(extendedEnd - 1);
            Check(fails, h.Session.Winner == null, "expired before extended end");
            h.Session.Tick(extendedEnd);
            Check(fails, h.Session.Winner != null && h.Session.Winner.Reason == WinReason.TimerExpired,
                "no timer expiry win at extended end");
        }

        private static void ScenarioTimerExpiryWolfWin(List<string> fails)
        {
            var h = new Harness();
            h.Start(5, seed: 1, (-1, Role.Werewolf), (-2, Role.BlackCat));

            h.Session.Tick(RoundEnd - 1);
            Check(fails, h.Session.Winner == null, "expired too early");

            h.Session.Tick(RoundEnd);
            Check(fails, h.Session.Winner != null, "no winner at round end");
            Check(fails, h.Session.Winner != null && h.Session.Winner.WinningTeam == Team.Werewolves,
                "winner is not werewolves");
            Check(fails, h.Session.Winner != null && h.Session.Winner.Reason == WinReason.TimerExpired,
                "reason is not TimerExpired");
            Check(fails, h.Session.Phase == GamePhase.GameOver, "phase is not GameOver");

            int idxGameOver = -1, idxLastPhase = -1;
            for (int i = 0; i < h.Sent.Count; i++)
            {
                if (h.Sent[i].Code == WWEventCodes.GameOver) idxGameOver = i;
                if (h.Sent[i].Code == WWEventCodes.PhaseChanged) idxLastPhase = i;
            }
            Check(fails, idxGameOver >= 0 && idxGameOver < idxLastPhase,
                "EV_GameOver is not before final EV_PhaseChanged");
        }

        private sealed class MeetingHarness
        {
            public readonly GameSession Game = new GameSession();
            public readonly MeetingClientState Client = new MeetingClientState();
            public readonly LoopbackNetBus Bus = new LoopbackNetBus(1);
            public readonly List<OutboundMessage> HostSent = new List<OutboundMessage>();
            public readonly List<InboundMessage> Received = new List<InboundMessage>();
            public readonly List<(int Caller, long End)> RoomMeeting = new List<(int, long)>();
            public readonly List<int> ExecutedActors = new List<int>();
            public readonly MeetingSession Meeting;

            public long Clock = Now;

            public MeetingHarness(int totalPlayers, params (int Actor, Role Role)[] forced)
                : this(new GameConfig
                {
                    RoundSeconds = RoundSeconds,
                    MeetingRightsPerPlayer = 1,
                    ConveneSuppressStartSec = 0,
                    ConveneSuppressAfterSec = 0,
                    MeetingCountdownSec = 5,
                    MeetingDurationSec = 120,
                    VoteTimeCutEnabled = true,
                    ResultDisplaySec = 6,
                }, totalPlayers, forced)
            {
            }

            public MeetingHarness(GameConfig config, int totalPlayers, params (int Actor, Role Role)[] forced)
            {
                Bus.OnReceived += ApplyInbound;
                Bus.OnPlayerLeft += actor =>
                {
                    Client.ApplyPlayerLeft(actor);
                    Meeting?.NotifyPlayerLeft(actor, Clock);
                };

                foreach (var (actor, role) in forced) Game.ReserveForcedRole(actor, role);

                Game.OnSend += SendViaBus;
                var result = Game.Start(config, Harness.SelfPlusBots(totalPlayers), Now, new Random(6));
                if (!result.Success)
                {
                    throw new InvalidOperationException("start rejected: " + result.Reason);
                }

                Meeting = new MeetingSession(config, Game, Game.Players, Now);
                Meeting.OnSend += SendViaBus;
                Meeting.OnPhaseChangeRequest += phase => Game.RequestPhaseChange(phase, Clock);
                Meeting.OnMeetingStateChanged += (caller, end) => RoomMeeting.Add((caller, end));
                Meeting.OnExecutePlayer += actor =>
                {
                    ExecutedActors.Add(actor);
                    Game.MarkNextDeathAsVote(actor);
                    Game.RecordDeath(actor, Clock);
                    Meeting.NotifyPlayerDied(actor);
                };
            }

            public void Convene() => Bus.SendToMaster(WWMeetingRequestCode, new object[] { (byte)ConveneKind.Button });

            public void SelfVote(int targetActor)
                => Bus.SendToMaster(WWCastVoteCode, new object[] { targetActor });

            public long StartVoting()
            {
                Convene();
                var start = LastReceived(WWMeetingCodesStartMeeting);
                if (start == null)
                {
                    throw new InvalidOperationException("163 not received after convene");
                }
                Clock = (long)start.Payload[1];
                Meeting.Tick(Clock);
                return Clock;
            }

            public InboundMessage LastReceived(byte code)
            {
                for (int i = Received.Count - 1; i >= 0; i--)
                {
                    if (Received[i].Code == code) return Received[i];
                }
                return null;
            }

            public int CountReceived(byte code)
            {
                int count = 0;
                foreach (var m in Received)
                {
                    if (m.Code == code) count++;
                }
                return count;
            }

            public void SendHostMessage(OutboundMessage m) => SendViaBus(m);

            private void SendViaBus(OutboundMessage m)
            {
                HostSent.Add(m);
                switch (m.Target)
                {
                    case MessageTarget.All:
                        Bus.SendToAll(m.Code, m.Payload);
                        break;
                    case MessageTarget.Actors:
                        Bus.SendToActors(m.Code, m.Payload, m.TargetActors);
                        break;
                    case MessageTarget.Master:
                        Bus.SendToMaster(m.Code, m.Payload);
                        break;
                }
            }

            private void ApplyInbound(InboundMessage msg)
            {
                Received.Add(msg);
                object[] p = msg.Payload;

                if (EventCodes.IsMasterInbound(msg.Code))
                {
                    if (Meeting == null) return;
                    switch (msg.Code)
                    {
                        case WWMeetingRequestCode:
                            Meeting.TryConvene(msg.SenderActor, Clock);
                            break;
                        case WWCastVoteCode:
                            Meeting.CastVote(msg.SenderActor, (int)p[0], Clock);
                            break;
                    }
                    return;
                }

                switch (msg.Code)
                {
                    case WWMeetingCodesStartMeeting:
                        Client.ApplyStartMeeting((int)p[0], (long)p[1], (long)p[2], (ConveneKind)(byte)p[3]);
                        break;
                    case WWVoteProgressCode:
                        Client.ApplyVoteProgress((int[])p[0], (long)p[1]);
                        break;
                    case WWMeetingResultCode:
                        Client.ApplyResult(new MeetingOutcome
                        {
                            ExecutedActor = (int)p[0],
                            TargetActors = (int[])p[1],
                            VoteCounts = (int[])p[2],
                        });
                        break;
                    case WWEventCodes.PlayerDied:
                        Client.ApplyPlayerDied((int)p[0], (DeathCause)(byte)p[1]);
                        break;
                    case WWEventCodes.PhaseChanged:
                        Client.ApplyPhase((GamePhase)(byte)p[0]);
                        break;
                }
            }
        }

        private const byte WWMeetingCodesStartMeeting = EventCodes.StartMeeting;
        private const byte WWCastVoteCode = EventCodes.CastVote;
        private const byte WWVoteProgressCode = EventCodes.VoteProgress;
        private const byte WWMeetingResultCode = EventCodes.MeetingResult;
        private const byte WWMeetingRequestCode = EventCodes.RequestMeeting;

        private static bool ContainsInt(IEnumerable<int> values, int value)
        {
            if (values == null) return false;
            foreach (int v in values)
            {
                if (v == value) return true;
            }
            return false;
        }

        private static void ScenarioMeetingFullFlow(List<string> fails)
        {
            var h = new MeetingHarness(4, (-3, Role.Werewolf));

            h.Convene();
            Check(fails, h.Meeting.Stage == MeetingStage.Countdown, "stage is not Countdown after convene");
            var start = h.LastReceived(WWMeetingCodesStartMeeting);
            Check(fails, start != null, "163 not received");
            if (start == null) return;
            Check(fails, (int)start.Payload[0] == 1, "163 caller is not 1");
            long warp = (long)start.Payload[1];
            long end0 = (long)start.Payload[2];
            Check(fails, h.Client.MeetingActive, "client state not active after 163");
            var phaseMeeting = h.LastReceived(WWEventCodes.PhaseChanged);
            Check(fails, phaseMeeting != null &&
                (GamePhase)(byte)phaseMeeting.Payload[0] == GamePhase.Meeting,
                "172(Meeting) not received");

            h.Clock = warp;
            h.Meeting.Tick(h.Clock);
            Check(fails, h.Meeting.Stage == MeetingStage.Voting, "stage is not Voting at warp");
            Check(fails, h.Client.WarpDone(h.Clock), "client warp not done");

            h.SelfVote(-2);
            var prog = h.LastReceived(WWVoteProgressCode);
            Check(fails, prog != null, "165 not received after self vote");
            if (prog != null)
            {
                Check(fails, ContainsInt((int[])prog.Payload[0], 1), "165 voted set missing self");
                Check(fails, (long)prog.Payload[1] < end0, "meeting end not shortened by vote");
            }
            Check(fails, ContainsInt(h.Client.VotedActors, 1), "client voted set missing self");

            h.Meeting.CastVote(-1, -2, h.Clock);
            h.Meeting.CastVote(-2, -1, h.Clock);
            h.Meeting.CastVote(-3, -2, h.Clock);
            h.Meeting.Tick(h.Clock);
            Check(fails, h.Meeting.Stage == MeetingStage.Closing, "stage is not Closing after all votes");

            var result = h.LastReceived(WWMeetingResultCode);
            Check(fails, result != null && (int)result.Payload[0] == -2, "166 executed is not -2");
            Check(fails, h.ExecutedActors.Count == 1 && h.ExecutedActors[0] == -2,
                "execute instruction not issued exactly once");
            var died = h.LastReceived(WWEventCodes.PlayerDied);
            Check(fails, died != null && (int)died.Payload[0] == -2 &&
                (DeathCause)(byte)died.Payload[1] == DeathCause.Vote,
                "168 with cause=Vote not received");
            Check(fails, h.Client.GetRowStatus(-2) == RowStatus.Executed, "row status is not Executed");
            Check(fails, h.Client.Result != null && h.Client.Result.ExecutedActor == -2,
                "client result not applied");

            h.Clock += 6_000;
            h.Meeting.Tick(h.Clock);
            Check(fails, h.Meeting.Stage == MeetingStage.Idle, "stage is not Idle after ClosingHold");
            var phasePlay = h.LastReceived(WWEventCodes.PhaseChanged);
            Check(fails, phasePlay != null &&
                (GamePhase)(byte)phasePlay.Payload[0] == GamePhase.Play, "172(Play) not received");
            Check(fails, !h.Client.MeetingActive, "client state still active after Play");
            Check(fails, h.RoomMeeting.Count >= 2 &&
                h.RoomMeeting[h.RoomMeeting.Count - 1] == (-1, 0L),
                "room property not cleared at meeting end");
            Check(fails, h.Game.Winner == null, "unexpected game over");
        }

        private static void ScenarioMeetingVoteSecrecy(List<string> fails)
        {
            var recorded = new List<(string Line, bool Secret)>();
            Action<string, bool> prev = WLog.Sink;
            WLog.Sink = (line, secret) =>
            {
                recorded.Add((line, secret));
                prev?.Invoke(line, secret);
            };
            try
            {
                var h = new MeetingHarness(4, (-3, Role.Werewolf));
                h.StartVoting();

                h.SelfVote(-2);

                int lines164 = 0;
                foreach (var (line, secret) in recorded)
                {
                    if (line.Contains("code=164"))
                    {
                        lines164++;
                        if (!secret) fails.Add("164 bus record is not secret: " + line);
                    }
                }
                Check(fails, lines164 >= 1, "no 164 bus record captured");

                foreach (var m in h.HostSent)
                {
                    if (m.Code == WWCastVoteCode) fails.Add("host re-sent code 164");
                }

                var prog = h.LastReceived(WWVoteProgressCode);
                Check(fails, prog != null && prog.Payload.Length == 2, "165 payload shape unexpected");
                if (prog != null)
                {
                    Check(fails, ContainsInt((int[])prog.Payload[0], 1), "165 voted set missing voter");
                }

                h.Meeting.CastVote(-1, -1, h.Clock);
                h.Meeting.CastVote(-2, -1, h.Clock);
                h.Meeting.CastVote(-3, -1, h.Clock);
                h.Meeting.Tick(h.Clock);
                var result = h.LastReceived(WWMeetingResultCode);
                Check(fails, result != null && result.Payload.Length == 3, "166 payload shape unexpected");

                Check(fails, EventCodes.IsSecret(WWCastVoteCode), "164 not classified as secret");
                Check(fails, EventCodes.IsMasterInbound(WWCastVoteCode), "164 not master-inbound");
                Check(fails, EventCodes.IsMasterInbound(WWMeetingRequestCode), "173 not master-inbound");
            }
            finally
            {
                WLog.Sink = prev;
            }
        }

        private static void ScenarioMeetingLeaveNoExecution(List<string> fails)
        {
            var h = new MeetingHarness(4, (-3, Role.Werewolf));
            h.StartVoting();

            h.SelfVote(-2);
            h.Meeting.CastVote(-1, -2, h.Clock);

            int progBefore = h.CountReceived(WWVoteProgressCode);
            h.Bus.SimulatePlayerLeft(-2);
            Check(fails, h.CountReceived(WWVoteProgressCode) == progBefore + 1,
                "165 not resent on player left");
            Check(fails, h.Client.GetRowStatus(-2) == RowStatus.Disconnected,
                "row status is not Disconnected");

            h.Meeting.Tick(h.Clock);
            Check(fails, h.Meeting.Stage == MeetingStage.Voting, "closed before remaining voters voted");

            h.Meeting.CastVote(-3, -1, h.Clock);
            h.Meeting.Tick(h.Clock);
            Check(fails, h.Meeting.Stage == MeetingStage.Closing, "not closed after all remaining votes");

            var result = h.LastReceived(WWMeetingResultCode);
            Check(fails, result != null && (int)result.Payload[0] == -1, "166 executed is not -1 (no execution)");
            if (result != null)
            {
                var targets = (int[])result.Payload[1];
                var counts = (int[])result.Payload[2];
                bool found = false;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == -2 && counts[i] == 2) found = true;
                }
                Check(fails, found, "vote counts for disconnected target not disclosed");
            }
            Check(fails, h.ExecutedActors.Count == 0, "execute instruction issued unexpectedly");

            h.Clock += 6_000;
            h.Meeting.Tick(h.Clock);
            Check(fails, h.Meeting.Stage == MeetingStage.Idle, "stage is not Idle after ClosingHold");
            var phasePlay = h.LastReceived(WWEventCodes.PhaseChanged);
            Check(fails, phasePlay != null &&
                (GamePhase)(byte)phasePlay.Payload[0] == GamePhase.Play, "172(Play) not received");
        }

        private static void ScenarioMeetingRestoreFromRoomState(List<string> fails)
        {
            var h = new MeetingHarness(4, (-3, Role.Werewolf));
            h.Convene();

            Check(fails, h.RoomMeeting.Count == 1, "room property publish not issued on convene");
            if (h.RoomMeeting.Count == 0) return;
            var (caller, end) = h.RoomMeeting[h.RoomMeeting.Count - 1];
            Check(fails, caller == 1, "published caller is not 1");
            Check(fails, end == h.Meeting.EndUnixMs, "published end differs from session end");

            var start = h.LastReceived(WWMeetingCodesStartMeeting);
            h.Clock = (long)start.Payload[1];
            h.Meeting.Tick(h.Clock);
            h.SelfVote(-2);

            var late = new MeetingClientState();
            late.RestoreFromRoomState(caller, end);
            Check(fails, late.MeetingActive, "restored client not active");
            Check(fails, late.CallerActor == 1, "restored caller is not 1");
            Check(fails, late.WarpDone(h.Clock), "restore did not treat warp as done");
            Check(fails, late.RemainingMs(h.Clock) > 0, "no remaining time after restore");
            Check(fails, late.VotedActors.Count == 0, "restored voted set is not empty");

            var prog = h.LastReceived(WWVoteProgressCode);
            Check(fails, prog != null, "165 not received");
            if (prog == null) return;
            long remainBefore = late.RemainingMs(h.Clock);
            late.ApplyVoteProgress((int[])prog.Payload[0], (long)prog.Payload[1]);
            Check(fails, ContainsInt(late.VotedActors, 1), "voted set not synced by next 165");
            Check(fails, late.RemainingMs(h.Clock) < remainBefore,
                "shortened end not synced by next 165");
        }

        private static void ScenarioMeetingVoteRejectReenable(List<string> fails)
        {
            var h = new MeetingHarness(4, (-3, Role.Werewolf));
            h.StartVoting();

            h.Bus.SimulatePlayerLeft(-2);
            int progAfterLeave = h.CountReceived(WWVoteProgressCode);
            Check(fails, progAfterLeave >= 1, "165 not resent on player left");

            h.SelfVote(-2);
            Check(fails, h.CountReceived(WWVoteProgressCode) == progAfterLeave + 1,
                "vote for disconnected target did not emit 165");
            Check(fails, ContainsInt(h.Client.VotedActors, 1), "voter for disconnected target not marked as voted");
        }

        private static GameConfig RolesConfig() => new GameConfig
        {
            RoundSeconds = RoundSeconds,
            BlackCatRevealDelaySec = DelaySec,
            WerewolfCount = 2,
            BlackCatChancePercent = 50,
            BomberChancePercent = 50,
            MeetingRightsPerPlayer = 2,
            ConveneSuppressStartSec = 0,
            ConveneSuppressAfterSec = 0,
            MeetingCountdownSec = 5,
            MeetingDurationSec = 120,
            ResultDisplaySec = 6,
            BeaconSuppressStartSec = 0,
            CurseWaitSec = 10,
            StaminaUnlockPct = 15,
            JumpUnlockPct = 30,
            EnemyIgnoreUnlockPct = 50,
            HealUnlockPct = 70,
            BeaconChargePct = 10,
            InformantThresholdPct = 60,
            BomberAmmoRefillPct = 30,
        };

        private sealed class RolesHarness
        {
            public readonly MeetingHarness M;
            public readonly RolesSession Roles;
            public readonly List<(int Actor, bool Enabled)> EnemyIgnoreChanges = new List<(int, bool)>();
            public readonly List<int> BeaconTriggers = new List<int>();
            public readonly List<int> CurseKills = new List<int>();
            public int InformantFires;

            public RolesHarness(int totalPlayers, params (int Actor, Role Role)[] forced)
                : this(RolesConfig(), totalPlayers, forced) { }

            public RolesHarness(GameConfig config, int totalPlayers, params (int Actor, Role Role)[] forced)
            {
                M = new MeetingHarness(config, totalPlayers, forced);
                Roles = new RolesSession(config, M.Game, Now, new Random(7));
                Roles.OnSend += M.SendHostMessage;
                Roles.OnInformantEstablished += () =>
                {
                    InformantFires++;
                    M.Game.NotifyDisclosureCondition(DisclosureKind.BlackCatSeesWerewolves);
                };
                Roles.OnEnemyIgnoreChanged += (actor, enabled) => EnemyIgnoreChanges.Add((actor, enabled));
                Roles.OnBeaconTriggered += actor => BeaconTriggers.Add(actor);
                Roles.OnCurseKill += actor =>
                {
                    CurseKills.Add(actor);
                    M.Game.RecordDeath(actor, M.Clock);
                    M.Meeting.NotifyPlayerDied(actor);
                };
                M.Meeting.OnExecutePlayer += actor =>
                {
                    if (Roles.TryStartCurse(actor, M.Clock, out long holdMs, M.Meeting.VotersFor(actor)))
                    {
                        M.Meeting.ExtendClosingHold(holdMs);
                    }
                };
                M.Meeting.OnVotingStarted += () => Roles.OnMeetingStarted(M.Clock);
                M.Meeting.OnMeetingStateChanged += (caller, _) =>
                {
                    if (caller < 0) Roles.OnMeetingEnded(M.Clock);
                };
            }

            public int CountHostSent(byte code, int subtype = -1)
            {
                int count = 0;
                foreach (var m in M.HostSent)
                {
                    if (m.Code != code) continue;
                    if (subtype >= 0 && (byte)m.Payload[0] != (byte)subtype) continue;
                    count++;
                }
                return count;
            }

            public int CountGaugeSentTo(int actor)
            {
                int count = 0;
                foreach (var m in M.HostSent)
                {
                    if (m.Code != WWRolesCodes.SyncPerkGauge) continue;
                    if (m.Target != MessageTarget.Actors || m.TargetActors == null) continue;
                    if (ContainsInt(m.TargetActors, actor)) count++;
                }
                return count;
            }
        }

        private static int LastBeaconAuditCount(RolesHarness h)
        {
            for (int i = h.M.HostSent.Count - 1; i >= 0; i--)
            {
                if (h.M.HostSent[i].Code == WWRolesCodes.BeaconAudit)
                {
                    return (byte)h.M.HostSent[i].Payload[0];
                }
            }
            return -1;
        }

        private static void ScenarioRolesGaugeFreezeAndFloor(List<string> fails)
        {
            Check(fails, PerkGauge.ComputeRealLoss(600f, 700f, 1000f) == 600f,
                "floor loss is not full remaining");
            Check(fails, PerkGauge.ComputeRealLoss(600f, 100f, 1000f) == 100f,
                "normal loss is not valueLost");

            var gauge = new PerkGauge(RolesConfig());
            Check(fails, gauge.AddLoss(100f).Count == 0, "loss counted before freeze");
            Check(fails, gauge.DisplayPermille == 0, "permille not 0 before freeze");

            gauge.FreezeBase(10000f);
            gauge.FreezeBase(99999f);
            Check(fails, gauge.BaseDollars == 10000f, "base changed by second freeze");

            gauge.AddLoss(15000f);
            Check(fails, gauge.DisplayPermille == 1000, "permille not capped at 1000");
            Check(fails, gauge.LostDollars == 15000f, "internal total unexpectedly capped");

            var cfg = RolesConfig();
            cfg.OrbGaugeEnabled = false;
            var h = new RolesHarness(cfg, 4, (-1, Role.Werewolf));
            h.Roles.FreezeBase(10000f);
            h.Roles.AddValueLoss(1000f, isOrb: true);
            Check(fails, h.Roles.Gauge.LostDollars == 0f, "orb loss counted despite disabled");
            h.Roles.AddValueLoss(1000f, isOrb: false);
            Check(fails, h.Roles.Gauge.LostDollars == 1000f, "normal loss not counted");
        }

        private static void ScenarioRolesPerkUnlockOrderOnce(List<string> fails)
        {
            var gauge = new PerkGauge(RolesConfig());
            gauge.FreezeBase(10000f);

            var ev = gauge.AddLoss(2000f);
            Check(fails, ev.Count == 2, $"20pct events={ev.Count} expected=2");
            Check(fails, ev.Count == 2 && ev[0].Kind == GaugeEventKind.PerkUnlocked
                && ev[0].Perk == PerkId.InfiniteStamina, "first unlock is not stamina");
            Check(fails, ev.Count == 2 && ev[1].Kind == GaugeEventKind.BeaconCharged
                && ev[1].BeaconChargeCount == 2, "charge count at 20pct is not 2");

            Check(fails, gauge.AddLoss(500f).Count == 0, "duplicate events on 25pct");

            ev = gauge.AddLoss(3000f);
            Check(fails, ev.Count == 3, $"55pct events={ev.Count} expected=3");
            Check(fails, ev.Count == 3 && ev[0].Perk == PerkId.InfiniteJump, "second unlock is not jump");
            Check(fails, ev.Count == 3 && ev[1].Perk == PerkId.EnemyIgnore, "third unlock is not enemyIgnore");
            Check(fails, ev.Count == 3 && ev[2].Kind == GaugeEventKind.BeaconCharged
                && ev[2].BeaconChargeCount == 3, "charge count at 55pct is not 3");

            ev = gauge.AddLoss(1000f);
            bool informantSeen = false;
            foreach (var e in ev)
            {
                if (e.Kind == GaugeEventKind.InformantReady) informantSeen = true;
            }
            Check(fails, informantSeen, "informant not fired at 65pct");
            ev = gauge.AddLoss(1000f);
            bool healSeen = false;
            foreach (var e in ev)
            {
                Check(fails, e.Kind != GaugeEventKind.InformantReady, "informant fired twice");
                if (e.Kind == GaugeEventKind.PerkUnlocked)
                {
                    Check(fails, e.Perk == PerkId.NaturalHeal, "unexpected perk at 75pct");
                    healSeen = true;
                }
            }
            Check(fails, healSeen, "heal not unlocked at 75pct");

            ev = gauge.AddLoss(1000f);
            foreach (var e in ev)
            {
                Check(fails, e.Kind != GaugeEventKind.InformantReady, "informant fired twice");
                Check(fails, e.Kind != GaugeEventKind.PerkUnlocked, "perk unlocked twice");
            }
            Check(fails, gauge.UnlockedFlags ==
                (PerkFlags.InfiniteStamina | PerkFlags.InfiniteJump | PerkFlags.EnemyIgnore | PerkFlags.NaturalHeal),
                "unlocked flags incomplete");
        }

        private static void ScenarioRolesBeaconGate(List<string> fails)
        {
            var cfg = RolesConfig();
            cfg.BeaconCooldownSec = 60;
            var beacon = new BeaconState(cfg);

            Check(fails, beacon.TryUse(Now) == BeaconStatus.NoCharge, "empty use not NoCharge");

            beacon.AddCharges(2);

            beacon.Suppress(Now + 30_000);
            Check(fails, beacon.TryUse(Now) == BeaconStatus.Suppressed, "suppressed use not rejected");
            Check(fails, beacon.Charges == 2, "charge consumed while suppressed");
            Check(fails, beacon.ReadyUnixMs == Now + 30_000, "readyUnixMs not suppression end");

            long t1 = Now + 30_000;
            Check(fails, beacon.TryUse(t1) == BeaconStatus.Ok, "use after suppression failed");
            Check(fails, beacon.Charges == 1, "charge not consumed on ok");
            Check(fails, beacon.TryUse(t1 + 1000) == BeaconStatus.Cooldown, "cooldown not enforced");
            Check(fails, beacon.Charges == 1, "charge consumed during cooldown");
            Check(fails, beacon.ReadyUnixMs == t1 + 60_000, "readyUnixMs not cooldown end");

            Check(fails, beacon.TryUse(t1 + 60_000) == BeaconStatus.Ok, "use after cooldown failed");
            Check(fails, beacon.Charges == 0, "charge count wrong after second use");
        }

        private static void ScenarioRolesBeaconMeetingEndSync(List<string> fails)
        {
            GameConfig cfg = RolesConfig();
            cfg.BeaconSuppressAfterMeetingSec = 45;
            var h = new RolesHarness(cfg, 4, (1, Role.Werewolf));

            int before = h.CountGaugeSentTo(1);
            h.Roles.OnMeetingEnded(h.M.Clock);
            Check(fails, h.CountGaugeSentTo(1) == before + 1, "171 not sent on meeting end");

            long ready = LastGaugeReadyUnixMsTo(h, 1);
            Check(fails, ready == h.M.Clock + 45_000, "readyUnixMs not meeting-end suppression end");
        }

        private static long LastGaugeReadyUnixMsTo(RolesHarness h, int actor)
        {
            long ready = -1;
            foreach (var m in h.M.HostSent)
            {
                if (m.Code != WWRolesCodes.SyncPerkGauge) continue;
                if (m.Target != MessageTarget.Actors || m.TargetActors == null
                    || !ContainsInt(m.TargetActors, actor))
                {
                    continue;
                }
                ready = (long)m.Payload[4];
            }
            return ready;
        }

        private static void ScenarioRolesInformantConditions(List<string> fails)
        {
            var h1 = new RolesHarness(5, (-1, Role.Werewolf), (-2, Role.BlackCat));
            h1.M.Game.RecordDeath(-2, Now + 1000);
            h1.Roles.FreezeBase(10000f);
            h1.Roles.AddValueLoss(7000f, isOrb: false);
            Check(fails, h1.InformantFires == 0, "informant fired though cat dead");
            Check(fails, !h1.Roles.InformantEstablished, "informant established though cat dead");

            var h2 = new RolesHarness(5, (-1, Role.Werewolf), (-2, Role.BlackCat));
            h2.Roles.FreezeBase(10000f);
            h2.Roles.AddValueLoss(7000f, isOrb: false);
            h2.Roles.AddValueLoss(1000f, isOrb: false);
            Check(fails, h2.InformantFires == 1, $"informant fires={h2.InformantFires} expected=1");
            Check(fails, h2.Roles.InformantEstablished, "informant not established");
            bool teammatesToCat = false;
            foreach (var m in h2.M.HostSent)
            {
                if (m.Code == WWEventCodes.RevealTeammates && m.Target == MessageTarget.Actors
                    && m.TargetActors != null && ContainsInt(m.TargetActors, -2))
                {
                    teammatesToCat = true;
                }
            }
            Check(fails, teammatesToCat, "162 not sent to blackcat on informant");
        }

        private static void ScenarioRolesCurseResolution(List<string> fails)
        {
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Role = Role.Villager },
                new WPlayer { ActorNumber = -1, Role = Role.Werewolf },
                new WPlayer { ActorNumber = -2, Role = Role.BlackCat, Alive = false },
                new WPlayer { ActorNumber = -3, Role = Role.Villager },
            };
            long deadline = Now + 10_000;

            var curse = new CurseSession(-2, deadline);
            Check(fails, !curse.Designate(1, -1, Now + 1000), "non-cat designate accepted");
            Check(fails, curse.Designate(-2, -1, Now + 1000), "cat designate rejected");
            Check(fails, curse.Designate(-2, -3, Now + 5000), "overwrite rejected");
            Check(fails, curse.TryResolve(deadline - 1, players, false, new Random(1)) == null,
                "resolved before deadline");
            Check(fails, curse.ResolveAtUnixMs == deadline, "deadline changed by designation");
            var res = curse.TryResolve(deadline, players, false, new Random(1));
            Check(fails, res != null && res.VictimActor == -3 && res.WasDesignated,
                "designated victim not -3");
            Check(fails, curse.TryResolve(deadline + 1000, players, false, new Random(1)) == null,
                "resolved twice");

            var seen = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                var c = new CurseSession(-2, deadline);
                var r = c.TryResolve(deadline, players, informantEstablished: false, new Random(seed));
                if (r != null) seen.Add(r.VictimActor);
            }
            Check(fails, seen.Contains(-1), "pre-informant random never hits werewolf");
            Check(fails, !seen.Contains(-2), "random hit the executed cat");

            seen.Clear();
            for (int seed = 0; seed < 40; seed++)
            {
                var c = new CurseSession(-2, deadline);
                var r = c.TryResolve(deadline, players, informantEstablished: true, new Random(seed));
                if (r != null) seen.Add(r.VictimActor);
            }
            Check(fails, !seen.Contains(-1), "post-informant random hit werewolf");
            Check(fails, seen.Contains(1) && seen.Contains(-3), "post-informant range too narrow");

            var c2 = new CurseSession(-2, deadline);
            c2.Designate(-2, -1, Now + 1000);
            players[1].Alive = false;
            var r2 = c2.TryResolve(deadline, players, false, new Random(3));
            Check(fails, r2 != null && !r2.WasDesignated && (r2.VictimActor == 1 || r2.VictimActor == -3),
                "dead designation did not fall back to random");

            var players2 = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Role = Role.Villager },
                new WPlayer { ActorNumber = -1, Role = Role.Werewolf },
                new WPlayer { ActorNumber = -2, Role = Role.BlackCat, Alive = false },
                new WPlayer { ActorNumber = -3, Role = Role.Villager },
            };
            var votersOnly = new[] { 1, -1 };

            var c3 = new CurseSession(-2, deadline, votersOnly);
            Check(fails, !c3.Designate(-2, -3, Now + 1000), "non-voter designation accepted");
            Check(fails, c3.Designate(-2, 1, Now + 1000), "voter designation rejected");

            seen.Clear();
            for (int seed = 0; seed < 40; seed++)
            {
                var c = new CurseSession(-2, deadline, votersOnly);
                var r = c.TryResolve(deadline, players2, informantEstablished: false, new Random(seed));
                if (r != null) seen.Add(r.VictimActor);
            }
            Check(fails, seen.Contains(1) && seen.Contains(-1) && !seen.Contains(-3),
                "restricted random not limited to voters");

            seen.Clear();
            for (int seed = 0; seed < 40; seed++)
            {
                var c = new CurseSession(-2, deadline, votersOnly);
                var r = c.TryResolve(deadline, players2, informantEstablished: true, new Random(seed));
                if (r != null) seen.Add(r.VictimActor);
            }
            Check(fails, seen.Contains(1) && !seen.Contains(-1),
                "post-informant restricted random did not prefer villager voters");

            var wolfOnly = new CurseSession(-2, deadline, new[] { -1 });
            var rw = wolfOnly.TryResolve(deadline, players2, informantEstablished: true, new Random(1));
            Check(fails, rw != null && rw.VictimActor == -1,
                "wolf-only voters did not hit wolf post-informant");

            var players3 = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Role = Role.Villager },
                new WPlayer { ActorNumber = -2, Role = Role.BlackCat, Alive = false },
                new WPlayer { ActorNumber = -3, Role = Role.Villager, Alive = false },
            };
            var deadVoters = new CurseSession(-2, deadline, new[] { -3 });
            var rn = deadVoters.TryResolve(deadline, players3, informantEstablished: false, new Random(1));
            Check(fails, rn != null && !rn.HasVictim,
                "no alive voters did not fizzle (expected NoVictim)");
        }

        private static void ScenarioRolesCurseWinJudge(List<string> fails)
        {
            var cfg = RolesConfig();
            cfg.WerewolfCount = 1;
            var h = new RolesHarness(cfg, 4, (-1, Role.Werewolf), (-2, Role.BlackCat));

            h.M.Game.MarkNextDeathAsVote(-2);
            h.M.Game.RecordDeath(-2, h.M.Clock);
            Check(fails, h.Roles.TryStartCurse(-2, h.M.Clock, out long holdMs), "curse not started");
            long expectedHoldMs = (10 + RolesSession.CurseKillDelaySec
                - MeetingSession.PostResultKillDelaySec) * 1000L;
            Check(fails, holdMs == expectedHoldMs, $"holdMs={holdMs} expected={expectedHoldMs}");

            h.Roles.HandleRoleAction(-2, RoleActionSubtype.CurseDesignate, -1, 0, h.M.Clock + 1000);
            h.M.Clock += 10_000;
            h.Roles.Tick(h.M.Clock);
            Check(fails, h.CurseKills.Count == 1 && h.CurseKills[0] == -1, "curse kill not issued to -1");
            Check(fails, h.M.Game.Winner != null && h.M.Game.Winner.WinningTeam == Team.Villagers,
                "villagers did not win after curse eradication");
            Check(fails, h.CountHostSent(WWRolesCodes.RoleState, RoleStateSubtype.CurseResolved) == 1,
                "175(resolved) not sent exactly once");
        }

        private static void ScenarioRolesActionValidation(List<string> fails)
        {
            var cfg = RolesConfig();
            cfg.WerewolfCount = 1;
            var h = new RolesHarness(cfg, 5, (-1, Role.Werewolf), (-2, Role.BlackCat));
            h.Roles.DebugChargeBeacon(3);

            h.Roles.HandleRoleAction(1, RoleActionSubtype.BeaconUse, 0, 0, h.M.Clock);
            h.Roles.HandleRoleAction(-2, RoleActionSubtype.BeaconUse, 0, 0, h.M.Clock);
            Check(fails, h.BeaconTriggers.Count == 0, "non-wolf beacon accepted");
            h.Roles.HandleRoleAction(1, RoleActionSubtype.WolfModeSync, 0, 1, h.M.Clock);
            Check(fails, h.EnemyIgnoreChanges.Count == 0, "non-wolf wolfmode accepted");

            h.Roles.HandleRoleAction(-1, RoleActionSubtype.BeaconUse, 0, 0, h.M.Clock);
            Check(fails, h.BeaconTriggers.Count == 1 && h.BeaconTriggers[0] == -1, "wolf beacon not triggered");
            Check(fails, h.CountHostSent(WWRolesCodes.BeaconAudit) == 0, "167 sent on use");

            int gaugeToWolfBefore = h.CountGaugeSentTo(-1);
            h.M.Convene();
            Check(fails, h.M.Game.Phase == GamePhase.Meeting, "phase did not become Meeting");
            h.Roles.HandleRoleAction(-1, RoleActionSubtype.BeaconUse, 0, 0, h.M.Clock);
            Check(fails, h.BeaconTriggers.Count == 1, "beacon triggered during meeting");
            Check(fails, h.CountHostSent(WWRolesCodes.BeaconAudit) == 0, "167 sent during meeting");
            Check(fails, h.CountGaugeSentTo(-1) == gaugeToWolfBefore + 1,
                "MeetingActive 171 not returned to caller");

            var start163 = h.M.LastReceived(WWMeetingCodesStartMeeting);
            Check(fails, start163 != null, "163 not received after convene");
            if (start163 != null)
            {
                h.M.Clock = (long)start163.Payload[1];
                h.M.Meeting.Tick(h.M.Clock);
                Check(fails, h.CountHostSent(WWRolesCodes.BeaconAudit) == 1, "audit 167 not sent on voting start");
                Check(fails, LastBeaconAuditCount(h) == 1, "audit count is not 1");
                h.Roles.OnMeetingEnded(h.M.Clock);
                h.Roles.OnMeetingStarted(h.M.Clock);
                Check(fails, h.CountHostSent(WWRolesCodes.BeaconAudit) == 2, "second audit 167 not sent");
                Check(fails, LastBeaconAuditCount(h) == 0, "audit ledger not reset after disclosure");
            }

            h.Roles.HandleRoleAction(-1, RoleActionSubtype.WolfModeSync, 0, 1, h.M.Clock);
            Check(fails, h.EnemyIgnoreChanges.Count == 1 && !h.EnemyIgnoreChanges[0].Enabled,
                "enemy ignore effective without unlock");

            h.Roles.DebugUnlockPerk(PerkId.EnemyIgnore);
            Check(fails, h.EnemyIgnoreChanges.Count == 2 && h.EnemyIgnoreChanges[1].Enabled
                && h.EnemyIgnoreChanges[1].Actor == -1,
                "enemy ignore not enabled on unlock during wolfmode");

            h.Roles.OnPlayerDied(-1);
            Check(fails, h.EnemyIgnoreChanges.Count == 3 && !h.EnemyIgnoreChanges[2].Enabled,
                "enemy ignore not disabled on death");

            var client = new RolesClientState();
            Check(fails, !client.TryToggleWolfMode(Role.Werewolf), "toggle succeeded with no unlocks");
            Check(fails, !client.TryToggleWolfMode(Role.Villager), "toggle succeeded for villager");
            client.ApplyGaugeSync(200, (byte)PerkFlags.InfiniteStamina, 0, 0, 0);
            Check(fails, !client.WolfMode, "wolfmode auto-on by unlock");
            Check(fails, client.TryToggleWolfMode(Role.Werewolf), "toggle failed with unlock");
            Check(fails, client.StaminaActive && !client.JumpActive, "effective state wrong");
            client.ApplyGaugeSync(400, (byte)(PerkFlags.InfiniteStamina | PerkFlags.InfiniteJump), 0, 0, 0);
            Check(fails, client.JumpActive, "new unlock not effective immediately during wolfmode");
            client.Reset();
            Check(fails, !client.WolfMode && client.UnlockedFlags == PerkFlags.None, "reset incomplete");
        }

        private static void ScenarioRolesFullCurseFlow(List<string> fails)
        {
            var h = new RolesHarness(5, (-2, Role.BlackCat), (-3, Role.Werewolf));
            long roundEnd0 = 0;
            foreach (var m in h.M.HostSent)
            {
                if (m.Code == WWEventCodes.GameStart) roundEnd0 = (long)m.Payload[0];
            }

            h.M.StartVoting();
            long meetingStart = h.M.Clock;
            h.M.Meeting.CastVote(1, -2, h.M.Clock);
            h.M.Meeting.CastVote(-1, -2, h.M.Clock);
            h.M.Meeting.CastVote(-2, -3, h.M.Clock);
            h.M.Meeting.CastVote(-3, -2, h.M.Clock);
            h.M.Meeting.CastVote(-4, -3, h.M.Clock);
            h.M.Meeting.Tick(h.M.Clock);
            Check(fails, h.M.Meeting.Stage == MeetingStage.Closing, "not Closing after votes");

            var curse = h.Roles.ActiveCurse;
            Check(fails, curse != null && curse.CatActor == -2, "curse not started for cat");
            Check(fails, h.CountHostSent(WWRolesCodes.RoleState, RoleStateSubtype.CurseStarted) == 1,
                "175(started) not sent exactly once");
            if (curse == null) return;

            h.M.Clock += 6_500;
            h.M.Meeting.Tick(h.M.Clock);
            Check(fails, h.M.Meeting.Stage == MeetingStage.Closing, "closing ended despite curse hold");

            Check(fails, h.CountHostSent(WWRolesCodes.CurseCandidates) == 0,
                "179 sent for bot cat");

            h.Roles.HandleRoleAction(-2, RoleActionSubtype.CurseDesignate, -4, 0, h.M.Clock);
            h.Roles.HandleRoleAction(-2, RoleActionSubtype.CurseDesignate, -1, 0, h.M.Clock);
            h.M.Clock = curse.ResolveAtUnixMs;
            h.Roles.Tick(h.M.Clock);
            h.M.Meeting.Tick(h.M.Clock);
            Check(fails, h.CurseKills.Count == 1 && h.CurseKills[0] == -1,
                "designated voter victim not killed (non-voter rejection broken?)");
            Check(fails, h.CountHostSent(WWRolesCodes.RoleState, RoleStateSubtype.CurseResolved) == 1,
                "175(resolved) not sent");
            var died = h.M.LastReceived(WWEventCodes.PlayerDied);
            Check(fails, died != null && (int)died.Payload[0] == -1
                && (DeathCause)(byte)died.Payload[1] == DeathCause.Other,
                "curse death not recorded as non-vote");
            Check(fails, h.M.Meeting.Stage == MeetingStage.Closing, "closing ended at resolve time");

            h.M.Clock = meetingStart + (6 + 10 + RolesSession.CurseKillDelaySec
                - MeetingSession.PostResultKillDelaySec) * 1000L;
            h.M.Meeting.Tick(h.M.Clock);
            Check(fails, h.M.Meeting.Stage == MeetingStage.Idle, "meeting not finished after hold");
            var phasePlay = h.M.LastReceived(WWEventCodes.PhaseChanged);
            Check(fails, phasePlay != null && (GamePhase)(byte)phasePlay.Payload[0] == GamePhase.Play,
                "172(Play) not received");
            if (phasePlay != null && roundEnd0 != 0)
            {
                long newEnd = (long)phasePlay.Payload[2];
                long meetingElapsed = h.M.Clock - meetingStart;
                Check(fails, newEnd >= roundEnd0 + meetingElapsed,
                    $"round end not extended by curse hold: newEnd={newEnd} expected>={roundEnd0 + meetingElapsed}");
            }
            Check(fails, h.M.Game.Winner == null, "unexpected game over");
        }

        private static void ScenarioRolesGaugeDistributionTargets(List<string> fails)
        {
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Name = "V", Role = Role.Villager },
                new WPlayer { ActorNumber = 2, Name = "W", Role = Role.Werewolf },
                new WPlayer { ActorNumber = 3, Name = "C", Role = Role.BlackCat },
            };
            var game = new GameSession();
            var sent = new List<OutboundMessage>();
            game.OnSend += sent.Add;
            game.ReserveForcedRole(1, Role.Villager);
            game.ReserveForcedRole(2, Role.Werewolf);
            game.ReserveForcedRole(3, Role.BlackCat);
            var cfg = RolesConfig();
            var start = game.Start(cfg, players, Now, new Random(9));
            if (!start.Success) throw new InvalidOperationException("start rejected");

            var roles = new RolesSession(cfg, game, Now, new Random(9));
            roles.OnSend += sent.Add;

            sent.Clear();
            roles.FreezeBase(10000f);
            int toWolf = 0, toCat = 0, toVillager = 0;
            CountGaugeTargets(sent, ref toWolf, ref toCat, ref toVillager);
            Check(fails, toWolf == 1, $"gauge to wolf={toWolf} expected=1");
            Check(fails, toCat == 0, "gauge sent to unawakened cat");
            Check(fails, toVillager == 0, "gauge sent to villager");

            game.NotifyDisclosureCondition(DisclosureKind.BlackCatSelfAwareness);
            sent.Clear();
            roles.AddValueLoss(500f, isOrb: false);
            toWolf = 0; toCat = 0; toVillager = 0;
            CountGaugeTargets(sent, ref toWolf, ref toCat, ref toVillager);
            Check(fails, toWolf == 1 && toCat == 0,
                $"gauge on change wolf={toWolf} cat={toCat} expected wolf=1 cat=0(periodic)");
            Check(fails, toVillager == 0, "gauge sent to villager after awaken");

            roles.Tick(Now + 1000);
            toWolf = 0; toCat = 0; toVillager = 0;
            CountGaugeTargets(sent, ref toWolf, ref toCat, ref toVillager);
            Check(fails, toCat == 1, $"periodic tick cat={toCat} expected=1");
            int[] catMeta = null;
            foreach (var m in sent)
            {
                if (m.Code == WWRolesCodes.SyncPerkGauge && m.TargetActors != null
                    && ContainsInt(m.TargetActors, 3))
                {
                    catMeta = (int[])m.Payload[5];
                }
            }
            Check(fails, catMeta != null && catMeta.Length >= 7
                && catMeta[6] == cfg.CatGaugeSyncIntervalSec,
                "cat gaugeMeta[6] not next-update seconds");

            roles.Tick(Now + 2000);
            roles.AddValueLoss(500f, isOrb: false);
            toWolf = 0; toCat = 0; toVillager = 0;
            CountGaugeTargets(sent, ref toWolf, ref toCat, ref toVillager);
            Check(fails, toCat == 1, $"cat resynced within interval cat={toCat} expected=1");

            foreach (var m in sent)
            {
                if (m.Code == WWRolesCodes.SyncPerkGauge)
                {
                    Check(fails, m.Target == MessageTarget.Actors && m.TargetActors != null
                        && m.TargetActors.Length == 1, "171 not single-target");
                }
            }
        }

        private static void CountGaugeTargets(
            List<OutboundMessage> sent, ref int toWolf, ref int toCat, ref int toVillager)
        {
            foreach (var m in sent)
            {
                if (m.Code != WWRolesCodes.SyncPerkGauge || m.TargetActors == null) continue;
                if (ContainsInt(m.TargetActors, 2)) toWolf++;
                if (ContainsInt(m.TargetActors, 3)) toCat++;
                if (ContainsInt(m.TargetActors, 1)) toVillager++;
            }
        }

        private static void ScenarioRolesMeetingGaugeSnapshot(List<string> fails)
        {
            var h = new RolesHarness(4, (-1, Role.Werewolf));
            h.Roles.FreezeBase(10000f);
            h.Roles.AddValueLoss(2500f, isOrb: false);

            h.M.StartVoting();
            Check(fails, h.CountHostSent(WWRolesCodes.RoleState, RoleStateSubtype.MeetingGauge) == 1,
                "175(gauge) not sent exactly once at meeting start");

            OutboundMessage snapshot = null;
            foreach (var m in h.M.HostSent)
            {
                if (m.Code == WWRolesCodes.RoleState
                    && (byte)m.Payload[0] == RoleStateSubtype.MeetingGauge) snapshot = m;
            }
            Check(fails, snapshot != null && snapshot.Target == MessageTarget.All, "175(gauge) not to all");
            if (snapshot != null)
            {
                int[] data = (int[])snapshot.Payload[1];
                Check(fails, data.Length == 13, $"gauge data length={data.Length} expected=13");
                Check(fails, data[0] == 250, $"gauge permille={data[0]} expected=250");
                Check(fails, data[1] == 10000, $"gauge base={data[1]} expected=10000");
                Check(fails, data[7] == 2500, $"gauge lostDollars={data[7]} expected=2500");
                Check(fails, data[8] == -1 && data[9] == -1,
                    $"gauge delivery defaults=({data[8]},{data[9]}) expected=(-1,-1)");
                Check(fails, data[10] == 30, $"gauge bombRefillPct={data[10]} expected=30");
                Check(fails, data[11] == -1, $"gauge checkmateLine={data[11]} expected=-1");
                Check(fails, data[12] == 70, $"gauge healPct={data[12]} expected=70");
            }

            h.Roles.AddValueLoss(2000f, isOrb: false);
            Check(fails, h.CountHostSent(WWRolesCodes.RoleState, RoleStateSubtype.MeetingGauge) == 1,
                "175(gauge) resent during meeting");

            h.M.Meeting.CastVote(1, -1, h.M.Clock);
            h.M.Meeting.CastVote(-1, -1, h.M.Clock);
            h.M.Meeting.CastVote(-2, -1, h.M.Clock);
            h.M.Meeting.CastVote(-3, -1, h.M.Clock);
            h.M.Meeting.Tick(h.M.Clock);
            h.M.Clock += 7_000;
            h.M.Meeting.Tick(h.M.Clock);
            Check(fails, h.M.Meeting.Stage == MeetingStage.Idle, "meeting not finished");
            h.M.Clock += 1_000;
            h.M.StartVoting();
            Check(fails, h.CountHostSent(WWRolesCodes.RoleState, RoleStateSubtype.MeetingGauge) == 2,
                "175(gauge) not sent on second meeting");
        }

        private sealed class BombHarness
        {
            public readonly GameConfig Config;
            public readonly List<WPlayer> Players;
            public readonly LoopbackNetBus Bus;
            public readonly BombSession Bomb;
            public readonly BombClientState Client = new BombClientState();
            public readonly List<InboundMessage> Received = new List<InboundMessage>();

            public BombHarness(GameConfig cfg, List<WPlayer> players, long now)
            {
                Config = cfg;
                Players = players;
                int bomberActor = 1;
                foreach (var p in players)
                {
                    if (p.Role == Role.Bomber) { bomberActor = p.ActorNumber; break; }
                }
                Bus = new LoopbackNetBus(bomberActor);
                Bomb = new BombSession(cfg, players, now);

                Bus.OnReceived += m =>
                {
                    Received.Add(m);
                    if (m.Code == EventCodes.BomberState)
                    {
                        Client.ApplyState(new BomberStateSnapshot(
                            (int)m.Payload[0], (byte)m.Payload[1],
                            (long)m.Payload[3], (long)m.Payload[4],
                            (BombDenyReason)(byte)m.Payload[2]));
                    }
                    else if (m.Code == EventCodes.BombDetonation)
                    {
                        Client.ApplyPendingDetonation((int)m.Payload[0], (long)m.Payload[1]);
                    }
                };

                FlushBomberState();
            }

            public void FlushBomberState()
            {
                if (!Bomb.Dirty) return;
                int bomberActor = Bomb.BomberActor;
                var snap = Bomb.BuildSnapshot();
                if (bomberActor < 0) return;
                Bus.SendToActors(
                    EventCodes.BomberState,
                    new object[]
                    {
                        snap.TargetActor,
                        snap.Ammo,
                        (byte)snap.LastDeny,
                        snap.PlantReadyUnixMs,
                        snap.DetonateReadyUnixMs,
                    },
                    new[] { bomberActor });
            }

            public void SendDetonation(int targetActor, long detonateAtUnixMs)
                => Bus.SendToAll(EventCodes.BombDetonation,
                    new object[] { targetActor, detonateAtUnixMs });

            public int CountReceived(byte code)
            {
                int c = 0;
                foreach (var m in Received) if (m.Code == code) c++;
                return c;
            }

            public InboundMessage LastReceived(byte code)
            {
                for (int i = Received.Count - 1; i >= 0; i--)
                {
                    if (Received[i].Code == code) return Received[i];
                }
                return null;
            }

            public WPlayer Player(int actor)
            {
                foreach (var p in Players) if (p.ActorNumber == actor) return p;
                return null;
            }
        }

        private static void ScenarioBomberFullFlow(List<string> fails)
        {
            var cfg = new GameConfig
            {
                RoundSeconds = RoundSeconds,
                BomberInitialCooldownSec = 60,
                BomberCooldownSec = 30,
                BomberAmmoRefillPct = 30,
                BomberWarningSec = 1f,
            };
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Name = "Self", Role = Role.Bomber },
                new WPlayer { ActorNumber = 2, Name = "Bot1", IsBot = true, Role = Role.Werewolf },
                new WPlayer { ActorNumber = 3, Name = "Bot2", IsBot = true, Role = Role.Villager },
                new WPlayer { ActorNumber = 4, Name = "Bot3", IsBot = true, Role = Role.Villager },
            };
            var h = new BombHarness(cfg, players, Now);

            Check(fails, h.Bomb.BomberActor == 1, "bomber actor not resolved to 1");
            Check(fails, h.CountReceived(EventCodes.BomberState) == 1,
                $"initial 181 count={h.CountReceived(EventCodes.BomberState)} expected=1");
            Check(fails, h.Client.Ammo == 1, "client ammo not 1 after initial 181");
            Check(fails, !h.Client.HasBomb, "client HasBomb not false initially");
            Check(fails, h.Client.PlantReadyUnixMs == Now + cfg.BomberInitialCooldownSec * 1000L,
                $"initial plant ready={h.Client.PlantReadyUnixMs}");
            Check(fails, h.Client.DetonateReadyUnixMs == 0,
                $"initial detonate ready={h.Client.DetonateReadyUnixMs} expected=0");

            h.Bomb.OnGaugeChanged(30f);
            h.FlushBomberState();
            Check(fails, h.Bomb.Ammo == 2, $"gauge grant ammo={h.Bomb.Ammo} expected=2");
            Check(fails, h.Client.Ammo == 2, "client ammo not synced after gauge");

            long t1 = Now + cfg.BomberInitialCooldownSec * 1000L;
            var plantReason = h.Bomb.TryPlant(1, 3, t1);
            Check(fails, plantReason == BombDenyReason.None, $"plant rejected: {plantReason}");
            Check(fails, h.Bomb.TargetActor == 3, "target not 3 after plant");
            Check(fails, h.Bomb.Ammo == 1, "ammo not decremented after plant");
            h.FlushBomberState();
            Check(fails, h.Client.TargetActor == 3, "client target not 3");
            Check(fails, h.Client.HasBomb, "client HasBomb not true after plant");

            long tDetonate = t1 + cfg.BomberCooldownSec * 1000L;
            var detonateReason = h.Bomb.TryDetonate(1, tDetonate, false, false, out int detonatedTarget);
            Check(fails, detonateReason == BombDenyReason.None && detonatedTarget == 3,
                $"detonate rejected: reason={detonateReason} target={detonatedTarget}");
            long detonateAt = tDetonate + (long)(cfg.BomberWarningSec * 1000f);
            h.SendDetonation(detonatedTarget, detonateAt);
            h.FlushBomberState();
            Check(fails, !h.Bomb.HasBomb, "bomb not consumed after detonate");
            Check(fails, h.CountReceived(EventCodes.BombDetonation) == 1,
                $"180 count={h.CountReceived(EventCodes.BombDetonation)} expected=1");
            var evDet = h.LastReceived(EventCodes.BombDetonation);
            Check(fails, evDet != null && (int)evDet.Payload[0] == 3 && (long)evDet.Payload[1] == detonateAt,
                "180 payload mismatch");
            Check(fails, h.Client.HasPendingDetonation && h.Client.PendingTargetActor == 3,
                "client pending detonation not tracked");
            Check(fails, !h.Client.HasBomb, "client HasBomb not false after detonate");
            Check(fails, h.Client.PlantReadyUnixMs == tDetonate + cfg.BomberCooldownSec * 1000L,
                "client plant cooldown not restarted after detonate");
            Check(fails, h.Client.DetonateReadyUnixMs == 0,
                "client detonate cooldown not cleared after detonate");

            long t2 = tDetonate + cfg.BomberCooldownSec * 1000L;
            var replant = h.Bomb.TryPlant(1, 4, t2);
            Check(fails, replant == BombDenyReason.None, $"replant rejected: {replant}");
            h.FlushBomberState();
            h.Player(4).Alive = false;
            long tDud = t2 + cfg.BomberCooldownSec * 1000L;
            var dud = h.Bomb.TryDetonate(1, tDud, false, false, out int _);
            Check(fails, dud == BombDenyReason.TargetDead, $"dud not TargetDead: {dud}");
            Check(fails, !h.Bomb.HasBomb, "bomb not consumed on dud");
            Check(fails, h.CountReceived(EventCodes.BombDetonation) == 1,
                "180 unexpectedly sent on dud");
            h.FlushBomberState();
            Check(fails, h.Client.LastDeny == BombDenyReason.TargetDead || h.Client.Ammo == h.Bomb.Ammo,
                "client did not receive dud state sync");
            Check(fails, h.Client.DetonateReadyUnixMs == 0,
                "client detonate cooldown not cleared after dud");

            h.Player(1).Alive = false;
            h.Bomb.OnPlayerDied(1);
            Check(fails, h.Bomb.BomberActor == -1, "bomber actor not -1 after death");
            var postDeath = h.Bomb.TryPlant(1, 2, tDud + 60_000);
            Check(fails, postDeath == BombDenyReason.NotBomber, $"post-death plant accepted: {postDeath}");
        }
    }
}
