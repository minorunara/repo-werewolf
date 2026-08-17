using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum RowStatus : byte
    {
        Alive = 0,

        Dead = 1,

        Executed = 2,

        Disconnected = 3,
    }

    public sealed class MeetingClientState
    {
        private const long WarpAlreadyDone = long.MinValue;

        private bool _active;
        private bool _votingClosed;
        private bool _discussionOpen;
        private long _warpUnixMs;
        private long _endUnixMs;
        private readonly Dictionary<int, RowStatus> _rows = new Dictionary<int, RowStatus>();
        private readonly HashSet<int> _voted = new HashSet<int>();

        private long _votingUiOffsetMs;
        private readonly HashSet<int> _announcedDead = new HashSet<int>();

        public bool MeetingActive => _active;

        public bool DiscussionOpen => _active && _discussionOpen;

        public int CallerActor { get; private set; } = -1;

        public ConveneKind Kind { get; private set; } = ConveneKind.Button;

        public IReadOnlyDictionary<int, RowStatus> Rows => _rows;

        public IReadOnlyCollection<int> VotedActors => _voted;

        public MeetingOutcome Result { get; private set; }

        public RowStatus GetRowStatus(int actorNumber)
            => _rows.TryGetValue(actorNumber, out var status) ? status : RowStatus.Alive;

        public int CompareRowOrder(int leftActor, int rightActor)
        {
            bool leftAlive = GetRowStatus(leftActor) == RowStatus.Alive;
            bool rightAlive = GetRowStatus(rightActor) == RowStatus.Alive;
            if (leftAlive != rightAlive) return leftAlive ? -1 : 1;
            return leftActor.CompareTo(rightActor);
        }

        public bool WarpDone(long nowUnixMs) => _active && nowUnixMs >= _warpUnixMs;

        public bool VotingUiReady(long nowUnixMs)
            => _active && nowUnixMs - _votingUiOffsetMs >= _warpUnixMs;

        public void MarkDiscussionOpen()
        {
            if (_active) _discussionOpen = true;
        }

        public bool GaugeIntroReady(long nowUnixMs)
            => _active && nowUnixMs - (_votingUiOffsetMs == 0 ? 0 : MeetingIntro.GaugeRevealOffsetMs) >= _warpUnixMs;

        public double GaugeMoveProgress(long nowUnixMs)
        {
            if (!_active || _votingUiOffsetMs == 0) return 1.0;
            return MeetingIntro.MoveProgress(nowUnixMs - (_warpUnixMs + _votingUiOffsetMs));
        }

        public bool IsDeadUnannounced(int actorNumber)
        {
            if (!_rows.TryGetValue(actorNumber, out RowStatus status)) return false;
            if (status != RowStatus.Dead && status != RowStatus.Executed) return false;
            return !_announcedDead.Contains(actorNumber);
        }

        public void MarkAllDeadAnnounced(List<int> newlyAnnounced = null)
        {
            foreach (var pair in _rows)
            {
                if (pair.Value == RowStatus.Dead || pair.Value == RowStatus.Executed)
                {
                    if (_announcedDead.Add(pair.Key)) newlyAnnounced?.Add(pair.Key);
                }
            }
        }

        public long RemainingMs(long nowUnixMs)
        {
            if (!_active || _votingClosed) return 0;
            long remaining = _endUnixMs - nowUnixMs;
            return remaining > 0 ? remaining : 0;
        }

        public bool ClosedEarly(long nowUnixMs)
            => _active && _votingClosed && nowUnixMs + EarlyCloseSlackMs < _endUnixMs;

        public const long EarlyCloseSlackMs = 1_000;

        public void ApplyStartMeeting(int caller, long warpUnixMs, long endUnixMs,
                                      ConveneKind kind = ConveneKind.Button)
        {
            _active = true;
            _votingClosed = false;
            _discussionOpen = false;
            CallerActor = caller;
            Kind = kind;
            _warpUnixMs = warpUnixMs;
            _endUnixMs = endUnixMs;
            _votingUiOffsetMs = MeetingIntro.VotingUiDelayMs;
            _voted.Clear();
            Result = null;
        }

        public void ApplyVoteProgress(int[] votedActors, long endUnixMs)
        {
            _voted.Clear();
            if (votedActors != null)
            {
                foreach (int actor in votedActors) _voted.Add(actor);
            }
            _endUnixMs = endUnixMs;
        }

        public void ApplyResult(MeetingOutcome outcome)
        {
            Result = outcome;
            _votingClosed = true;
        }

        public void ApplyPlayerDied(int actor, DeathCause cause)
        {
            _rows[actor] = cause == DeathCause.Vote ? RowStatus.Executed : RowStatus.Dead;
        }

        public void ApplyPlayerLeft(int actor)
        {
            _rows[actor] = RowStatus.Disconnected;
        }

        public void ApplyCancelled()
        {
            _active = false;
            _votingClosed = false;
            _discussionOpen = false;
            CallerActor = -1;
            Kind = ConveneKind.Button;
            _voted.Clear();
            Result = null;
        }

        public void ApplyPhase(GamePhase phase, List<int> newlyAnnounced = null)
        {
            if (phase == GamePhase.Play || phase == GamePhase.GameOver)
            {
                if (_active) MarkAllDeadAnnounced(newlyAnnounced);
                _active = false;
                _discussionOpen = false;
            }
        }

        public void RestoreFromRoomState(int caller, long endUnixMs)
        {
            _active = true;
            _votingClosed = false;
            _discussionOpen = true;
            CallerActor = caller;
            Kind = ConveneKind.Button;
            _warpUnixMs = WarpAlreadyDone;
            _endUnixMs = endUnixMs;
            _votingUiOffsetMs = 0;
            _voted.Clear();
            Result = null;
        }

        public void Reset()
        {
            _active = false;
            _votingClosed = false;
            _discussionOpen = false;
            CallerActor = -1;
            Kind = ConveneKind.Button;
            _warpUnixMs = 0;
            _endUnixMs = 0;
            _votingUiOffsetMs = 0;
            _rows.Clear();
            _voted.Clear();
            _announcedDead.Clear();
            Result = null;
        }
    }
}
