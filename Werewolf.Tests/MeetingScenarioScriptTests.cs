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
        public void Roster_IsFivePlayersWithExactlyOneWerewolf()
        {
            var bots = Def.Cast.Bots;

            Assert.Equal(4, bots.Count);
            Assert.Equal(new[] { 2, 3, 4, 5 }, bots.Select(b => b.ParticipantId));
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
        public void Pages_CoverTheWholeScriptFromIntroToPostgame()
        {
            var used = Def.Pages.SelectMany(p => p.Lines).ToArray();

            foreach (var id in new[]
            {
                TextId.ScenarioMeetingIntro1, TextId.ScenarioMeetingIntro6,
                TextId.ScenarioMeetingConvene1, TextId.ScenarioMeetingConvene2,
                TextId.ScenarioMeetingOpened1, TextId.ScenarioMeetingOpened9,
                TextId.ScenarioMeetingClock1, TextId.ScenarioMeetingClock4,
                TextId.ScenarioMeetingGauge1, TextId.ScenarioMeetingGauge5,
                TextId.ScenarioMeetingMap1, TextId.ScenarioMeetingMap5,
                TextId.ScenarioMeetingChatUnavailable1, TextId.ScenarioMeetingChatUnavailable2,
                TextId.ScenarioMeetingVote1, TextId.ScenarioMeetingVote3,
                TextId.ScenarioMeetingTally, TextId.ScenarioMeetingResult2,
                TextId.ScenarioMeetingPostgame3,
            })
            {
                Assert.Contains(id, used);
            }
        }

        [Fact]
        public void Pages_ReadableGuidanceIsGroupedNotOnePerSentence()
        {
            Assert.Equal(3, PageOf(TextId.ScenarioMeetingIntro1).Lines.Count);
            Assert.Equal(3, PageOf(TextId.ScenarioMeetingVote1).Lines.Count);
            Assert.Equal(2, PageOf(TextId.ScenarioMeetingPostgame1).Lines.Count);
            Assert.Equal(2, PageOf(TextId.ScenarioMeetingReplay1).Lines.Count);
            Assert.Equal(2, PageOf(TextId.ScenarioMeetingConvene1).Lines.Count);
            Assert.Equal(2, PageOf(TextId.ScenarioMeetingChatUnavailable1).Lines.Count);
            Assert.Equal(2, PageOf(TextId.ScenarioMeetingResult1).Lines.Count);
            Assert.Equal(3, PageOf(TextId.ScenarioMeetingGauge1).Lines.Count);
            Assert.Equal(2, PageOf(TextId.ScenarioMeetingGauge4).Lines.Count);
            Assert.Equal(3, PageOf(TextId.ScenarioMeetingMap1).Lines.Count);
            Assert.Single(PageOf(TextId.ScenarioMeetingMap4).Lines);

            Assert.Single(PageOf(TextId.ScenarioMeetingOpened5).Lines);
            Assert.Single(PageOf(TextId.ScenarioMeetingOpened9).Lines);
        }

        [Fact]
        public void Pages_EachCueAppearsExactlyOnce()
        {
            Assert.Single(Def.Pages, p => p.Cue == ScenarioCue.ChatScript);
            Assert.Single(Def.Pages, p => p.Cue == ScenarioCue.BotVotes);
            Assert.Single(Def.Pages, p => p.Cue == ScenarioCue.ActionNag);
        }

        [Fact]
        public void Pages_VoteGuidanceRequiresTheMapToBeClosed()
        {
            Assert.Equal(ScenarioNeed.MapClosed, PageOf(TextId.ScenarioMeetingVote1).Need);
            Assert.Equal(ScenarioNeed.None, PageOf(TextId.ScenarioMeetingIntro1).Need);
        }

        [Fact]
        public void Pages_ChatScriptWaitRequiresTheChatLog()
        {
            var page = Def.Pages.Single(p => p.Cue == ScenarioCue.ChatScript);

            Assert.False(page.HasText);
            Assert.Equal(ScenarioNeed.ChatLogOpen, page.Need);
        }

        [Fact]
        public void Pages_MeetingLogExplanationSitsBesideTheLog()
        {
            var logLines = new[]
            {
                TextId.ScenarioMeetingOpened3, TextId.ScenarioMeetingOpened4,
                TextId.ScenarioMeetingOpened5, TextId.ScenarioMeetingOpened6,
                TextId.ScenarioMeetingOpened7, TextId.ScenarioMeetingOpened8,
                TextId.ScenarioMeetingOpened9,
            };

            foreach (var line in logLines)
            {
                var page = PageOf(line);
                Assert.Equal(ScenarioAnchor.ChatLog, page.Anchor);
                Assert.Equal(ScenarioNeed.ChatLogOpen, page.Need);
            }
        }

        [Fact]
        public void Pages_ResultScreenGuidanceClearsTheReplayEntry()
        {
            foreach (var id in new[]
            {
                TextId.ScenarioMeetingPostgame1, TextId.ScenarioMeetingReplay1,
                TextId.ScenarioMeetingPostgame3,
            })
            {
                Assert.Equal(ScenarioAnchor.ReplayEntry, PageOf(id).Anchor);
            }
        }

        [Fact]
        public void Pages_ReplayIntroductionPointsAtTheEntry()
        {
            ScenarioPage page = PageOf(TextId.ScenarioMeetingReplay1);

            Assert.Equal(ScenarioHighlight.ReplayEntry, page.Highlight);
            Assert.Equal(ScenarioSignal.Advance, page.Gate);
            Assert.Equal(ScenarioNeed.None, page.Need);
        }

        [Fact]
        public void Pages_GaugeExplanationHangsUnderTheGaugeAndPointsAtIt()
        {
            foreach (var id in new[] { TextId.ScenarioMeetingGauge1, TextId.ScenarioMeetingGauge4 })
            {
                Assert.Equal(ScenarioAnchor.MeetingGauge, PageOf(id).Anchor);
                Assert.Equal(ScenarioHighlight.MeetingGauge, PageOf(id).Highlight);
            }
        }

        [Fact]
        public void Pages_MapExplanationRequiresTheMapToBeOpen()
        {
            foreach (var id in new[] { TextId.ScenarioMeetingMap1, TextId.ScenarioMeetingMap4 })
            {
                var page = PageOf(id);
                Assert.Equal(ScenarioNeed.MapOpen, page.Need);
                Assert.Equal(ScenarioHighlight.MapOverlay, page.Highlight);
            }
        }

        [Fact]
        public void Pages_MinutesExplanationPointsAtOneLineEach()
        {
            var expected = new[]
            {
                ScenarioHighlight.RecapTitle, ScenarioHighlight.RecapDeaths,
                ScenarioHighlight.RecapLost, ScenarioHighlight.RecapHaul,
                ScenarioHighlight.RecapBeacon,
            };
            var pointed = new[]
            {
                TextId.ScenarioMeetingOpened3, TextId.ScenarioMeetingOpened5,
                TextId.ScenarioMeetingOpened6, TextId.ScenarioMeetingOpened7,
                TextId.ScenarioMeetingOpened8,
            }.Select(id => PageOf(id).Highlight).ToArray();

            Assert.Equal(expected, pointed);
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
