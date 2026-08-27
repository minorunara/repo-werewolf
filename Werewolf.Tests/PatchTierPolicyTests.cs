using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class PatchTierPolicyTests
    {
        [Theory]
        [InlineData("Werewolf.Scenarios.Game.Patches.ScenarioDeathGuardPatch", true)]
        [InlineData("Werewolf.Scenarios.Game.Patches.MenuHoverBlockPatch", true)]
        [InlineData("Werewolf.Scenarios.Core.ScenarioFlow", true)]
        [InlineData("Werewolf.Game.Patches.PlayerDeathPatch", false)]
        [InlineData("Werewolf.Game.Patches.DirectorRevivePatch", false)]
        [InlineData("Werewolf.Core.GameSession", false)]
        [InlineData("Werewolf.Plugin", false)]
        public void IsScenarioTier_SplitsOnTheNamespacePrefix(string fullName, bool expected)
        {
            Assert.Equal(expected, PatchTierPolicy.IsScenarioTier(fullName));
        }

        [Fact]
        public void IsScenarioTier_TreatsUnknownNamesAsEssential()
        {
            Assert.False(PatchTierPolicy.IsScenarioTier(null));
            Assert.False(PatchTierPolicy.IsScenarioTier(""));
            Assert.False(PatchTierPolicy.IsScenarioTier("Werewolf.ScenariosExtra.Thing"));
        }

        [Fact]
        public void Decide_KeepsEverythingWhenNothingFailed()
        {
            Assert.Equal(PatchApplyOutcome.AllApplied, PatchTierPolicy.Decide(0, 0));
        }

        [Fact]
        public void Decide_DisablesOnlyScenariosWhenTheScenarioTierFailedAlone()
        {
            Assert.Equal(PatchApplyOutcome.ScenariosDisabled, PatchTierPolicy.Decide(0, 1));
            Assert.Equal(PatchApplyOutcome.ScenariosDisabled, PatchTierPolicy.Decide(0, 5));
        }

        [Fact]
        public void Decide_DisablesTheWholeModWheneverTheEssentialTierFailed()
        {
            Assert.Equal(PatchApplyOutcome.ModDisabled, PatchTierPolicy.Decide(1, 0));
            Assert.Equal(PatchApplyOutcome.ModDisabled, PatchTierPolicy.Decide(1, 3));
        }
    }
}
