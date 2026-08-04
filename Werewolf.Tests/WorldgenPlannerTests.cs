using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WorldgenPlannerTests
    {
        private sealed class FakeCatalog : IWorldgenCatalog
        {
            public IReadOnlyDictionary<string, int> ItemMaxAmounts { get; }
            public IReadOnlyList<string> PlayableLevelNames { get; }

            public FakeCatalog(
                IReadOnlyDictionary<string, int> items = null,
                IReadOnlyList<string> levels = null)
            {
                ItemMaxAmounts = items ?? new Dictionary<string, int>();
                PlayableLevelNames = levels ?? new List<string>();
            }
        }

        private static FakeCatalog DefaultCatalog() => new FakeCatalog(
            items: new Dictionary<string, int>
            {
                ["Item Gun Handgun"] = 2,
                ["Item Drone Battery"] = 4,
                ["Item Health Pack Small"] = 10,
            },
            levels: new List<string> { "Level - Manor", "Level - Arctic", "Level - Wizard" });

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 0)]
        [InlineData(-5, 0)]
        [InlineData(5, 4)]
        [InlineData(99, 98)]
        [InlineData(100, 98)]
        [InlineData(9999, 98)]
        public void BuildPlan_ClampsLevelNumber_AndConvertsToLevelsCompleted(int levelNumber, int expected)
        {
            var config = new GameConfig { StartLevelNumber = levelNumber };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Equal(expected, plan.LevelsCompleted);
        }

        [Fact]
        public void BuildPlan_EmptyMapName_YieldsNullForcedLevel()
        {
            var config = new GameConfig { StartMapName = "" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Null(plan.ForcedLevelName);
        }

        [Fact]
        public void BuildPlan_WhitespaceMapName_YieldsNullForcedLevel()
        {
            var config = new GameConfig { StartMapName = "   " };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Null(plan.ForcedLevelName);
        }

        [Fact]
        public void BuildPlan_UnknownMapName_FallsBackToNull()
        {
            var config = new GameConfig { StartMapName = "Level - Removed By Update" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Null(plan.ForcedLevelName);
        }

        [Fact]
        public void BuildPlan_KnownMapName_IsForced()
        {
            var config = new GameConfig { StartMapName = "Level - Arctic" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Equal("Level - Arctic", plan.ForcedLevelName);
        }

        [Fact]
        public void BuildPlan_MapNameComparison_IsCaseSensitiveOrdinal()
        {
            var config = new GameConfig { StartMapName = "level - arctic" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Null(plan.ForcedLevelName);
        }

        [Fact]
        public void BuildPlan_EmptyItemsSpec_YieldsEmptyItems()
        {
            var config = new GameConfig { StartItemsSpec = "" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Empty(plan.Items);
        }

        [Fact]
        public void BuildPlan_ItemsAreClampedToCatalogMax()
        {
            var config = new GameConfig
            {
                StartItemsSpec = "Item Gun Handgun:99,Item Drone Battery:3",
            };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Equal(2, plan.Items["Item Gun Handgun"]);
            Assert.Equal(3, plan.Items["Item Drone Battery"]);
            Assert.Equal(2, plan.Items.Count);
        }

        [Fact]
        public void BuildPlan_ItemsNotInCatalog_AreExcluded()
        {
            var config = new GameConfig { StartItemsSpec = "Item Unknown:5,Item Gun Handgun:1" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.False(plan.Items.ContainsKey("Item Unknown"));
            Assert.Equal(1, plan.Items["Item Gun Handgun"]);
            Assert.Single(plan.Items);
        }

        [Fact]
        public void BuildPlan_EmptyCatalog_ClampsAllItemsToZero_YieldingEmpty()
        {
            var config = new GameConfig { StartItemsSpec = "Item Gun Handgun:2,Item Drone Battery:1" };
            var plan = WorldgenPlanner.BuildPlan(config, new FakeCatalog());
            Assert.Empty(plan.Items);
        }

        [Fact]
        public void BuildPlan_CatalogMaxZero_ExcludesItem()
        {
            var catalog = new FakeCatalog(items: new Dictionary<string, int> { ["Item Disabled"] = 0 });
            var config = new GameConfig { StartItemsSpec = "Item Disabled:3" };
            var plan = WorldgenPlanner.BuildPlan(config, catalog);
            Assert.Empty(plan.Items);
        }

        [Fact]
        public void BuildPlan_Items_ContainNoZeroOrNegativeEntries()
        {
            var catalog = new FakeCatalog(items: new Dictionary<string, int>
            {
                ["A"] = 0,
                ["B"] = 5,
            });
            var config = new GameConfig { StartItemsSpec = "A:1,B:0,B:2" };
            var plan = WorldgenPlanner.BuildPlan(config, catalog);
            Assert.All(plan.Items.Values, v => Assert.True(v > 0));
            Assert.Equal(new Dictionary<string, int> { ["B"] = 2 }, plan.Items);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 1, 1)]
        [InlineData(10, 10, 1)]
        [InlineData(11, 11, 2)]
        [InlineData(55, 55, 6)]
        [InlineData(100, 100, 10)]
        [InlineData(-20, 0, 0)]
        [InlineData(150, 100, 10)]
        public void BuildPlan_DerivesChargeAndPowerCrystals(int pct, int expectedTotal, int expectedCrystals)
        {
            var config = new GameConfig { StartEnergyPct = pct };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Equal(expectedTotal, plan.ChargeTotal);
            Assert.Equal(expectedCrystals, plan.PowerCrystals);
        }

        [Fact]
        public void BuildPlan_EmptyUpgradesSpec_YieldsEmptyUpgrades()
        {
            var config = new GameConfig { StartUpgradesSpec = "" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Empty(plan.Upgrades);
        }

        [Fact]
        public void BuildPlan_ZeroTargetUpgrades_AreExcluded()
        {
            var config = new GameConfig { StartUpgradesSpec = "Strength:2,Speed:0,Stamina:-1" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Equal(new Dictionary<string, int> { ["Strength"] = 2 }, plan.Upgrades);
        }

        [Fact]
        public void BuildPlan_Upgrades_AreTargetStages_NotDeltas()
        {
            var config = new GameConfig { StartUpgradesSpec = "Strength:1,Speed:3" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Equal(1, plan.Upgrades["Strength"]);
            Assert.Equal(3, plan.Upgrades["Speed"]);
            Assert.Equal(2, plan.Upgrades.Count);
        }

        [Fact]
        public void BuildPlan_Upgrades_AreNotClampedByItemCatalog()
        {
            var config = new GameConfig { StartUpgradesSpec = "Item Gun Handgun:9" };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Equal(9, plan.Upgrades["Item Gun Handgun"]);
        }

        [Theory]
        [InlineData(3, 3)]
        [InlineData(0, 0)]
        [InlineData(25, 25)]
        [InlineData(-1, 0)]
        [InlineData(100, 25)]
        public void BuildPlan_ClampsOrbDropMax(int value, int expected)
        {
            var config = new GameConfig { OrbDropMax = value };
            var plan = WorldgenPlanner.BuildPlan(config, DefaultCatalog());
            Assert.Equal(expected, plan.OrbDropMax);
        }

        [Fact]
        public void BuildPlan_IsDeterministic_SameInputSameOutput()
        {
            var config = new GameConfig
            {
                StartLevelNumber = 7,
                StartMapName = "Level - Wizard",
                StartItemsSpec = "Item Drone Battery:9,Item Gun Handgun:1",
                StartEnergyPct = 55,
                StartUpgradesSpec = "Speed:2,Strength:1",
                OrbDropMax = 10,
            };
            var catalog = DefaultCatalog();

            var a = WorldgenPlanner.BuildPlan(config, catalog);
            var b = WorldgenPlanner.BuildPlan(config, catalog);

            Assert.Equal(a.LevelsCompleted, b.LevelsCompleted);
            Assert.Equal(a.ForcedLevelName, b.ForcedLevelName);
            Assert.Equal(a.ChargeTotal, b.ChargeTotal);
            Assert.Equal(a.PowerCrystals, b.PowerCrystals);
            Assert.Equal(a.OrbDropMax, b.OrbDropMax);
            Assert.Equal(
                a.Items.OrderBy(p => p.Key, System.StringComparer.Ordinal),
                b.Items.OrderBy(p => p.Key, System.StringComparer.Ordinal));
            Assert.Equal(
                a.Upgrades.OrderBy(p => p.Key, System.StringComparer.Ordinal),
                b.Upgrades.OrderBy(p => p.Key, System.StringComparer.Ordinal));

            Assert.Equal(6, a.LevelsCompleted);
            Assert.Equal("Level - Wizard", a.ForcedLevelName);
            Assert.Equal(new Dictionary<string, int> { ["Item Drone Battery"] = 4, ["Item Gun Handgun"] = 1 }, a.Items);
            Assert.Equal(55, a.ChargeTotal);
            Assert.Equal(6, a.PowerCrystals);
            Assert.Equal(new Dictionary<string, int> { ["Speed"] = 2, ["Strength"] = 1 }, a.Upgrades);
            Assert.Equal(10, a.OrbDropMax);
        }

        [Fact]
        public void BuildPlan_NeutralConfig_MatchesVanillaNonIntervention()
        {
            var neutral = new GameConfig
            {
                StartLevelNumber = 1,
                StartMapName = "",
                StartItemsSpec = "",
                StartEnergyPct = 100,
                StartUpgradesSpec = "",
                OrbDropMax = 3,
            };
            var plan = WorldgenPlanner.BuildPlan(neutral, DefaultCatalog());
            Assert.Equal(0, plan.LevelsCompleted);
            Assert.Null(plan.ForcedLevelName);
            Assert.Empty(plan.Items);
            Assert.Equal(100, plan.ChargeTotal);
            Assert.Equal(10, plan.PowerCrystals);
            Assert.Empty(plan.Upgrades);
            Assert.Equal(3, plan.OrbDropMax);
        }
    }
}
