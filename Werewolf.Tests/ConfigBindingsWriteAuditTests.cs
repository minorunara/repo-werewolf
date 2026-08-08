using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class ConfigBindingsWriteAuditTests
    {
        private const string BindingsTypeName = "Werewolf.ConfigBindings";

        private static readonly Dictionary<string, string> AllowedExternalWriters =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["StartMapName"] = "Werewolf.WorldgenMapBinding",
            };

        private static string Meta(string key)
        {
            return typeof(ConfigBindingsWriteAuditTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static bool IsBindPath(MethodReference method)
        {
            if (method.DeclaringType.FullName != BindingsTypeName) return false;
            return method.Name == ".ctor" || method.Name.StartsWith("Bind", StringComparison.Ordinal);
        }

        [Fact]
        public void ConfigEntryFields_AreWrittenOnlyFromBindPath()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);
            var bindings = module.GetType(BindingsTypeName);
            Assert.NotNull(bindings);

            var guarded = new HashSet<string>(
                bindings.Fields
                    .Where(f => !f.IsStatic && f.FieldType.FullName.Contains("ConfigEntry"))
                    .Select(f => f.Name),
                StringComparer.Ordinal);

            Assert.True(guarded.Count > 50,
                $"監査対象の ConfigEntry フィールドが {guarded.Count} 個しか見つからない" +
                "（型名の判定条件が実装とずれている疑い）。");

            var violations = new List<string>();

            foreach (TypeDefinition type in module.Types.SelectMany(Flatten))
            {
                foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
                {
                    if (IsBindPath(method)) continue;

                    foreach (Instruction ins in method.Body.Instructions)
                    {
                        if (ins.OpCode != OpCodes.Stfld || !(ins.Operand is FieldReference fr)) continue;
                        if (fr.DeclaringType.FullName != BindingsTypeName) continue;
                        if (!guarded.Contains(fr.Name)) continue;

                        if (AllowedExternalWriters.TryGetValue(fr.Name, out string allowedType) &&
                            type.FullName == allowedType)
                            continue;

                        violations.Add($"  - {fr.Name} ← {type.FullName}::{method.Name}");
                    }
                }
            }

            Assert.True(violations.Count == 0,
                $"Bind 経路の外から ConfigEntry フィールドが書き換えられている（{violations.Count} 箇所）:\n" +
                string.Join("\n", violations.Distinct().OrderBy(s => s, StringComparer.Ordinal)) + "\n" +
                "Plugin 側の static エイリアスが古い ConfigEntry を指したままになり、" +
                "REPOConfig でのその場編集が一部の参照元へ反映されなくなる。" +
                "Bind は ConfigBindings のコンストラクタまたは Bind* メソッドで行うこと。" +
                "正当な再 Bind 経路は AllowedExternalWriters へ理由付きで登録する。");
        }

        private static IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
        {
            yield return type;
            foreach (TypeDefinition nested in type.NestedTypes.SelectMany(Flatten))
                yield return nested;
        }
    }
}
