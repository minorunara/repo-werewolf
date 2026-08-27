using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioSessionGate
    {
        public static bool ShouldEngage(bool werewolfModeEnabled, bool scenarioActive)
            => scenarioActive || werewolfModeEnabled;

        public static bool PlayerCountSatisfied(int realPlayerCount, bool debugMode, bool scenarioActive)
            => scenarioActive || PlayerCountGate.IsSatisfied(realPlayerCount, debugMode);

        public static bool RequiresConfigValidation(bool debugMode, bool scenarioActive)
            => !debugMode && !scenarioActive;

        public static bool ChatLogEnabled(bool userSetting, bool scenarioActive)
            => scenarioActive || userSetting;

        public static bool MapGridEnabled(bool userSetting, bool scenarioActive)
            => scenarioActive || userSetting;

        public static bool ReturnsToTitle(bool scenarioActive) => scenarioActive;

        public static bool VoidMatchEnabled(bool scenarioActive) => !scenarioActive;
    }
}
