using System.Linq;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WerewolfScenarioScriptTests
    {
        private static ScenarioDefinition Def => WerewolfScenarioScript.Definition;

        [Fact]
        public void Cast_PlayerIsTheOnlyWerewolf()
        {
            Assert.Equal(Role.Werewolf, Def.Cast.PlayerRole);
            Assert.Equal(1, Def.Cast.WerewolfCount);
            Assert.All(Def.Cast.Bots, b => Assert.Equal(Role.Villager, b.Role));
        }

        [Fact]
        public void Cast_HasNoVariants()
        {
            Assert.Equal(0, Def.Cast.BlackCatChancePercent);
            Assert.Equal(0, Def.Cast.BomberChancePercent);
            Assert.Equal(0, Def.Cast.ShamanChancePercent);
        }

        [Fact]
        public void Environment_OverridesTheQuotaByShareOfMapTotal()
        {
            Assert.True(Def.Environment.HaulGoalOverridden);
            Assert.InRange(Def.Environment.HaulGoalPctOfTotal, 1, 99);
        }

        [Fact]
        public void Environment_SeedsNoFakeLoss()
        {
            Assert.Equal(0, Def.Environment.SeededLossPct);
        }

        [Fact]
        public void Environment_GivesEnoughGrabStrengthToLiftValuables()
        {
            Assert.True(Def.Environment.StartUpgrades.TryGetValue("Strength", out int strength));
            Assert.True(strength > 0);
        }

        [Fact]
        public void Environment_NamesOnlyUpgradesTheGameAccepts()
        {
            foreach (var pair in Def.Environment.StartUpgrades)
            {
                Assert.Contains(pair.Key, WorldgenUpgrades.Names);
                Assert.True(pair.Value > 0, pair.Key);
            }
        }

        [Fact]
        public void Environment_DoesNotLeashThePlayerToTheTruck()
        {
            Assert.False(Def.Environment.TruckLeashEnabled);
        }

        [Fact]
        public void Environment_RaisesWeakEnemies_AndExcludesTier3()
        {
            EnemySetCounts sets = Def.Environment.EnemySets;

            Assert.True(sets.Danger1 >= 1);
            Assert.True(sets.Danger2 >= 0);
            Assert.Equal(0, sets.Danger3);
        }

        [Fact]
        public void Flow_HoldsEnemiesDespawned_OnThePageThatWaitsForTheBeacon()
        {
            var beaconPage = Def.Pages.Single(p => p.Has(ScenarioEntryAction.HoldEnemiesDespawned));

            Assert.Contains(ScenarioSignal.BeaconUsed, beaconPage.SkipIf);
        }

        [Fact]
        public void Safety_LetsEnemiesSpawnAndDamageLand()
        {
            Assert.False(Def.Safety.SuppressEnemies);
            Assert.False(Def.Safety.Invincible);
            Assert.True(Def.Safety.ClampDamage);
        }

        [Fact]
        public void Safety_ReleasesThePlayerFromAProlongedGrab()
        {
            Assert.True(Def.Safety.GrabReleaseEnabled);
        }

        [Fact]
        public void Room_ClosesTheMeeting()
        {
            Assert.Equal(0, Def.Room.MeetingRightsPerPlayer);
        }

        [Fact]
        public void Room_UnlocksEveryPerkBeforeCheckmateIsReachable()
        {
            int destroyedAtCheckmate = 100 - Def.Environment.HaulGoalPctOfTotal;

            Assert.True(Def.Room.StaminaUnlockPct.HasValue);
            Assert.True(Def.Room.JumpUnlockPct.HasValue);
            Assert.True(Def.Room.EnemyIgnoreUnlockPct.HasValue);
            Assert.True(Def.Room.HealUnlockPct.HasValue);
            Assert.True(Def.Room.BeaconChargePct.HasValue);

            Assert.InRange(Def.Room.StaminaUnlockPct.Value, 1, destroyedAtCheckmate - 1);
            Assert.InRange(Def.Room.JumpUnlockPct.Value, 1, destroyedAtCheckmate - 1);
            Assert.InRange(Def.Room.EnemyIgnoreUnlockPct.Value, 1, destroyedAtCheckmate - 1);
            Assert.InRange(Def.Room.HealUnlockPct.Value, 1, destroyedAtCheckmate - 1);
            Assert.InRange(Def.Room.BeaconChargePct.Value, 1, destroyedAtCheckmate - 1);
        }

        [Fact]
        public void Room_KeepsUnlockOrderAndSpacing()
        {
            int stamina = Def.Room.StaminaUnlockPct.Value;
            int jump = Def.Room.JumpUnlockPct.Value;
            int ignore = Def.Room.EnemyIgnoreUnlockPct.Value;
            int heal = Def.Room.HealUnlockPct.Value;

            Assert.True(stamina < jump);
            Assert.True(jump < ignore);
            Assert.True(ignore < heal);
            Assert.True(jump - stamina >= 5);
            Assert.True(ignore - jump >= 5);
            Assert.True(heal - ignore >= 5);
        }

        [Fact]
        public void Room_LetsTheBeaconFireFromTheStart()
        {
            Assert.Equal(0, Def.Room.BeaconSuppressStartSec);
        }

        [Fact]
        public void Watch_TreatsAllExtractionsCompletedAsUnrecoverable()
        {
            Assert.True(Def.Watch.BreakOnAllExtractionsCompleted);
            Assert.Equal(0, CheckmateJudge.RemainingQuotaDollars(10000, 2, 2));
        }

        [Fact]
        public void Watch_DoesNotRequireATallyBeforeTheResultScreen()
        {
            Assert.False(Def.Watch.RequireTallyBeforeResult);
            Assert.Null(Def.Watch.MeetingRequiredFrom);
        }

        [Fact]
        public void Pages_HoldCheckmateUntilTheFreeDestructionPage()
        {
            ScenarioFlow flow = Def.CreateFlow();
            Assert.False(flow.HasReached(ScenarioMilestone.CheckmateArmed));

            int armIndex = Def.Pages.ToList()
                .FindIndex(p => p.Milestone == ScenarioMilestone.CheckmateArmed);
            Assert.True(armIndex > 0);

            Assert.Equal(ScenarioSignal.WinCeremonyDone, Def.Pages[armIndex + 1].Gate);
        }

        [Fact]
        public void Pages_TeachTheEnemyMapBeforeSendingThePlayerOut()
        {
            var lines = Def.Pages.Select(p => p.HasText ? p.Text : default).ToList();
            int enemyMap = lines.IndexOf(TextId.ScenarioWolfEnemyMap1);
            int firstDestroy = lines.IndexOf(TextId.ScenarioWolfDestroy1);

            Assert.True(enemyMap >= 0);
            Assert.True(firstDestroy >= 0);
            Assert.True(enemyMap < firstDestroy);
        }

        [Fact]
        public void Pages_WaitForTheWolfFormToggleBeforeMovingOn()
        {
            var gates = Def.Pages.Select(p => p.Gate).ToList();
            var lines = Def.Pages.Select(p => p.HasText ? p.Text : default).ToList();

            int prompt = lines.IndexOf(TextId.ScenarioWolfFormOn1);
            int turnedOn = gates.IndexOf(ScenarioSignal.WolfFormTurnedOn);

            Assert.True(prompt >= 0);
            Assert.True(turnedOn > prompt);
        }

        [Fact]
        public void Pages_TeachTheRecordToggleBetweenTheTwoDestructions()
        {
            var gates = Def.Pages.Select(p => p.Gate).ToList();
            int firstDestroy = gates.IndexOf(ScenarioSignal.ValuableDestroyed);
            int recordOff = gates.IndexOf(ScenarioSignal.RecordTurnedOff);
            int stealthDestroy = gates.IndexOf(ScenarioSignal.StealthDestroyed);

            Assert.True(firstDestroy >= 0);
            Assert.True(firstDestroy < recordOff);
            Assert.True(recordOff < stealthDestroy);
        }

        [Fact]
        public void Pages_ShowTheMarkerLessonWithTheMapOpen()
        {
            ScenarioPage marker = Def.Pages.First(p => p.Gate == ScenarioSignal.ValuableDestroyed);
            ScenarioPage stealth = Def.Pages.First(p => p.Gate == ScenarioSignal.StealthDestroyed);

            Assert.Equal(ScenarioNeed.MapToolOpen, marker.Need);
            Assert.Equal(ScenarioNeed.MapToolOpen, stealth.Need);
        }

        [Fact]
        public void Pages_PromptFurtherDestructionOnItsOwnPage()
        {
            var pushes = Def.Pages
                .Where(p => p.HasText
                            && (p.Text == TextId.ScenarioWolfPush1 || p.Text == TextId.ScenarioWolfPush2))
                .ToList();

            Assert.NotEmpty(pushes);
            Assert.All(pushes, p => Assert.Single(p.Lines));
            Assert.All(pushes, p => Assert.NotEmpty(p.SkipIf));
        }

        [Fact]
        public void Pages_SkipTheWaitWhenThePerkIsAlreadyUnlocked()
        {
            for (int i = 0; i + 1 < Def.Pages.Count; i++)
            {
                ScenarioSignal next = Def.Pages[i + 1].Gate;
                if (!IsPerkUnlock(next)) continue;
                Assert.Equal(new[] { next }, Def.Pages[i].SkipIf);
            }
        }

        private static bool IsPerkUnlock(ScenarioSignal signal)
            => signal == ScenarioSignal.PerkStaminaUnlocked
               || signal == ScenarioSignal.PerkJumpUnlocked
               || signal == ScenarioSignal.PerkEnemyIgnoreUnlocked
               || signal == ScenarioSignal.PerkHealUnlocked;

        [Fact]
        public void Pages_PromptedActionsCanBeSkippedWhenAlreadyDone()
        {
            foreach (ScenarioPage page in Def.Pages.Where(p => p.Cue == ScenarioCue.ActionNag))
            {
                Assert.NotEmpty(page.SkipIf);
            }
        }

        [Fact]
        public void Pages_StayOffTheWolfStatusHud()
        {
            foreach (ScenarioPage page in Def.Pages)
            {
                Assert.True(page.Anchor == ScenarioAnchor.PlayHud
                            || page.Anchor == ScenarioAnchor.PlayHudRight
                            || page.Anchor == ScenarioAnchor.CheckmateStamp
                            || page.Anchor == ScenarioAnchor.ReplayEntry);
            }
            Assert.Contains(Def.Pages, p => p.Anchor == ScenarioAnchor.PlayHud);
        }

        [Fact]
        public void Pages_ReadTheCheckmateCeremonyBesideItsStamp()
        {
            ScenarioPage ceremony = Def.Pages.First(p => p.Gate == ScenarioSignal.WinCeremonyDone);

            Assert.Equal(ScenarioAnchor.CheckmateStamp, ceremony.Anchor);
            Assert.True(ceremony.MustRead);
        }

        [Fact]
        public void Pages_ClearTheCheckmateStampFromThePageTheCeremonyStartsOn()
        {
            ScenarioPage armed = Def.Pages.First(p => p.Milestone == ScenarioMilestone.CheckmateArmed);
            ScenarioPlayHud playHud = ScenarioGuideLayout.PlayHudOf(Def.Cast.PlayerRole);

            Assert.Equal(ScenarioHighlight.None, armed.Highlight);

            foreach (float height in new[] { 120f, 220f, 320f })
            {
                GuidePlacement placement = ScenarioGuideLayout.Place(
                    new CanvasBounds(1920f, 1080f), armed.Anchor, 648f, height, playHud);
                Assert.True(placement.Y >= ScenarioGuideLayout.CheckmateTitleTopY, $"h={height}");
            }
        }

        [Fact]
        public void Pages_HandOverABeaconChargeBeforeAskingForAShot()
        {
            var pages = Def.Pages;
            int grant = pages.ToList()
                .FindIndex(p => p.Has(ScenarioEntryAction.GrantBeaconCharge));
            int prompt = pages.Select(p => p.HasText ? p.Text : default)
                              .ToList().IndexOf(TextId.ScenarioWolfBeacon1);

            Assert.True(grant >= 0);
            Assert.True(prompt > grant);
        }

        [Fact]
        public void Pages_MoveTheBubbleToTheBottomRightTargets()
        {
            foreach (ScenarioPage page in Def.Pages)
            {
                if (page.Highlight != ScenarioHighlight.PlayGauge
                    && page.Highlight != ScenarioHighlight.RecordIcon) continue;
                Assert.Equal(ScenarioAnchor.PlayHudRight, page.Anchor);
            }
        }

        [Fact]
        public void Pages_EndOnTheResultScreen()
        {
            var gates = Def.Pages.Select(p => p.Gate).ToList();
            Assert.Contains(ScenarioSignal.WinCeremonyDone, gates);
            Assert.Contains(ScenarioSignal.ResultScreenShown, gates);
        }

        [Fact]
        public void Notices_AnchorTheEarlyStartCheckToTheDestructionPrompt()
        {
            Assert.Equal(ScenarioSignal.ValuableDestroyed, Def.Notices.EarlyStartSignal);
            Assert.Equal(ScenarioMilestone.ActionPrompted, Def.Notices.EarlyStartAfter);

            ScenarioPage prompt = Def.Pages.Single(
                p => p.Milestone == ScenarioMilestone.ActionPrompted);
            Assert.Equal(TextId.ScenarioWolfDestroy1, prompt.Text);
        }

        [Fact]
        public void Notices_NameTheReasonWhenAllExtractionsEndTheRun()
        {
            Assert.True(Def.Watch.BreakOnAllExtractionsCompleted);
            Assert.NotEmpty(Def.Notices.BreakLines(ScenarioBreakKind.AllExtractionsCompleted));
            Assert.DoesNotContain(TextId.ScenarioBrokenNotice,
                Def.Notices.BreakLines(ScenarioBreakKind.AllExtractionsCompleted));
        }

        [Fact]
        public void Notices_ScoldAnExtractionAndAnswerAnEagerPlayer()
        {
            Assert.NotEmpty(Def.Notices.ExtractionNag);
            Assert.NotEmpty(Def.Notices.EarlyStartLines);
            Assert.Equal(ScenarioSignal.ValuableDestroyed, Def.Notices.EarlyStartSignal);
            Assert.Empty(Def.Notices.LeashNag);
        }

        [Fact]
        public void Script_HasNoMeetingContent()
        {
            Assert.Empty(Def.Meetings[0].ChatScript);
            Assert.Empty(Def.Meetings[0].BotVotes);
        }
    }
}
