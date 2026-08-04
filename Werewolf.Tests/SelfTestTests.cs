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
        public void RunAll_CoversRequiredScenarios()
        {
            var names = SelfTest.RunAll().Select(r => r.Name).ToList();

            Assert.Contains("role_distribution_3", names);
            Assert.Contains("role_distribution_5", names);
            Assert.Contains("role_distribution_7", names);
            Assert.Contains("role_distribution_10", names);
            Assert.Contains("forced_role", names);
            Assert.Contains("win_priority_simultaneous", names);
            Assert.Contains("blackcat_excluded_from_wolf_count", names);
            Assert.Contains("blackcat_death_continues", names);
            Assert.Contains("blackcat_shares_wolf_win", names);
            Assert.Contains("disclosure_order_and_dedup", names);
            Assert.Contains("meeting_pause_and_resume_extend", names);
            Assert.Contains("timer_expiry_wolf_win", names);

            Assert.Contains("meeting_full_flow", names);
            Assert.Contains("meeting_vote_secrecy", names);
            Assert.Contains("meeting_leave_no_execution", names);
            Assert.Contains("meeting_restore_from_room_state", names);
            Assert.Contains("meeting_vote_reject_reenable", names);
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
