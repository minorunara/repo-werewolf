using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum ConveneRejectReason : byte
    {
        None = 0,

        NoRight = 1,

        Suppressed = 2,

        WrongPhase = 3,

        CallerDead = 4,

        AlreadyMeeting = 5,

        UnknownCaller = 6,

        CorpseReportLastRun = 7,

        NoCorpse = 8,
    }

    public enum ConveneKind : byte
    {
        Button = 0,

        CorpseReport = 1,

        ScatterGuard = 2,
    }

    public enum MeetingStage : byte
    {
        Idle = 0,

        Countdown = 1,

        Voting = 2,

        Closing = 3,
    }

    public sealed class MeetingSession
    {
        private readonly GameConfig _config;
        private readonly GameSession _gameSession;
        private readonly IReadOnlyList<WPlayer> _players;
        private readonly long _gameStartUnixMs;

        private readonly Dictionary<int, int> _rights = new Dictionary<int, int>();

        private readonly HashSet<int> _disconnected = new HashSet<int>();

        private readonly MeetingTimer _timer = new MeetingTimer();
        private VoteBox _voteBox;

        private long _warpUnixMs;
        private long _closingUntilUnixMs;

        public MeetingSession(GameConfig config, GameSession gameSession,
                              IReadOnlyList<WPlayer> players, long gameStartUnixMs)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _gameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _gameStartUnixMs = gameStartUnixMs;

            foreach (var p in _players)
            {
                _rights[p.ActorNumber] = config.MeetingRightsPerPlayer;
            }
        }

        public MeetingStage Stage { get; private set; } = MeetingStage.Idle;

        public MeetingOutcome Outcome { get; private set; }

        public long ResultCeremonyDelayMs
            => Outcome == null ? 0 : VoteTallyTimeline.CeremonyDelayMs(Outcome.VoteCounts);

        public long LastMeetingEndUnixMs { get; private set; } = long.MinValue;

        public int CallerActor { get; private set; } = -1;

        public ConveneKind CurrentKind { get; private set; } = ConveneKind.Button;

        public int RightsRemaining(int actorNumber)
            => _rights.TryGetValue(actorNumber, out int r) ? r : 0;

        public int VotedCount => _voteBox?.VotedActors.Count ?? 0;

        public int[] VotersFor(int targetActor) => _voteBox?.VotersFor(targetActor);

        public long EndUnixMs => Stage == MeetingStage.Idle ? 0 : _timer.EndUnixMs;

        public event Action<OutboundMessage> OnSend;

        public event Action<int> OnExecutePlayer;

        public event Action<GamePhase> OnPhaseChangeRequest;

        public event Action<int, long> OnMeetingStateChanged;

        public event Action OnVotingStarted;

        public event Action<int, int> OnRightsChanged;

        public ConveneRejectReason TryConvene(int callerActor, long nowUnixMs)
            => TryConvene(callerActor, nowUnixMs, ConveneKind.Button,
                          lastRunActive: false, corpseAvailable: true);

        public ConveneRejectReason TryConvene(
            int callerActor, long nowUnixMs, ConveneKind kind,
            bool lastRunActive, bool corpseAvailable)
        {
            var reason = EvaluateConvene(callerActor, nowUnixMs, kind, lastRunActive, corpseAvailable);
            if (reason != ConveneRejectReason.None)
            {
                WLog.Line("convene_rejected", secret: false,
                    ("caller", callerActor), ("kind", kind), ("reason", reason));

                if (reason != ConveneRejectReason.UnknownCaller)
                {
                    Send(new OutboundMessage(
                        WWMeetingCodes.ConveneDenied,
                        new object[] { ConveneDeniedWire.ToWire(reason) },
                        MessageTarget.Actors,
                        new[] { callerActor }));
                }
                return reason;
            }

            if (kind != ConveneKind.CorpseReport)
            {
                _rights[callerActor] = _rights[callerActor] - 1;
                OnRightsChanged?.Invoke(callerActor, _rights[callerActor]);
            }
            Accept(callerActor, nowUnixMs, kind, _config.MeetingCountdownSec);
            return ConveneRejectReason.None;
        }

        public const int ScatterGuardCountdownSec = 0;

        public bool TryConveneScatterGuard(int victimActor, long nowUnixMs)
        {
            if (Stage != MeetingStage.Idle || _gameSession.Phase != GamePhase.Play
                || _gameSession.WinLocked || FindPlayer(victimActor) == null)
            {
                WLog.Line("scatter_guard_convene_rejected", secret: false,
                    ("victim", victimActor), ("stage", Stage), ("phase", _gameSession.Phase));
                return false;
            }

            Accept(victimActor, nowUnixMs, ConveneKind.ScatterGuard, ScatterGuardCountdownSec);
            return true;
        }

        private void Accept(int callerActor, long nowUnixMs, ConveneKind kind, int countdownSec)
        {
            CallerActor = callerActor;
            CurrentKind = kind;
            Outcome = null;
            _voteBox = null;

            _warpUnixMs = nowUnixMs + countdownSec * 1000L;
            _timer.Start(_warpUnixMs + MeetingIntro.VotingUiDelayMs, _config.MeetingDurationSec);
            Stage = MeetingStage.Countdown;

            WLog.Line("convene_accepted", secret: false,
                ("caller", callerActor), ("kind", kind),
                ("warpUnixMs", _warpUnixMs), ("endUnixMs", _timer.EndUnixMs));

            Send(new OutboundMessage(
                WWMeetingCodes.StartMeeting,
                new object[] { callerActor, _warpUnixMs, _timer.EndUnixMs, (byte)kind },
                MessageTarget.All, null));
            OnPhaseChangeRequest?.Invoke(GamePhase.Meeting);
            OnMeetingStateChanged?.Invoke(callerActor, _timer.EndUnixMs);
        }

        private ConveneRejectReason EvaluateConvene(
            int callerActor, long nowUnixMs, ConveneKind kind,
            bool lastRunActive, bool corpseAvailable)
        {
            var caller = FindPlayer(callerActor);
            if (caller == null) return ConveneRejectReason.UnknownCaller;

            if (Stage != MeetingStage.Idle) return ConveneRejectReason.AlreadyMeeting;

            if (_gameSession.Phase != GamePhase.Play) return ConveneRejectReason.WrongPhase;

            if (_gameSession.WinLocked) return ConveneRejectReason.WrongPhase;

            if (!caller.Alive) return ConveneRejectReason.CallerDead;

            if (kind == ConveneKind.CorpseReport)
            {
                if (lastRunActive) return ConveneRejectReason.CorpseReportLastRun;
                if (!corpseAvailable) return ConveneRejectReason.NoCorpse;
            }
            else
            {
                if (nowUnixMs < _gameStartUnixMs + _config.ConveneSuppressStartSec * 1000L)
                    return ConveneRejectReason.Suppressed;
                if (LastMeetingEndUnixMs != long.MinValue &&
                    nowUnixMs < LastMeetingEndUnixMs + _config.ConveneSuppressAfterSec * 1000L)
                    return ConveneRejectReason.Suppressed;
            }

            if (kind != ConveneKind.CorpseReport && RightsRemaining(callerActor) <= 0)
                return ConveneRejectReason.NoRight;

            return ConveneRejectReason.None;
        }

        public bool TryCancelCorpseReportCountdown(long nowUnixMs)
        {
            if (Stage != MeetingStage.Countdown || CurrentKind != ConveneKind.CorpseReport)
                return false;

            int caller = CallerActor;

            Stage = MeetingStage.Idle;
            CallerActor = -1;
            CurrentKind = ConveneKind.Button;

            WLog.Line("meeting_cancelled", secret: false,
                ("caller", caller), ("reason", "final_extraction_started"), ("nowUnixMs", nowUnixMs));

            Send(new OutboundMessage(
                WWMeetingCodes.MeetingCancelled,
                new object[] { (byte)0 },
                MessageTarget.All, null));
            OnMeetingStateChanged?.Invoke(-1, 0);
            OnPhaseChangeRequest?.Invoke(GamePhase.Play);
            return true;
        }

        public bool AbortForGameOver()
        {
            if (Stage == MeetingStage.Idle) return false;

            WLog.Line("meeting_aborted", secret: false,
                ("caller", CallerActor), ("stage", Stage), ("reason", "game_over"));

            Stage = MeetingStage.Idle;
            CallerActor = -1;
            CurrentKind = ConveneKind.Button;
            Outcome = null;
            _voteBox = null;

            OnMeetingStateChanged?.Invoke(-1, 0);
            return true;
        }

        public void Tick(long nowUnixMs)
        {
            if (_gameSession.WinLocked) return;

            switch (Stage)
            {
                case MeetingStage.Countdown:
                    if (nowUnixMs >= _warpUnixMs) BeginVoting();
                    break;

                case MeetingStage.Voting:
                    if (_voteBox.AllVotesIn || _timer.IsExpired(nowUnixMs)) CloseVoting(nowUnixMs);
                    break;

                case MeetingStage.Closing:
                    if (nowUnixMs >= _closingUntilUnixMs) FinishMeeting(nowUnixMs);
                    break;
            }
        }

        public VoteRejectReason CastVote(int voterActor, int targetActor, long nowUnixMs)
        {
            if (Stage != MeetingStage.Voting)
            {
                WLog.Line("vote_rejected", secret: false,
                    ("voter", voterActor), ("reason", VoteRejectReason.NotVotingStage), ("stage", Stage));
                return VoteRejectReason.NotVotingStage;
            }

            var reason = _voteBox.TryCast(voterActor, targetActor);
            if (reason != VoteRejectReason.None)
            {
                WLog.Line("vote_rejected", secret: false, ("voter", voterActor), ("reason", reason));
                return reason;
            }

            WLog.Line("vote_accepted", secret: false, ("voter", voterActor));

            if (_config.VoteTimeCutEnabled)
            {
                long endBefore = _timer.EndUnixMs;
                int preVoteRemaining = _voteBox.RemainingVoterCount + 1;
                long endAfter = _timer.ReduceByVote(preVoteRemaining, nowUnixMs);
                WLog.Line("vote_shorten", secret: false,
                    ("preVoteRemaining", preVoteRemaining),
                    ("endBefore", endBefore), ("endAfter", endAfter),
                    ("remainMsBefore", endBefore - nowUnixMs),
                    ("remainMsAfter", endAfter - nowUnixMs));
            }

            EmitVoteProgress();

            return VoteRejectReason.None;
        }

        public void ExtendClosingHold(long extraMs)
        {
            if (Stage == MeetingStage.Closing) _closingUntilUnixMs += extraMs;
        }

        public void EnsureClosingHoldRemaining(long nowUnixMs, long minRemainMs)
        {
            if (Stage != MeetingStage.Closing) return;
            long minUntil = nowUnixMs + minRemainMs;
            if (_closingUntilUnixMs < minUntil) _closingUntilUnixMs = minUntil;
        }

        private void BeginVoting()
        {
            var aliveActors = new List<int>();
            foreach (var p in _players)
            {
                if (p.Alive && !_disconnected.Contains(p.ActorNumber))
                    aliveActors.Add(p.ActorNumber);
            }
            _voteBox = new VoteBox(aliveActors);
            Stage = MeetingStage.Voting;

            WLog.Line("meeting_voting", secret: false, ("expectedVoters", aliveActors.Count));

            OnVotingStarted?.Invoke();
        }

        public const int PostResultKillDelaySec = 5;

        private void CloseVoting(long nowUnixMs)
        {
            Outcome = _voteBox.Tally(IsEligibleForExecution);
            Stage = MeetingStage.Closing;
            _closingUntilUnixMs = nowUnixMs + ResultCeremonyDelayMs
                + Math.Max(_config.ResultDisplaySec, PostResultKillDelaySec + 1) * 1000L;

            Send(new OutboundMessage(
                WWMeetingCodes.MeetingResult,
                new object[] { Outcome.ExecutedActor, Outcome.TargetActors, Outcome.VoteCounts },
                MessageTarget.All, null));

            WLog.Line("meeting_result", secret: false,
                ("executed", Outcome.ExecutedActor),
                ("targets", Outcome.TargetActors), ("counts", Outcome.VoteCounts));

            if (Outcome.ExecutedActor != -1)
            {
                OnExecutePlayer?.Invoke(Outcome.ExecutedActor);
            }
        }

        private void FinishMeeting(long nowUnixMs)
        {
            LastMeetingEndUnixMs = nowUnixMs;
            Stage = MeetingStage.Idle;
            CallerActor = -1;
            CurrentKind = ConveneKind.Button;

            OnMeetingStateChanged?.Invoke(-1, 0);

            if (_gameSession.Phase != GamePhase.GameOver)
            {
                OnPhaseChangeRequest?.Invoke(GamePhase.Play);
            }

            WLog.Line("meeting_end", secret: false, ("lastEndUnixMs", LastMeetingEndUnixMs));
        }

        public void NotifyPlayerLeft(int actorNumber, long nowUnixMs)
        {
            _disconnected.Add(actorNumber);

            if (Stage == MeetingStage.Voting)
            {
                _voteBox.RemoveVoter(actorNumber);
                _voteBox.RemoveTarget(actorNumber, disconnected: true);
                EmitVoteProgress();
            }

            WLog.Line("meeting_player_left", secret: false,
                ("actor", actorNumber), ("stage", Stage), ("nowUnixMs", nowUnixMs));
        }

        public void NotifyPlayerDied(int actorNumber)
        {
            if (Stage == MeetingStage.Voting)
            {
                _voteBox.RemoveVoter(actorNumber);
                _voteBox.RemoveTarget(actorNumber, disconnected: false);
                EmitVoteProgress();
            }

            WLog.Line("meeting_player_died", secret: false, ("actor", actorNumber), ("stage", Stage));
        }

        private bool IsEligibleForExecution(int actorNumber)
        {
            var p = FindPlayer(actorNumber);
            return p != null && p.Alive;
        }

        private void EmitVoteProgress()
        {
            var voted = new List<int>(_voteBox.VotedActors);
            Send(new OutboundMessage(
                WWMeetingCodes.VoteProgress,
                new object[] { voted.ToArray(), _timer.EndUnixMs },
                MessageTarget.All, null));
        }

        private WPlayer FindPlayer(int actorNumber)
        {
            foreach (var p in _players)
            {
                if (p.ActorNumber == actorNumber) return p;
            }
            return null;
        }

        private void Send(OutboundMessage message) => OnSend?.Invoke(message);
    }

    internal static class WWMeetingCodes
    {
        public const byte StartMeeting = 163;

        public const byte VoteProgress = 165;

        public const byte MeetingResult = 166;

        public const byte ConveneDenied = 176;

        public const byte MeetingCancelled = 178;
    }
}
