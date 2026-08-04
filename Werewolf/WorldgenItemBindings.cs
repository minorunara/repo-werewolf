using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Werewolf.Core;
using Werewolf.Game;

namespace Werewolf
{
    internal static class WorldgenItemBindings
    {
        private static ConfigFile _config;
        private static ConfigBindings _bindings;

        private static Dictionary<string, ConfigEntry<int>> _entries;

        private static bool _initialized;
        private static bool _failed;
        private static bool _skipLogged;

        private static readonly Dictionary<string, int> DefaultAmounts = new Dictionary<string, int>
        {
            ["Item Cart Cannon"] = 1,
            ["Item Cart Medium"] = 2,
            ["Item Cart Small"] = 1,
            ["Item Drone Indestructible"] = 1,
            ["Item Drone Zero Gravity"] = 1,
            ["Item Grenade Explosive"] = 2,
            ["Item Grenade Shockwave"] = 2,
            ["Item Grenade Stun"] = 2,
            ["Item Gun Laser"] = 1,
            ["Item Gun Shockwave"] = 1,
            ["Item Gun Shotgun"] = 1,
            ["Item Gun Stun"] = 1,
            ["Item Gun Tranq"] = 1,
            ["Item Health Pack Large"] = 1,
            ["Item Health Pack Medium"] = 2,
            ["Item Health Pack Small"] = 3,
            ["Item Leaf Blower"] = 1,
            ["Item Melee Baseball Bat"] = 1,
            ["Item Melee Frying Pan"] = 1,
            ["Item Melee Inflatable Hammer"] = 0,
            ["Item Melee Stun Baton"] = 1,
            ["Item Melee Sword"] = 1,
            ["Item Mine Explosive"] = 2,
            ["Item Mine Shockwave"] = 2,
            ["Item Mine Stun"] = 2,
            ["Item Orb Zero Gravity"] = 1,
            ["Item Phase Bridge"] = 1,
            ["Item Staff Torque"] = 1,
            ["Item Staff Void"] = 1,
            ["Item Staff Zero Gravity"] = 1,
            ["Item Upgrade Map Player Count"] = 1,
            ["Item Vehicle Semiscooter"] = 1,
            ["Item Vehicle Semiscooter Small"] = 1,
            ["Item WalkieTalkieBox"] = 1,
        };

        internal static void Install(ConfigFile config, ConfigBindings bindings)
        {
            _config = config;
            _bindings = bindings;
        }

        internal static void TryInitialize()
        {
            if (_initialized || _failed || _config == null) return;
            try
            {
                var catalog = GameItemDatabase.Instance.ItemMaxAmounts;
                if (catalog == null || catalog.Count == 0) return;

                var entries = new Dictionary<string, ConfigEntry<int>>(catalog.Count);
                foreach (var pair in catalog)
                {
                    try
                    {
                        var defaultAmount = DefaultAmounts.TryGetValue(pair.Key, out var amount)
                            ? Math.Min(amount, pair.Value)
                            : 0;
                        entries[pair.Key] = _config.Bind(
                            ConfigBindings.SecLoadoutItems, pair.Key, defaultAmount,
                            new ConfigDescription(
                                $"Amount brought to the truck at werewolf session start (0 = none, max = {pair.Value}) / " +
                                $"人狼セッション開始時にトラックへ持ち込む個数（0 = 持ち込まない, 上限 = {pair.Value}）",
                                new AcceptableValueRange<int>(0, pair.Value)));
                    }
                    catch (Exception e)
                    {
                        WLog.Line("worldgen_itembind_skip", secret: false,
                            ("item", pair.Key), ("err", e.Message));
                    }
                }

                _entries = entries;
                _initialized = true;
                if (_bindings != null) _bindings.ItemsSpecProvider = ComposeSpec;
                WLog.Line("worldgen_itembind_init", secret: false, ("count", entries.Count));
            }
            catch (Exception e)
            {
                _failed = true;
                WLog.Line("worldgen_itembind_error", secret: false, ("err", e.Message));
            }
        }

        internal static string ComposeSpec()
        {
            var entries = _entries;
            if (entries == null) return "";

            var map = new Dictionary<string, int>(entries.Count);
            foreach (var pair in entries) map[pair.Key] = pair.Value.Value;

            var skipped = new List<string>();
            var spec = WorldgenSpec.Encode(map, skipped);
            if (skipped.Count > 0 && !_skipLogged)
            {
                _skipLogged = true;
                WLog.Line("worldgen_itemspec_skip", secret: false,
                    ("names", string.Join(" / ", skipped)));
            }
            return spec;
        }
    }
}
