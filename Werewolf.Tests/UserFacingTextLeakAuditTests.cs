using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class UserFacingTextLeakAuditTests
    {
        private static readonly string[] AuditedTypes =
        {
            "Werewolf.ConfigBindings",
            "Werewolf.Core.Texts",
            "Werewolf.Core.SettingsCatalog",
        };

        private static readonly Regex InternalJargon = new Regex(
            @"ADR-\d|ISSUE-\d|design\.md|requirements\.md|tasks\.md|dev-gotchas|MANUAL_VERIFY|docs[/\\]adr",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static string Meta(string key)
        {
            return typeof(UserFacingTextLeakAuditTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        [Fact]
        public void UserFacingStrings_DoNotLeakInternalIdentifiers()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);

            var types = AuditedTypes
                .Select(name => module.GetType(name))
                .ToArray();

            for (int i = 0; i < types.Length; i++)
                Assert.True(types[i] != null, $"監査対象の型が見つからない: {AuditedTypes[i]}（改名の疑い）");

            var violations = new List<string>();

            foreach (TypeDefinition type in types.SelectMany(Flatten))
            {
                foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
                {
                    foreach (Instruction ins in method.Body.Instructions)
                    {
                        if (ins.OpCode != OpCodes.Ldstr || !(ins.Operand is string text)) continue;

                        Match hit = InternalJargon.Match(text);
                        if (!hit.Success) continue;

                        violations.Add($"  - {type.FullName}::{method.Name}: \"{hit.Value}\" in \"{Excerpt(text)}\"");
                    }
                }
            }

            Assert.True(violations.Count == 0,
                $"利用者に見える文字列へ開発用の識別子が混ざっている（{violations.Count} 箇所）:\n" +
                string.Join("\n", violations.Distinct().OrderBy(s => s, StringComparer.Ordinal)) + "\n" +
                "cfg・ゲーム内文言・設定確認パネルは配布物の一部で、ADR / ISSUE 番号やスペックの" +
                "ファイル名は読み手から参照できない。説明は挙動だけを書き、決定の経緯は" +
                "コミットメッセージと決定記録（ADR）へ置くこと。");
        }

        private static string Excerpt(string text)
        {
            const int max = 80;
            string single = text.Replace("\n", " ").Replace("\r", " ");
            return single.Length <= max ? single : single.Substring(0, max) + "…";
        }

        private static IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
        {
            yield return type;
            foreach (TypeDefinition nested in type.NestedTypes.SelectMany(Flatten))
                yield return nested;
        }
    }
}
