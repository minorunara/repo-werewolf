using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class BomberScenarioScriptTests
    {
        private const int PlayerActor = 1;

        private static ScenarioDefinition Def => BomberScenarioScript.Definition;
        private static int Actor(int participantId) => Def.Cast.ActorOf(participantId, PlayerActor);
        private static int WolfActor => Actor(BomberScenarioScript.WerewolfParticipantId);
        private static IReadOnlyList<ScenarioPage> Pages => Def.Pages;

        private static int IndexOfText(TextId id)
            => Pages.ToList().FindIndex(p => p.HasText && p.Text == id);

        private static int IndexOfGate(ScenarioSignal gate)
            => Pages.ToList().FindIndex(p => p.Gate == gate);

        private static ScenarioPage PageOf(TextId id)
        {
            int index = IndexOfText(id);
            Assert.True(index >= 0, id.ToString());
            return Pages[index];
        }

        [Fact]
        public void Cast_PlayerIsTheBomberWithOneFellowWerewolf()
        {
            Assert.Equal(Role.Bomber, Def.Cast.PlayerRole);

            Assert.Equal(2, Def.Cast.WerewolfCount);
            Assert.Equal(100, Def.Cast.BomberChancePercent);
            Assert.Equal(0, Def.Cast.BlackCatChancePercent);
            Assert.Equal(0, Def.Cast.ShamanChancePercent);

            var wolves = Def.Cast.Bots.Where(b => b.Role == Role.Werewolf).ToArray();
            Assert.Single(wolves);
            Assert.Equal(BomberScenarioScript.WerewolfParticipantId, wolves[0].ParticipantId);
        }

        [Fact]
        public void Cast_TargetAndBystanderAreVillagersAndDistinct()
        {
            Assert.True(Def.Cast.TryGetBot(BomberScenarioScript.TargetParticipantId, out var target));
            Assert.Equal(Role.Villager, target.Role);
            Assert.True(Def.Cast.TryGetBot(BomberScenarioScript.BystanderParticipantId, out var by));
            Assert.Equal(Role.Villager, by.Role);
            Assert.NotEqual(BomberScenarioScript.TargetParticipantId,
                BomberScenarioScript.BystanderParticipantId);
        }

        [Fact]
        public void Pages_PlantTheFirstBombOnTheFellowWerewolfAndTheSecondOnAVillager()
        {
            Assert.Equal(BomberScenarioScript.WerewolfParticipantId,
                PageOf(TextId.ScenarioBomberPlant3).BombTargetParticipantId);
            Assert.Equal(BomberScenarioScript.WerewolfParticipantId,
                PageOf(TextId.ScenarioBomberSafe1).BombTargetParticipantId);
            Assert.Equal(BomberScenarioScript.TargetParticipantId,
                PageOf(TextId.ScenarioBomberReplant4).BombTargetParticipantId);
            Assert.Equal(BomberScenarioScript.TargetParticipantId,
                PageOf(TextId.ScenarioBomberFire1).BombTargetParticipantId);
        }

        [Fact]
        public void Pages_NameThePlantTargetEverywhereABombCanBePlanted()
        {
            int firstPlant = IndexOfText(TextId.ScenarioBomberPlant3);
            int firstFired = IndexOfGate(ScenarioSignal.BombDetonationAccepted);
            for (int i = 0; i <= firstPlant; i++)
            {
                Assert.Equal(BomberScenarioScript.WerewolfParticipantId,
                    Pages[i].BombTargetParticipantId);
            }
            for (int i = firstPlant; i < firstFired; i++)
            {
                Assert.Equal(BomberScenarioScript.WerewolfParticipantId,
                    Pages[i].BombTargetParticipantId);
            }

            int grant = Pages.ToList().FindIndex(p => p.Has(ScenarioEntryAction.GrantBombAmmo));
            int secondFired = LastIndexOfGate(ScenarioSignal.BombDetonationAccepted);
            Assert.True(grant >= 0 && secondFired > grant);
            for (int i = grant; i < secondFired; i++)
            {
                Assert.Equal(BomberScenarioScript.TargetParticipantId,
                    Pages[i].BombTargetParticipantId);
            }
        }

        [Fact]
        public void Notices_CorrectTheAimWhenThePlantGoesToTheWrongBot()
        {
            Assert.NotEmpty(Def.Notices.WrongTargetLines);

            Assert.NotEmpty(Def.Notices.AlreadyOnTargetLines);

            foreach (Language language in new[] { Language.Japanese, Language.English })
            {
                Assert.Contains(Def.Notices.AlreadyOnTargetLines,
                    line => Texts.TableFor(language)[line].Contains("{0}"));
                Assert.Contains(Def.Notices.WrongTargetLines,
                    line => Texts.TableFor(language)[line].Contains("{0}"));
            }
        }

        [Fact]
        public void Flow_ExplainsWhyTheFirstBombGoesOnTheAllyBeforeThePlantPrompt()
        {
            int aim = IndexOfText(TextId.ScenarioBomberAim1);
            int plant = IndexOfText(TextId.ScenarioBomberPlant3);

            Assert.True(aim >= 0 && plant >= 0);
            Assert.True(aim < plant, "仲間へ植える理由は操作を促す前に言う");
        }

        [Fact]
        public void Environment_UsesLevelTwoWithNoLeashNoSeedNoUpgrades()
        {
            Assert.Equal(1, Def.Environment.LevelsCompleted);
            Assert.Equal(0, Def.Environment.SeededLossPct);
            Assert.False(Def.Environment.TruckLeashEnabled);
            Assert.Empty(Def.Environment.StartUpgrades);
            Assert.False(Def.Environment.HaulGoalOverridden);
        }

        [Fact]
        public void Room_ShortensBothBomberCooldownsAndGivesNoMeetingRights()
        {
            var defaults = new GameConfig();

            Assert.NotNull(Def.Room.BomberInitialCooldownSec);
            Assert.NotNull(Def.Room.BomberCooldownSec);
            Assert.InRange(Def.Room.BomberInitialCooldownSec.Value, 1, defaults.BomberInitialCooldownSec - 1);
            Assert.InRange(Def.Room.BomberCooldownSec.Value, 1, defaults.BomberCooldownSec - 1);
            Assert.Equal(0, Def.Room.MeetingRightsPerPlayer);

            var config = ScenarioGameConfig.Build(defaults, Def);
            Assert.Equal(Def.Room.BomberInitialCooldownSec.Value, config.BomberInitialCooldownSec);
            Assert.Equal(Def.Room.BomberCooldownSec.Value, config.BomberCooldownSec);
            Assert.Equal(defaults.BomberGaugeFullSec, config.BomberGaugeFullSec);
            Assert.Equal(defaults.BomberBlastRadiusMeters, config.BomberBlastRadiusMeters);
        }

        [Fact]
        public void Flow_ExplainsTheShortenedCooldownWhereItPointsAtTheBomberHud()
        {
            ScenarioPage hud = PageOf(TextId.ScenarioBomberIntro3);
            Assert.Contains(TextId.ScenarioBomberIntro4, hud.Lines);
            Assert.Contains(TextId.ScenarioBomberIntro5, hud.Lines);
            Assert.Equal(ScenarioHighlight.BomberHud, hud.Highlight);
            Assert.True(IndexOfText(TextId.ScenarioBomberIntro3)
                < IndexOfText(TextId.ScenarioBomberPlant3));
        }

        [Fact]
        public void Safety_StartsShelteredSoTheRunIsSafeUntilTheFatalBlast()
        {
            Assert.True(Def.Safety.SuppressEnemies);
            Assert.True(Def.Safety.Invincible);
            Assert.False(Def.Safety.ClampDamage);
        }

        [Fact]
        public void FatalBlastArmed_MarkedOnceAtTheSecondDetonationAccept()
        {
            var armed = Pages.Where(p => p.Milestone == ScenarioMilestone.FatalBlastArmed).ToArray();
            Assert.Single(armed);

            int armedIndex = Pages.ToList().FindIndex(p => p.Milestone == ScenarioMilestone.FatalBlastArmed);
            Assert.Equal(ScenarioSignal.BombDetonationAccepted, Pages[armedIndex].Gate);
            Assert.Equal(LastIndexOfGate(ScenarioSignal.BombDetonationAccepted), armedIndex);
            Assert.True(Def.Watch.BreakIfFatalBlastSurvived);
            Assert.NotEmpty(Def.Notices.BreakLines(ScenarioBreakKind.FatalBlastSurvived));
        }

        [Fact]
        public void Flow_SafetyIsLiftedOnlyAtTheFatalBlastArmedPage()
        {
            Assert.True(Def.CreateFlow().HasMilestone(ScenarioMilestone.FatalBlastArmed));

            ScenarioFlow flow = Def.CreateFlow();
            RunToSecondPlant(flow);
            Assert.False(flow.HasReached(ScenarioMilestone.FatalBlastArmed));

            Assert.True(flow.Feed(ScenarioSignal.BombPlanted));
            Assert.Equal(TextId.ScenarioBomberFire1, flow.Current.Text);
            Assert.False(flow.HasReached(ScenarioMilestone.FatalBlastArmed));

            Assert.True(flow.Feed(ScenarioSignal.BombDetonationAccepted));
            Assert.True(flow.HasReached(ScenarioMilestone.FatalBlastArmed));
        }

        [Fact]
        public void Flow_TheDeliberateSelfDestructIsAnnouncedBeforeTheSecondPlantAndTheFatalBlast()
        {
            int notice = IndexOfText(TextId.ScenarioBomberReplant1);
            int replant = IndexOfText(TextId.ScenarioBomberReplant4);
            int armed = Pages.ToList().FindIndex(p => p.Milestone == ScenarioMilestone.FatalBlastArmed);

            Assert.True(notice >= 0 && replant >= 0 && armed >= 0);
            Assert.True(notice < replant, "自爆の明示は2発目のプラントより前");
            Assert.True(notice < armed, "自爆の明示は安全化の解除より前");

            Assert.True(IndexOfText(TextId.ScenarioBomberResult1)
                < IndexOfText(TextId.ScenarioBomberPostgame1));
            foreach (Language language in new[] { Language.Japanese, Language.English })
            {
                Assert.False(string.IsNullOrWhiteSpace(
                    Texts.TableFor(language)[TextId.ScenarioBomberResult1]));
            }
        }

        [Fact]
        public void Pages_CarryTheRightDetonationRule()
        {
            Assert.Equal(ScenarioBlastRule.AwaitTruckZone, PageOf(TextId.ScenarioBomberTry1).BlastRule);
            Assert.Equal(ScenarioBlastRule.OutsideBlast, PageOf(TextId.ScenarioBomberSafe1).BlastRule);
            Assert.Equal(ScenarioBlastRule.PointBlank, PageOf(TextId.ScenarioBomberFire1).BlastRule);
            Assert.Equal(ScenarioBlastRule.None, PageOf(TextId.ScenarioBomberPlant1).BlastRule);
            Assert.Equal(ScenarioBlastRule.None, PageOf(TextId.ScenarioBomberBoom1).BlastRule);
        }

        [Fact]
        public void Pages_NameTheKeyWhileTheyWaitForThatKey()
        {
            var expected = new Dictionary<ScenarioSignal, string>
            {
                [ScenarioSignal.BombPlanted] = ScenarioKeyTags.BombPlant,
                [ScenarioSignal.BombDetonationAccepted] = ScenarioKeyTags.BombDetonate,
                [ScenarioSignal.BombDeniedTruckZone] = ScenarioKeyTags.BombDetonate,
            };

            foreach (Language language in new[] { Language.Japanese, Language.English })
            {
                var table = Texts.TableFor(language);
                for (int i = 1; i < Pages.Count; i++)
                {
                    if (!expected.TryGetValue(Pages[i].Gate, out string tag)) continue;

                    int shown = i - 1;
                    while (shown > 0 && !Pages[shown].HasText) shown--;

                    string text = string.Join(" ", Pages[shown].Lines.Select(id => table[id]));
                    Assert.True(text.Contains(tag),
                        $"{language} page {shown} が {tag} を名指ししないまま {Pages[i].Gate} を待つ");
                }
            }
        }

        [Fact]
        public void NagPages_InheritTheDetonationRuleFromTheirPrompt()
        {
            int fire = IndexOfText(TextId.ScenarioBomberFire1);
            Assert.Equal(ScenarioSignal.ActionNag, Pages[fire + 1].Gate);
            Assert.Equal(ScenarioBlastRule.PointBlank, Pages[fire + 1].BlastRule);

            Assert.Equal(BomberScenarioScript.TargetParticipantId, Pages[fire + 1].BombTargetParticipantId);

            int safe = IndexOfText(TextId.ScenarioBomberSafe1);
            Assert.Equal(ScenarioSignal.ActionNag, Pages[safe + 1].Gate);
            Assert.Equal(ScenarioBlastRule.OutsideBlast, Pages[safe + 1].BlastRule);
            Assert.Equal(BomberScenarioScript.WerewolfParticipantId,
                Pages[safe + 1].BombTargetParticipantId);
        }

        [Fact]
        public void Pages_MarkTheTargetOnlyWhileWalkingToIt()
        {
            foreach (TextId approach in new[]
                     {
                         TextId.ScenarioBomberPlant1, TextId.ScenarioBomberPlant3,
                         TextId.ScenarioBomberReplant1, TextId.ScenarioBomberReplant4,
                     })
            {
                Assert.True(PageOf(approach).MarkBombTarget, approach.ToString());
            }

            foreach (TextId away in new[] { TextId.ScenarioBomberSafe1, TextId.ScenarioBomberFire1 })
            {
                Assert.False(PageOf(away).MarkBombTarget, away.ToString());
            }

            foreach (ScenarioPage page in Pages)
            {
                if (page.MarkBombTarget) Assert.NotEqual(0, page.BombTargetParticipantId);
            }
        }

        [Fact]
        public void Pages_RequireBeingNearTheTargetToPlantAndOnTheTargetToDetonate()
        {
            Assert.Equal(ScenarioNeed.BombTargetNear, PageOf(TextId.ScenarioBomberPlant3).Need);
            Assert.Equal(ScenarioNeed.BombTargetNear, PageOf(TextId.ScenarioBomberReplant4).Need);
            Assert.Equal(ScenarioNeed.BombOnTarget, PageOf(TextId.ScenarioBomberSafe1).Need);
            Assert.Equal(ScenarioNeed.BombOnTarget, PageOf(TextId.ScenarioBomberFire1).Need);
        }

        [Fact]
        public void Pages_GrantTheSecondAmmoOnceBeforeTheFatalBlast()
        {
            var grants = Pages.Select((p, i) => (p, i))
                .Where(t => t.p.Has(ScenarioEntryAction.GrantBombAmmo)).ToArray();
            Assert.Single(grants);
            Assert.Equal(TextId.ScenarioBomberReplant1, grants[0].p.Text);
            int armed = Pages.ToList().FindIndex(p => p.Milestone == ScenarioMilestone.FatalBlastArmed);
            Assert.True(grants[0].i < armed);
        }

        [Fact]
        public void Pages_ConveneTheFirstMeetingAsTheEpicenterBot()
        {
            var convene = Pages.Where(p => p.Has(ScenarioEntryAction.ConveneAsParticipant)).ToArray();
            Assert.Single(convene);
            Assert.Equal(BomberScenarioScript.WerewolfParticipantId, convene[0].EntrySteps
                .First(s => s.Action == ScenarioEntryAction.ConveneAsParticipant).Arg);
        }

        [Fact]
        public void Meeting1_IsLedByTheEpicenterWhoBlursTheTestimony()
        {
            var script = Def.Meetings[0].ChatScript;

            Assert.Equal(BomberScenarioScript.WerewolfParticipantId, script[0].ParticipantId);
            Assert.True(script.Count(l => l.ParticipantId == BomberScenarioScript.WerewolfParticipantId) >= 2);

            var accusers = Def.Meetings[0].BotVotes
                .Where(v => v.TargetParticipantId == BomberScenarioScript.PlayerParticipantId).ToArray();
            Assert.Single(accusers);
        }

        [Fact]
        public void Pages_PlaceEveryBodyAtTheStartAndLeaveOnlyTheAllyInThePlantRoom()
        {
            ScenarioPage intro = Pages[0];
            var plantRoom = intro.EntrySteps
                .Where(s => s.Action == ScenarioEntryAction.BodyToPlantSite).Select(s => s.Arg).ToArray();
            Assert.Equal(new[] { BomberScenarioScript.WerewolfParticipantId }, plantRoom);

            Assert.Contains(intro.EntrySteps, s => s.Action == ScenarioEntryAction.BodyToGatherSite
                && s.Arg == BomberScenarioScript.TargetParticipantId);
            Assert.Contains(intro.EntrySteps, s => s.Action == ScenarioEntryAction.BodyToGatherSite
                && s.Arg == BomberScenarioScript.BystanderParticipantId);
            Assert.Equal(2, intro.EntrySteps.Count(s => s.Action == ScenarioEntryAction.BodyToFarSite));

            var actions = intro.EntrySteps.Select(s => s.Action).ToList();
            Assert.True(actions.IndexOf(ScenarioEntryAction.BodyToPlantSite)
                < actions.IndexOf(ScenarioEntryAction.BodyToGatherSite));
            Assert.True(actions.IndexOf(ScenarioEntryAction.BodyToGatherSite)
                < actions.IndexOf(ScenarioEntryAction.BodyToFarSite));
        }

        [Fact]
        public void Pages_PlaceTheBombTargetOnTheStandSiteBeforeAskingToApproachIt()
        {
            var placedBy = new Dictionary<int, ScenarioEntryAction>();
            foreach (ScenarioPage page in Pages)
            {
                foreach (var step in page.EntrySteps)
                {
                    if (step.Action == ScenarioEntryAction.BodyToPlantSite
                        || step.Action == ScenarioEntryAction.BodyToGatherSite
                        || step.Action == ScenarioEntryAction.BodyToFarSite)
                    {
                        placedBy[step.Arg] = step.Action;
                    }
                }

                if (page.Need != ScenarioNeed.BombTargetNear) continue;

                int target = page.BombTargetParticipantId;
                Assert.True(placedBy.TryGetValue(target, out ScenarioEntryAction action),
                    $"参加者 {target} へ近寄らせるページの前に、その参加者を置いていない");
                Assert.True(action == ScenarioEntryAction.BodyToPlantSite,
                    $"参加者 {target} が {action} で置かれている（近寄らせる的は BodyToPlantSite で置く）");
            }
        }

        [Fact]
        public void Pages_RecordBothVictimsOnlyAfterTheFatalBlast()
        {
            var kills = Pages.Select((p, i) => (p, i))
                .Where(t => t.p.Has(ScenarioEntryAction.KillParticipant)).ToArray();
            Assert.Single(kills);
            int armed = Pages.ToList().FindIndex(p => p.Milestone == ScenarioMilestone.FatalBlastArmed);
            Assert.True(kills[0].i > armed, "巻き込まれた者の死亡記録は自爆の後");

            var victims = kills[0].p.EntrySteps
                .Where(s => s.Action == ScenarioEntryAction.KillParticipant).Select(s => s.Arg).ToArray();
            Assert.Contains(BomberScenarioScript.WerewolfParticipantId, victims);
            Assert.Contains(BomberScenarioScript.BystanderParticipantId, victims);
            Assert.DoesNotContain(BomberScenarioScript.TargetParticipantId, victims);
        }

        [Fact]
        public void Meeting1_SkipsWhateverThePlayerDoes()
        {
            int[] alive = new[] { PlayerActor }.Concat(Def.Cast.Bots.Select(b => b.ActorNumber)).ToArray();
            Assert.Equal(6, alive.Length);
            Assert.Equal(alive.Length - 1, Def.Meetings[0].BotVotes.Count);

            var choices = alive.Select(a => (int?)a).Append(-1).Append(null).ToArray();
            foreach (int? choice in choices)
            {
                var box = new VoteBox(alive);
                if (choice.HasValue)
                {
                    Assert.Equal(VoteRejectReason.None, box.TryCast(PlayerActor, choice.Value));
                }
                foreach (var vote in Def.Meetings[0].BotVotes)
                {
                    Assert.Equal(VoteRejectReason.None,
                        box.TryCast(Actor(vote.VoterParticipantId), vote.TargetActor(Def.Cast, PlayerActor)));
                }
                MeetingOutcome outcome = box.Tally(_ => true);
                Assert.Equal(-1, outcome.ExecutedActor);
                int topNamed = outcome.VoteCounts.Where((_, i) => outcome.TargetActors[i] != -1)
                    .DefaultIfEmpty(0).Max();
                Assert.True(outcome.SkipVotes > topNamed, "スキップが単独最多");
            }
        }

        [Fact]
        public void SecondBlast_KillsBothWerewolvesForAVillagerWin()
        {
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = PlayerActor, Role = Role.Bomber, Alive = true },
            };
            foreach (var bot in Def.Cast.Bots)
            {
                players.Add(new WPlayer { ActorNumber = bot.ActorNumber, Role = bot.Role, Alive = true });
            }

            foreach (var p in players)
            {
                if (p.ActorNumber == WolfActor
                    || p.ActorNumber == Actor(BomberScenarioScript.BystanderParticipantId)
                    || p.ActorNumber == PlayerActor)
                {
                    p.Alive = false;
                }
            }
            Assert.True(players.First(p => p.ActorNumber == Actor(BomberScenarioScript.TargetParticipantId)).Alive);

            WinResult result = WinJudge.Judge(players);
            Assert.Equal(Team.Villagers, result.WinningTeam);
            Assert.Equal(WinReason.WerewolvesEradicated, result.Reason);
        }

        [Fact]
        public void Watch_RequiresTheMeetingAndBreaksOnAllExtractionsOrSurvivedBlast()
        {
            Assert.Equal(ScenarioSignal.ConveneAccepted, Def.Watch.MeetingRequiredFrom);
            Assert.Equal(ScenarioSignal.TallyRevealed, Def.Watch.MeetingRequiredUntil);
            Assert.True(Def.Watch.BreakOnAllExtractionsCompleted);
            Assert.True(Def.Watch.BreakIfFatalBlastSurvived);
            Assert.False(Def.Watch.RequireTallyBeforeResult);
            Assert.NotEmpty(Def.Notices.BreakLines(ScenarioBreakKind.AllExtractionsCompleted));
        }

        [Fact]
        public void Flow_RunsTheWholeArcToTheEnd()
        {
            ScenarioFlow flow = Def.CreateFlow();
            RunToSecondPlant(flow);

            Assert.True(flow.Feed(ScenarioSignal.BombPlanted));
            Assert.Equal(TextId.ScenarioBomberFire1, flow.Current.Text);
            Assert.False(flow.Feed(ScenarioSignal.Advance));
            Assert.True(flow.Feed(ScenarioSignal.BombDetonationAccepted));
            Assert.True(flow.HasReached(ScenarioMilestone.FatalBlastArmed));
            Assert.True(flow.Feed(ScenarioSignal.BombExploded));

            Assert.True(flow.Feed(ScenarioSignal.WinCeremonyDone));
            Assert.Equal(TextId.ScenarioBomberResult1, flow.Current.Text);
            flow.Feed(ScenarioSignal.ResultScreenShown);
            Assert.Equal(TextId.ScenarioBomberResult1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberPostgame1, flow.Current.Text);

            for (int i = 0; i < 10 && !flow.Finished; i++) flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.Finished);
        }

        private static void RunToSecondPlant(ScenarioFlow flow)
        {
            flow.Feed(ScenarioSignal.Advance);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberAim1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberPlant1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberPlant3, flow.Current.Text);
            Assert.True(flow.HasReached(ScenarioMilestone.ActionPrompted));

            Assert.True(flow.Feed(ScenarioSignal.BombPlanted));
            Assert.Equal(TextId.ScenarioBomberPlanted1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberTry1, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.BombDeniedTruckZone));
            Assert.Equal(TextId.ScenarioBomberLock1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberSafe1, flow.Current.Text);

            Assert.True(flow.Feed(ScenarioSignal.BombDetonationAccepted));
            Assert.True(flow.Feed(ScenarioSignal.BombExploded));
            Assert.Equal(TextId.ScenarioBomberBoom1, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberMeet1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.True(flow.Feed(ScenarioSignal.ConveneAccepted));
            Assert.True(flow.Feed(ScenarioSignal.MeetingOpened));
            Assert.Equal(TextId.ScenarioBomberOpened1, flow.Current.Text);
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(ScenarioCue.ChatScript, flow.Current.Cue);
            Assert.True(flow.Feed(ScenarioSignal.ChatScriptDone));
            Assert.Equal(TextId.ScenarioBomberVote1, flow.Current.Text);
            Assert.True(flow.Feed(ScenarioSignal.VoteAccepted));
            Assert.Equal(ScenarioCue.BotVotes, flow.Current.Cue);
            Assert.True(flow.Feed(ScenarioSignal.TallyRevealed));
            Assert.Equal(TextId.ScenarioBomberTally1, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberTally1, flow.Current.Text);
            flow.Feed(ScenarioSignal.MeetingReleased);
            Assert.Equal(TextId.ScenarioBomberTally1, flow.Current.Text);

            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberReplant1, flow.Current.Text);
            foreach (ScenarioSignal signal in new[]
                     {
                         ScenarioSignal.BombPlanted, ScenarioSignal.BombDeniedTruckZone,
                         ScenarioSignal.BombDetonationAccepted, ScenarioSignal.BombExploded,
                     })
            {
                Assert.False(flow.IsLatched(signal), signal.ToString());
            }
            flow.Feed(ScenarioSignal.Advance);
            Assert.Equal(TextId.ScenarioBomberReplant4, flow.Current.Text);
        }

        private static int LastIndexOfGate(ScenarioSignal gate)
            => Pages.ToList().FindLastIndex(p => p.Gate == gate);
    }
}
