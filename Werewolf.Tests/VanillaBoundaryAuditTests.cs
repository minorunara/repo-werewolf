using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace Werewolf.Tests
{
    public class VanillaBoundaryAuditTests
    {
        private const string VanillaScope = "Assembly-CSharp";
        private const string GameRefsTypeName = "Werewolf.GameRefs";

        private static readonly Dictionary<string, int> Baseline = new Dictionary<string, int>(StringComparer.Ordinal)
        {
        };

        internal static bool MayNameVanilla(string rootTypeFullName)
        {
            if (rootTypeFullName == GameRefsTypeName) return true;
            if (rootTypeFullName.StartsWith("Werewolf.Vanilla.", StringComparison.Ordinal)) return true;
            if (rootTypeFullName.StartsWith("Werewolf.Scenarios.Vanilla.", StringComparison.Ordinal)) return true;
            return rootTypeFullName.Contains(".Patches.", StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("Werewolf.Vanilla.Session", true)]
        [InlineData("Werewolf.Vanilla.Enemies.EnemyFreezer", true)]
        [InlineData("Werewolf.Scenarios.Vanilla.ScenarioBodyLooks", true)]
        [InlineData("Werewolf.Game.Patches.PlayerDeathPatch", true)]
        [InlineData("Werewolf.Scenarios.Game.Patches.ScenarioDeathGuardPatch", true)]
        [InlineData("Werewolf.GameRefs", true)]
        [InlineData("Werewolf.Game.WerewolfDirector", false)]
        [InlineData("Werewolf.Game.MeetingClientController", false)]
        [InlineData("Werewolf.UI.ListedAvatar", false)]
        [InlineData("Werewolf.Scenarios.Game.ScenarioRun", false)]
        public void Policy_DecidesByNamespace(string typeName, bool expected)
        {
            Assert.Equal(expected, MayNameVanilla(typeName));
        }

        [Fact]
        public void OnlyTheVanillaLayerNamesTheGame()
        {
            var refsByType = VanillaRefsByRootType();

            var violations = new List<string>();
            foreach (var entry in refsByType)
            {
                if (MayNameVanilla(entry.Key)) continue;
                int allowed = Baseline.TryGetValue(entry.Key, out var b) ? b : 0;
                if (entry.Value.Count <= allowed) continue;
                violations.Add($"[\"{entry.Key}\"] = {entry.Value.Count},   // 台帳={allowed}\n      "
                    + string.Join("\n      ", entry.Value));
            }
            var stale = Baseline.Keys
                .Where(k => !refsByType.ContainsKey(k) || MayNameVanilla(k))
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            Assert.True(violations.Count == 0,
                "接着層（Werewolf.Vanilla.* / *.Patches.* / GameRefs）の外で本体を名指ししている型。"
                + "接着層の語彙へ寄せるか、型ごと接着層へ移す（移行中の台帳 Baseline は減らすことしかできない）:\n  "
                + string.Join("\n  ", violations));
            Assert.True(stale.Count == 0,
                "本体参照が無くなった（または接着層へ移った）のに台帳 Baseline に残っている型（消す）:\n  "
                + string.Join("\n  ", stale));
        }

        [Fact]
        public void Baseline_OnlyShrinks()
        {
            var refsByType = VanillaRefsByRootType();

            var loose = Baseline
                .Where(kv => refsByType.TryGetValue(kv.Key, out var set) && set.Count < kv.Value)
                .Select(kv => $"[\"{kv.Key}\"] = {refsByType[kv.Key].Count},   // 台帳={kv.Value}")
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            Assert.True(loose.Count == 0,
                "台帳 Baseline の値が実態より大きい（実態の値へ下げる）:\n  " + string.Join("\n  ", loose));
        }

        private static SortedDictionary<string, SortedSet<string>> VanillaRefsByRootType()
        {
            var dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            using var module = ModuleDefinition.ReadModule(dllPath);
            return VanillaRefsByRootType(module);
        }

        internal static SortedDictionary<string, SortedSet<string>> VanillaRefsByRootType(ModuleDefinition module)
        {
            var result = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (var type in CecilTypes.AllTypes(module))
            {
                TypeDefinition root = type;
                while (root.DeclaringType != null) root = root.DeclaringType;
                string owner = root.FullName;
                if (owner == "<Module>") continue;

                foreach (var name in VanillaRefNames(type))
                {
                    if (!result.TryGetValue(owner, out var set))
                        result[owner] = set = new SortedSet<string>(StringComparer.Ordinal);
                    set.Add(name);
                }
            }
            return result;
        }

        private static IEnumerable<string> VanillaRefNames(TypeDefinition type)
        {
            foreach (var n in VanillaTypeNames(type.BaseType)) yield return n;
            foreach (var i in type.Interfaces)
            foreach (var n in VanillaTypeNames(i.InterfaceType))
                yield return n;
            foreach (var f in type.Fields)
            foreach (var n in VanillaTypeNames(f.FieldType))
                yield return n;

            foreach (var m in type.Methods)
            {
                foreach (var n in VanillaTypeNames(m.ReturnType)) yield return n;
                foreach (var p in m.Parameters)
                foreach (var n in VanillaTypeNames(p.ParameterType))
                    yield return n;
                if (!m.HasBody) continue;

                foreach (var v in m.Body.Variables)
                foreach (var n in VanillaTypeNames(v.VariableType))
                    yield return n;

                foreach (var instr in m.Body.Instructions)
                {
                    switch (instr.Operand)
                    {
                        case TypeReference t:
                            foreach (var n in VanillaTypeNames(t)) yield return n;
                            break;
                        case MethodReference method:
                            foreach (var n in VanillaTypeNames(method.DeclaringType)) yield return n;
                            foreach (var n in VanillaTypeNames(method.ReturnType)) yield return n;
                            foreach (var p in method.Parameters)
                            foreach (var n in VanillaTypeNames(p.ParameterType))
                                yield return n;
                            if (IsVanilla(method.DeclaringType))
                                yield return $"{method.DeclaringType.FullName}::{method.Name}";
                            break;
                        case FieldReference field:
                            foreach (var n in VanillaTypeNames(field.DeclaringType)) yield return n;
                            foreach (var n in VanillaTypeNames(field.FieldType)) yield return n;
                            if (IsVanilla(field.DeclaringType))
                                yield return $"{field.DeclaringType.FullName}::{field.Name}";
                            else if (field.DeclaringType.FullName == GameRefsTypeName)
                                yield return $"{GameRefsTypeName}::{field.Name}";
                            break;
                    }
                }
            }
        }

        private static IEnumerable<string> VanillaTypeNames(TypeReference type)
        {
            foreach (var t in Flatten(type))
                if (IsVanilla(t)) yield return t.FullName;
        }

        private static bool IsVanilla(TypeReference type)
        {
            var element = type;
            while (element is TypeSpecification spec) element = spec.ElementType;
            return element?.Scope?.Name == VanillaScope;
        }

        private static IEnumerable<TypeReference> Flatten(TypeReference type)
        {
            if (type == null) yield break;
            yield return type;
            if (type is GenericInstanceType generic)
            {
                foreach (var arg in generic.GenericArguments)
                foreach (var t in Flatten(arg))
                    yield return t;
            }
            else if (type is TypeSpecification spec)
            {
                foreach (var t in Flatten(spec.ElementType)) yield return t;
            }
        }
    }
}
