namespace Werewolf.Game
{
    internal static class LiveConfig
    {
        internal static bool WerewolfModeEnabled()
        {
            if (Plugin.Bindings != null) return Plugin.Bindings.WerewolfModeEnabled.Value;
            return Plugin.GameConfig != null && Plugin.GameConfig.WerewolfModeEnabled;
        }

        internal static bool DebugMode()
        {
            if (Plugin.Bindings != null) return Plugin.Bindings.DebugMode.Value;
            return Plugin.GameConfig != null && Plugin.GameConfig.DebugMode;
        }

        internal static int WerewolfCount()
        {
            if (Plugin.Bindings != null) return Plugin.Bindings.WerewolfCount.Value;
            return Plugin.GameConfig != null ? Plugin.GameConfig.WerewolfCount : 1;
        }
    }
}
