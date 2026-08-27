using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioKeyTags
    {
        public const string WolfMode = "[wolfkey]";

        public const string Beacon = "[beaconkey]";

        public const string CorpseReport = "[reportkey]";

        public const string BombPlant = "[plantkey]";

        public const string BombDetonate = "[bombkey]";

        public static readonly string[] All = { WolfMode, Beacon, CorpseReport, BombPlant, BombDetonate };
    }
}
