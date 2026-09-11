using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingScenarioScriptTests
    {
        private const int PlayerActor = 1;

        private static ScenarioDefinition Def => MeetingScenarioScript.Definition;

        private static int Actor(int participantId) => Def.Cast.ActorOf(participantId, PlayerActor);

        private static int[] AliveActors()
        {
            var actors = new List<int> { PlayerActor };
            actors.AddRange(Def.Cast.Bots.Select(b => b.ActorNumber));
            return actors.ToArray();
        }

        [Fact]
        public void Roster_HasExactlyOneWerewolfAndAVillagerPlayer()
        {
            var bots = Def.Cast.Bots;

            Assert.All(bots, b => Assert.True(b.ActorNumber <= ScenarioRoster.FirstBotActor));

            var wolves = bots.Where(b => b.Role == Role.Werewolf).ToArray();
            Assert.Single(wolves);
            Assert.Equal(MeetingScenarioScript.WerewolfParticipantId, wolves[0].ParticipantId);

            Assert.Equal(MeetingScenarioScript.PlayerParticipantId, Def.Cast.PlayerParticipantId);
            Assert.Equal(Role.Villager, Def.Cast.PlayerRole);
        }

        private static int ExecutedActorAfterPlayerVotes(int playerTargetActor)
        {
            var box = new VoteBox(AliveActors());
            Assert.Equal(VoteRejectReason.None, box.TryCast(PlayerActor, playerTargetActor));
            foreach (var vote in Def.Meetings[0].BotVotes)
            {
                Assert.Equal(VoteRejectReason.None,
                    box.TryCast(Actor(vote.VoterParticipantId), Actor(vote.TargetParticipantId)));
            }
            return box.Tally(_ => true).ExecutedActor;
        }

        [Fact]
        public void BotVotes_CoverEveryBotExactlyOnce()
        {
            var voters = Def.Meetings[0].BotVotes.Select(v => v.VoterParticipantId).ToArray();

            Assert.Equal(Def.Cast.Bots.Count, voters.Length);
            Assert.All(voters, id => Assert.True(Def.Cast.TryGetBot(id, out _)));
        }

        [Fact]
        public void Tally_WerewolfExecuted_WhicheverCandidateThePlayerPicks()
        {
            int wolfActor = Actor(MeetingScenarioScript.WerewolfParticipantId);

            foreach (int target in AliveActors())
            {
                Assert.Equal(wolfActor, ExecutedActorAfterPlayerVotes(target));
            }
        }

        [Fact]
        public void Tally_WerewolfExecuted_WhenPlayerSkips()
        {
            Assert.Equal(Actor(MeetingScenarioScript.WerewolfParticipantId), ExecutedActorAfterPlayerVotes(-1));
        }

        [Fact]
        public void Tally_WerewolfExecuted_WhenPlayerDoesNotVoteAtAll()
        {
            var box = new VoteBox(AliveActors());
            foreach (var vote in Def.Meetings[0].BotVotes)
            {
                box.TryCast(Actor(vote.VoterParticipantId), Actor(vote.TargetParticipantId));
            }

            Assert.Equal(Actor(MeetingScenarioScript.WerewolfParticipantId), box.Tally(_ => true).ExecutedActor);
        }

        [Fact]
        public void Flow_AdvanceInputDoesNotSkipActionGatedPage()
        {
            var flow = Def.CreateFlow();
            for (int i = 0; i < 2; i++) Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(TextId.ScenarioMeetingConvene1, flow.Current.Text);

            int index = flow.Index;
            for (int i = 0; i < 10; i++) Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(index, flow.Index);

            Assert.True(flow.Feed(ScenarioSignal.ConveneAccepted));
        }

        [Fact]
        public void Flow_EventSignalsAreLatched()
        {
            var flow = Def.CreateFlow();
            flow.Feed(ScenarioSignal.ConveneAccepted);
            flow.Feed(ScenarioSignal.MeetingOpened);

            for (int i = 0; i < 2; i++) flow.Feed(ScenarioSignal.Advance);

            Assert.True(flow.Current.HasText);
            Assert.Equal(TextId.ScenarioMeetingOpened1, flow.Current.Text);
        }

        [Fact]
        public void Flow_SkipsConveneGuidanceWhenAlreadyConvened()
        {
            var flow = Def.CreateFlow();
            flow.Feed(ScenarioSignal.Advance);

            flow.Feed(ScenarioSignal.ConveneAccepted);
            flow.Feed(ScenarioSignal.Advance);

            Assert.False(flow.Current.HasText);
            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(TextId.ScenarioMeetingOpened1, flow.Current.Text);
        }

        [Fact]
        public void Flow_ConveneNagIsShownWhenTheButtonIsNotPressed()
        {
            var flow = AtConveneGuidance();

            Assert.True(flow.Feed(ScenarioSignal.ActionNag));
            Assert.Equal(TextId.ScenarioMeetingConveneNag1, flow.Current.Text);
            Assert.Contains(TextId.ScenarioMeetingConveneNag2Format, flow.Current.Lines);

            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.ConveneAccepted));
        }

        [Fact]
        public void Flow_ConveneNagIsSkippedWhenConvenedInTime()
        {
            var flow = AtConveneGuidance();

            Assert.True(flow.Feed(ScenarioSignal.ConveneAccepted));

            Assert.False(flow.Current.HasText);
            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(TextId.ScenarioMeetingOpened1, flow.Current.Text);
        }

        [Fact]
        public void Flow_ConveneBeforeTheGuidanceCountsAsGettingAhead()
        {
            var flow = Def.CreateFlow();
            Assert.False(flow.HasReached(ScenarioMilestone.ActionPrompted));

            flow.Feed(ScenarioSignal.ConveneAccepted);
            Assert.Equal(TextId.ScenarioMeetingIntro1, flow.Current.Text);
            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(TextId.ScenarioMeetingIntro4, flow.Current.Text);
        }

        [Fact]
        public void Flow_ConveneAtTheGuidanceIsNotGettingAhead()
        {
            Assert.True(AtConveneGuidance().HasReached(ScenarioMilestone.ActionPrompted));
        }

        [Fact]
        public void Notices_EarlyStartIsWiredToTheConveneGuidance()
        {
            Assert.Equal(ScenarioSignal.ConveneAccepted, Def.Notices.EarlyStartSignal);
            Assert.Equal(ScenarioMilestone.ActionPrompted, Def.Notices.EarlyStartAfter);
            Assert.Equal(TextId.ScenarioMeetingConvene1,
                Def.Pages.Single(p => p.Milestone == ScenarioMilestone.ActionPrompted).Text);
        }

        [Fact]
        public void Flow_SkipsVoteGuidanceWhenPlayerVotedDuringChatScript()
        {
            var flow = AtChatScriptPage();

            Assert.False(flow.Feed(ScenarioSignal.VoteAccepted));
            Assert.True(flow.Feed(ScenarioSignal.ChatScriptDone));

            flow.Feed(ScenarioSignal.Advance);
            Assert.False(flow.Current.HasText);
            Assert.Equal(ScenarioCue.BotVotes, flow.Current.Cue);

            Assert.True(flow.Feed(ScenarioSignal.TallyRevealed));
            Assert.Equal(TextId.ScenarioMeetingTally, flow.Current.Text);
        }

        [Fact]
        public void Flow_ShowsVoteGuidanceWhenPlayerHasNotVotedYet()
        {
            var flow = AtChatScriptPage();
            flow.Feed(ScenarioSignal.ChatScriptDone);
            flow.Feed(ScenarioSignal.Advance);

            Assert.Equal(TextId.ScenarioMeetingVote1, flow.Current.Text);
        }

        [Fact]
        public void Flow_MapCloseGuidanceWaitsForTheCloseItAsksFor()
        {
            Assert.Equal(ScenarioNeed.MapOpen, PageOf(TextId.ScenarioMeetingMap4).Need);
            Assert.Equal(ScenarioNeed.None, PageOf(TextId.ScenarioMeetingMap5).Need);

            var flow = DriveTo(TextId.ScenarioMeetingMap5,
                ScenarioSignal.ConveneAccepted, ScenarioSignal.MeetingOpened);

            Assert.False(flow.AdvancesOnInput);
            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.MapClosed));
            Assert.Equal(ScenarioCue.ChatScript, flow.Current.Cue);
        }

        [Fact]
        public void Flow_WinExplanationIsNotSkippedByTheResultScreen()
        {
            var flow = DriveToResultExplanation();

            Assert.True(flow.Current.MustRead);
            Assert.False(flow.AdvancesOnInput);

            Assert.False(flow.Feed(ScenarioSignal.ResultScreenShown));
            Assert.Equal(TextId.ScenarioMeetingResult1, flow.Current.Text);
            Assert.True(flow.AdvancesOnInput);

            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.Equal(TextId.ScenarioMeetingPostgame1, flow.Current.Text);
        }

        [Fact]
        public void Flow_ActionGatedPagesDoNotOfferTheAdvanceHint()
        {
            var flow = AtConveneGuidance();
            Assert.False(flow.AdvancesOnInput);

            flow.Feed(ScenarioSignal.ActionNag);
            Assert.False(flow.AdvancesOnInput);

            flow = AtChatScriptPage();
            Assert.False(flow.AdvancesOnInput);
            flow.Feed(ScenarioSignal.ChatScriptDone);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioMeetingVote1, flow.Current.Text);
            Assert.False(flow.AdvancesOnInput);
        }

        [Fact]
        public void EveryMeetingTextId_IsReachableFromTheDefinition()
        {
            var reachable = new HashSet<TextId>(
                Def.Pages.SelectMany(p => p.Lines)
                    .Concat(Def.Pages.SelectMany(p => p.NagLines))
                    .Concat(Def.Meetings.SelectMany(m => m.ChatScript).Select(l => l.Text))
                    .Concat(Def.Notices.LeashNag)
                    .Concat(Def.Notices.EarlyStartLines)
                    .Concat(KeyHintLines(Def.Notices.KeyHint)));

            var orphans = Enum.GetValues(typeof(TextId)).Cast<TextId>()
                .Where(id => id.ToString().StartsWith("ScenarioMeeting", StringComparison.Ordinal))
                .Where(id => !reachable.Contains(id))
                .ToArray();

            Assert.True(orphans.Length == 0,
                "台本のどこからも参照されない文言: " + string.Join(", ", orphans));
        }

        [Fact]
        public void Pages_EachCueAppearsExactlyOnce()
        {
            Assert.Single(Def.Pages, p => p.Cue == ScenarioCue.ChatScript);
            Assert.Single(Def.Pages, p => p.Cue == ScenarioCue.BotVotes);
            Assert.Single(Def.Pages, p => p.Cue == ScenarioCue.ActionNag);
        }

        private static IEnumerable<TextId> KeyHintLines(ScenarioKeyHint hint)
        {
            if (!hint.HasValue) yield break;
            yield return hint.FormatId;
            yield return hint.GrabNoteId;
            yield return hint.InteractNoteId;
        }

        private static ScenarioPage PageOf(TextId id) => Def.Pages.Single(p => p.Lines.Contains(id));

        private static ScenarioFlow DriveTo(TextId target, params ScenarioSignal[] events)
        {
            var flow = Def.CreateFlow();
            int next = 0;
            for (int guard = 0; guard < 300; guard++)
            {
                if (flow.Current.HasText && flow.Current.Text == target) return flow;
                if (flow.Feed(ScenarioSignal.Advance)) continue;
                Assert.True(next < events.Length, $"{target} の手前で進行が止まった");
                flow.Feed(events[next++]);
            }
            Assert.Fail($"{target} へ到達しなかった");
            return flow;
        }

        private static ScenarioFlow DriveToResultExplanation()
            => DriveTo(TextId.ScenarioMeetingResult1,
                ScenarioSignal.ConveneAccepted, ScenarioSignal.MeetingOpened,
                ScenarioSignal.MapClosed, ScenarioSignal.ChatScriptDone,
                ScenarioSignal.VoteAccepted,
                ScenarioSignal.TallyRevealed, ScenarioSignal.WinCeremonyDone);

        private static ScenarioFlow AtConveneGuidance()
        {
            var flow = Def.CreateFlow();
            for (int guard = 0; guard < 200 && flow.Current.Cue != ScenarioCue.ActionNag; guard++)
            {
                Assert.True(flow.Feed(ScenarioSignal.Advance));
            }
            Assert.Equal(TextId.ScenarioMeetingConvene1, flow.Current.Text);
            return flow;
        }

        private static ScenarioFlow AtChatScriptPage()
        {
            var flow = Def.CreateFlow();
            flow.Feed(ScenarioSignal.ConveneAccepted);
            flow.Feed(ScenarioSignal.MeetingOpened);

            for (int guard = 0; guard < 200 && flow.Current.Cue != ScenarioCue.ChatScript; guard++)
            {
                if (flow.Feed(ScenarioSignal.Advance)) continue;
                Assert.True(flow.Feed(ScenarioSignal.MapClosed));
            }
            Assert.Equal(ScenarioCue.ChatScript, flow.Current.Cue);
            return flow;
        }
    }
}
