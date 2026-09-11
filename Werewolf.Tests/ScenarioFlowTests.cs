using System;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioFlowTests
    {
        private const TextId A = TextId.ScenarioAdvanceHint;
        private const TextId B = TextId.ScenarioAbortHint;
        private const TextId C = TextId.ScenarioAbortCancelHint;
        private const TextId D = TextId.ScenarioNeedOpenMapNag;

        private static ScenarioFlow Flow(params ScenarioPage[] pages) => new ScenarioFlow(pages);

        private static ScenarioDefinition Definition(params ScenarioPage[] pages)
            => new ScenarioDefinition(ScenarioId.MeetingVote, default, default, pages,
                timing: default, notices: default, watch: default);

        [Fact]
        public void WithNag_RaisesTheReminderCueAndCarriesTheLines()
        {
            ScenarioPage plain = ScenarioPage.Say(ScenarioSignal.Advance, A);
            ScenarioPage prompt = plain.WithNag(B, C);

            Assert.Empty(plain.NagLines);
            Assert.Equal(ScenarioCue.None, plain.Cue);
            Assert.Equal(ScenarioCue.ActionNag, prompt.Cue);
            Assert.Equal(new[] { B, C }, prompt.NagLines);
        }

        [Fact]
        public void WithNag_RejectsEmptyLines()
        {
            Assert.Throws<ArgumentException>(
                () => ScenarioPage.Say(ScenarioSignal.Advance, A).WithNag());
        }

        [Fact]
        public void WithCue_RejectsTheReminderCue()
        {
            Assert.Throws<ArgumentException>(
                () => ScenarioPage.Say(ScenarioSignal.Advance, A).WithCue(ScenarioCue.ActionNag));
        }

        [Fact]
        public void Definition_ExpandsANagIntoTheFollowingReminderPage()
        {
            var definition = Definition(
                ScenarioPage.Say(ScenarioSignal.Advance, A)
                    .At(ScenarioAnchor.PlayHudRight).PointsAt(ScenarioHighlight.RecordIcon)
                    .Requires(ScenarioNeed.MapToolOpen)
                    .SkipWhen(ScenarioSignal.RecordTurnedOff)
                    .InMeeting(1)
                    .Does(ScenarioEntryAction.SpawnQuotaBag)
                    .Marks(ScenarioMilestone.ActionPrompted)
                    .WithNag(B, C));

            Assert.Equal(2, definition.Pages.Count);
            ScenarioPage prompt = definition.Pages[0];
            ScenarioPage nag = definition.Pages[1];

            Assert.Equal(ScenarioCue.ActionNag, prompt.Cue);
            Assert.Equal(ScenarioSignal.ActionNag, nag.Gate);
            Assert.Equal(new[] { B, C }, nag.Lines);

            Assert.Equal(prompt.Anchor, nag.Anchor);
            Assert.Equal(prompt.Highlight, nag.Highlight);
            Assert.Equal(prompt.Need, nag.Need);
            Assert.Equal(prompt.SkipIf, nag.SkipIf);
            Assert.Equal(prompt.MeetingIndex, nag.MeetingIndex);

            Assert.Equal(ScenarioCue.None, nag.Cue);
            Assert.Empty(nag.EntrySteps);
            Assert.Equal(ScenarioMilestone.None, nag.Milestone);
        }

        [Fact]
        public void Definition_CarriesTheTargetMarkIntoTheReminderPage()
        {
            var definition = Definition(
                ScenarioPage.Say(ScenarioSignal.Advance, A)
                    .BombTarget(3).MarksBombTarget()
                    .WithNag(B));

            Assert.True(definition.Pages[0].MarkBombTarget);
            Assert.True(definition.Pages[1].MarkBombTarget);
            Assert.Equal(3, definition.Pages[1].BombTargetParticipantId);
        }

        [Fact]
        public void Definition_LeavesTheTargetMarkOffPagesThatDoNotAskForIt()
        {
            var definition = Definition(
                ScenarioPage.Say(ScenarioSignal.Advance, A).BombTarget(3).WithNag(B));

            Assert.False(definition.Pages[0].MarkBombTarget);
            Assert.False(definition.Pages[1].MarkBombTarget);
        }

        [Fact]
        public void Definition_LeavesPagesWithoutANagAlone()
        {
            var definition = Definition(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B));

            Assert.Equal(2, definition.Pages.Count);
            Assert.Equal(A, definition.Pages[0].Text);
            Assert.Equal(B, definition.Pages[1].Text);
        }

        [Fact]
        public void Ctor_RejectsNullOrEmptyPages()
        {
            Assert.Throws<ArgumentNullException>(() => new ScenarioFlow(null));
            Assert.Throws<ArgumentException>(() => new ScenarioFlow(new ScenarioPage[0]));
        }

        [Fact]
        public void Advance_MovesOnePageAtATime()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B),
                ScenarioPage.Say(ScenarioSignal.Advance, C));

            Assert.Equal(A, flow.Current.Text);
            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(B, flow.Current.Text);
            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(C, flow.Current.Text);
        }

        [Fact]
        public void Advance_DoesNotPassAnEventGate()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.MeetingOpened, B));

            for (int i = 0; i < 10; i++) Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(A, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(B, flow.Current.Text);
        }

        [Fact]
        public void UnrelatedSignal_DoesNotMove()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.MeetingOpened, B));

            Assert.False(flow.Feed(ScenarioSignal.VoteAccepted));
            Assert.False(flow.Feed(ScenarioSignal.ResultScreenShown));
            Assert.Equal(A, flow.Current.Text);
        }

        [Fact]
        public void EventSignal_IsLatchedUntilTheReaderCatchesUp()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B),
                ScenarioPage.Say(ScenarioSignal.MeetingOpened, C));

            Assert.False(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.True(flow.IsLatched(ScenarioSignal.MeetingOpened));
            Assert.Equal(A, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(C, flow.Current.Text);
        }

        [Fact]
        public void LatchedSignal_DoesNotSkipTheTextPageBeforeIt()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B),
                ScenarioPage.Say(ScenarioSignal.MeetingOpened, C));

            flow.Feed(ScenarioSignal.MeetingOpened);

            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(B, flow.Current.Text);
        }

        [Fact]
        public void SkipIf_PassesThroughWithoutInputOrGate()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B).SkipWhen(ScenarioSignal.VoteAccepted),
                ScenarioPage.Say(ScenarioSignal.VoteAccepted, C));

            flow.Feed(ScenarioSignal.VoteAccepted);

            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(C, flow.Current.Text);
        }

        [Fact]
        public void SkipIf_DoesNotInterruptChatCueBeforeCompletion()
        {
            var flow = Flow(
                ScenarioPage.Wait(ScenarioSignal.Advance).WithCue(ScenarioCue.ChatScript),
                ScenarioPage.Say(ScenarioSignal.ChatScriptDone, B)
                    .SkipWhen(ScenarioSignal.VoteAccepted),
                ScenarioPage.Wait(ScenarioSignal.VoteAccepted).WithCue(ScenarioCue.BotVotes));

            Assert.False(flow.Feed(ScenarioSignal.VoteAccepted));
            Assert.Equal(ScenarioCue.ChatScript, flow.Current.Cue);
            Assert.False(flow.AdvancesOnInput);

            Assert.True(flow.Feed(ScenarioSignal.ChatScriptDone));
            Assert.Equal(ScenarioCue.BotVotes, flow.Current.Cue);
        }

        [Fact]
        public void MustRead_HoldsThePageUntilAdvanceEvenWithTheNextGateLatched()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A).MustBeRead(),
                ScenarioPage.Say(ScenarioSignal.ResultScreenShown, B));

            Assert.False(flow.Feed(ScenarioSignal.ResultScreenShown));
            Assert.Equal(A, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(B, flow.Current.Text);
        }

        [Fact]
        public void AdvancesOnInput_TellsReadablePagesFromActionGatedOnes()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.MeetingOpened, B),
                ScenarioPage.Say(ScenarioSignal.Advance, C));

            Assert.False(flow.AdvancesOnInput);

            flow.Feed(ScenarioSignal.MeetingOpened);
            Assert.Equal(B, flow.Current.Text);
            Assert.True(flow.AdvancesOnInput);

            flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.AdvancesOnInput);
        }

        [Fact]
        public void AdvancesOnInput_IsFalseWhileAnActionIsStillPending()
        {
            var flow = Flow(
                ScenarioPage.Wait(ScenarioSignal.MapClosed),
                ScenarioPage.Say(ScenarioSignal.MapClosed, A));

            Assert.False(flow.AdvancesOnInput);
            Assert.False(flow.Feed(ScenarioSignal.Advance));

            flow.Feed(ScenarioSignal.MapClosed);
            Assert.True(flow.AdvancesOnInput);
        }

        [Fact]
        public void AdvancesOnInput_IsTrueWhenTheNextPageIsSkipped()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.MeetingOpened, B).SkipWhen(ScenarioSignal.VoteAccepted),
                ScenarioPage.Say(ScenarioSignal.VoteAccepted, C));

            Assert.False(flow.AdvancesOnInput);

            flow.Feed(ScenarioSignal.VoteAccepted);
            Assert.True(flow.AdvancesOnInput);
            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(C, flow.Current.Text);
        }

        [Fact]
        public void Finished_StopsFeedingAndKeepsTheLastPage()
        {
            var flow = Flow(ScenarioPage.Say(ScenarioSignal.Advance, A));

            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Finished);
            Assert.Equal(flow.PageCount, flow.Index);
            Assert.Equal(A, flow.Current.Text);

            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.False(flow.Feed(ScenarioSignal.TallyRevealed));
            Assert.False(flow.AdvancesOnInput);
        }

        [Fact]
        public void ActionNag_OpensOnlyTheReminderItWasRaisedFor()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.ActionNag, B),
                ScenarioPage.Say(ScenarioSignal.Advance, C),
                ScenarioPage.Say(ScenarioSignal.ActionNag, D));

            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.ActionNag));
            Assert.Equal(B, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(C, flow.Current.Text);

            Assert.False(flow.AdvancesOnInput);
            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(C, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.ActionNag));
            Assert.Equal(D, flow.Current.Text);
        }

        [Fact]
        public void BombOnWrongTarget_IsNotLatchedSoItNeverOpensALaterGate()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B));

            Assert.False(flow.Feed(ScenarioSignal.BombOnWrongTarget));
            Assert.Equal(A, flow.Current.Text);
            Assert.False(flow.IsLatched(ScenarioSignal.BombOnWrongTarget));

            Assert.False(flow.Feed(ScenarioSignal.BombAlreadyOnTarget));
            Assert.False(flow.IsLatched(ScenarioSignal.BombAlreadyOnTarget));
        }

        [Fact]
        public void ActionNag_MovesOnlyOnePageEvenWhenSeveralWaitOnIt()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.ActionNag, B),
                ScenarioPage.Say(ScenarioSignal.ActionNag, C));

            Assert.True(flow.Feed(ScenarioSignal.ActionNag));
            Assert.Equal(B, flow.Current.Text);
            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(B, flow.Current.Text);
        }

        [Fact]
        public void Forgets_ClearsTheLatchOnEntrySoTheSameGateWaitsAgain()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.MeetingOpened, B),
                ScenarioPage.Say(ScenarioSignal.Advance, C).Forgets(ScenarioSignal.MeetingOpened),
                ScenarioPage.Say(ScenarioSignal.MeetingOpened, D));

            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(B, flow.Current.Text);
            Assert.True(flow.IsLatched(ScenarioSignal.MeetingOpened));

            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(C, flow.Current.Text);
            Assert.False(flow.IsLatched(ScenarioSignal.MeetingOpened));
            Assert.False(flow.AdvancesOnInput);

            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(C, flow.Current.Text);
            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(D, flow.Current.Text);
        }

        [Fact]
        public void Forgets_AlsoReopensASkipCondition()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B).Forgets(ScenarioSignal.VoteAccepted),
                ScenarioPage.Say(ScenarioSignal.Advance, C).SkipWhen(ScenarioSignal.VoteAccepted),
                ScenarioPage.Say(ScenarioSignal.VoteAccepted, D));

            flow.Feed(ScenarioSignal.VoteAccepted);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(B, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(C, flow.Current.Text);
            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.VoteAccepted));
            Assert.Equal(D, flow.Current.Text);
        }

        [Fact]
        public void SkipIf_PassesThroughWhenAnyOfSeveralSignalsIsLatched()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B)
                    .SkipWhen(ScenarioSignal.ShamanGhostFired, ScenarioSignal.ShamanStormEntered),
                ScenarioPage.Say(ScenarioSignal.ShamanGhostFired, C)
                    .SkipWhen(ScenarioSignal.ShamanStormEntered),
                ScenarioPage.Say(ScenarioSignal.ShamanStormEntered, D));

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(B, flow.Current.Text);
            Assert.Equal(new[] { ScenarioSignal.ShamanGhostFired, ScenarioSignal.ShamanStormEntered },
                flow.Current.SkipIf);
            Assert.True(flow.Feed(ScenarioSignal.ShamanStormEntered));
            Assert.Equal(D, flow.Current.Text);
        }

        [Fact]
        public void Page_KeepsEntryStepsMeetingIndexAndForgets()
        {
            var page = ScenarioPage.Say(ScenarioSignal.Advance, A)
                .Does(ScenarioEntryAction.PlaceFarCorpse, 7)
                .InMeeting(1)
                .Forgets(ScenarioSignal.ConveneAccepted, ScenarioSignal.TallyRevealed);

            Assert.True(page.Has(ScenarioEntryAction.PlaceFarCorpse, 7));
            Assert.False(page.Has(ScenarioEntryAction.PlaceFarCorpse, 8));
            Assert.Equal(1, page.MeetingIndex);
            Assert.Equal(new[] { ScenarioSignal.ConveneAccepted, ScenarioSignal.TallyRevealed },
                page.ForgetsOnEntry);

            var plain = ScenarioPage.Say(ScenarioSignal.Advance, A);
            Assert.Empty(plain.EntrySteps);
            Assert.Equal(0, plain.MeetingIndex);
            Assert.Empty(plain.ForgetsOnEntry);
        }

        [Fact]
        public void HasReached_IsFalseBeforeTheMarkedPageAndTrueOnceThere()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B).Marks(ScenarioMilestone.ActionPrompted));

            Assert.False(flow.HasReached(ScenarioMilestone.ActionPrompted));
            flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.HasReached(ScenarioMilestone.ActionPrompted));
        }

        [Fact]
        public void HasReached_TreatsAnAbsentMilestoneAsReached()
        {
            Assert.True(Flow(ScenarioPage.Say(ScenarioSignal.Advance, A))
                .HasReached(ScenarioMilestone.CheckmateArmed));
        }

        [Fact]
        public void HasEntered_IsFalseForAnAbsentMilestoneEvenThoughHasReachedIsTrue()
        {
            var flow = Flow(ScenarioPage.Say(ScenarioSignal.Advance, A));

            Assert.True(flow.HasReached(ScenarioMilestone.CheckmateArmed));
            Assert.False(flow.HasEntered(ScenarioMilestone.CheckmateArmed));
        }

        [Fact]
        public void HasEntered_IsFalseBeforeTheMarkedPageAndTrueOnceThere()
        {
            var flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A),
                ScenarioPage.Say(ScenarioSignal.Advance, B).Marks(ScenarioMilestone.CheckmateArmed));

            Assert.False(flow.HasEntered(ScenarioMilestone.CheckmateArmed));
            flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.HasEntered(ScenarioMilestone.CheckmateArmed));
        }

        [Fact]
        public void Milestone_IsIndependentOfTheCueOnTheSamePage()
        {
            ScenarioPage page = ScenarioPage.Say(ScenarioSignal.Advance, A)
                .WithNag(B)
                .Marks(ScenarioMilestone.ActionPrompted)
                .Does(ScenarioEntryAction.GrantBeaconCharge);

            Assert.Equal(ScenarioCue.ActionNag, page.Cue);
            Assert.Equal(ScenarioMilestone.ActionPrompted, page.Milestone);
            Assert.True(page.Has(ScenarioEntryAction.GrantBeaconCharge));
        }

        [Fact]
        public void Page_KeepsWhatTheBuildersSet()
        {
            ScenarioPage page = ScenarioPage.Say(ScenarioSignal.MeetingOpened, A, B)
                .At(ScenarioAnchor.ChatLog)
                .SkipWhen(ScenarioSignal.VoteAccepted)
                .WithCue(ScenarioCue.ChatScript)
                .Does(ScenarioEntryAction.GrantBeaconCharge)
                .Marks(ScenarioMilestone.CheckmateArmed)
                .Requires(ScenarioNeed.ChatLogOpen)
                .PointsAt(ScenarioHighlight.ChatLogPanel)
                .MustBeRead();

            Assert.Equal(ScenarioSignal.MeetingOpened, page.Gate);
            Assert.Equal(new[] { A, B }, page.Lines);
            Assert.Equal(A, page.Text);
            Assert.True(page.HasText);
            Assert.Equal(ScenarioAnchor.ChatLog, page.Anchor);
            Assert.Equal(new[] { ScenarioSignal.VoteAccepted }, page.SkipIf);
            Assert.Equal(ScenarioCue.ChatScript, page.Cue);
            Assert.True(page.Has(ScenarioEntryAction.GrantBeaconCharge));
            Assert.Equal(ScenarioMilestone.CheckmateArmed, page.Milestone);
            Assert.Equal(ScenarioNeed.ChatLogOpen, page.Need);
            Assert.Equal(ScenarioHighlight.ChatLogPanel, page.Highlight);
            Assert.True(page.MustRead);
        }

        [Fact]
        public void Page_WaitCarriesNoText()
        {
            ScenarioPage page = ScenarioPage.Wait(ScenarioSignal.MapClosed);

            Assert.False(page.HasText);
            Assert.Empty(page.Lines);
        }

        [Fact]
        public void CueTiming_ChatDelayGrowsWithLengthAndIsCapped()
        {
            var timing = new ScenarioCueTiming(700, 2000, 50, 4500, 800, 25000);

            Assert.Equal(2000, timing.ChatDelayMs(""));
            Assert.Equal(2000, timing.ChatDelayMs(null));
            Assert.True(timing.ChatDelayMs(new string('あ', 10)) > timing.ChatDelayMs(new string('あ', 5)));
            Assert.Equal(4500, timing.ChatDelayMs(new string('あ', 1000)));
        }

        [Fact]
        public void CueTiming_StartDelayIsPerCue()
        {
            var timing = new ScenarioCueTiming(700, 2000, 50, 4500, 800, 25000);

            Assert.Equal(700L, timing.StartDelayMs(ScenarioCue.ChatScript));
            Assert.Equal(800L, timing.StartDelayMs(ScenarioCue.BotVotes));
            Assert.Equal(25000L, timing.StartDelayMs(ScenarioCue.ActionNag));
            Assert.Equal(0L, timing.StartDelayMs(ScenarioCue.None));
        }

        [Fact]
        public void ChatLogExplanationPending_HoldsFromTheStartUntilTheLastPageInsideTheLog()
        {
            ScenarioFlow flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A).PointsAt(ScenarioHighlight.MeetingGauge),
                ScenarioPage.Say(ScenarioSignal.Advance, B).PointsAt(ScenarioHighlight.RecapTitle),
                ScenarioPage.Say(ScenarioSignal.Advance, C).PointsAt(ScenarioHighlight.ChatLogPanel),
                ScenarioPage.Say(ScenarioSignal.Advance, D).PointsAt(ScenarioHighlight.MapOverlay));

            Assert.True(flow.ChatLogExplanationPending);
            flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.ChatLogExplanationPending);
            flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.ChatLogExplanationPending);
            flow.Feed(ScenarioSignal.Advance);
            Assert.False(flow.ChatLogExplanationPending);
            flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.Finished);
            Assert.False(flow.ChatLogExplanationPending);
        }

        [Fact]
        public void ChatLogExplanationPending_IsFalseForScriptsThatNeverPointIntoTheLog()
        {
            ScenarioFlow flow = Flow(
                ScenarioPage.Say(ScenarioSignal.Advance, A).PointsAt(ScenarioHighlight.ChatLogButton),
                ScenarioPage.Say(ScenarioSignal.Advance, B));
            Assert.False(flow.ChatLogExplanationPending);
        }
    }
}
