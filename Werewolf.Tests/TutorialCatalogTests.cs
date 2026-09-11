using System;
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

        [Fact]
        public void AllIds_ReturnNonEmptyMessage()
        {
            foreach (TutorialId id in Enum.GetValues(typeof(TutorialId)))
            {
                Assert.False(string.IsNullOrEmpty(TutorialCatalog.Format(id)), $"TutorialId.{id} の文言が空");
            }
        }

        [Fact]
        public void UnknownId_ReturnsNull()
        {
            string text = TutorialCatalog.Format((TutorialId)255);

            Assert.Null(text);
        }
    }
}
