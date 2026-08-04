using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Werewolf.Core
{

    public interface IWorldgenCatalog
    {
        IReadOnlyDictionary<string, int> ItemMaxAmounts { get; }

        IReadOnlyList<string> PlayableLevelNames { get; }
    }

    public sealed class WorldgenPlan
    {
        public int LevelsCompleted;

        public string ForcedLevelName;

        public IReadOnlyDictionary<string, int> Items;

        public int PowerCrystals;

        public int ChargeTotal;

        public IReadOnlyDictionary<string, int> Upgrades;

        public int OrbDropMax;
    }

    public static class WorldgenPlanner
    {
        private const int MinLevelNumber = 1;
        private const int MaxLevelNumber = 99;

        private const int MinEnergyPct = 0;
        private const int MaxEnergyPct = 100;
        private const int PctPerCrystal = 10;

        private const int MinOrbDropMax = 0;
        private const int MaxOrbDropMax = 25;

        public static WorldgenPlan BuildPlan(GameConfig config, IWorldgenCatalog catalog)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            int levelNumber = Clamp(config.StartLevelNumber, MinLevelNumber, MaxLevelNumber);
            int chargeTotal = Clamp(config.StartEnergyPct, MinEnergyPct, MaxEnergyPct);

            return new WorldgenPlan
            {
                LevelsCompleted = levelNumber - 1,
                ForcedLevelName = ResolveForcedLevelName(config.StartMapName, catalog.PlayableLevelNames),
                Items = BuildItems(config.StartItemsSpec, catalog.ItemMaxAmounts),
                ChargeTotal = chargeTotal,
                PowerCrystals = (chargeTotal + PctPerCrystal - 1) / PctPerCrystal,
                Upgrades = WorldgenSpec.Decode(config.StartUpgradesSpec),
                OrbDropMax = Clamp(config.OrbDropMax, MinOrbDropMax, MaxOrbDropMax),
            };
        }

        private static string ResolveForcedLevelName(string mapName, IReadOnlyList<string> playableLevelNames)
        {
            if (string.IsNullOrWhiteSpace(mapName)) return null;

            var name = mapName.Trim();
            for (int i = 0; i < playableLevelNames.Count; i++)
            {
                if (string.Equals(playableLevelNames[i], name, StringComparison.Ordinal))
                    return name;
            }
            return null;
        }

        private static IReadOnlyDictionary<string, int> BuildItems(
            string itemsSpec, IReadOnlyDictionary<string, int> itemMaxAmounts)
        {
            var result = new Dictionary<string, int>();
            foreach (var pair in WorldgenSpec.Decode(itemsSpec))
            {
                if (!itemMaxAmounts.TryGetValue(pair.Key, out int max)) continue;
                int count = pair.Value < max ? pair.Value : max;
                if (count <= 0) continue;
                result[pair.Key] = count;
            }
            return result;
        }

        private static int Clamp(int value, int min, int max)
            => value < min ? min : (value > max ? max : value);
    }

    public static class WorldgenUpgrades
    {
        public static readonly IReadOnlyList<string> Names = new[]
        {
            "CrouchRest",
            "DeathHeadBattery",
            "ExtraJump",
            "Health",
            "Launch",
            "MapPlayerCount",
            "Range",
            "Speed",
            "Stamina",
            "Strength",
            "Throw",
            "TumbleClimb",
            "TumbleWings",
        };
    }

    public static class WorldgenUpgradeDeltas
    {
        public static IReadOnlyList<(string Name, int Delta)> Compute(
            IReadOnlyDictionary<string, int> targets, Func<string, int> currentStage)
        {
            if (currentStage == null) throw new ArgumentNullException(nameof(currentStage));

            var result = new List<(string Name, int Delta)>();
            if (targets == null || targets.Count == 0) return result;

            var names = new List<string>(targets.Keys);
            names.Sort(StringComparer.Ordinal);
            foreach (var name in names)
            {
                int delta = targets[name] - currentStage(name);
                if (delta == 0) continue;
                result.Add((name, delta));
            }
            return result;
        }
    }

    public static class WorldgenStartHealth
    {
        private const int BaseMaxHealth = 100;

        private const int HealthPerStage = 20;

        private const int MinHealth = 1;

        public static int Compute(int currentHp, int healthDelta, int targetStage)
        {
            int max = BaseMaxHealth + HealthPerStage * targetStage;
            int hp = currentHp + HealthPerStage * healthDelta;
            return hp < MinHealth ? MinHealth : (hp > max ? max : hp);
        }
    }

    public static class WorldgenSpec
    {
        private const char EntrySep = ',';
        private const char KvSep = ':';

        private static readonly char[] ForbiddenNameChars = { EntrySep, KvSep, '|', ';', '=' };

        public static string Encode(IReadOnlyDictionary<string, int> map)
            => Encode(map, null);

        public static string Encode(IReadOnlyDictionary<string, int> map, ICollection<string> skippedNames)
        {
            if (map == null || map.Count == 0) return "";

            var names = new List<string>(map.Count);
            foreach (var pair in map)
            {
                if (pair.Value <= 0) continue;
                var name = pair.Key;
                if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(ForbiddenNameChars) >= 0)
                {
                    skippedNames?.Add(name);
                    continue;
                }
                names.Add(name);
            }
            if (names.Count == 0) return "";

            names.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0) sb.Append(EntrySep);
                sb.Append(names[i]);
                sb.Append(KvSep);
                sb.Append(map[names[i]].ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        public static IReadOnlyDictionary<string, int> Decode(string spec)
        {
            var result = new Dictionary<string, int>();
            if (string.IsNullOrWhiteSpace(spec)) return result;

            foreach (var token in spec.Split(EntrySep))
            {
                var parts = token.Split(KvSep);
                if (parts.Length != 2) continue;

                var name = parts[0].Trim();
                if (name.Length == 0) continue;

                if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
                    continue;
                if (count <= 0) continue;

                result[name] = count;
            }
            return result;
        }
    }

    public static class WorldgenSaveGuard
    {
        public static bool ShouldDelete(bool leaveGame, bool active,
            string markedSaveFileName, string currentSaveFileName)
        {
            if (!leaveGame) return false;
            if (!active) return false;
            if (string.IsNullOrEmpty(markedSaveFileName)) return false;
            if (string.IsNullOrEmpty(currentSaveFileName)) return false;
            return string.Equals(markedSaveFileName, currentSaveFileName, StringComparison.Ordinal);
        }
    }
}
