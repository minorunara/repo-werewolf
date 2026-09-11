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
                violations.Add($"{entry.Key} ({entry.Value.Count})\n      "
                    + string.Join("\n      ", entry.Value));
            }

            Assert.True(violations.Count == 0,
                "接着層（Werewolf.Vanilla.* / *.Patches.* / GameRefs）の外で本体を名指ししている型。"
                + "接着層の語彙へ寄せるか、型ごと接着層へ移す:\n  "
                + string.Join("\n  ", violations));
        }

        private static readonly string[] UpperLayerPrefixes =
        {
            "Werewolf.Game.",
            "Werewolf.UI.",
            "Werewolf.Scenarios.Game.",
            "Werewolf.Scenarios.UI.",
            "Werewolf.Debugging.",
        };

        internal static bool IsGlueLayer(string rootTypeFullName)
            => rootTypeFullName.StartsWith("Werewolf.Vanilla.", StringComparison.Ordinal)
               || rootTypeFullName.StartsWith("Werewolf.Scenarios.Vanilla.", StringComparison.Ordinal);

        internal static bool IsUpperLayer(string typeFullName)
            => !typeFullName.Contains(".Patches.", StringComparison.Ordinal)
               && UpperLayerPrefixes.Any(p => typeFullName.StartsWith(p, StringComparison.Ordinal));

        [Fact]
        public void GlueLayerDoesNotReachUpperLayers()
        {
            var dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            using var module = ModuleDefinition.ReadModule(dllPath);

            var violations = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (var type in CecilTypes.AllTypes(module))
            {
                TypeDefinition root = type;
                while (root.DeclaringType != null) root = root.DeclaringType;
                if (!IsGlueLayer(root.FullName)) continue;

                foreach (var referenced in ReferencedTypes(type))
                {
                    if (!IsUpperLayer(referenced.FullName)) continue;
                    if (!violations.TryGetValue(root.FullName, out var set))
                        violations[root.FullName] = set = new SortedSet<string>(StringComparer.Ordinal);
                    set.Add(referenced.FullName);
                }
            }

            Assert.True(violations.Count == 0,
                "接着層（Werewolf.Vanilla.* / Werewolf.Scenarios.Vanilla.*）が上位層（Game / UI / Debugging。"
                + "パッチ層を除く）を参照している。読み口の反転（例: IReplayDirectorView / ITutorialBubble）か"
                + "構築時の流し込み（例: CursorMirror.SortingOrder）で依存方向を Vanilla ← Game / UI に戻す:\n  "
                + string.Join("\n  ", violations.Select(kv => kv.Key + "\n      " + string.Join("\n      ", kv.Value))));
        }

        private static IEnumerable<TypeReference> ReferencedTypes(TypeDefinition type)
        {
            foreach (var t in Flatten(type.BaseType)) yield return t;
            foreach (var i in type.Interfaces)
            foreach (var t in Flatten(i.InterfaceType))
                yield return t;
            foreach (var f in type.Fields)
            foreach (var t in Flatten(f.FieldType))
                yield return t;

            foreach (var m in type.Methods)
            {
                foreach (var t in Flatten(m.ReturnType)) yield return t;
                foreach (var p in m.Parameters)
                foreach (var t in Flatten(p.ParameterType))
                    yield return t;
                if (!m.HasBody) continue;

                foreach (var v in m.Body.Variables)
                foreach (var t in Flatten(v.VariableType))
                    yield return t;

                foreach (var instr in m.Body.Instructions)
                {
                    switch (instr.Operand)
                    {
                        case TypeReference tr:
                            foreach (var t in Flatten(tr)) yield return t;
                            break;
                        case MethodReference method:
                            foreach (var t in Flatten(method.DeclaringType)) yield return t;
                            foreach (var t in Flatten(method.ReturnType)) yield return t;
                            foreach (var p in method.Parameters)
                            foreach (var t in Flatten(p.ParameterType))
                                yield return t;
                            break;
                        case FieldReference field:
                            foreach (var t in Flatten(field.DeclaringType)) yield return t;
                            foreach (var t in Flatten(field.FieldType)) yield return t;
                            break;
                    }
                }
            }
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
