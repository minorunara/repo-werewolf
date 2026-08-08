using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScatterPlanTests
    {
        [Fact]
        public void 生存6人未満は分散しない()
        {
            for (int players = 1; players <= 2 * ScatterPlan.MinGroupSize - 1; players++)
            {
                Assert.All(ScatterPlan.Assign(players, 5, new Random(1)), s => Assert.Equal(0, s));
            }
        }

        [Fact]
        public void スロット1以下は全員スロット0()
        {
            Assert.All(ScatterPlan.Assign(8, 1, new Random(1)), s => Assert.Equal(0, s));
            Assert.All(ScatterPlan.Assign(8, 0, new Random(1)), s => Assert.Equal(0, s));
        }

        [Fact]
        public void プレイヤー0人は空配列()
        {
            Assert.Empty(ScatterPlan.Assign(0, 3, new Random(1)));
        }

        [Fact]
        public void 全組が最低3人かつ人数差は最大1()
        {
            for (int players = 6; players <= 12; players++)
            {
                for (int seed = 0; seed < 10; seed++)
                {
                    int[] result = ScatterPlan.Assign(players, 4, new Random(seed));
                    List<int> sizes = GroupSizes(result);
                    Assert.All(sizes, size => Assert.True(size >= ScatterPlan.MinGroupSize));
                    Assert.True(sizes.Max() - sizes.Min() <= 1);
                }
            }
        }

        [Fact]
        public void 組数は人数とスロット数の少ない方に律速される()
        {
            Assert.Equal(3, GroupSizes(ScatterPlan.Assign(9, 4, new Random(2))).Count);
            Assert.Equal(2, GroupSizes(ScatterPlan.Assign(12, 2, new Random(2))).Count);
        }

        [Fact]
        public void 行き先の全スロット使用は要求しない()
        {
            Assert.Equal(2, ScatterPlan.Assign(6, 5, new Random(3)).Distinct().Count());
        }

        [Fact]
        public void 同一シードで決定的()
        {
            Assert.Equal(
                ScatterPlan.Assign(8, 3, new Random(7)),
                ScatterPlan.Assign(8, 3, new Random(7)));
        }

        [Fact]
        public void スロットは範囲内()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                Assert.All(ScatterPlan.Assign(10, 4, new Random(seed)),
                    s => Assert.InRange(s, 0, 3));
            }
        }

        [Fact]
        public void 組の構成メンバーはシードにより入れ替わる()
        {
            var partitions = new HashSet<string>();
            for (int seed = 0; seed < 30; seed++)
            {
                partitions.Add(PartitionKey(ScatterPlan.Assign(6, 3, new Random(seed))));
            }
            Assert.True(partitions.Count > 1);
        }

        [Fact]
        public void 行き先スロットの選抜もシードにより変わる()
        {
            bool truckUsed = false, truckSkipped = false;
            for (int seed = 0; seed < 50 && !(truckUsed && truckSkipped); seed++)
            {
                bool used = ScatterPlan.Assign(6, 5, new Random(seed)).Contains(0);
                truckUsed |= used;
                truckSkipped |= !used;
            }
            Assert.True(truckUsed);
            Assert.True(truckSkipped);
        }

        [Fact]
        public void 検証用の独立一様抽選はソロ1人でもスロット0以外へ入り得る()
        {
            bool nonZeroSeen = false;
            var rng = new Random(3);
            for (int trial = 0; trial < 50 && !nonZeroSeen; trial++)
            {
                nonZeroSeen = ScatterPlan.AssignUniformDebug(1, 3, rng)[0] != 0;
            }
            Assert.True(nonZeroSeen);
        }

        private static List<int> GroupSizes(int[] result) =>
            result.GroupBy(s => s).Select(g => g.Count()).ToList();

        private static string PartitionKey(int[] result) =>
            string.Join("|",
                result.Select((slot, index) => (slot, index))
                    .GroupBy(t => t.slot)
                    .Select(g => string.Join(",", g.Select(t => t.index).OrderBy(i => i)))
                    .OrderBy(s => s, StringComparer.Ordinal));
    }
}
