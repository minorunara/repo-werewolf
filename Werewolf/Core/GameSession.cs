using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum StartRejectReason : byte
    {
        None = 0,

        TooFewPlayers = 1,

        NotInLobby = 2,

        InvalidConfig = 3,
    }

    public sealed class StartResult
    {
        private static readonly IReadOnlyList<ConfigIssue> NoIssues = new ConfigIssue[0];

        private StartResult(bool success, StartRejectReason reason, IReadOnlyList<ConfigIssue> issues)
        {
            Success = success;
            Reason = reason;
            Issues = issues ?? NoIssues;
        }

        public bool Success { get; }
        public StartRejectReason Reason { get; }

        public IReadOnlyList<ConfigIssue> Issues { get; }

        public static StartResult Ok() => new StartResult(true, StartRejectReason.None, NoIssues);
        public static StartResult Rejected(StartRejectReason reason) => new StartResult(false, reason, NoIssues);
        public static StartResult Rejected(StartRejectReason reason, IReadOnlyList<ConfigIssue> issues)
            => new StartResult(false, reason, issues);
    }

    public enum PhaseChangeRejectReason : byte
    {
        None = 0,

        InvalidTransition = 1,
    }

    public sealed class PhaseChangeResult
    {
        private PhaseChangeResult(bool success, PhaseChangeRejectReason reason)
        {
            Success = success;
            Reason = reason;
        }

        public bool Success { get; }
        public PhaseChangeRejectReason Reason { get; }

        public static PhaseChangeResult Ok() => new PhaseChangeResult(true, PhaseChangeRejectReason.None);
        public static PhaseChangeResult Rejected(PhaseChangeRejectReason reason)
            => new PhaseChangeResult(false, reason);
    }

    public sealed class GameSession
    {
        private readonly Dictionary<int, Role> _forcedRoles = new Dictionary<int, Role>();
        private readonly HashSet<int> _voteMarked = new HashSet<int>();
        private List<WPlayer> _players = new List<WPlayer>();
        private bool _checkmateLocked;
        private RoundTimer _timer = new RoundTimer();
        private DisclosureManager _disclosures;
        private GameConfig _config;

        public GamePhase Phase { get; private set; } = GamePhase.Lobby;

        public IReadOnlyList<WPlayer> Players => _players;

        public WinResult Winner { get; private set; }

        public bool Voided { get; private set; }

        public long StartUnixMs { get; private set; }

        public event Action<OutboundMessage> OnSend;

        public event Action<SessionEvent> OnSessionEvent;

        public void ReserveForcedRole(int actorNumber, Role role)
        {
            _forcedRoles[actorNumber] = role;
        }

        public StartResult Start(GameConfig config, IReadOnlyList<WPlayer> players,
                                 long nowUnixMs, Random rng)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            if (Phase != GamePhase.Lobby)
            {
                WLog.Line("start_rejected", secret: false,
                    ("reason", "not_in_lobby"), ("phase", Phase));
                return StartResult.Rejected(StartRejectReason.NotInLobby);
            }
            if (players.Count < PlayerCountGate.MinimumPlayers)
            {
                WLog.Line("start_rejected", secret: false,
                    ("reason", "too_few_players"), ("players", players.Count));
                return StartResult.Rejected(StartRejectReason.TooFewPlayers);
            }

            var configIssues = ConfigValidator.Validate(config, players.Count);
            if (configIssues.Count > 0)
            {
                WLog.Line("start_rejected", secret: false,
                    ("reason", "invalid_config"), ("issues", string.Join(",", configIssues)));
                return StartResult.Rejected(StartRejectReason.InvalidConfig, configIssues);
            }

            _config = config;
            _players = new List<WPlayer>(players);
            Winner = null;
            Voided = false;
            _checkmateLocked = false;
            _voteMarked.Clear();

            var assigned = RoleAssigner.Assign(_players, config, rng, _forcedRoles);
            _forcedRoles.Clear();

            _timer = new RoundTimer();
            _timer.Start(nowUnixMs, config.RoundSeconds);
            StartUnixMs = nowUnixMs;
            _disclosures = new DisclosureManager(_players, nowUnixMs, config.BlackCatRevealDelaySec);

            foreach (var disclosure in _disclosures.IssueInitialDisclosures())
            {
                Send(ToMessage(disclosure));
            }

            bool blackCatPossible = config.BlackCatPossible(_players.Count);
            Send(new OutboundMessage(
                WWEventCodes.GameStart,
                new object[]
                {
                    _timer.EndUnixMs,
                    config.RoundSeconds,
                    (byte)assigned.Werewolves,
                    (byte)(blackCatPossible ? 1 : 0),
                    config.BlackCatRevealDelaySec,
                    (byte)(config.DebugMode ? 1 : 0),
                    ParticipantIds.AssignOrder(_players),
                },
                MessageTarget.All, null));

            SetPhase(GamePhase.Play, "start", nowUnixMs);
            return StartResult.Ok();
        }

        public void Tick(long nowUnixMs)
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return;

            if (Phase == GamePhase.Play)
            {
                foreach (var disclosure in _disclosures.Tick(nowUnixMs))
                {
                    Send(ToMessage(disclosure));
                }
            }

            if (_timer.CheckExpiry(nowUnixMs))
            {
                ConfirmWinner(
                    WinJudge.Judge(_players, extractionCompleted: false, timerExpired: true),
                    "timer_expired", nowUnixMs);
            }
        }

        public bool RecordDeath(int actorNumber, long nowUnixMs)
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return false;

            WPlayer player = null;
            foreach (var candidate in _players)
            {
                if (candidate.ActorNumber == actorNumber)
                {
                    player = candidate;
                    break;
                }
            }
            if (player == null)
            {
                WLog.Line("drop", secret: false,
                    ("reason", "death_unknown_actor"), ("actor", actorNumber));
                return false;
            }
            if (!player.Alive) return false;

            var cause = _voteMarked.Remove(actorNumber) ? DeathCause.Vote : DeathCause.Other;
            player.Alive = false;
            player.DeathCause = cause;

            Send(new OutboundMessage(
                WWEventCodes.PlayerDied,
                new object[] { actorNumber, (byte)cause },
                MessageTarget.All, null));
            OnSessionEvent?.Invoke(SessionEvent.ForPlayerDied(actorNumber, cause));

            var result = WinJudge.Judge(_players);
            if (result != null)
            {
                ConfirmWinner(result, "death", nowUnixMs);
            }
            return true;
        }

        public void NotifyPlayerLeft(int actorNumber, long nowUnixMs)
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return;

            WPlayer player = null;
            foreach (var candidate in _players)
            {
                if (candidate.ActorNumber == actorNumber)
                {
                    player = candidate;
                    break;
                }
            }
            if (player == null || !player.Alive) return;

            player.Alive = false;
            WLog.Line("player_left_roster", secret: false, ("actor", actorNumber));

            var result = WinJudge.Judge(_players);
            if (result != null)
            {
                ConfirmWinner(result, "player_left", nowUnixMs);
            }
        }

        public void MarkNextDeathAsVote(int actorNumber)
        {
            _voteMarked.Add(actorNumber);
        }

        public bool BlackCatSelfAwarenessIssued => _disclosures != null && _disclosures.SelfAwarenessIssued;

        public void ForceExpireTimer(long nowUnixMs)
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return;

            _timer.ForceExpire(nowUnixMs);
            WLog.Line("timer_force_expire", secret: false, ("endUnixMs", _timer.EndUnixMs));
        }

        public long RemainingMs(long nowUnixMs) => _timer.RemainingMs(nowUnixMs);

        public void NotifyExtractionOutcome(bool completed, bool failed, long nowUnixMs)
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return;

            if (completed)
            {
                ConfirmWinner(
                    WinJudge.Judge(_players, extractionCompleted: true),
                    "extraction_completed", nowUnixMs);
            }
            else if (failed)
            {
                var result = WinJudge.Judge(_players)
                    ?? new WinResult(Team.Werewolves, WinReason.ExtractionFailed);
                ConfirmWinner(result, "extraction_failed", nowUnixMs);
            }
        }

        public void NotifyMailDeparture(long nowUnixMs)
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return;

            var result = WinJudge.Judge(_players, extractionCompleted: true);
            ConfirmWinner(result, "mail_departure", nowUnixMs);
        }

        public void LockValueCheckmate()
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return;
            if (_checkmateLocked) return;
            _checkmateLocked = true;
            WLog.Line("checkmate_locked", secret: false);
        }

        public void NotifyValueCheckmate(long nowUnixMs)
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return;

            ConfirmWinner(
                new WinResult(Team.Werewolves, WinReason.ValueCheckmate),
                "value_checkmate", nowUnixMs);
        }

        public void NotifyDisclosureCondition(DisclosureKind kind)
        {
            if (Winner != null || (Phase != GamePhase.Play && Phase != GamePhase.Meeting)) return;

            foreach (var disclosure in _disclosures.NotifyCondition(kind))
            {
                Send(ToMessage(disclosure));
            }
        }

        public PhaseChangeResult RequestPhaseChange(GamePhase target, long nowUnixMs)
        {
            bool allowed =
                Winner == null &&
                ((Phase == GamePhase.Play && target == GamePhase.Meeting) ||
                 (Phase == GamePhase.Meeting && target == GamePhase.Play) ||
                 (Phase == GamePhase.Play && target == GamePhase.GameOver) ||
                 (Phase == GamePhase.Meeting && target == GamePhase.GameOver));
            if (!allowed)
            {
                WLog.Line("phase_rejected", secret: false,
                    ("from", Phase), ("to", target));
                return PhaseChangeResult.Rejected(PhaseChangeRejectReason.InvalidTransition);
            }

            if (target == GamePhase.Meeting)
            {
                _timer.PauseForMeeting(nowUnixMs);
            }
            else if (target == GamePhase.Play)
            {
                _timer.ResumeFromMeeting(nowUnixMs);
            }

            SetPhase(target, "request", nowUnixMs);
            return PhaseChangeResult.Ok();
        }

        public void VoidMatch(long nowUnixMs)
        {
            if (Winner != null || Voided) return;
            if (Phase != GamePhase.Play && Phase != GamePhase.Meeting) return;

            Voided = true;
            WLog.Line("match_voided", secret: false, ("phase", Phase));

            var actors = new int[_players.Count];
            var roles = new byte[_players.Count];
            for (int i = 0; i < _players.Count; i++)
            {
                actors[i] = _players[i].ActorNumber;
                roles[i] = (byte)_players[i].Role;
            }

            Send(new OutboundMessage(
                WWEventCodes.GameOver,
                new object[] { TeamCodes.VoidMatch, actors, roles },
                MessageTarget.All, null));
            OnSessionEvent?.Invoke(SessionEvent.ForMatchVoided());

            SetPhase(GamePhase.GameOver, "void_match", nowUnixMs);
        }

        private void ConfirmWinner(WinResult result, string reason, long nowUnixMs)
        {
            if (Voided)
            {
                WLog.Line("win_suppressed_by_void", secret: false,
                    ("team", result.WinningTeam), ("reason", result.Reason), ("trigger", reason));
                return;
            }

            if (_checkmateLocked && result.Reason != WinReason.ValueCheckmate)
            {
                WLog.Line("win_suppressed_by_checkmate", secret: false,
                    ("team", result.WinningTeam), ("reason", result.Reason), ("trigger", reason));
                return;
            }

            Winner = result;
            WLog.Line("win", secret: false,
                ("team", result.WinningTeam), ("reason", result.Reason), ("trigger", reason));

            var actors = new int[_players.Count];
            var roles = new byte[_players.Count];
            for (int i = 0; i < _players.Count; i++)
            {
                actors[i] = _players[i].ActorNumber;
                roles[i] = (byte)_players[i].Role;
            }

            Send(new OutboundMessage(
                WWEventCodes.GameOver,
                new object[] { (byte)result.WinningTeam, actors, roles },
                MessageTarget.All, null));
            OnSessionEvent?.Invoke(SessionEvent.ForWinnerConfirmed(result));

            SetPhase(GamePhase.GameOver, "win_" + reason, nowUnixMs);
        }

        private void SetPhase(GamePhase to, string reason, long nowUnixMs)
        {
            var from = Phase;
            Phase = to;
            WLog.Phase(from, to, reason);

            Send(new OutboundMessage(
                WWEventCodes.PhaseChanged,
                new object[] { (byte)to, nowUnixMs, _timer.EndUnixMs },
                MessageTarget.All, null));

            OnSessionEvent?.Invoke(SessionEvent.ForPhaseChanged(to, _timer.EndUnixMs));
        }

        private static OutboundMessage ToMessage(Disclosure disclosure)
        {
            switch (disclosure.Type)
            {
                case DisclosureType.RoleNotice:
                    return new OutboundMessage(WWEventCodes.AssignRole,
                        new object[] { (byte)disclosure.ShownRole },
                        MessageTarget.Actors, disclosure.TargetActors);

                case DisclosureType.SelfRoleReveal:
                    return new OutboundMessage(WWEventCodes.RevealSelfRole,
                        new object[] { (byte)disclosure.ShownRole },
                        MessageTarget.Actors, disclosure.TargetActors);

                case DisclosureType.TeammatesReveal:
                {
                    int[] actors = disclosure.WerewolfActors ?? System.Array.Empty<int>();
                    byte[] roles = disclosure.WerewolfActorRoles;
                    if (roles == null || roles.Length != actors.Length)
                    {
                        roles = new byte[actors.Length];
                        for (int i = 0; i < roles.Length; i++) roles[i] = (byte)Role.Werewolf;
                    }
                    return new OutboundMessage(WWEventCodes.RevealTeammates,
                        new object[] { actors, roles },
                        MessageTarget.Actors, disclosure.TargetActors);
                }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(disclosure), disclosure.Type, "未知の開示種別です。");
            }
        }

        private void Send(OutboundMessage message)
        {
            OnSend?.Invoke(message);
        }
    }
}
