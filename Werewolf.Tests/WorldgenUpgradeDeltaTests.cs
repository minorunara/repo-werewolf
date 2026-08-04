using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WorldgenUpgradeDeltaTests
    {
        private static Dictionary<string, int> Map(params (string Name, int Value)[] entries)
        {
            var map = new Dictionary<string, int>();
            foreach (var (name, value) in entries) map[name] = value;
            return map;
        }

        [Fact]
        public void Compute_CurrentZero_DeltaEqualsTarget()
        {
            var deltas = WorldgenUpgradeDeltas.Compute(
                Map(("Strength", 2), ("Speed", 1)), _ => 0);

            Assert.Equal(2, deltas.Count);
            Assert.Contains(("Speed", 1), deltas);
            Assert.Contains(("Strength", 2), deltas);
        }

        [Fact]
        public void Compute_SameStage_SkipsZeroDelta()
        {
            var deltas = WorldgenUpgradeDeltas.Compute(
                Map(("Strength", 2)), name => name == "Strength" ? 2 : 0);

            Assert.Empty(deltas);
        }

        [Fact]
        public void Compute_CurrentAboveTarget_YieldsNegativeDelta()
        {
            var deltas = WorldgenUpgradeDeltas.Compute(
                Map(("Strength", 2)), name => name == "Strength" ? 5 : 0);

            var single = Assert.Single(deltas);
            Assert.Equal(("Strength", -3), single);
        }

        [Fact]
        public void Compute_UnspecifiedUpgrade_IsNotTouched()
        {
            var deltas = WorldgenUpgradeDeltas.Compute(
                Map(("Speed", 1)), name => name == "Strength" ? 4 : 0);

            var single = Assert.Single(deltas);
            Assert.Equal(("Speed", 1), single);
        }

        [Fact]
        public void Compute_EmptyOrNullTargets_ReturnsEmpty()
        {
            Assert.Empty(WorldgenUpgradeDeltas.Compute(Map(), _ => 3));
            Assert.Empty(WorldgenUpgradeDeltas.Compute(null, _ => 3));
        }

        [Fact]
        public void Compute_NullCurrentLookup_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => WorldgenUpgradeDeltas.Compute(Map(("Speed", 1)), null));
        }

        [Fact]
        public void Compute_ResultIsOrdinalSortedByName_Deterministic()
        {
            var deltas = WorldgenUpgradeDeltas.Compute(
                Map(("Throw", 1), ("ExtraJump", 2), ("Speed", 3)), _ => 0);

            Assert.Equal(new[] { "ExtraJump", "Speed", "Throw" },
                deltas.Select(d => d.Name).ToArray());
        }
    }
}
