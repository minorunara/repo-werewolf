using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScatterGroupsWireTests
    {
        private static List<(int actor, string slot)> TwoGroups() => new List<(int, string)>
        {
            (1, "truck"), (2, "ep1"), (3, "truck"), (4, "ep1"), (5, "truck"), (6, "ep1"),
        };

        [Fact]
        public void 組が2つ以上なら並列配列を返す()
        {
            object[] wire = ScatterGroupsWire.ToWire(TwoGroups(), new Random(1));
            Assert.NotNull(wire);
            Assert.Equal(2, wire.Length);
            var actors = Assert.IsType<int[]>(wire[0]);
            var groupIds = Assert.IsType<byte[]>(wire[1]);
            Assert.Equal(6, actors.Length);
            Assert.Equal(6, groupIds.Length);
            Assert.Equal(2, groupIds.Distinct().Count());
            Assert.All(groupIds, id => Assert.InRange((int)id, 0, 1));
        }

        [Fact]
        public void 全員同組なら発表なし()
        {
            var allTruck = new List<(int, string)> { (1, "truck"), (2, "truck"), (3, "truck") };
            Assert.Null(ScatterGroupsWire.ToWire(allTruck, new Random(1)));
            Assert.Null(ScatterGroupsWire.ToWire(new List<(int, string)>(), new Random(1)));
            Assert.Null(ScatterGroupsWire.ToWire(null, new Random(1)));
        }

        [Fact]
        public void トラック縮退者はトラック組へ畳まれる()
        {
            var mixed = new List<(int, string)>
            {
                (1, "truck"), (2, "truck_fallback"), (3, "ep1"), (4, "ep1"),
            };
            List<List<int>> groups = ScatterGroupsWire.FromWire(
                ScatterGroupsWire.ToWire(mixed, new Random(1)));
            Assert.Equal(2, groups.Count);
            List<List<int>> sorted = groups
                .Select(g => g.OrderBy(a => a).ToList())
                .OrderBy(g => g[0]).ToList();
            Assert.Equal(new[] { 1, 2 }, sorted[0]);
            Assert.Equal(new[] { 3, 4 }, sorted[1]);
        }

        [Fact]
        public void 往復で組の構成が保存される()
        {
            List<List<int>> groups = ScatterGroupsWire.FromWire(
                ScatterGroupsWire.ToWire(TwoGroups(), new Random(9)));
            Assert.Equal(2, groups.Count);
            List<string> sets = groups
                .Select(g => string.Join(",", g.OrderBy(a => a)))
                .OrderBy(s => s, StringComparer.Ordinal).ToList();
            Assert.Equal(new[] { "1,3,5", "2,4,6" }, sets);
        }

        [Fact]
        public void 組IDの並びはシャッフルされる()
        {
            bool truckFirst = false, epFirst = false;
            for (int seed = 0; seed < 50 && !(truckFirst && epFirst); seed++)
            {
                List<List<int>> groups = ScatterGroupsWire.FromWire(
                    ScatterGroupsWire.ToWire(TwoGroups(), new Random(seed)));
                if (groups[0].Contains(1)) truckFirst = true;
                else epFirst = true;
            }
            Assert.True(truckFirst);
            Assert.True(epFirst);
        }

        [Fact]
        public void ボットの負のactor番号も往復で保存される()
        {
            var withBots = new List<(int, string)>
            {
                (1, "truck"), (-101, "truck"), (-102, "truck"),
                (-103, "ep1"), (-104, "ep1"), (-105, "ep1"),
            };
            List<List<int>> groups = ScatterGroupsWire.FromWire(
                ScatterGroupsWire.ToWire(withBots, new Random(4)));
            Assert.Equal(2, groups.Count);
            List<string> sets = groups
                .Select(g => string.Join(",", g.OrderBy(a => a)))
                .OrderBy(s => s, StringComparer.Ordinal).ToList();
            Assert.Equal(new[] { "-102,-101,1", "-105,-104,-103" }, sets);
        }

        [Fact]
        public void 破損payloadはnull()
        {
            Assert.Null(ScatterGroupsWire.FromWire(null));
            Assert.Null(ScatterGroupsWire.FromWire(new object[] { new[] { 1 } }));
            Assert.Null(ScatterGroupsWire.FromWire(new object[] { new[] { 1, 2 }, new byte[] { 0 } }));
            Assert.Null(ScatterGroupsWire.FromWire(new object[] { "x", new byte[] { 0 } }));
            Assert.Null(ScatterGroupsWire.FromWire(new object[] { new int[0], new byte[0] }));
        }

        [Fact]
        public void 文言は1組1行で組ラベルとメンバー表記を含む()
        {
            var groups = new List<List<int>>
            {
                new List<int> { 1, 3, 5 },
                new List<int> { 2, 4, 6 },
            };
            List<string> lines = ScatterGroupsText.FormatLines(groups, actor => "m" + actor);
            Assert.Equal(2, lines.Count);
            Assert.Contains("A", lines[0]);
            Assert.Contains("m1", lines[0]);
            Assert.Contains("m3", lines[0]);
            Assert.Contains("m5", lines[0]);
            Assert.Contains("B", lines[1]);
            Assert.Contains("m2", lines[1]);
            Assert.DoesNotContain("truck", lines[0] + lines[1]);
        }

        [Fact]
        public void 行体裁は差し替えられる_会議ログのリマインドは簡潔形()
        {
            var groups = new List<List<int>> { new List<int> { 1 }, new List<int> { 2 } };

            List<string> toast = ScatterGroupsText.FormatLines(groups, actor => "m" + actor);
            List<string> chat = ScatterGroupsText.FormatLines(groups, actor => "m" + actor,
                TextId.ChatLogScatterLineFormat);

            Assert.Contains(Texts.Format(TextId.ChatLogScatterLineFormat, 'A', "m1"), chat[0]);
            Assert.NotEqual(toast[0], chat[0]);
        }

        [Fact]
        public void 文言整形は空入力に耐える()
        {
            Assert.Empty(ScatterGroupsText.FormatLines(null, a => "x"));
            Assert.Empty(ScatterGroupsText.FormatLines(new List<List<int>>(), null));
        }
    }
}
