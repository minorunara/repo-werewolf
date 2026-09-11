using Werewolf.Core;

namespace Werewolf.Debugging
{
    public enum StructuredLogRoute
    {
        Drop = 0,
        Immediate = 1,
        Deferred = 2,
    }

    public static class StructuredLogPolicy
    {
        public static StructuredLogRoute Decide(
            WLogLevel level,
            bool secret,
            bool debugLogging,
            bool debugMode)
        {
            if (secret)
            {
                if (debugMode) return StructuredLogRoute.Immediate;
                if (debugLogging || level != WLogLevel.Debug) return StructuredLogRoute.Deferred;
                return StructuredLogRoute.Drop;
            }

            if (level == WLogLevel.Debug && !debugLogging && !debugMode)
                return StructuredLogRoute.Drop;

            return StructuredLogRoute.Immediate;
        }
    }
}
