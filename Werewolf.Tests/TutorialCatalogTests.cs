using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class TutorialCatalogTests
    {
        [Theory]
        [InlineData(TutorialId.FirstMeetingAsBlackCat)]
        [InlineData(TutorialId.VillagerSeesCatAwakened)]
        [InlineData(TutorialId.BlackCatSelectedForExecution)]
        [InlineData(TutorialId.BlackCatExecutionRevealed)]
        public void ShouldShow_CurseDisabled_SuppressesCurseTutorials(TutorialId id)
        {
            Assert.False(TutorialCatalog.ShouldShow(id, blackCatCurseEnabled: false));
            Assert.True(TutorialCatalog.ShouldShow(id, blackCatCurseEnabled: true));
        }

        [Fact]
        public void Format_BlackCatRoleDrawnWithoutCurse_ExplainsHiddenAlliance()
        {
            string message = TutorialCatalog.Format(TutorialId.BlackCatRoleDrawn,
                blackCatCurseEnabled: false);

            Assert.Contains("お互いの正体を知らされない", message);
            Assert.Contains("人狼陣営", message);
            Assert.DoesNotContain("道連れ", message);
        }
        [Theory]
        [InlineData(TutorialId.CorpseDiscovery)]
        [InlineData(TutorialId.MeetingCountdownStarted)]
        [InlineData(TutorialId.FirstMeetingAsVillager)]
        [InlineData(TutorialId.WerewolfRoleDrawn)]
        [InlineData(TutorialId.FirstValuableSeen)]
        [InlineData(TutorialId.WolfModeFirstUnlock)]
        [InlineData(TutorialId.BeaconFirstCharged)]
        [InlineData(TutorialId.FirstMeetingAsWerewolf)]
        [InlineData(TutorialId.FirstMeetingAsBlackCat)]
        [InlineData(TutorialId.VillagerSeesCatAwakened)]
        [InlineData(TutorialId.BlackCatRoleDrawn)]
        [InlineData(TutorialId.LastRunApproaching)]
        [InlineData(TutorialId.RoundTimeWarningVillager)]
        [InlineData(TutorialId.RoundTimeWarningWerewolf)]
        [InlineData(TutorialId.FinalExtractionVillager)]
        [InlineData(TutorialId.FinalExtractionWerewolf)]
        [InlineData(TutorialId.InformantUnlockedAsWerewolf)]
        [InlineData(TutorialId.InformantUnlockedAsBlackCat)]
        [InlineData(TutorialId.EnemyIgnoreUnlockedAsWerewolf)]
        [InlineData(TutorialId.WerewolfSeesCatAwakened)]
        [InlineData(TutorialId.BeaconFirstUsedAsWerewolf)]
        [InlineData(TutorialId.BlackCatSelectedForExecution)]
        [InlineData(TutorialId.BlackCatExecutionRevealed)]
        [InlineData(TutorialId.FirstDeath)]
        [InlineData(TutorialId.BomberRoleDrawn)]
        [InlineData(TutorialId.BombPlantedAsBomber)]
        [InlineData(TutorialId.BomberProximityWarnedAsVillager)]
        [InlineData(TutorialId.SelfBombExplodedAsVillager)]
        [InlineData(TutorialId.ShamanRoleDrawn)]
        [InlineData(TutorialId.ShamanGhostSighted)]
        [InlineData(TutorialId.ShamanTranceEntered)]
        [InlineData(TutorialId.ShamanStormEntered)]
        [InlineData(TutorialId.EquipBlockedByOtherGrabber)]
        [InlineData(TutorialId.ValuableRecordSuppressed)]
        public void AllIds_ReturnNonEmptyMessage(TutorialId id)
        {
            string text = TutorialCatalog.Format(id);

            Assert.False(string.IsNullOrEmpty(text));
        }

        [Fact]
        public void UnknownId_ReturnsNull()
        {
            string text = TutorialCatalog.Format((TutorialId)255);

            Assert.Null(text);
        }
    }
}
