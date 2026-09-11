using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ShamanScenarioScriptTests
    {
        private const int PlayerActor = 1;

        private static ScenarioDefinition Def => ShamanScenarioScript.Definition;

        private static int Actor(int participantId) => Def.Cast.ActorOf(participantId, PlayerActor);

        private static int WolfActor => Actor(ShamanScenarioScript.WerewolfParticipantId);

        private static int[] AliveAtMeeting1()
            => new[] { PlayerActor }
                .Concat(Def.Cast.Bots
                    .Where(b => b.ParticipantId != ShamanScenarioScript.FirstCorpseParticipantId)
                    .Select(b => b.ActorNumber))
                .ToArray();

        private static int[] AliveAtMeeting2()
            => AliveAtMeeting1()
                .Where(a => a != Actor(ShamanScenarioScript.SecondCorpseParticipantId))
                .ToArray();

        private static IReadOnlyList<ScenarioPage> Pages => Def.Pages;

        private static int IndexOfText(TextId id)
            => Pages.ToList().FindIndex(p => p.HasText && p.Text == id);

        private static ScenarioPage PageOf(TextId id)
        {
            int index = IndexOfText(id);
            Assert.True(index >= 0, id.ToString());
            return Pages[index];
        }

        private static int IndexOfGate(ScenarioSignal gate)
            => Pages.ToList().FindIndex(p => p.Gate == gate);

        private static int LastIndexOfGate(ScenarioSignal gate)
            => Pages.ToList().FindLastIndex(p => p.Gate == gate);

        [Fact]
        public void Cast_PlayerIsTheShamanWithExactlyOneWerewolf()
        {
            Assert.Equal(Role.Shaman, Def.Cast.PlayerRole);

            var wolves = Def.Cast.Bots.Where(b => b.Role == Role.Werewolf).ToArray();
            Assert.Single(wolves);
            Assert.Equal(ShamanScenarioScript.WerewolfParticipantId, wolves[0].ParticipantId);
            Assert.Equal(1, Def.Cast.WerewolfCount);
            Assert.Equal(0, Def.Cast.BlackCatChancePercent);
            Assert.Equal(0, Def.Cast.BomberChancePercent);
            Assert.Equal(0, Def.Cast.ShamanChancePercent);
        }

        [Fact]
        public void Cast_CorpsesAreVillagerBotsAndNotTheWerewolf()
        {
            foreach (int id in new[] { ShamanScenarioScript.FirstCorpseParticipantId,
                                       ShamanScenarioScript.SecondCorpseParticipantId })
            {
                Assert.True(Def.Cast.TryGetBot(id, out var bot));
                Assert.Equal(Role.Villager, bot.Role);
            }
            Assert.NotEqual(ShamanScenarioScript.FirstCorpseParticipantId,
                ShamanScenarioScript.SecondCorpseParticipantId);
        }

        [Fact]
        public void Environment_UsesLevelThreeWithNoLossSeedAndNoQuotaOverride()
        {
            Assert.Equal(2, Def.Environment.LevelsCompleted);
            Assert.Equal(0, Def.Environment.SeededLossPct);
            Assert.False(Def.Environment.HaulGoalOverridden);
        }

        [Fact]
        public void Environment_GivesMobilityUpgradesTheGameAccepts()
        {
            var upgrades = Def.Environment.StartUpgrades;
            Assert.True(upgrades["ExtraJump"] >= 10);
            Assert.True(upgrades["Stamina"] > 0);
            Assert.True(upgrades["Speed"] > 0);
            foreach (var pair in upgrades)
            {
                Assert.Contains(pair.Key, WorldgenUpgrades.Names);
                Assert.True(pair.Value > 0, pair.Key);
            }
        }

        [Fact]
        public void Environment_LeashStartsAtTheFirstTallyAndStopsAtTheNextConvene()
        {
            Assert.True(Def.Environment.TruckLeashEnabled);
            Assert.Equal(GameConfig.DefaultTruckSafeRadiusMeters, Def.Environment.TruckLeashMeters);
            Assert.Equal(ScenarioMilestone.TruckBound, Def.Environment.TruckLeashAfter);
            Assert.Equal(ScenarioSignal.ConveneAccepted, Def.Environment.TruckLeashUntil);

            int bound = Pages.ToList().FindIndex(p => p.Milestone == ScenarioMilestone.TruckBound);
            Assert.Equal(IndexOfGate(ScenarioSignal.TallyRevealed), bound);
            Assert.True(bound < IndexOfGate(ScenarioSignal.LastRunReached));
        }

        [Fact]
        public void Flow_LeashIsReachedAsSoonAsTheFirstTallyIsShown()
        {
            Assert.DoesNotContain(Pages.Take(IndexOfGate(ScenarioSignal.TallyRevealed)),
                p => p.Milestone == ScenarioMilestone.TruckBound);

            ScenarioFlow flow = Def.CreateFlow();
            Assert.False(flow.HasReached(ScenarioMilestone.TruckBound));

            AdvanceToFirstMeetingTally(flow);
            Assert.Equal(TextId.ScenarioShamanTally1, flow.Current.Text);
            Assert.True(flow.HasReached(ScenarioMilestone.TruckBound));
        }

        [Fact]
        public void Environment_LeashHoldsTheTruckUntilTheDowsingPage()
        {
            Assert.Equal(ScenarioMilestone.Roaming, Def.Environment.TruckLeashBefore);

            ScenarioPage dowse = PageOf(TextId.ScenarioShamanDowse1);
            Assert.Equal(ScenarioMilestone.Roaming, dowse.Milestone);
            Assert.True(dowse.Has(ScenarioEntryAction.PlaceFarCorpse));

            Assert.True(IndexOfText(TextId.ScenarioShamanDowse1)
                < Pages.ToList().FindIndex(p => p.Milestone == ScenarioMilestone.TruckBound));
        }

        [Fact]
        public void Flow_RoamingIsNotReachedWhileTheSenseIsBeingExplained()
        {
            ScenarioFlow flow = Def.CreateFlow();
            Assert.False(flow.HasReached(ScenarioMilestone.Roaming));

            for (int i = 0; i < 4; i++)
            {
                flow.Feed(ScenarioSignal.Advance);
                Assert.False(flow.HasReached(ScenarioMilestone.Roaming));
            }
            Assert.Equal(TextId.ScenarioShamanDrip1, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanDowse1, flow.Current.Text);
            Assert.True(flow.HasReached(ScenarioMilestone.Roaming));
        }

        [Fact]
        public void Safety_IsShelteredAndTheMeetingRightStartsAtZero()
        {
            Assert.True(Def.Safety.SuppressEnemies);
            Assert.True(Def.Safety.Invincible);
            Assert.False(Def.Safety.ClampDamage);
            Assert.Equal(0, Def.Room.MeetingRightsPerPlayer);
        }

        private static MeetingOutcome TallyMeeting(int meetingIndex, int[] alive, int? playerTarget)
        {
            var box = new VoteBox(alive);
            if (playerTarget.HasValue)
            {
                Assert.Equal(VoteRejectReason.None, box.TryCast(PlayerActor, playerTarget.Value));
            }
            foreach (var vote in Def.Meetings[meetingIndex].BotVotes)
            {
                Assert.Equal(VoteRejectReason.None,
                    box.TryCast(Actor(vote.VoterParticipantId), vote.TargetActor(Def.Cast, PlayerActor)));
            }
            return box.Tally(_ => true);
        }

        [Fact]
        public void Meeting1_EndsInASkipWhateverThePlayerDoes()
        {
            int[] alive = AliveAtMeeting1();
            Assert.Equal(6, alive.Length);
            Assert.Equal(alive.Length - 1, Def.Meetings[0].BotVotes.Count);

            var choices = alive.Select(a => (int?)a).Append(-1).Append(null).ToArray();
            foreach (int? choice in choices)
            {
                MeetingOutcome outcome = TallyMeeting(0, alive, choice);
                Assert.Equal(-1, outcome.ExecutedActor);
                Assert.True(outcome.SkipVotes > outcome.VoteCounts.Where((_, i) => outcome.TargetActors[i] != -1)
                    .DefaultIfEmpty(0).Max());
            }
        }

        [Fact]
        public void Meeting2_ExecutesTheWerewolfWhateverThePlayerDoes()
        {
            int[] alive = AliveAtMeeting2();
            Assert.Equal(5, alive.Length);
            Assert.Equal(alive.Length - 1, Def.Meetings[1].BotVotes.Count);

            var choices = alive.Select(a => (int?)a).Append(-1).Append(null).ToArray();
            foreach (int? choice in choices)
            {
                Assert.Equal(WolfActor, TallyMeeting(1, alive, choice).ExecutedActor);
            }
        }

        [Fact]
        public void Meeting2_VotersAreAllAliveAfterBothCorpses()
        {
            int[] alive = AliveAtMeeting2();
            foreach (var vote in Def.Meetings[1].BotVotes)
            {
                Assert.Contains(Actor(vote.VoterParticipantId), alive);
                Assert.True(vote.IsSkip || alive.Contains(vote.TargetActor(Def.Cast, PlayerActor)));
            }
        }

        [Fact]
        public void Meeting1_SkipVotesResolveToTheSkipSentinel()
        {
            var skips = Def.Meetings[0].BotVotes.Where(v => v.IsSkip).ToArray();
            Assert.Equal(4, skips.Length);
            Assert.All(skips, v => Assert.Equal(-1, v.TargetActor(Def.Cast, PlayerActor)));
        }

        private static void AdvanceToFirstMeetingTally(ScenarioFlow flow)
        {
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanIntro4, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanTrance1, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanTranceOn1, flow.Current.Text);
            Assert.Equal(ScenarioNeed.ShamanTranceActive, flow.Current.Need);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanDrip1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanDowse1, flow.Current.Text);
            Assert.True(flow.Current.Has(ScenarioEntryAction.PlaceFarCorpse,
                ShamanScenarioScript.FirstCorpseParticipantId));

            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.ShamanGhostFired));
            Assert.Equal(TextId.ScenarioShamanBurst1, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.ShamanStormEntered));
            Assert.Equal(TextId.ScenarioShamanStorm1, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanReport1, flow.Current.Text);
            Assert.Equal(ScenarioHighlight.ReportIcon, flow.Current.Highlight);
            Assert.True(flow.HasReached(ScenarioMilestone.ActionPrompted));

            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.ConveneAccepted));
            Assert.False(flow.Current.HasText);

            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(TextId.ScenarioShamanOpened1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(ScenarioCue.ChatScript, flow.Current.Cue);
            Assert.Equal(0, flow.Current.MeetingIndex);

            Assert.True(flow.Feed(ScenarioSignal.ChatScriptDone));
            Assert.Equal(TextId.ScenarioShamanVote1, flow.Current.Text);
            Assert.True(flow.Feed(ScenarioSignal.VoteAccepted));
            Assert.Equal(ScenarioCue.BotVotes, flow.Current.Cue);
            Assert.Equal(0, flow.Current.MeetingIndex);

            Assert.True(flow.Feed(ScenarioSignal.TallyRevealed));
            Assert.Equal(TextId.ScenarioShamanTally1, flow.Current.Text);
        }

        [Fact]
        public void Flow_RunsBothMeetingsToTheVillagerWin()
        {
            ScenarioFlow flow = Def.CreateFlow();
            AdvanceToFirstMeetingTally(flow);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanTally1, flow.Current.Text);
            flow.Feed(ScenarioSignal.MeetingReleased);
            Assert.Equal(TextId.ScenarioShamanTally1, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanDeliver1, flow.Current.Text);
            Assert.True(flow.Current.Has(ScenarioEntryAction.SpawnQuotaBag));
            foreach (ScenarioSignal signal in new[] { ScenarioSignal.ConveneAccepted, ScenarioSignal.MeetingOpened,
                         ScenarioSignal.ChatScriptDone, ScenarioSignal.VoteAccepted, ScenarioSignal.TallyRevealed })
            {
                Assert.False(flow.IsLatched(signal), signal.ToString());
            }

            Assert.False(flow.Feed(ScenarioSignal.Advance));
            flow.Feed(ScenarioSignal.ExtractionCompleted);
            Assert.True(flow.Feed(ScenarioSignal.LastRunReached));
            Assert.Equal(TextId.ScenarioShamanLastRun1, flow.Current.Text);
            Assert.True(flow.Current.Has(ScenarioEntryAction.PlaceTruckCorpse,
                ShamanScenarioScript.SecondCorpseParticipantId));

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanBlocked1, flow.Current.Text);
            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.CorpseReportDenied));
            Assert.Equal(TextId.ScenarioShamanDenied1, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanButton1, flow.Current.Text);
            Assert.True(flow.Current.Has(ScenarioEntryAction.GrantMeetingRight));

            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.ConveneAccepted));
            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(TextId.ScenarioShamanOpened4, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(ScenarioCue.ChatScript, flow.Current.Cue);
            Assert.Equal(1, flow.Current.MeetingIndex);

            Assert.False(flow.Feed(ScenarioSignal.VoteAccepted));
            Assert.Equal(ScenarioCue.ChatScript, flow.Current.Cue);
            Assert.Equal(1, flow.Current.MeetingIndex);

            Assert.True(flow.Feed(ScenarioSignal.ChatScriptDone));
            Assert.Equal(ScenarioCue.BotVotes, flow.Current.Cue);
            Assert.Equal(1, flow.Current.MeetingIndex);
            Assert.True(flow.Feed(ScenarioSignal.TallyRevealed));
            Assert.Equal(TextId.ScenarioShamanTally3, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.WinCeremonyDone));
            Assert.Equal(TextId.ScenarioShamanResult1, flow.Current.Text);
            flow.Feed(ScenarioSignal.ResultScreenShown);
            Assert.Equal(TextId.ScenarioShamanResult1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanPostgame1, flow.Current.Text);

            for (int i = 0; i < 3; i++) flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanPostgame3, flow.Current.Text);
            Assert.True(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Finished);
        }

        [Fact]
        public void Flow_WalkingIntoTheStormEarlySkipsTheBurstLesson()
        {
            ScenarioFlow flow = Def.CreateFlow();
            for (int i = 0; i < 5; i++) flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanDowse1, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.ShamanStormEntered));
            Assert.Equal(TextId.ScenarioShamanStorm1, flow.Current.Text);
        }

        [Fact]
        public void Flow_ReportingBeforeThePromptSkipsToTheMeeting()
        {
            ScenarioFlow flow = Def.CreateFlow();
            for (int i = 0; i < 5; i++) flow.Feed(ScenarioSignal.Advance);
            flow.Feed(ScenarioSignal.ShamanStormEntered);
            Assert.Equal(TextId.ScenarioShamanStorm1, flow.Current.Text);
            Assert.False(flow.HasReached(ScenarioMilestone.ActionPrompted));

            Assert.True(flow.Feed(ScenarioSignal.ConveneAccepted));
            Assert.False(flow.Current.HasText);
            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(TextId.ScenarioShamanOpened1, flow.Current.Text);
        }

        [Fact]
        public void Flow_ReportingBeforeTheTranceLessonStillReachesTheMeeting()
        {
            ScenarioFlow flow = Def.CreateFlow();
            for (int i = 0; i < 2; i++) flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanTrance1, flow.Current.Text);

            flow.Feed(ScenarioSignal.ShamanStormEntered);
            Assert.Equal(TextId.ScenarioShamanTrance1, flow.Current.Text);
            Assert.True(flow.Feed(ScenarioSignal.ConveneAccepted));
            Assert.False(flow.Current.HasText);
            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(TextId.ScenarioShamanOpened1, flow.Current.Text);
        }

        [Fact]
        public void Flow_ExtractionBeforeTheFirstMeetingIsUnrecoverable()
        {
            Assert.Equal(ScenarioMilestone.TruckBound, Def.Watch.ExtractionBreaksBefore);
            Assert.True(Def.Watch.BreakOnAllExtractionsCompleted);
            Assert.True(Def.Watch.RequireTallyBeforeResult);
            Assert.NotEmpty(Def.Notices.BreakLines(ScenarioBreakKind.EarlyExtraction));
            Assert.NotEmpty(Def.Notices.BreakLines(ScenarioBreakKind.AllExtractionsCompleted));
            Assert.Empty(Def.Notices.ExtractionNag);

            ScenarioFlow flow = Def.CreateFlow();
            Assert.False(flow.HasReached(Def.Watch.ExtractionBreaksBefore));
            AdvanceToFirstMeetingTally(flow);
            Assert.True(flow.HasReached(Def.Watch.ExtractionBreaksBefore));
        }

        [Fact]
        public void Pages_GrantTheMeetingRightOnlyOnTheButtonPage()
        {
            var grants = Pages.Select((p, i) => (p, i))
                .Where(t => t.p.Has(ScenarioEntryAction.GrantMeetingRight)).ToArray();
            Assert.Single(grants);

            int grant = grants[0].i;
            Assert.Equal(TextId.ScenarioShamanButton1, Pages[grant].Text);
            Assert.True(grant > IndexOfGate(ScenarioSignal.ConveneAccepted));
            Assert.True(grant < LastIndexOfGate(ScenarioSignal.ConveneAccepted));
            Assert.True(grant > IndexOfGate(ScenarioSignal.CorpseReportDenied));
        }

        [Fact]
        public void Pages_PlaceEachCorpseRightBeforeItIsNeeded()
        {
            int far = Pages.ToList().FindIndex(p => p.Has(ScenarioEntryAction.PlaceFarCorpse));
            int truck = Pages.ToList().FindIndex(p => p.Has(ScenarioEntryAction.PlaceTruckCorpse));

            Assert.Equal(IndexOfText(TextId.ScenarioShamanDowse1), far);
            Assert.Equal(IndexOfGate(ScenarioSignal.LastRunReached), truck);
            Assert.True(far < IndexOfGate(ScenarioSignal.ShamanGhostFired));
            Assert.True(truck < IndexOfGate(ScenarioSignal.CorpseReportDenied));

            var needsACorpse = new[] { ScenarioSignal.ShamanGhostFired,
                ScenarioSignal.ShamanStormEntered, ScenarioSignal.ConveneAccepted };
            Assert.All(Pages[far].SkipIf, s => Assert.Contains(s, needsACorpse));
            for (int i = 0; i <= far; i++) Assert.DoesNotContain(Pages[i].Gate, needsACorpse);
            Assert.Equal(0, Def.Room.MeetingRightsPerPlayer);
        }

        [Fact]
        public void Pages_SpawnTheQuotaBagOnceOnThePrompt()
        {
            var bags = Pages.Select((p, i) => (p, i))
                .Where(t => t.p.Has(ScenarioEntryAction.SpawnQuotaBag)).ToArray();

            Assert.Single(bags);
            Assert.Equal(TextId.ScenarioShamanDeliver1, bags[0].p.Text);
            Assert.Contains(ScenarioSignal.LastRunReached, bags[0].p.SkipIf);
            Assert.Equal(ScenarioSignal.ActionNag, Pages[bags[0].i + 1].Gate);
        }

        [Fact]
        public void Pages_ReportPromptsNameTheReportKey()
        {
            foreach (TextId id in new[] { TextId.ScenarioShamanReport1, TextId.ScenarioShamanReportNag1,
                                          TextId.ScenarioShamanBlocked1, TextId.ScenarioShamanBlockedNag1 })
            {
                Assert.Contains(ScenarioKeyTags.CorpseReport, Texts.TableFor(Language.Japanese)[id]);
                Assert.Contains(ScenarioKeyTags.CorpseReport, Texts.TableFor(Language.English)[id]);
            }
        }

        [Fact]
        public void Pages_MeetingOneEndsBeforeMeetingTwoStarts()
        {
            var forgetPages = Pages.Select((p, i) => (p, i))
                .Where(t => t.p.ForgetsOnEntry.Count > 0).ToArray();
            Assert.Single(forgetPages);
            int forget = forgetPages[0].i;

            Assert.Equal(IndexOfGate(ScenarioSignal.TallyRevealed), forget);
            Assert.True(forget < LastIndexOfGate(ScenarioSignal.ConveneAccepted));
            Assert.Equal(ScenarioMilestone.TruckBound, Pages[forget].Milestone);
        }

        [Fact]
        public void Notices_PickUpAnEarlyReportAndLeashAfterTheFirstMeeting()
        {
            Assert.Equal(ScenarioSignal.ConveneAccepted, Def.Notices.EarlyStartSignal);
            Assert.Equal(ScenarioMilestone.ActionPrompted, Def.Notices.EarlyStartAfter);
            Assert.NotEmpty(Def.Notices.LeashNag);
            Assert.True(Def.Notices.KeyHint.HasValue);
        }

        [Fact]
        public void Pages_WaitOnLiveStateInsteadOfLatchedSignals()
        {
            Assert.Equal(ScenarioNeed.ShamanTranceActive,
                PageOf(TextId.ScenarioShamanTranceOn1).Need);
            Assert.Equal(ScenarioNeed.ShamanStormActive,
                PageOf(TextId.ScenarioShamanStorm1).Need);
            Assert.Equal(ScenarioNeed.CorpseNear,
                PageOf(TextId.ScenarioShamanBlocked1).Need);
            Assert.Equal(ScenarioNeed.CorpseNear,
                PageOf(TextId.ScenarioShamanBlockedNag1).Need);

            Assert.Equal(ScenarioNeed.None, PageOf(TextId.ScenarioShamanTrance1).Need);
        }

        [Fact]
        public void Flow_TranceLessonIsAlwaysRead()
        {
            ScenarioPage lesson = PageOf(TextId.ScenarioShamanTrance1);
            Assert.Equal(new[] { ScenarioSignal.ConveneAccepted }, lesson.SkipIf);

            ScenarioFlow flow = Def.CreateFlow();
            for (int i = 0; i < 2; i++) flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioShamanTrance1, flow.Current.Text);
        }

        [Fact]
        public void Environment_WidensTheMapWithoutAddingExtractionPoints()
        {
            ScenarioEnvironmentSpec env = Def.Environment;

            Assert.True(env.ModuleAmount
                > ScenarioLevelSizeRules.VanillaModuleAmount(env.LevelsCompleted));

            Assert.Equal(2, env.ExtractionPoints);
            Assert.Equal(1, ScenarioLevelSizeRules.ExtractionRooms(env.ExtractionPoints));
        }

        [Fact]
        public void Notices_AnswerTheMapWithTheCoordinateAside()
        {
            Assert.NotEmpty(Def.Notices.MapAside);
            Assert.Contains(Def.Pages, p => p.Cue == ScenarioCue.ChatScript);

            var scripted = Def.Meetings.SelectMany(m => m.ChatScript).Select(l => l.Text).ToArray();
            foreach (TextId line in Def.Notices.MapAside)
            {
                Assert.DoesNotContain(line, scripted);
                Assert.DoesNotContain(Def.Pages, p => p.Lines.Contains(line));
            }
        }

        [Fact]
        public void CorpseHint_ShowsTheFirstCorpseAfterTheStormUntilTheReport()
        {
            ScenarioCorpseHint hint = Def.CorpseHint;
            Assert.True(hint.Enabled);
            Assert.Equal(ShamanScenarioScript.FirstCorpseParticipantId, hint.ParticipantId);
            Assert.Equal(ScenarioSignal.ShamanStormEntered, hint.After);
            Assert.Equal(ScenarioSignal.ConveneAccepted, hint.Until);
            Assert.True(hint.DelayMs > 0);
            Assert.NotEmpty(hint.Lines);
        }

        [Fact]
        public void CorpseHint_StartSignalIsNeverForgottenBeforeTheReport()
        {
            int report = IndexOfGate(ScenarioSignal.ConveneAccepted);
            for (int i = 0; i <= report; i++)
            {
                Assert.DoesNotContain(Def.CorpseHint.After, Pages[i].ForgetsOnEntry);
            }
        }
    }
}
