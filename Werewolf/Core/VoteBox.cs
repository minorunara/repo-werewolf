using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum VoteRejectReason : byte
    {
        None = 0,

        NotVotingStage = 1,

        VoterDead = 2,

        VoterUnknown = 3,

        AlreadyVoted = 4,

        TargetDead = 5,

        TargetGone = 6,

        TargetUnknown = 7,
    }

    public sealed class MeetingOutcome
    {
        public int ExecutedActor;

        public int[] TargetActors;

        public int[] VoteCounts;

        public int SkipVotes
        {
            get
            {
                if (TargetActors == null || VoteCounts == null) return 0;
                for (int i = 0; i < TargetActors.Length && i < VoteCounts.Length; i++)
                {
                    if (TargetActors[i] == -1) return VoteCounts[i];
                }
                return 0;
            }
        }
    }

    public sealed class VoteBox
    {
        private readonly HashSet<int> _originalVoters;
        private readonly HashSet<int> _expectedVoters;

        private readonly HashSet<int> _originalTargets;
        private readonly HashSet<int> _deadTargets = new HashSet<int>();
        private readonly HashSet<int> _disconnectedActors = new HashSet<int>();

        private readonly Dictionary<int, int> _votes = new Dictionary<int, int>();

        public VoteBox(IEnumerable<int> aliveActors)
        {
            if (aliveActors == null) throw new ArgumentNullException(nameof(aliveActors));

            _originalVoters = new HashSet<int>(aliveActors);
            _expectedVoters = new HashSet<int>(_originalVoters);
            _originalTargets = new HashSet<int>(_originalVoters);
        }

        public int ExpectedVoterCount => _expectedVoters.Count;

        public int RemainingVoterCount
        {
            get
            {
                int count = 0;
                foreach (int voter in _expectedVoters)
                {
                    if (!_votes.ContainsKey(voter)) count++;
                }
                return count;
            }
        }

        public bool AllVotesIn
        {
            get
            {
                foreach (int voter in _expectedVoters)
                {
                    if (!_votes.ContainsKey(voter)) return false;
                }
                return true;
            }
        }

        public IReadOnlyCollection<int> VotedActors => _votes.Keys;

        public int[] VotersFor(int targetActor)
        {
            var voters = new List<int>();
            foreach (var kv in _votes)
            {
                if (kv.Value == targetActor) voters.Add(kv.Key);
            }
            voters.Sort();
            return voters.ToArray();
        }

        public VoteRejectReason TryCast(int voterActor, int targetActor)
        {
            if (!_originalVoters.Contains(voterActor)) return VoteRejectReason.VoterUnknown;
            if (!_expectedVoters.Contains(voterActor)) return VoteRejectReason.VoterDead;
            if (_votes.ContainsKey(voterActor)) return VoteRejectReason.AlreadyVoted;

            if (targetActor != -1)
            {
                if (_deadTargets.Contains(targetActor)) return VoteRejectReason.TargetDead;
                if (!_originalTargets.Contains(targetActor)) return VoteRejectReason.TargetUnknown;
            }

            _votes[voterActor] = targetActor;
            return VoteRejectReason.None;
        }

        public void RemoveVoter(int actorNumber)
        {
            _expectedVoters.Remove(actorNumber);
        }

        public void RemoveTarget(int actorNumber, bool disconnected = false)
        {
            if (disconnected) _disconnectedActors.Add(actorNumber);
            else _deadTargets.Add(actorNumber);
        }

        public MeetingOutcome Tally(Func<int, bool> isEligibleForExecution)
        {
            var counts = new Dictionary<int, int>();
            foreach (int voter in _expectedVoters)
            {
                int target = _votes.TryGetValue(voter, out int t) ? t : -1;
                counts.TryGetValue(target, out int c);
                counts[target] = c + 1;
            }

            foreach (int gone in _disconnectedActors)
            {
                if (!_originalVoters.Contains(gone)) continue;
                if (_votes.ContainsKey(gone)) continue;
                counts.TryGetValue(-1, out int c);
                counts[-1] = c + 1;
            }

            var targets = new List<int>(counts.Keys);
            targets.Sort();
            var targetArr = new int[targets.Count];
            var countArr = new int[targets.Count];
            int max = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                targetArr[i] = targets[i];
                countArr[i] = counts[targets[i]];
                if (countArr[i] > max) max = countArr[i];
            }

            int executed = -1;
            if (max > 0)
            {
                int topCountTargets = 0;
                int candidate = -1;
                for (int i = 0; i < targetArr.Length; i++)
                {
                    if (countArr[i] == max)
                    {
                        topCountTargets++;
                        candidate = targetArr[i];
                    }
                }
                if (topCountTargets == 1 && candidate != -1 &&
                    (isEligibleForExecution == null || isEligibleForExecution(candidate)))
                {
                    executed = candidate;
                }
            }

            return new MeetingOutcome
            {
                ExecutedActor = executed,
                TargetActors = targetArr,
                VoteCounts = countArr,
            };
        }
    }
}
