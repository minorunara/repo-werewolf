using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Xunit;

namespace Werewolf.Tests
{
    public class VersionConsistencyTests
    {
        private static string Meta(string key)
        {
            return typeof(VersionConsistencyTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static string SourcePath(params string[] parts)
        {
            return Path.GetFullPath(Path.Combine(new[] { Meta("Werewolf.SourceDir") }.Concat(parts).ToArray()));
        }

        private static string ReadSource(params string[] parts)
        {
            string path = SourcePath(parts);
            Assert.True(File.Exists(path), $"ソースファイルが見つからない: {path}");
            return File.ReadAllText(path);
        }

        private static string MatchOne(string text, string pattern, string what)
        {
            Match m = Regex.Match(text, pattern);
            Assert.True(m.Success, $"{what} を抽出できなかった（パターン: {pattern}）");
            return m.Groups[1].Value.Trim();
        }

        private static string DefaultVersion()
            => MatchOne(
                ReadSource("Werewolf.csproj"),
                @"<WerewolfVersion\s+Condition=""[^""]+"">([^<]+)</WerewolfVersion>",
                "csproj の既定 WerewolfVersion");

        private static string ManifestVersion(string metadataDirectory)
            => MatchOne(
                ReadSource(metadataDirectory, "manifest.json"),
                @"""version_number""\s*:\s*""([^""]+)""",
                $"{metadataDirectory}/manifest.json の version_number");

        private static string ManifestName(string metadataDirectory)
            => MatchOne(
                ReadSource(metadataDirectory, "manifest.json"),
                @"""name""\s*:\s*""([^""]+)""",
                $"{metadataDirectory}/manifest.json の name");

        [Fact]
        public void DefaultBuildVersion_MatchesStableManifest()
        {
            string expected = ManifestVersion("thunderstore");
            Assert.Equal(expected, DefaultVersion());
        }

        [Fact]
        public void PluginVersion_MatchesDefaultBuildVersion()
        {
            string assemblyPath = Path.GetFullPath(Meta("Werewolf.ModDllPath"));
            using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            TypeDefinition plugin = assembly.MainModule.Types.Single(t => t.FullName == "Werewolf.Plugin");
            CustomAttribute attribute = plugin.CustomAttributes.Single(
                a => a.AttributeType.FullName == "BepInEx.BepInPlugin");
            string pluginVersion = (string)attribute.ConstructorArguments[2].Value;

            Assert.Equal(DefaultVersion(), pluginVersion);
        }

        [Fact]
        public void ProjectVersions_AreDerivedFromWerewolfVersion()
        {
            string text = ReadSource("Werewolf.csproj");
            string version = MatchOne(text, @"<Version>([^<]+)</Version>", "csproj の <Version>");
            string assembly = MatchOne(text, @"<AssemblyVersion>([^<]+)</AssemblyVersion>", "csproj の <AssemblyVersion>");
            string file = MatchOne(text, @"<FileVersion>([^<]+)</FileVersion>", "csproj の <FileVersion>");
            string plugin = ReadSource("Plugin.cs");

            Assert.Equal("$(WerewolfVersion)", version);
            Assert.Equal("$(WerewolfVersion).0", assembly);
            Assert.Equal("$(WerewolfVersion).0", file);
            Assert.Matches(@"PluginVersion\s*=\s*WerewolfBuildInfo\.Version\s*;", plugin);
        }

        [Fact]
        public void StableAndBetaHaveIndependentPackageNamesAndVersions()
        {
            string stableName = ManifestName("thunderstore");
            string betaName = ManifestName("thunderstore-beta");
            string stableVersion = ManifestVersion("thunderstore");
            string betaVersion = ManifestVersion("thunderstore-beta");

            Assert.Equal("REPO_Werewolf", stableName);
            Assert.Equal("REPO_Werewolf_BETA", betaName);
            Assert.NotEqual(stableName, betaName);
            Assert.NotEqual(stableVersion, betaVersion);
            Assert.Matches(@"^\d+\.\d+\.\d+$", stableVersion);
            Assert.Matches(@"^\d+\.\d+\.\d+$", betaVersion);
        }

        [Theory]
        [InlineData("thunderstore")]
        [InlineData("thunderstore-beta")]
        public void Changelog_HasSectionForManifestVersion(string metadataDirectory)
        {
            string version = ManifestVersion(metadataDirectory);
            string changelog = ReadSource(metadataDirectory, "CHANGELOG.md");

            Assert.Matches(@"(?m)^##\s+" + Regex.Escape(version) + @"\s*$", changelog);
        }
    }
}
