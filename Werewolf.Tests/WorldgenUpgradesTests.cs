using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WorldgenUpgradesTests
    {
        private static readonly string[] CanonicalNames =
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

        [Fact]
        public void Names_ContainExactlyThe13CanonicalUpgrades()
        {
            Assert.Equal(
                CanonicalNames.OrderBy(n => n, System.StringComparer.Ordinal),
                WorldgenUpgrades.Names.OrderBy(n => n, System.StringComparer.Ordinal));
        }

        [Fact]
        public void Names_AreDistinct()
        {
            Assert.Equal(WorldgenUpgrades.Names.Count, WorldgenUpgrades.Names.Distinct().Count());
        }

        [Fact]
        public void ComposeSpec_AllZero_YieldsEmpty_SameAsGameConfigDefault()
        {
            var map = new Dictionary<string, int>();
            foreach (var name in WorldgenUpgrades.Names) map[name] = 0;

            Assert.Equal(new GameConfig().StartUpgradesSpec, WorldgenSpec.Encode(map));
            Assert.Equal("", WorldgenSpec.Encode(map));
        }

        [Fact]
        public void ComposeSpec_PositiveLevels_RoundTripThroughCodec()
        {
            var map = new Dictionary<string, int>();
            int level = 0;
            foreach (var name in WorldgenUpgrades.Names) map[name] = ++level;

            var skipped = new List<string>();
            var spec = WorldgenSpec.Encode(map, skipped);

            Assert.Empty(skipped);
            var decoded = WorldgenSpec.Decode(spec);
            Assert.Equal(map.Count, decoded.Count);
            foreach (var pair in map) Assert.Equal(pair.Value, decoded[pair.Key]);
        }
    }
}
