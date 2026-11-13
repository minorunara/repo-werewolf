#nullable enable
using System;

namespace Werewolf.Scenarios.Core
{
    internal sealed class ScenarioScriptRun
    {
        public ScenarioScriptRun(ScenarioId scenario, ScenarioDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (definition.Id != scenario)
            {
                throw new ArgumentException("scenario does not match definition", nameof(scenario));
            }

            Scenario = scenario;
            Flow = definition.CreateFlow();
        }

        public ScenarioId Scenario { get; }

        public ScenarioDefinition Definition { get; }

        public ScenarioFlow Flow { get; }
    }
}
