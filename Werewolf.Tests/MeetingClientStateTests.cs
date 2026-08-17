using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingClientStateTests
    {

        [Fact]
        public void ApplyStartMeeting_ActivatesAndSetsCallerAndTimes()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(caller: 7, warpUnixMs: 5_000, endUnixMs: 125_000);

            Assert.True(s.MeetingActive);
            Assert.Equal(7, s.CallerActor);
            Assert.Equal(120_000, s.RemainingMs(5_000));
            Assert.Equal(20_000, s.RemainingMs(105_000));
            Assert.Equal(0, s.RemainingMs(999_999));
        }

        [Fact]
        public void WarpDone_TrueOnlyAtOrAfterWarp()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(caller: 1, warpUnixMs: 5_000, endUnixMs: 125_000);

            Assert.False(s.WarpDone(4_999));
            Assert.True(s.WarpDone(5_000));
        }

        [Fact]
        public void RemainingMs_WhenInactive_IsZero()
        {
            var s = new MeetingClientState();

            Assert.False(s.MeetingActive);
            Assert.Equal(0, s.RemainingMs(1_000));
        }

        [Fact]
        public void ApplyVoteProgress_ReplacesVotedSetAndUpdatesEnd()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);

            s.ApplyVoteProgress(new[] { 1, 2 }, endUnixMs: 90_000);
            Assert.Equal(new[] { 1, 2 }, System.Linq.Enumerable.OrderBy(s.VotedActors, x => x));
            Assert.Equal(90_000, s.RemainingMs(0));

            s.ApplyVoteProgress(new[] { 3 }, endUnixMs: 80_000);
            Assert.Equal(new[] { 3 }, s.VotedActors);
        }

        [Fact]
        public void ApplyResult_StoresOutcome()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);
            Assert.Null(s.Result);

            var outcome = new MeetingOutcome
            {
                ExecutedActor = 3,
                TargetActors = new[] { -1, 3 },
                VoteCounts = new[] { 1, 2 },
            };
            s.ApplyResult(outcome);

            Assert.Same(outcome, s.Result);
            Assert.Equal(3, s.Result.ExecutedActor);
        }

        [Fact]
        public void ClosedEarly_FalseBeforeResult()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);

            Assert.False(s.ClosedEarly(50_000));
        }

        [Fact]
        public void ClosedEarly_TrueWhenResultArrivesBeforeEnd()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);
            s.ApplyResult(new MeetingOutcome());

            Assert.True(s.ClosedEarly(112_000));
        }

        [Fact]
        public void ClosedEarly_FalseOnTimeoutClose_IncludingClockSlack()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);
            s.ApplyResult(new MeetingOutcome());

            Assert.False(s.ClosedEarly(120_000));
            Assert.False(s.ClosedEarly(120_000 - MeetingClientState.EarlyCloseSlackMs));
            Assert.True(s.ClosedEarly(120_000 - MeetingClientState.EarlyCloseSlackMs - 1));
        }

        [Fact]
        public void ClosedEarly_FalseWhenInactive()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);
            s.ApplyResult(new MeetingOutcome());
            s.ApplyPhase(GamePhase.Play);

            Assert.False(s.ClosedEarly(50_000));
        }

        [Fact]
        public void RowStatus_DefaultsToAlive_ForUnknownActor()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);

            Assert.Equal(RowStatus.Alive, s.GetRowStatus(1));
        }

        [Fact]
        public void ApplyPlayerDied_VoteCause_MarksExecuted()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);

            s.ApplyPlayerDied(actor: 3, cause: DeathCause.Vote);

            Assert.Equal(RowStatus.Executed, s.GetRowStatus(3));
            Assert.Equal(RowStatus.Executed, s.Rows[3]);
        }

        [Fact]
        public void ApplyPlayerDied_OtherCause_MarksDead()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);

            s.ApplyPlayerDied(actor: 4, cause: DeathCause.Other);

            Assert.Equal(RowStatus.Dead, s.GetRowStatus(4));
        }

        [Fact]
        public void ApplyPlayerLeft_MarksDisconnected()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);

            s.ApplyPlayerLeft(actor: 2);

            Assert.Equal(RowStatus.Disconnected, s.GetRowStatus(2));
        }

        [Fact]
        public void CompareRowOrder_IsIndependentOfRosterInputOrder()
        {
            var s = new MeetingClientState();
            s.ApplyPlayerDied(actor: 5, cause: DeathCause.Other);
            s.ApplyPlayerDied(actor: 2, cause: DeathCause.Vote);
            s.ApplyPlayerLeft(actor: 9);

            int[] firstClient = { 5, 7, 2, 1, 9 };
            int[] secondClient = { 9, 1, 2, 7, 5 };

            System.Array.Sort(firstClient, s.CompareRowOrder);
            System.Array.Sort(secondClient, s.CompareRowOrder);

            int[] expected = { 1, 7, 2, 5, 9 };
            Assert.Equal(expected, firstClient);
            Assert.Equal(expected, secondClient);
        }

        [Fact]
        public void ApplyPhase_PlayOrGameOver_DeactivatesMeeting()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);
            Assert.True(s.MeetingActive);

            s.ApplyPhase(GamePhase.Play);
            Assert.False(s.MeetingActive);

            s.ApplyStartMeeting(1, 0, 120_000);
            s.ApplyPhase(GamePhase.GameOver);
            Assert.False(s.MeetingActive);
        }

        [Fact]
        public void RestoreFromRoomState_ActivatesWithWarpTreatedAsDone()
        {
            var s = new MeetingClientState();

            s.RestoreFromRoomState(caller: 9, endUnixMs: 200_000);

            Assert.True(s.MeetingActive);
            Assert.Equal(9, s.CallerActor);
            Assert.True(s.WarpDone(0));
            Assert.Equal(50_000, s.RemainingMs(150_000));
        }

        [Fact]
        public void RestoreFromRoomState_ThenVoteProgress_RefreshesVoted()
        {
            var s = new MeetingClientState();
            s.RestoreFromRoomState(9, 200_000);

            Assert.Empty(s.VotedActors);
            s.ApplyVoteProgress(new[] { 9 }, 190_000);
            Assert.Contains(9, s.VotedActors);
        }

        [Fact]
        public void Reset_ClearsEverything()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 120_000);
            s.ApplyVoteProgress(new[] { 1 }, 90_000);
            s.ApplyPlayerDied(3, DeathCause.Vote);

            s.Reset();

            Assert.False(s.MeetingActive);
            Assert.Empty(s.VotedActors);
            Assert.Empty(s.Rows);
            Assert.Null(s.Result);
            Assert.Equal(0, s.RemainingMs(0));
        }

        [Fact]
        public void VotingUiReady_DelayedByIntroDuration_AfterStartMeeting()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(caller: 1, warpUnixMs: 5_000, endUnixMs: 130_000);

            Assert.True(s.WarpDone(5_000));
            Assert.False(s.VotingUiReady(5_000));
            Assert.False(s.VotingUiReady(5_000 + MeetingIntro.VotingUiDelayMs - 1));
            Assert.True(s.VotingUiReady(5_000 + MeetingIntro.VotingUiDelayMs));
        }

        [Fact]
        public void GaugeIntroReady_StartsAfterDeathReveal_BeforeVotingUi()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(caller: 1, warpUnixMs: 5_000, endUnixMs: 130_000);

            Assert.False(s.GaugeIntroReady(5_000));
            Assert.False(s.GaugeIntroReady(5_000 + MeetingIntro.GaugeRevealOffsetMs - 1));
            Assert.True(s.GaugeIntroReady(5_000 + MeetingIntro.GaugeRevealOffsetMs));
            Assert.False(s.VotingUiReady(5_000 + MeetingIntro.GaugeRevealOffsetMs));
        }

        [Fact]
        public void GaugeMoveProgress_ZeroDuringCenterReveal_OneAfterMove()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(caller: 1, warpUnixMs: 5_000, endUnixMs: 130_000);

            long uiReady = 5_000 + MeetingIntro.VotingUiDelayMs;
            Assert.Equal(0.0, s.GaugeMoveProgress(uiReady - 1));
            Assert.Equal(0.0, s.GaugeMoveProgress(uiReady));
            double mid = s.GaugeMoveProgress(uiReady + MeetingIntro.GaugeMoveMs / 2);
            Assert.InRange(mid, 0.0001, 0.9999);
            Assert.Equal(1.0, s.GaugeMoveProgress(uiReady + MeetingIntro.GaugeMoveMs));
        }

        [Fact]
        public void VotingUiReady_ImmediateOnRestore_NoRevealForLateJoiner()
        {
            var s = new MeetingClientState();
            s.RestoreFromRoomState(caller: 9, endUnixMs: 200_000);

            Assert.True(s.VotingUiReady(0));
            Assert.True(s.GaugeIntroReady(0));
            Assert.Equal(1.0, s.GaugeMoveProgress(0));
        }

        [Fact]
        public void VotingUiReady_FalseWhenInactive()
        {
            var s = new MeetingClientState();
            Assert.False(s.VotingUiReady(999_999));
            Assert.False(s.GaugeIntroReady(999_999));
            Assert.Equal(1.0, s.GaugeMoveProgress(999_999));
        }

        [Fact]
        public void IsDeadUnannounced_TrueOnlyForUnannouncedDeadOrExecuted()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 125_000);
            s.ApplyPlayerDied(2, DeathCause.Other);
            s.ApplyPlayerDied(3, DeathCause.Vote);
            s.ApplyPlayerLeft(4);

            Assert.True(s.IsDeadUnannounced(2));
            Assert.True(s.IsDeadUnannounced(3));
            Assert.False(s.IsDeadUnannounced(4));
            Assert.False(s.IsDeadUnannounced(5));
        }

        [Fact]
        public void MarkAllDeadAnnounced_ExcludesFromNextReveal()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 125_000);
            s.ApplyPlayerDied(2, DeathCause.Other);

            s.MarkAllDeadAnnounced();
            Assert.False(s.IsDeadUnannounced(2));

            s.ApplyPlayerDied(5, DeathCause.Other);
            Assert.True(s.IsDeadUnannounced(5));
            Assert.False(s.IsDeadUnannounced(2));
        }

        [Fact]
        public void ApplyPhase_MeetingEnd_MarksInMeetingDeathsAnnounced()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 125_000);
            s.MarkAllDeadAnnounced();

            s.ApplyPlayerDied(3, DeathCause.Vote);
            s.ApplyPlayerDied(4, DeathCause.Other);
            s.ApplyPhase(GamePhase.Play);

            Assert.False(s.IsDeadUnannounced(3));
            Assert.False(s.IsDeadUnannounced(4));
        }

        [Fact]
        public void MarkAllDeadAnnounced_CollectsOnlyNewlyAnnounced()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 125_000);
            s.ApplyPlayerDied(2, DeathCause.Other);
            s.ApplyPlayerDied(3, DeathCause.Vote);
            s.ApplyPlayerLeft(4);

            var first = new List<int>();
            s.MarkAllDeadAnnounced(first);
            first.Sort();
            Assert.Equal(new[] { 2, 3 }, first);

            s.ApplyPlayerDied(5, DeathCause.Other);
            var second = new List<int>();
            s.MarkAllDeadAnnounced(second);
            Assert.Equal(new[] { 5 }, second);
        }

        [Fact]
        public void ApplyPhase_CollectsNewlyAnnouncedOnMeetingEnd()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 125_000);
            s.MarkAllDeadAnnounced();

            s.ApplyPlayerDied(3, DeathCause.Vote);
            var announced = new List<int>();
            s.ApplyPhase(GamePhase.Play, announced);
            Assert.Equal(new[] { 3 }, announced);

            s.ApplyPlayerDied(6, DeathCause.Other);
            var inactive = new List<int>();
            s.ApplyPhase(GamePhase.Play, inactive);
            Assert.Empty(inactive);
        }

        [Fact]
        public void Reset_ClearsAnnouncedLedger()
        {
            var s = new MeetingClientState();
            s.ApplyStartMeeting(1, 0, 125_000);
            s.ApplyPlayerDied(2, DeathCause.Other);
            s.MarkAllDeadAnnounced();

            s.Reset();

            s.ApplyStartMeeting(1, 0, 125_000);
            s.ApplyPlayerDied(2, DeathCause.Other);
            Assert.True(s.IsDeadUnannounced(2));
        }

        [Fact]
        public void FullNotificationSequence_BuildsExpectedViewState()
        {
            var s = new MeetingClientState();

            s.ApplyStartMeeting(caller: 1, warpUnixMs: 5_000, endUnixMs: 125_000);
            s.ApplyVoteProgress(new[] { 1 }, 100_000);
            s.ApplyVoteProgress(new[] { 1, 2 }, 80_000);
            s.ApplyResult(new MeetingOutcome
            {
                ExecutedActor = 2,
                TargetActors = new[] { 2 },
                VoteCounts = new[] { 2 },
            });
            s.ApplyPlayerDied(2, DeathCause.Vote);

            Assert.True(s.MeetingActive);
            Assert.True(s.WarpDone(5_000));
            Assert.Equal(RowStatus.Executed, s.GetRowStatus(2));
            Assert.Equal(RowStatus.Alive, s.GetRowStatus(1));
            Assert.Equal(2, s.Result.ExecutedActor);
            Assert.Equal(new[] { 1, 2 }, System.Linq.Enumerable.OrderBy(s.VotedActors, x => x));

            s.ApplyPhase(GamePhase.Play);
            Assert.False(s.MeetingActive);
        }
    }
}
