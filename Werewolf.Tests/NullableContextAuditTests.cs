using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Werewolf.Tests
{
    public sealed class NullableContextAuditTests
    {
        private static readonly HashSet<string> PhotonNetFiles = new HashSet<string>(
            new[] { "PhotonRpcBus.cs", "RpcEndpoint.cs", "PhotonRoomCallbackAdapter.cs" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly string[] LinkedDebugFiles =
        {
            "CommandGate.cs",
            "DebugChatAuto.cs",
            "RepeatedLogLimiter.cs",
            "SecretLogBuffer.cs",
            "StructuredLogPolicy.cs",
            "SelfTest.cs",
        };

        [Fact]
        public void ProductionProject_EnablesNullableAnalysis_WithOnlyAuditedExceptions()
        {
            string sourceDir = AuditEnvironment.Meta("Werewolf.SourceDir");
            string project = File.ReadAllText(Path.Combine(sourceDir, "Werewolf.csproj"));
            Assert.Contains("<Nullable>enable</Nullable>", project, StringComparison.Ordinal);
            Assert.Contains("<WarningsAsErrors>$(WarningsAsErrors);nullable</WarningsAsErrors>",
                project, StringComparison.Ordinal);

            string separator = Path.DirectorySeparatorChar.ToString();
            var sourceFiles = Directory.EnumerateFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(path => !(separator + Path.GetRelativePath(sourceDir, path)).Contains(separator + "obj" + separator, StringComparison.OrdinalIgnoreCase))
                .Where(path => !(separator + Path.GetRelativePath(sourceDir, path)).Contains(separator + "out" + separator, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var disabled = sourceFiles
                .SelectMany(path => File.ReadLines(path)
                    .Select(line => (Path: path, Directive: line.TrimStart('\uFEFF', ' ', '\t'))))
                .Where(entry => entry.Directive.StartsWith("#nullable disable", StringComparison.Ordinal))
                .Select(entry => Path.GetRelativePath(sourceDir, entry.Path) + ": " + entry.Directive)
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

            Assert.Empty(disabled);

            var warningPragmas = sourceFiles
                .SelectMany(path => File.ReadLines(path)
                    .Select(line => (Path: path, Directive: line.TrimStart('\uFEFF', ' ', '\t'))))
                .Where(entry => entry.Directive.StartsWith("#pragma warning disable", StringComparison.Ordinal)
                    || entry.Directive.StartsWith("#pragma warning restore", StringComparison.Ordinal))
                .Select(entry => Path.GetRelativePath(sourceDir, entry.Path) + ": "
                    + entry.Directive.Split(new[] { "//" }, StringSplitOptions.None)[0].TrimEnd())
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

            Assert.Equal(new[]
            {
                "ConfigBindings.cs: #pragma warning disable CS8618",
                "ConfigBindings.cs: #pragma warning restore CS8618",
            }, warningPragmas);
        }

        [Fact]
        public void SourceLinkedPureFiles_EnableNullableAnalysis()
        {
            string sourceDir = AuditEnvironment.Meta("Werewolf.SourceDir");
            var files = new List<string>();
            files.AddRange(Directory.EnumerateFiles(
                Path.Combine(sourceDir, "Core"), "*.cs", SearchOption.AllDirectories));
            files.AddRange(Directory.EnumerateFiles(
                Path.Combine(sourceDir, "Scenarios", "Core"), "*.cs", SearchOption.AllDirectories));
            files.AddRange(Directory.EnumerateFiles(
                    Path.Combine(sourceDir, "Net"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !PhotonNetFiles.Contains(Path.GetFileName(path))));
            files.AddRange(LinkedDebugFiles.Select(name => Path.Combine(sourceDir, "Debug", name)));

            var disabled = files
                .Where(path => !File.ReadAllText(path).TrimStart('\uFEFF')
                    .StartsWith("#nullable enable", StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(sourceDir, path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            Assert.True(disabled.Count == 0,
                "純粋C#層でnullable解析が無効なファイルがある:\n  - "
                + string.Join("\n  - ", disabled));
        }

        [Fact]
        public void PhotonTransportFiles_EnableNullableAnalysis()
        {
            string sourceDir = AuditEnvironment.Meta("Werewolf.SourceDir");
            string netDir = Path.Combine(sourceDir, "Net");
            var disabled = PhotonNetFiles
                .Select(name => Path.Combine(netDir, name))
                .Where(path => !File.ReadAllText(path).TrimStart('\uFEFF')
                    .StartsWith("#nullable enable", StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(sourceDir, path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            Assert.True(disabled.Count == 0,
                "Photon transport層でnullable解析が無効なファイルがある:\n  - "
                + string.Join("\n  - ", disabled));
        }

        [Fact]
        public void UnityBoundaryFiles_EnableNullableAnalysis()
        {
            string sourceDir = AuditEnvironment.Meta("Werewolf.SourceDir");
            var files = new List<string>
            {
                Path.Combine(sourceDir, "Game", "ClientMirrors.cs"),
            };
            string[] directories =
            {
                Path.Combine(sourceDir, "Game", "Controllers"),
                Path.Combine(sourceDir, "Game", "Patches"),
                Path.Combine(sourceDir, "UI"),
                Path.Combine(sourceDir, "Vanilla"),
                Path.Combine(sourceDir, "Scenarios", "Vanilla"),
                Path.Combine(sourceDir, "Scenarios", "Game", "Patches"),
            };
            foreach (string directory in directories)
            {
                files.AddRange(Directory.EnumerateFiles(
                    directory, "*.cs", SearchOption.AllDirectories));
            }

            var disabled = files
                .Where(path => !File.ReadAllText(path).TrimStart('\uFEFF')
                    .StartsWith("#nullable enable", StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(sourceDir, path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            Assert.True(disabled.Count == 0,
                "Unity境界層でnullable解析が無効なファイルがある:\n  - "
                + string.Join("\n  - ", disabled));
        }
    }
}
