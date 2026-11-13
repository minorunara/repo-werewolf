using System;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioScriptRunTests
    {
        [Fact]
        public void Constructor_BindsScenarioDefinitionAndFreshFlow()
        {
            ScenarioDefinition definition = MeetingScenarioScript.Definition;

            var first = new ScenarioScriptRun(definition.Id, definition);
            var second = new ScenarioScriptRun(definition.Id, definition);

            Assert.Equal(definition.Id, first.Scenario);
            Assert.Same(definition, first.Definition);
            Assert.NotSame(first.Flow, second.Flow);
            Assert.Equal(definition.Pages.Count, first.Flow.PageCount);
        }

        [Fact]
        public void Constructor_RejectsNullDefinition()
        {
            var error = Assert.Throws<ArgumentNullException>(
                () => new ScenarioScriptRun(ScenarioId.MeetingVote, null));

            Assert.Equal("definition", error.ParamName);
        }

        [Fact]
        public void Constructor_RejectsMismatchedScenario()
        {
            var error = Assert.Throws<ArgumentException>(
                () => new ScenarioScriptRun(ScenarioId.WerewolfPerks, MeetingScenarioScript.Definition));

            Assert.Equal("scenario", error.ParamName);
        }
    }
}
