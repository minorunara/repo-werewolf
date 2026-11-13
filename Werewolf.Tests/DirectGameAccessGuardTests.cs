using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Werewolf.Tests
{
    public class DirectGameAccessGuardTests
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
        public void Sources_DoNotReintroduceStringBasedGameAccess()
        {
            var srcDir = typeof(DirectGameAccessGuardTests).Assembly
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
                "本体メンバーを文字列解決している箇所がある（Publicizer による直接参照を使う。" +
                "Harmony 等で必要な解決は用途を確認して監査対象を調整する）:\n  " + string.Join("\n  ", offenders));
        }
    }
}
