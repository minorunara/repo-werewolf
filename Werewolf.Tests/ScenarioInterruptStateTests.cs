using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioInterruptStateTests
    {
        private static readonly TextId[] Lines = { TextId.ScenarioBrokenNotice };

        private static readonly TextId[] TooClose =
        {
            TextId.ScenarioBlastDenyTooClose1, TextId.ScenarioBlastDenyTooClose2,
        };

        private static readonly TextId[] TooFar = { TextId.ScenarioBlastDenyTooFar1 };

        [Fact]
        public void Fresh_ResolvesToNone()
        {
            var state = new ScenarioInterruptState();

            Assert.Equal(ScenarioInterrupt.None, state.Resolve(0));
            Assert.False(state.Broken);
            Assert.False(state.AbortPending);
            Assert.Equal(0, state.BrokenExitAtMs);
            Assert.Empty(state.BrokenLines);
        }

        [Fact]
        public void Resolve_PicksByPriority_BrokenAbortLeashExtractionEarlyStartWrongTargetBlastDenied()
        {
            var state = new ScenarioInterruptState();
            state.Notice(ScenarioInterrupt.BlastDenied, 100);
            Assert.Equal(ScenarioInterrupt.BlastDenied, state.Resolve(10));

            state.Notice(ScenarioInterrupt.WrongTarget, 100);
            Assert.Equal(ScenarioInterrupt.WrongTarget, state.Resolve(10));

            state.Notice(ScenarioInterrupt.EarlyStart, 100);
            Assert.Equal(ScenarioInterrupt.EarlyStart, state.Resolve(10));

            state.Notice(ScenarioInterrupt.ExtractionNag, 100);
            Assert.Equal(ScenarioInterrupt.ExtractionNag, state.Resolve(10));

            state.Notice(ScenarioInterrupt.LeashNag, 100);
            Assert.Equal(ScenarioInterrupt.LeashNag, state.Resolve(10));

            state.RequestAbort();
            Assert.Equal(ScenarioInterrupt.AbortConfirm, state.Resolve(10));

            state.Break(ScenarioBreakKind.MeetingLost, Lines, 500);
            Assert.Equal(ScenarioInterrupt.Broken, state.Resolve(10));
        }

        [Theory]
        [InlineData(ScenarioInterrupt.LeashNag)]
        [InlineData(ScenarioInterrupt.ExtractionNag)]
        [InlineData(ScenarioInterrupt.EarlyStart)]
        [InlineData(ScenarioInterrupt.WrongTarget)]
        [InlineData(ScenarioInterrupt.AlreadyOnTarget)]
        [InlineData(ScenarioInterrupt.BlastDenied)]
        [InlineData(ScenarioInterrupt.CorpseHint)]
        public void Notice_ShowsUntilItsDeadlineThenNone(ScenarioInterrupt kind)
        {
            var state = new ScenarioInterruptState();
            state.Notice(kind, 100);

            Assert.Equal(kind, state.Resolve(99));
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(100));
        }

        [Theory]
        [InlineData(ScenarioInterrupt.None)]
        [InlineData(ScenarioInterrupt.Broken)]
        [InlineData(ScenarioInterrupt.AbortConfirm)]
        [InlineData(ScenarioInterrupt.MapAside)]
        public void Notice_IgnoresKindsThatAreNotTimedNotices(ScenarioInterrupt kind)
        {
            var state = new ScenarioInterruptState();
            state.Notice(kind, 100);

            Assert.Equal(ScenarioInterrupt.None, state.Resolve(10));
            Assert.False(state.Broken);
            Assert.False(state.AbortPending);
        }

        [Theory]
        [InlineData(ScenarioInterrupt.AbortConfirm)]
        [InlineData(ScenarioInterrupt.ExtractionNag)]
        [InlineData(ScenarioInterrupt.EarlyStart)]
        [InlineData(ScenarioInterrupt.WrongTarget)]
        [InlineData(ScenarioInterrupt.AlreadyOnTarget)]
        [InlineData(ScenarioInterrupt.BlastDenied)]
        [InlineData(ScenarioInterrupt.CorpseHint)]
        public void Dismiss_ClosesTheKindsThatAdvanceMayClose(ScenarioInterrupt kind)
        {
            var state = new ScenarioInterruptState();
            if (kind == ScenarioInterrupt.AbortConfirm) state.RequestAbort();
            else state.Notice(kind, 100);
            Assert.Equal(kind, state.Resolve(10));

            Assert.True(state.Dismiss(kind));
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(10));
            Assert.False(state.Dismiss(kind));
        }

        [Fact]
        public void Dismiss_DoesNotCloseTheLeashNag()
        {
            var state = new ScenarioInterruptState();
            state.Notice(ScenarioInterrupt.LeashNag, 100);

            Assert.False(state.Dismiss(ScenarioInterrupt.LeashNag));
            Assert.Equal(ScenarioInterrupt.LeashNag, state.Resolve(10));
        }

        [Theory]
        [InlineData(ScenarioInterrupt.None)]
        [InlineData(ScenarioInterrupt.Broken)]
        [InlineData(ScenarioInterrupt.MapAside)]
        public void Dismiss_DoesNothingForKindsThatAreNotHeldHere(ScenarioInterrupt kind)
        {
            var state = new ScenarioInterruptState();
            state.Break(ScenarioBreakKind.MeetingLost, Lines, 500);

            Assert.False(state.Dismiss(kind));
            Assert.True(state.Broken);
        }

        [Fact]
        public void Dismiss_ClosesOnlyTheNamedKind()
        {
            var state = new ScenarioInterruptState();
            state.Notice(ScenarioInterrupt.ExtractionNag, 100);
            state.Notice(ScenarioInterrupt.EarlyStart, 100);

            Assert.True(state.Dismiss(ScenarioInterrupt.ExtractionNag));
            Assert.Equal(ScenarioInterrupt.EarlyStart, state.Resolve(10));
        }

        [Fact]
        public void Break_RecordsKindLinesAndExitTime()
        {
            var state = new ScenarioInterruptState();

            Assert.True(state.Break(ScenarioBreakKind.QuotaBagLost, Lines, 700));
            Assert.True(state.Broken);
            Assert.Equal(700, state.BrokenExitAtMs);
            Assert.Equal(ScenarioBreakKind.QuotaBagLost, state.BrokenKind);
            Assert.Equal(Lines, state.BrokenLines);
        }

        [Fact]
        public void Break_WithNoLines_LeavesBrokenLinesEmpty()
        {
            var state = new ScenarioInterruptState();
            state.Break(ScenarioBreakKind.ResultBeforeTally, null, 700);
            Assert.Empty(state.BrokenLines);

            state.Reset();
            state.Break(ScenarioBreakKind.ResultBeforeTally, new TextId[0], 700);
            Assert.Empty(state.BrokenLines);
        }

        [Fact]
        public void Break_ClearsAbortAndEveryNotice()
        {
            var state = new ScenarioInterruptState();
            state.RequestAbort();
            state.Notice(ScenarioInterrupt.LeashNag, 100);
            state.Notice(ScenarioInterrupt.ExtractionNag, 100);
            state.Notice(ScenarioInterrupt.EarlyStart, 100);
            state.Notice(ScenarioInterrupt.WrongTarget, 100);
            state.Notice(ScenarioInterrupt.BlastDenied, 100);

            state.Break(ScenarioBreakKind.MeetingLost, Lines, 500);

            Assert.False(state.AbortPending);
            Assert.Equal(ScenarioInterrupt.Broken, state.Resolve(10));
            Assert.False(state.Dismiss(ScenarioInterrupt.ExtractionNag));
            Assert.False(state.Dismiss(ScenarioInterrupt.EarlyStart));
            Assert.False(state.Dismiss(ScenarioInterrupt.WrongTarget));
            Assert.False(state.Dismiss(ScenarioInterrupt.BlastDenied));
        }

        [Fact]
        public void Break_IsFirstWins()
        {
            var state = new ScenarioInterruptState();
            Assert.True(state.Break(ScenarioBreakKind.MeetingLost, Lines, 500));

            Assert.False(state.Break(ScenarioBreakKind.QuotaBagLost, null, 900));
            Assert.Equal(ScenarioBreakKind.MeetingLost, state.BrokenKind);
            Assert.Equal(500, state.BrokenExitAtMs);
            Assert.Equal(Lines, state.BrokenLines);
        }

        [Fact]
        public void Break_IsHeldByAFlagNotByTheExitTime()
        {
            var state = new ScenarioInterruptState();
            state.Break(ScenarioBreakKind.MeetingLost, Lines, 0);

            Assert.True(state.Broken);
            Assert.Equal(0, state.BrokenExitAtMs);
            Assert.Equal(ScenarioInterrupt.Broken, state.Resolve(0));
        }

        [Fact]
        public void Abort_CannotBeRaisedOverBrokenAndDoesNotOutrankIt()
        {
            var state = new ScenarioInterruptState();
            state.Break(ScenarioBreakKind.MeetingLost, Lines, 500);
            state.RequestAbort();

            Assert.Equal(ScenarioInterrupt.Broken, state.Resolve(10));
        }

        [Fact]
        public void Reset_ClearsEverything()
        {
            var state = new ScenarioInterruptState();
            state.Break(ScenarioBreakKind.MeetingLost, Lines, 500);
            state.Reset();

            Assert.False(state.Broken);
            Assert.Equal(0, state.BrokenExitAtMs);
            Assert.Empty(state.BrokenLines);
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(0));

            state.RequestAbort();
            state.Notice(ScenarioInterrupt.LeashNag, 100);
            state.Notice(ScenarioInterrupt.ExtractionNag, 100);
            state.Notice(ScenarioInterrupt.EarlyStart, 100);
            state.Notice(ScenarioInterrupt.WrongTarget, 100);
            state.Notice(ScenarioInterrupt.BlastDenied, 100);
            state.Reset();

            Assert.False(state.AbortPending);
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(0));
        }

        [Fact]
        public void NoticeKinds_AreExactlyTheKindsThatHaveADuration()
        {
            Assert.Equal(ScenarioInterruptState.NoticeKinds.Count,
                System.Linq.Enumerable.Count(System.Linq.Enumerable.Distinct(ScenarioInterruptState.NoticeKinds)));

            foreach (ScenarioInterrupt kind in System.Enum.GetValues(typeof(ScenarioInterrupt)))
            {
                bool timed = System.Linq.Enumerable.Contains(ScenarioInterruptState.NoticeKinds, kind);
                Assert.Equal(timed, ScenarioInterruptState.NoticeDurationMs(kind) > 0);
            }
        }

        [Theory]
        [InlineData(ScenarioInterrupt.LeashNag)]
        [InlineData(ScenarioInterrupt.BlastDenied)]
        public void Raise_ShowsForTheKindsDurationFromNow(ScenarioInterrupt kind)
        {
            var state = new ScenarioInterruptState();
            long duration = ScenarioInterruptState.NoticeDurationMs(kind);
            state.Raise(kind, 1000);

            Assert.Equal(kind, state.Resolve(1000 + duration - 1));
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(1000 + duration));
        }

        [Fact]
        public void Defer_ShowsNothingUntilStarted_ThenCountsFromTheStart()
        {
            var state = new ScenarioInterruptState();
            state.Defer(ScenarioInterrupt.EarlyStart);
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(0));
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(100000));

            long duration = ScenarioInterruptState.NoticeDurationMs(ScenarioInterrupt.EarlyStart);
            state.StartDeferred(5000);
            Assert.Equal(ScenarioInterrupt.EarlyStart, state.Resolve(5000));
            Assert.Equal(ScenarioInterrupt.EarlyStart, state.Resolve(5000 + duration - 1));
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(5000 + duration));
        }

        [Fact]
        public void StartDeferred_EmptiesTheHold()
        {
            var state = new ScenarioInterruptState();
            state.Defer(ScenarioInterrupt.WrongTarget);
            state.StartDeferred(0);
            Assert.True(state.Dismiss(ScenarioInterrupt.WrongTarget));

            state.StartDeferred(10);
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(10));
        }

        [Fact]
        public void Defer_StartsEveryHeldKindAtOnce_InPriorityOrder()
        {
            var state = new ScenarioInterruptState();
            state.Defer(ScenarioInterrupt.BlastDenied);
            state.Defer(ScenarioInterrupt.EarlyStart);
            state.StartDeferred(0);

            Assert.Equal(ScenarioInterrupt.EarlyStart, state.Resolve(0));
            Assert.True(state.Dismiss(ScenarioInterrupt.EarlyStart));
            Assert.Equal(ScenarioInterrupt.BlastDenied, state.Resolve(0));
        }

        [Theory]
        [InlineData(ScenarioInterrupt.None)]
        [InlineData(ScenarioInterrupt.Broken)]
        [InlineData(ScenarioInterrupt.AbortConfirm)]
        [InlineData(ScenarioInterrupt.MapAside)]
        public void Defer_IgnoresKindsThatAreNotTimedNotices(ScenarioInterrupt kind)
        {
            var state = new ScenarioInterruptState();
            state.Defer(kind);
            state.StartDeferred(0);

            Assert.Equal(ScenarioInterrupt.None, state.Resolve(0));
            Assert.False(state.Broken);
            Assert.False(state.AbortPending);
        }

        [Fact]
        public void Break_DropsDeferredNotices()
        {
            var state = new ScenarioInterruptState();
            state.Defer(ScenarioInterrupt.EarlyStart);
            state.Break(ScenarioBreakKind.MeetingLost, Lines, 500);
            state.Reset();

            state.StartDeferred(0);
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(0));
        }

        [Fact]
        public void Reset_DropsDeferredNotices()
        {
            var state = new ScenarioInterruptState();
            state.Defer(ScenarioInterrupt.BlastDenied);
            state.Reset();

            state.StartDeferred(0);
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(0));
        }

        [Fact]
        public void IsNotice_AgreesWithNoticeKindsForEveryKind()
        {
            foreach (ScenarioInterrupt kind in System.Enum.GetValues(typeof(ScenarioInterrupt)))
            {
                bool listed = System.Linq.Enumerable.Contains(ScenarioInterruptState.NoticeKinds, kind);
                Assert.Equal(listed, ScenarioInterruptState.IsNotice(kind));
            }
        }

        [Fact]
        public void Notice_KeepsTheLinesAndArgItWasRaisedWith()
        {
            var state = new ScenarioInterruptState();
            state.Notice(ScenarioInterrupt.BlastDenied, 100, TooClose, 3);

            Assert.Equal(TooClose, state.NoticeLines(ScenarioInterrupt.BlastDenied));
            Assert.Equal(3, state.NoticeArg(ScenarioInterrupt.BlastDenied));
        }

        [Fact]
        public void NoticeLines_AreEmptyAndArgIsZeroWithoutASnapshot()
        {
            var state = new ScenarioInterruptState();

            Assert.Empty(state.NoticeLines(ScenarioInterrupt.BlastDenied));
            Assert.Equal(0, state.NoticeArg(ScenarioInterrupt.BlastDenied));
            Assert.Empty(state.NoticeLines(ScenarioInterrupt.MapAside));
            Assert.Equal(0, state.NoticeArg(ScenarioInterrupt.MapAside));
        }

        [Fact]
        public void StartDeferred_KeepsTheSnapshotTakenWhenItWasDeferred()
        {
            var state = new ScenarioInterruptState();
            state.Defer(ScenarioInterrupt.BlastDenied, TooClose, 3);
            state.StartDeferred(1000);

            Assert.Equal(ScenarioInterrupt.BlastDenied, state.Resolve(1000));
            Assert.Equal(TooClose, state.NoticeLines(ScenarioInterrupt.BlastDenied));
            Assert.Equal(3, state.NoticeArg(ScenarioInterrupt.BlastDenied));
        }

        [Fact]
        public void Raise_ReplacesTheEarlierSnapshot()
        {
            var state = new ScenarioInterruptState();
            state.Raise(ScenarioInterrupt.BlastDenied, 0, TooClose, 3);
            state.Raise(ScenarioInterrupt.BlastDenied, 0, TooFar, 4);

            Assert.Equal(TooFar, state.NoticeLines(ScenarioInterrupt.BlastDenied));
            Assert.Equal(4, state.NoticeArg(ScenarioInterrupt.BlastDenied));
        }

        [Fact]
        public void PressResponseKinds_AreASubsetOfTheNotices()
        {
            Assert.NotEmpty(ScenarioInterruptState.PressResponseKinds);
            foreach (ScenarioInterrupt kind in ScenarioInterruptState.PressResponseKinds)
            {
                Assert.Contains(kind, ScenarioInterruptState.NoticeKinds);
            }
        }

        [Theory]
        [InlineData(ScenarioInterrupt.WrongTarget)]
        [InlineData(ScenarioInterrupt.AlreadyOnTarget)]
        [InlineData(ScenarioInterrupt.BlastDenied)]
        [InlineData(ScenarioInterrupt.BlastDeniedMoving)]
        public void DropPressResponses_ClosesTheOnesOnScreen(ScenarioInterrupt kind)
        {
            var state = new ScenarioInterruptState();
            state.Notice(kind, 100, TooClose, 3);

            state.DropPressResponses();
            Assert.Equal(ScenarioInterrupt.None, state.Resolve(10));
        }

        [Theory]
        [InlineData(ScenarioInterrupt.LeashNag)]
        [InlineData(ScenarioInterrupt.ExtractionNag)]
        [InlineData(ScenarioInterrupt.EarlyStart)]
        public void DropPressResponses_LeavesTheOtherNotices(ScenarioInterrupt kind)
        {
            var state = new ScenarioInterruptState();
            state.Notice(kind, 100);

            state.DropPressResponses();
            Assert.Equal(kind, state.Resolve(10));
        }

        [Fact]
        public void DropPressResponses_KeepsWhatIsStillOnHold()
        {
            var state = new ScenarioInterruptState();
            state.Defer(ScenarioInterrupt.BlastDenied, TooClose, 3);

            state.DropPressResponses();
            state.StartDeferred(0);

            Assert.Equal(ScenarioInterrupt.BlastDenied, state.Resolve(0));
            Assert.Equal(TooClose, state.NoticeLines(ScenarioInterrupt.BlastDenied));
        }
    }
}
