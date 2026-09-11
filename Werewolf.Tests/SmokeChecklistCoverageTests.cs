using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Werewolf.Tests
{
    public class SmokeChecklistCoverageTests
    {
        private const string ChecklistRelativePath = "Vanilla/SMOKE_CHECKLIST.md";

        [Fact]
        public void EveryGlueModuleIsListed()
        {
            string sourceDir = AuditEnvironment.Meta("Werewolf.SourceDir");
            string path = Path.Combine(sourceDir, ChecklistRelativePath);
            Assert.True(File.Exists(path), $"復旧後スモークチェックリスト {ChecklistRelativePath} が無い。");
            string checklist = File.ReadAllText(path);

            var missing = new[] { "Vanilla", Path.Combine("Scenarios", "Vanilla") }
                .SelectMany(dir => Directory.GetFiles(Path.Combine(sourceDir, dir), "*.cs"))
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !checklist.Contains("`" + name + "`", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.True(missing.Count == 0,
                $"接着モジュールが {ChecklistRelativePath} に載っていない。どの操作で踏まれるかを追記する:\n  "
                + string.Join("\n  ", missing));
        }
    }
}
