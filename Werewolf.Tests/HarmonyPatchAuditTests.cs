using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Xunit;

namespace Werewolf.Tests
{
    public class HarmonyPatchAuditTests
    {
        private const int PatchClassFloor = 30;

        private static ModuleDefinition ReadModDll()
        {
            var dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            return ModuleDefinition.ReadModule(dllPath,
                new ReaderParameters { AssemblyResolver = AuditEnvironment.CreateResolver() });
        }

        [Fact]
        public void PatchTargets_AllResolveAgainstGameAssemblies()
        {
            using var module = ReadModDll();
            var targets = HarmonyPatchAudit.Enumerate(module);
            var failures = HarmonyPatchAudit.FindUnresolvedTargets(targets, AuditEnvironment.InAuditScope);

            Assert.True(failures.Count == 0,
                $"解決できない Harmony パッチ対象 {failures.Count} 件"
                + "（本体アップデートで消失・改名・シグネチャ変更された可能性。"
                + "本編層が1件でも落ちると MOD 全体が無効化される＝ADR-0113。"
                + "修復手順: docs/steering/tech.md「本体アップデート対応プレイブック」）:\n  "
                + string.Join("\n  ", failures));
        }

        [Fact]
        public void PatchTargets_CoverEveryPatchClassPluginApplies()
        {
            using var module = ReadModDll();
            var targets = HarmonyPatchAudit.Enumerate(module);
            var patchClasses = targets.Select(t => t.PatchClass).Distinct().ToList();

            Assert.True(targets.Count > 0, "パッチ対象が1件も列挙されなかった（監査が空振りしている）");
            Assert.True(patchClasses.Count >= PatchClassFloor,
                $"パッチクラスが {patchClasses.Count} 件しか見つからない（下限 {PatchClassFloor}）:\n  "
                + string.Join("\n  ", targets.Select(t => t.ToString())));

            var inSource = CountPatchAttributesInSource();
            Assert.True(patchClasses.Count == inSource,
                $"DLL から列挙したパッチクラス {patchClasses.Count} 件に対しソースの [HarmonyPatch] 属性は {inSource} 件"
                + "（監査の取りこぼし、またはクラス属性以外の書き方の混入）:\n  "
                + string.Join("\n  ", patchClasses));

            var manual = targets.Where(t => t.IsManual).Select(t => t.ToString()).ToList();
            Assert.True(manual.Count == 0,
                "TargetMethod(s) で対象を決めるパッチクラス（静的監査対象外。Plugin の前提と併せて見直す）:\n  "
                + string.Join("\n  ", manual));
        }

        private static int CountPatchAttributesInSource()
        {
            var sourceDir = AuditEnvironment.Meta("Werewolf.SourceDir");
            var pattern = new Regex(@"^\s*\[HarmonyPatch[(\]]", RegexOptions.Compiled);
            char separator = Path.DirectorySeparatorChar;
            var excluded = new[] { "out", "obj", "bin" }
                .Select(d => $"{separator}{d}{separator}")
                .ToArray();
            string sourceRoot = Path.GetFullPath(sourceDir).TrimEnd(separator) + separator;
            return Directory.EnumerateFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !excluded.Any(x =>
                    (separator + Path.GetFullPath(f).Substring(sourceRoot.Length))
                        .Contains(x, StringComparison.OrdinalIgnoreCase)))
                .SelectMany(File.ReadLines)
                .Count(line => pattern.IsMatch(line));
        }
    }
}
