using System;
using System.Linq;
using Werewolf.Core;
using Werewolf.Debugging;
using Xunit;

namespace Werewolf.Tests
{
    public class SelfTestTests : IDisposable
    {
        public SelfTestTests()
        {
            WLog.Sink = null;
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Fact]
        public void RunAll_AllScenariosPass()
        {
            var results = SelfTest.RunAll();

            Assert.NotEmpty(results);
            var failed = results.Where(r => !r.Pass).ToList();
            Assert.True(failed.Count == 0,
                "FAILED: " + string.Join(" | ", failed.Select(r => r.Name + ": " + r.Detail)));
        }

        [Fact]
        public void RunAll_NamesAreNonEmptyAndUnique()
        {
            var names = SelfTest.RunAll().Select(r => r.Name).ToList();

            Assert.NotEmpty(names);
            Assert.All(names, n => Assert.False(string.IsNullOrEmpty(n)));
            Assert.Equal(names.Count, names.Distinct().Count());
        }

        [Fact]
        public void RunAll_LogsPassFailPerScenarioAndSummary()
        {
            var lines = new System.Collections.Generic.List<string>();
            WLog.Sink = (line, secret) => lines.Add(line);

            var results = SelfTest.RunAll();

            Assert.Equal(results.Count, lines.Count(l => l.Contains("selftest ") && l.Contains("result=")));
            Assert.Contains(lines, l => l.Contains("selftest_summary") && l.Contains("fail=0"));
        }
    }
}
