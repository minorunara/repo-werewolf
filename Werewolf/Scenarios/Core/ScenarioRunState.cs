namespace Werewolf.Scenarios.Core
{
    public static class ScenarioRunState
    {
        public static bool Active { get; internal set; }

        public static ScenarioId Scenario { get; internal set; }

        public static ScenarioDefinition Definition
            => Active && ScenarioCatalog.TryGetDefinition(Scenario, out var definition)
                ? definition
                : null;
    }
}
