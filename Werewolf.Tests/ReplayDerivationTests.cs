using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayDerivationTests
    {
        [Fact]
        public void RemainingTotalDollars_IsReconstructibleAtAnySeekPoint()
        {
            var header = new ReplaySegmentHeader
            {
                LevelName = "Level - Test",
                StartedAtIso = "2026-08-11T00:00:00+09:00",
                IsHost = true,
                LocalActor = 1,
                Valuables = new List<ReplayValuableInfo>
                {
                    new ReplayValuableInfo { Id = 10, Name = "Vase", Dollars = 1000 },
                    new ReplayValuableInfo { Id = 20, Name = "Clock", Dollars = 500 },
                },
            };
            var rec = new ReplayRecorder();
            rec.BeginSegment(header, 0.0);
            rec.NoteValuableValue(5.0, 10, 700);
            rec.Sample(8.0, new[]
            {
                new ReplayEntitySample(ReplayEntityKind.Valuable, 10, 0f, 0f, 0f),
            });
            rec.NoteValuableValue(12.0, 10, 100);
            rec.EndSegment(15.0);

            var lines = rec.ToJsonLines().ToList();
            Assert.Equal(1500, DeriveRemainingAt(lines, 4.9));
            Assert.Equal(1200, DeriveRemainingAt(lines, 5.0));
            Assert.Equal(700, DeriveRemainingAt(lines, 8.0));
            Assert.Equal(100, DeriveRemainingAt(lines, 14.0));
        }

        private static int DeriveRemainingAt(List<string> jsonLines, double t)
        {
            var dollarsById = new Dictionary<int, int>();
            foreach (string line in jsonLines)
            {
                using JsonDocument doc = JsonDocument.Parse(line);
                JsonElement root = doc.RootElement;
                string kind = root.GetProperty("k").GetString();

                if (kind == "seg")
                {
                    dollarsById.Clear();
                    foreach (JsonElement v in root.GetProperty("vals").EnumerateArray())
                    {
                        dollarsById[v.GetProperty("v").GetInt32()] = v.GetProperty("$").GetInt32();
                    }
                    continue;
                }
                if (kind != "ev" || root.GetProperty("t").GetDouble() > t) continue;

                switch (root.GetProperty("e").GetString())
                {
                    case "val_value":
                        dollarsById[root.GetProperty("v").GetInt32()] = root.GetProperty("$").GetInt32();
                        break;
                    case "val_gone":
                        dollarsById.Remove(root.GetProperty("v").GetInt32());
                        break;
                }
            }
            return dollarsById.Values.Sum();
        }
    }
}
