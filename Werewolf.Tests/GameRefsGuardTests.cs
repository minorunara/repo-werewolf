using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Werewolf.Tests
{
    public class GameRefsGuardTests
    {
        private static readonly string[] BannedPatterns =
        {
            "AccessTools.FieldRefAccess",
            "AccessTools.Field(",
            "AccessTools.Method(",
            "AccessTools.Property(",
            "AccessTools.TypeByName(",
            "Traverse.Create",
        };

        [Fact]
        public void Sources_ResolveGameMembersOnlyViaGameRefs()
        {
            var srcDir = typeof(GameRefsGuardTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "Werewolf.SourceDir").Value;
            Assert.True(Directory.Exists(srcDir), $"ソースディレクトリが見つからない: {srcDir}");

            var offenders = new List<string>();
            foreach (var file in Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}out{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }
                if (Path.GetFileName(file) == "GameRefs.cs") continue;

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (var pattern in BannedPatterns)
                    {
                        if (lines[i].Contains(pattern, StringComparison.Ordinal))
                        {
                            offenders.Add($"{Path.GetFileName(file)}:{i + 1} [{pattern}]");
                        }
                    }
                }
            }

            Assert.True(offenders.Count == 0,
                "GameRefs 外で本体メンバーを文字列解決している箇所がある（GameRefs へ集約し、" +
                "利用側は解決済みハンドルを参照する）:\n  " + string.Join("\n  ", offenders));
        }
    }
}
