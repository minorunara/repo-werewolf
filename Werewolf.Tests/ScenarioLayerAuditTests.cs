using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioLayerAuditTests
    {
        private const string ScenarioPrefix = PatchTierPolicy.ScenarioNamespacePrefix;
        private const string ScenarioCorePrefix = "Werewolf.Scenarios.Core";

        private static readonly Dictionary<string, string> SeamTypes = new Dictionary<string, string>
        {
            ["Werewolf.Plugin"] =
                "起動時のシナリオ用設定スナップショットと、パッチ層・参照層の帰結通知",
            ["Werewolf.ConfigBindings"] =
                "シナリオごとの完了フラグ（Cleared{Id}）の Bind",
            ["Werewolf.Game.WerewolfDirector"] =
                "シナリオ機能コントローラの保持・信号ファサード・選択UIの駆動",
            ["Werewolf.Vanilla.WorldgenApplier"] =
                "シナリオ専用の開始環境適用と人数・設定検証の分岐",
            ["Werewolf.Vanilla.HaulGoalOverride"] =
                "ノルマ上書き値の丸め（ScenarioSafetyRules）",
            ["Werewolf.Game.BomberClientController"] =
                "爆弾操作の出来事をシナリオ信号として発行",
            ["Werewolf.Game.MeetingClientController"] =
                "会議・投票の出来事をシナリオ信号として発行",
            ["Werewolf.Game.MeetingChatController"] =
                "会議チャットログの強制有効（ScenarioSessionGate）",
            ["Werewolf.Game.WinCeremonyController"] =
                "勝敗確定演出の完了をシナリオ信号として発行",
            ["Werewolf.Game.ResultFlowController"] =
                "無効試合の長押しの受付をチュートリアルで閉じる（ScenarioSessionGate）",
        };

        private static readonly Dictionary<string, string> ScenarioPatchClasses = new Dictionary<string, string>
        {
            ["Werewolf.Scenarios.Game.Patches.ScenarioDeathGuardPatch"] =
                "チュートリアル中のローカルプレイヤー死亡を止める",
            ["Werewolf.Scenarios.Game.Patches.MenuHoverBlockPatch"] =
                "選択UI表示中のバニラメニュー要素のホバー抑止",
            ["Werewolf.Scenarios.Game.Patches.ScenarioLevelSizePatch"] =
                "シナリオ指定のマップの広さ（レベルサイズ倍率）の書き込み",
            ["Werewolf.Scenarios.Game.Patches.ScenarioExtractionRoomsPatch"] =
                "シナリオ指定を超える抽出部屋の敷設の見送り",
            ["Werewolf.Scenarios.Game.Patches.ScenarioDamageClampPatch"] =
                "チュートリアル中のローカルプレイヤーのHP下限クランプ",
        };

        private static string Meta(string key)
        {
            return typeof(ScenarioLayerAuditTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static Dictionary<string, string> PatchClassesInSource()
        {
            string srcDir = Meta("Werewolf.SourceDir");
            Assert.True(Directory.Exists(srcDir), $"ソースディレクトリが見つからない: {srcDir}");

            var found = new Dictionary<string, string>(StringComparer.Ordinal);
            char sep = Path.DirectorySeparatorChar;
            string srcRoot = Path.GetFullPath(srcDir).TrimEnd(sep) + sep;
            foreach (var file in Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories))
            {
                string relative = sep + Path.GetFullPath(file).Substring(srcRoot.Length);
                if (relative.Contains($"{sep}obj{sep}") || relative.Contains($"{sep}out{sep}")) continue;

                string ns = null;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var nsMatch = Regex.Match(lines[i], @"^\s*namespace\s+([\w.]+)");
                    if (nsMatch.Success)
                    {
                        ns = nsMatch.Groups[1].Value;
                        continue;
                    }
                    if (!Regex.IsMatch(lines[i], @"^\s*\[HarmonyPatch")) continue;

                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        string line = lines[j].Trim();
                        if (line.Length == 0 || line.StartsWith("[", StringComparison.Ordinal)) continue;
                        var classMatch = Regex.Match(line, @"^(?:\w+\s+)*class\s+(\w+)");
                        if (classMatch.Success) found[$"{ns}.{classMatch.Groups[1].Value}"] = file;
                        break;
                    }
                }
            }
            return found;
        }

        private static ModuleDefinition LoadModModule()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            return ModuleDefinition.ReadModule(dllPath);
        }

        private static string DeclaringRootName(TypeDefinition type)
        {
            TypeDefinition root = type;
            while (root.DeclaringType != null) root = root.DeclaringType;
            return root.FullName;
        }

        private static bool IsScenarioName(string fullName)
            => fullName != null && fullName.StartsWith(ScenarioPrefix, StringComparison.Ordinal);

        private static IEnumerable<string> Flatten(TypeReference type)
        {
            if (type == null) yield break;
            yield return type.FullName;

            if (type is GenericInstanceType generic)
            {
                foreach (var arg in generic.GenericArguments)
                foreach (var name in Flatten(arg))
                    yield return name;
            }
            else if (type is TypeSpecification spec)
            {
                foreach (var name in Flatten(spec.ElementType)) yield return name;
            }
        }

        private static IEnumerable<string> ReferencedTypeNames(TypeDefinition type)
        {
            foreach (var name in Flatten(type.BaseType)) yield return name;
            foreach (var i in type.Interfaces)
            foreach (var name in Flatten(i.InterfaceType))
                yield return name;
            foreach (var f in type.Fields)
            foreach (var name in Flatten(f.FieldType))
                yield return name;

            foreach (var m in type.Methods)
            {
                foreach (var name in Flatten(m.ReturnType)) yield return name;
                foreach (var p in m.Parameters)
                foreach (var name in Flatten(p.ParameterType))
                    yield return name;
                if (!m.HasBody) continue;

                foreach (var v in m.Body.Variables)
                foreach (var name in Flatten(v.VariableType))
                    yield return name;

                foreach (var instr in m.Body.Instructions)
                {
                    switch (instr.Operand)
                    {
                        case TypeReference t:
                            foreach (var name in Flatten(t)) yield return name;
                            break;
                        case MethodReference method:
                            foreach (var name in Flatten(method.DeclaringType)) yield return name;
                            foreach (var name in Flatten(method.ReturnType)) yield return name;
                            foreach (var p in method.Parameters)
                            foreach (var name in Flatten(p.ParameterType))
                                yield return name;
                            break;
                        case FieldReference field:
                            foreach (var name in Flatten(field.DeclaringType)) yield return name;
                            foreach (var name in Flatten(field.FieldType)) yield return name;
                            break;
                    }
                }
            }
        }

        [Fact]
        public void MainCode_TouchesScenarioLayerOnlyThroughTheDocumentedSeam()
        {
            using var module = LoadModModule();
            var found = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

            foreach (var type in CecilTypes.AllTypes(module))
            {
                string owner = DeclaringRootName(type);
                if (IsScenarioName(owner)) continue;

                foreach (var referenced in ReferencedTypeNames(type))
                {
                    if (!IsScenarioName(referenced)) continue;
                    if (!found.TryGetValue(owner, out var set))
                        found[owner] = set = new SortedSet<string>(StringComparer.Ordinal);
                    set.Add(referenced);
                }
            }

            var undocumented = found.Keys.Where(k => !SeamTypes.ContainsKey(k)).ToList();
            var stale = SeamTypes.Keys.Where(k => !found.ContainsKey(k)).ToList();

            Assert.True(undocumented.Count == 0,
                "本編からチュートリアル層への未登録の参照（SeamTypes へ追加するか、Director のファサード経由へ寄せる）:\n  "
                + string.Join("\n  ", undocumented.Select(k => $"{k} -> {string.Join(", ", found[k])}")));
            Assert.True(stale.Count == 0,
                "SeamTypes に残った実体のない登録（参照が消えたので削る）:\n  " + string.Join("\n  ", stale));
        }

        [Fact]
        public void ScenarioCore_HasNoEngineDependency()
        {
            string[] engineScopes = { "Assembly-CSharp", "Photon", "Unity", "BepInEx", "0Harmony", "MenuLib" };
            using var module = LoadModModule();
            var violations = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var type in CecilTypes.AllTypes(module))
            {
                if (!DeclaringRootName(type).StartsWith(ScenarioCorePrefix, StringComparison.Ordinal)) continue;

                foreach (var m in type.Methods)
                {
                    if (!m.HasBody) continue;
                    foreach (var instr in m.Body.Instructions)
                    {
                        TypeReference declaring = instr.Operand switch
                        {
                            TypeReference t => t,
                            MemberReference member => member.DeclaringType,
                            _ => null,
                        };
                        string scope = declaring?.Scope?.Name;
                        if (scope != null
                            && engineScopes.Any(s => scope.StartsWith(s, StringComparison.Ordinal)))
                        {
                            violations.Add($"{type.FullName} -> {declaring.FullName} @ {scope}");
                        }
                    }
                }
            }

            Assert.True(violations.Count == 0,
                $"{ScenarioCorePrefix} は純C#（Unity/Photon/BepInEx 非参照）である必要がある:\n  "
                + string.Join("\n  ", violations));
        }

        [Fact]
        public void HarmonyPatchClasses_AreTieredByNamespaceAndSourceLocation()
        {
            using var module = LoadModModule();
            var compiled = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var type in CecilTypes.AllTypes(module))
            {
                if (type.CustomAttributes.Any(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch"))
                {
                    compiled.Add(type.FullName);
                }
            }

            var scenarioTier = compiled.Where(PatchTierPolicy.IsScenarioTier).ToList();
            var essentialTier = compiled.Where(n => !PatchTierPolicy.IsScenarioTier(n)).ToList();
            Assert.True(essentialTier.Count > 0, "本編層のパッチクラスが1件も見つからない（監査が空振りしている）");

            var undocumented = scenarioTier.Where(n => !ScenarioPatchClasses.ContainsKey(n)).ToList();
            var stale = ScenarioPatchClasses.Keys.Where(n => !scenarioTier.Contains(n)).ToList();
            Assert.True(undocumented.Count == 0,
                "台帳に無いチュートリアル層のパッチクラス（役割を添えて ScenarioPatchClasses へ足す）:\n  "
                + string.Join("\n  ", undocumented));
            Assert.True(stale.Count == 0,
                "台帳にあるが実体が無いチュートリアル層のパッチクラス（本編層へ移ったか消えた。"
                + "移動なら ADR-0113 の層分けが効かなくなる）:\n  " + string.Join("\n  ", stale));

            var inSource = PatchClassesInSource();
            var missingInSource = compiled.Where(n => !inSource.ContainsKey(n)).ToList();
            var missingInDll = inSource.Keys.Where(n => !compiled.Contains(n)).ToList();
            Assert.True(missingInSource.Count == 0,
                "DLLにあるがソース走査で見つからないパッチクラス（PatchClassesInSource の走査規則を直す）:\n  "
                + string.Join("\n  ", missingInSource));
            Assert.True(missingInDll.Count == 0,
                "ソース走査で見つかるがDLLに無いパッチクラス（走査の誤検出か、ネスト型の命名差）:\n  "
                + string.Join("\n  ", missingInDll));

            string scenarioDir = Path.GetFullPath(Path.Combine(Meta("Werewolf.SourceDir"), "Scenarios"))
                + Path.DirectorySeparatorChar;
            var misplaced = new List<string>();
            foreach (var entry in inSource)
            {
                bool underScenarios = Path.GetFullPath(entry.Value)
                    .StartsWith(scenarioDir, StringComparison.OrdinalIgnoreCase);
                if (underScenarios != PatchTierPolicy.IsScenarioTier(entry.Key))
                {
                    misplaced.Add($"{entry.Key}（ソース配置=Scenarios配下:{underScenarios} / "
                        + $"名前空間=チュートリアル層:{PatchTierPolicy.IsScenarioTier(entry.Key)}） @ {entry.Value}");
                }
            }
            Assert.True(misplaced.Count == 0,
                "パッチクラスの置き場所と名前空間が食い違っている（層は名前空間で決まるため、"
                + "ファイルと名前空間は必ず揃える）:\n  " + string.Join("\n  ", misplaced));
        }
    }
}
