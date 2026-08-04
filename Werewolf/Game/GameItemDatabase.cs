using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class GameItemDatabase : IWorldgenCatalog
    {
        public static readonly GameItemDatabase Instance = new GameItemDatabase();

        public IReadOnlyDictionary<string, int> ItemMaxAmounts
        {
            get
            {
                var result = new Dictionary<string, int>();
                try
                {
                    var stats = StatsManager.instance;
                    if (stats != null && stats.itemDictionary != null && stats.itemDictionary.Count > 0)
                    {
                        foreach (var pair in stats.itemDictionary)
                            AddIfBringable(result, pair.Key, pair.Value);
                    }
                    else
                    {
                        foreach (var item in Resources.FindObjectsOfTypeAll<Item>())
                            AddIfBringable(result, item != null ? item.name : null, item);
                    }
                }
                catch (Exception e)
                {
                    result.Clear();
                    WLog.Line("worldgen_catalog_error", secret: false,
                        ("part", "items"), ("err", e.Message));
                }
                return result;
            }
        }

        public IReadOnlyList<string> PlayableLevelNames
        {
            get
            {
                var result = new List<string>();
                try
                {
                    var run = RunManager.instance;
                    if (run == null || run.levels == null) return result;

                    foreach (var level in run.levels)
                    {
                        if (level == null) continue;
                        var name = level.name;
                        if (!string.IsNullOrEmpty(name)) result.Add(name);
                    }
                }
                catch (Exception e)
                {
                    result.Clear();
                    WLog.Line("worldgen_catalog_error", secret: false,
                        ("part", "levels"), ("err", e.Message));
                }
                return result;
            }
        }

        private static void AddIfBringable(Dictionary<string, int> catalog, string name, Item item)
        {
            if (item == null || string.IsNullOrEmpty(name)) return;
            if (item.disabled) return;
            if (item.itemType == SemiFunc.itemType.power_crystal) return;
            if (item.maxAmount <= 0) return;
            if (catalog.ContainsKey(name)) return;
            catalog[name] = item.maxAmount;
        }
    }
}
