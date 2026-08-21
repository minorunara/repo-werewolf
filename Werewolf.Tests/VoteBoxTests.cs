using System;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class VoteBoxTests
    {
        private static VoteBox Box(params int[] aliveActors) => new VoteBox(aliveActors);

        [Fact]
        public void TryCast_FirstVote_Accepted()
        {
            var box = Box(1, 2, 3);

            Assert.Equal(VoteRejectReason.None, box.TryCast(voterActor: 1, targetActor: 2));
            Assert.Contains(1, box.VotedActors);
        }

        [Fact]
        public void TryCast_SecondVoteBySameVoter_Rejected()
        {
            var box = Box(1, 2, 3);
            box.TryCast(1, 2);

            Assert.Equal(VoteRejectReason.AlreadyVoted, box.TryCast(1, 3));
        }

        [Fact]
        public void TryCast_Skip_Accepted()
        {
            var box = Box(1, 2, 3);

            Assert.Equal(VoteRejectReason.None, box.TryCast(voterActor: 1, targetActor: -1));
            Assert.Contains(1, box.VotedActors);
        }

        [Fact]
        public void TryCast_UnknownVoter_Rejected()
        {
            var box = Box(1, 2, 3);

            Assert.Equal(VoteRejectReason.VoterUnknown, box.TryCast(voterActor: 99, targetActor: 1));
        }

        [Fact]
        public void TryCast_RemovedVoter_RejectedAsDead()
        {
            var box = Box(1, 2, 3);
            box.RemoveVoter(1);

            Assert.Equal(VoteRejectReason.VoterDead, box.TryCast(voterActor: 1, targetActor: 2));
        }

        [Fact]
        public void TryCast_UnknownTarget_Rejected()
        {
            var box = Box(1, 2, 3);

            Assert.Equal(VoteRejectReason.TargetUnknown, box.TryCast(voterActor: 1, targetActor: 42));
        }

        [Fact]
        public void TryCast_DeadTarget_Rejected()
        {
            var box = Box(1, 2, 3);
            box.RemoveTarget(2, disconnected: false);

            Assert.Equal(VoteRejectReason.TargetDead, box.TryCast(voterActor: 1, targetActor: 2));
        }

        [Fact]
        public void TryCast_DisconnectedTarget_Accepted()
        {
            var box = Box(1, 2, 3);
            box.RemoveTarget(2, disconnected: true);

            Assert.Equal(VoteRejectReason.None, box.TryCast(voterActor: 1, targetActor: 2));
            Assert.Contains(1, box.VotedActors);
        }

        [Fact]
        public void RemoveTarget_DoesNotInvalidateAlreadyCastVote()
        {
            var box = Box(1, 2, 3);
            box.TryCast(1, 2);
            box.RemoveTarget(2, disconnected: false);

            Assert.Contains(1, box.VotedActors);
        }

        [Fact]
        public void AllVotesIn_TrueOnlyWhenEveryExpectedVoterVoted()
        {
            var box = Box(1, 2, 3);
            Assert.False(box.AllVotesIn);

            box.TryCast(1, -1);
            box.TryCast(2, -1);
            Assert.False(box.AllVotesIn);

            box.TryCast(3, -1);
            Assert.True(box.AllVotesIn);
        }

        [Fact]
        public void AllVotesIn_RemovingLastPendingVoter_MakesItTrue()
        {
            var box = Box(1, 2, 3);
            box.TryCast(1, -1);
            box.TryCast(2, -1);
            Assert.False(box.AllVotesIn);

            box.RemoveVoter(3);
            Assert.True(box.AllVotesIn);
        }

        [Fact]
        public void Tally_AbstainersCountAsSkip()
        {
            var box = Box(1, 2, 3, 4);
            box.TryCast(1, 2);

            MeetingOutcome outcome = box.Tally(_ => true);

            Assert.Equal(-1, outcome.ExecutedActor);
            Assert.Contains(-1, outcome.TargetActors);
            int skipIdx = Array.IndexOf(outcome.TargetActors, -1);
            Assert.Equal(3, outcome.VoteCounts[skipIdx]);
        }

        [Fact]
        public void Tally_SolePluralityPlayer_IsExecuted()
        {
            var box = Box(1, 2, 3, 4, 5);
            box.TryCast(1, 3);
            box.TryCast(2, 3);
            box.TryCast(4, 3);
            box.TryCast(5, 2);

            MeetingOutcome outcome = box.Tally(_ => true);

            Assert.Equal(3, outcome.ExecutedActor);
        }

        [Fact]
        public void Tally_TieBetweenPlayers_NoExecution()
        {
            var box = Box(1, 2, 3, 4);
            box.TryCast(1, 3);
            box.TryCast(2, 3);
            box.TryCast(3, 4);
            box.TryCast(4, 4);

            MeetingOutcome outcome = box.Tally(_ => true);

            Assert.Equal(-1, outcome.ExecutedActor);
        }

        [Fact]
        public void Tally_SkipIsMostVoted_NoExecution()
        {
            var box = Box(1, 2, 3, 4);
            box.TryCast(1, -1);
            box.TryCast(2, -1);
            box.TryCast(3, -1);
            box.TryCast(4, 2);

            MeetingOutcome outcome = box.Tally(_ => true);

            Assert.Equal(-1, outcome.ExecutedActor);
            Assert.Contains(-1, outcome.TargetActors);
        }

        [Fact]
        public void Tally_TieBetweenPlayerAndSkip_NoExecution()
        {
            var box = Box(1, 2, 3, 4);
            box.TryCast(1, 2);
            box.TryCast(2, 2);
            box.TryCast(3, -1);
            box.TryCast(4, -1);

            MeetingOutcome outcome = box.Tally(_ => true);

            Assert.Equal(-1, outcome.ExecutedActor);
        }

        [Fact]
        public void Tally_NoVotesAtAll_AllCountAsSkip()
        {
            var box = Box(1, 2, 3);

            MeetingOutcome outcome = box.Tally(_ => true);

            Assert.Equal(-1, outcome.ExecutedActor);
            Assert.Equal(new[] { -1 }, outcome.TargetActors);
            Assert.Equal(new[] { 3 }, outcome.VoteCounts);
        }

        [Fact]
        public void Tally_TopTargetIneligibleAtTally_NoExecutionButBreakdownRetained()
        {
            var box = Box(1, 2, 3, 4);
            box.TryCast(1, 3);
            box.TryCast(2, 3);
            box.TryCast(4, 3);

            MeetingOutcome outcome = box.Tally(isEligibleForExecution: a => a != 3);

            Assert.Equal(-1, outcome.ExecutedActor);
            Assert.Contains(3, outcome.TargetActors);
            int idx = Array.IndexOf(outcome.TargetActors, 3);
            Assert.Equal(3, outcome.VoteCounts[idx]);
        }

        [Fact]
        public void Tally_BreakdownArraysAreParallelAndComplete()
        {
            var box = Box(1, 2, 3, 4, 5);
            box.TryCast(1, 2);
            box.TryCast(2, 2);
            box.TryCast(3, -1);
            box.TryCast(4, 4);
            box.TryCast(5, 4);

            MeetingOutcome outcome = box.Tally(_ => true);

            Assert.Equal(outcome.TargetActors.Length, outcome.VoteCounts.Length);
            int total = outcome.VoteCounts.Sum();
            Assert.Equal(5, total);
            Assert.Equal(-1, outcome.ExecutedActor);
        }

        [Fact]
        public void SkipVotes_ReturnsSkipRowCount()
        {
            var box = Box(1, 2, 3, 4);
            box.TryCast(1, 2);
            box.TryCast(2, -1);
            box.TryCast(3, -1);

            MeetingOutcome outcome = box.Tally(_ => true);

            int skipIdx = Array.IndexOf(outcome.TargetActors, -1);
            Assert.Equal(outcome.VoteCounts[skipIdx], outcome.SkipVotes);
            Assert.Equal(3, outcome.SkipVotes);
        }

        [Fact]
        public void SkipVotes_NoSkipRowOrNoBreakdown_IsZero()
        {
            var withoutSkip = new MeetingOutcome
            {
                ExecutedActor = 2,
                TargetActors = new[] { 2, 3 },
                VoteCounts = new[] { 2, 1 },
            };
            Assert.Equal(0, withoutSkip.SkipVotes);

            Assert.Equal(0, new MeetingOutcome().SkipVotes);
        }
    }
}
