using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Werewolf.Core;
using Werewolf.Game;

namespace Werewolf
{
    internal static class WorldgenMapBinding
    {
        private static ConfigFile _config;
        private static ConfigBindings _bindings;

        private static bool _initialized;
        private static bool _failed;

        internal static void Install(ConfigFile config, ConfigBindings bindings)
        {
            _config = config;
            _bindings = bindings;
        }

        internal static void TryInitialize()
        {
            if (_initialized || _failed || _config == null || _bindings == null) return;
            try
            {
                var levels = GameItemDatabase.Instance.PlayableLevelNames;
                if (levels == null || levels.Count == 0) return;

                var values = new List<string>(levels.Count + 1) { "" };
                foreach (var name in levels)
                {
                    if (!values.Contains(name)) values.Add(name);
                }

                var previous = _bindings.StartMapName;
                var oldValue = previous.Value ?? "";
                _config.Remove(previous.Definition);

                var entry = _config.Bind(
                    ConfigBindings.SecStage, "StartMapName", "",
                    new ConfigDescription(
                        "Map type name for the werewolf session. Empty = random (vanilla default selection) / " +
                        "人狼セッションの開始マップ種別名。空 = ランダム（バニラ既定の選択）",
                        new AcceptableValueList<string>(values.ToArray())));
                entry.Value = oldValue;
                _bindings.StartMapName = entry;

                _initialized = true;
                WLog.Line("worldgen_mapbind_init", secret: false,
                    ("levels", values.Count - 1), ("value", entry.Value));
                if (!string.Equals(entry.Value, oldValue, StringComparison.Ordinal))
                {
                    WLog.Line("worldgen_mapbind_fallback", secret: false, ("from", oldValue));
                }
            }
            catch (Exception e)
            {
                _failed = true;
                WLog.Line("worldgen_mapbind_error", secret: false, ("err", e.Message));
            }
        }
    }
}
