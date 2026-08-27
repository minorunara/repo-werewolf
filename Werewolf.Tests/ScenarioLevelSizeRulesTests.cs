using System.IO;
using System.Reflection;
using System.Linq;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioLevelSizeRulesTests
    {
        [Theory]
        [InlineData(0, 5)]
        [InlineData(2, 7)]
        [InlineData(5, 10)]
        [InlineData(9, 10)]
        [InlineData(12, 13)]
        [InlineData(20, 15)]
        public void VanillaModuleAmount_FollowsTheGameFormula(int levelsCompleted, int expected)
        {
            Assert.Equal(expected, ScenarioLevelSizeRules.VanillaModuleAmount(levelsCompleted));
        }

        [Theory]
        [InlineData(2, 8)]
        [InlineData(2, 11)]
        [InlineData(2, 15)]
        [InlineData(0, 9)]
        [InlineData(5, 14)]
        public void SizeMultiplier_LandsExactlyOnTheRequestedModuleAmount(int levelsCompleted, int target)
        {
            float multiplier = ScenarioLevelSizeRules.SizeMultiplier(levelsCompleted, target);
            Assert.Equal(target, ScenarioLevelSizeRules.ScaledModuleAmount(levelsCompleted, multiplier));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(7)]
        public void SizeMultiplier_IsNeutralWhenTheMapIsNotWidened(int target)
        {
            Assert.Equal(1f, ScenarioLevelSizeRules.SizeMultiplier(2, target));
        }

        [Fact]
        public void SizeMultiplier_ClampsAnOversizedRequestToTheGameMaximum()
        {
            float multiplier = ScenarioLevelSizeRules.SizeMultiplier(2, 999);
            Assert.Equal(ScenarioLevelSizeRules.MaxModuleAmount,
                ScenarioLevelSizeRules.ScaledModuleAmount(2, multiplier));
        }

        [Fact]
        public void VanillaModuleAmount_MirrorsTheDecompiledGameSource()
        {
            string source = File.ReadAllText(DecompiledLevelGeneratorPath());

            string[] expected =
            {
                "ModuleAmount = Mathf.Min(5 + RunManager.instance.levelsCompleted, 10);",
                "if (RunManager.instance.levelsCompleted >= 10)",
                "ModuleAmount += Mathf.Min(RunManager.instance.levelsCompleted - 9, 5);",
                "ModuleAmount = Mathf.CeilToInt((float)ModuleAmount * DebugLevelSize);",
            };

            var missing = expected.Where(line => !source.Contains(line)).ToArray();

            Assert.True(missing.Length == 0,
                "本体のモジュール数の式が変わった。ScenarioLevelSizeRules.VanillaModuleAmount と " +
                "ScaledModuleAmount を本体へ合わせ直す:\n  " + string.Join("\n  ", missing));
        }

        private static string DecompiledLevelGeneratorPath()
        {
            string sourceDir = typeof(ScenarioLevelSizeRulesTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "Werewolf.SourceDir").Value;
            string repoRoot = Path.GetFullPath(Path.Combine(sourceDir, ".."));
            string path = Path.Combine(repoRoot, "DnSpy", "clean", "LevelGenerator.cs");

            Assert.True(File.Exists(path), $"逆コンパイル済みソースが見つからない: {path}");
            return path;
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(4, 3)]
        public void ExtractionRooms_LeavesTheStartRoomOutOfTheGeneratedCount(int points, int expected)
        {
            Assert.Equal(expected, ScenarioLevelSizeRules.ExtractionRooms(points));
        }
    }
}
