using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioSelection
    {
        private static bool _openRequested;

        public static bool OpenRequested => _openRequested;

        public static void RequestOpen() => _openRequested = true;

        public static bool ConsumeOpenRequest()
        {
            if (!_openRequested) return false;
            _openRequested = false;
            return true;
        }

        public static void Clear() => _openRequested = false;
    }
}
