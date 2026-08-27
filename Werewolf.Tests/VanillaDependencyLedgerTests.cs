using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Xunit;

namespace Werewolf.Tests
{
    public class VanillaDependencyLedgerTests
    {
        private const string LedgerRelativePath = "Vanilla/DEPENDENCIES.md";
        private const string WriteEnv = "WEREWOLF_WRITE_LEDGER";
        private const string TypeOnly = "(型のみ)";

        [Fact]
        public void Ledger_MatchesTheBuiltDll()
        {
            string path = Path.Combine(AuditEnvironment.Meta("Werewolf.SourceDir"), LedgerRelativePath);
            string generated = Generate();

            if (Environment.GetEnvironmentVariable(WriteEnv) == "1")
            {
                File.WriteAllText(path, generated, new UTF8Encoding(false));
            }

            string current = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : "";
            Assert.True(current == generated,
                $"本体依存目録 {LedgerRelativePath} がビルド済み DLL と食い違っている。"
                + $"更新するには `$env:{WriteEnv}=1; dotnet test Werewolf.Tests/Werewolf.Tests.csproj "
                + "--filter FullyQualifiedName~VanillaDependencyLedgerTests` を実行して差分をコミットする。\n"
                + FirstDifference(current, generated));
        }

        private static string FirstDifference(string current, string generated)
        {
            var a = current.Split('\n');
            var b = generated.Split('\n');
            for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                string x = i < a.Length ? a[i] : "<EOF>";
                string y = i < b.Length ? b[i] : "<EOF>";
                if (x != y) return $"最初の差分 {i + 1} 行目:\n  目録:  {x}\n  実態:  {y}";
            }
            return "";
        }

        private static string Generate()
        {
            using var module = ModuleDefinition.ReadModule(AuditEnvironment.Meta("Werewolf.ModDllPath"),
                new ReaderParameters { AssemblyResolver = AuditEnvironment.CreateResolver() });

            var byType = new SortedDictionary<string, SortedDictionary<string, SortedSet<string>>>(StringComparer.Ordinal);
            var gameRefs = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            const string gameRefsType = "Werewolf.GameRefs";
            foreach (var entry in VanillaBoundaryAuditTests.VanillaRefsByRootType(module))
            {
                foreach (string reference in entry.Value)
                {
                    if (reference.StartsWith(gameRefsType + "::", StringComparison.Ordinal))
                    {
                        if (entry.Key != gameRefsType)
                            Add(gameRefs, reference.Substring(gameRefsType.Length + 2), entry.Key);
                        continue;
                    }
                    if (entry.Key == gameRefsType) continue;

                    int sep = reference.IndexOf("::", StringComparison.Ordinal);
                    string type = sep < 0 ? reference : reference.Substring(0, sep);
                    string member = sep < 0 ? TypeOnly : reference.Substring(sep + 2);
                    if (type.EndsWith("[]") || type.EndsWith("&") || type.StartsWith("!")) continue;
                    if (!byType.TryGetValue(type, out var members))
                        byType[type] = members = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
                    Add(members, member, entry.Key);
                }
            }

            foreach (var members in byType.Values)
            {
                if (!members.TryGetValue(TypeOnly, out var typeOnlyOwners)) continue;
                var memberOwners = new HashSet<string>(members.Where(m => m.Key != TypeOnly).SelectMany(m => m.Value));
                typeOnlyOwners.RemoveWhere(memberOwners.Contains);
                if (typeOnlyOwners.Count == 0) members.Remove(TypeOnly);
            }

            var patches = new SortedDictionary<string, SortedDictionary<string, SortedSet<string>>>(StringComparer.Ordinal);
            foreach (var target in HarmonyPatchAudit.Enumerate(module))
            {
                if (target.DeclaringType == null || target.DeclaringType.Scope?.Name != "Assembly-CSharp") continue;
                string method = target.MethodName ?? "?";
                if (target.MethodType != null && target.MethodType != "Normal") method += $" [{target.MethodType}]";
                if (!patches.TryGetValue(target.DeclaringType.FullName, out var methods))
                    patches[target.DeclaringType.FullName] = methods = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
                Add(methods, method, target.PatchClass);
            }

            var allTypes = new SortedSet<string>(byType.Keys.Concat(patches.Keys), StringComparer.Ordinal);
            int memberCount = byType.Values.Sum(m => m.Keys.Count(k => k != TypeOnly));
            int patchCount = patches.Values.Sum(m => m.Count);

            var sb = new StringBuilder();
            sb.Append("# 本体依存目録（自動生成）\n\n");
            sb.Append("`VanillaDependencyLedgerTests` がビルド済み DLL の IL から生成する。手で編集しない（更新は `$env:WEREWOLF_WRITE_LEDGER=1` でテストを実行）。\n");
            sb.Append("本体アップデート後に `git diff DnSpy/clean` で変わったクラスをここで引き、右側の接着モジュール・パッチクラスを読む（プレイブック系統B）。\n\n");
            sb.Append($"本体の型 {allTypes.Count} 種 ／ 直接参照するメンバー {memberCount} 件 ／ パッチ対象 {patchCount} 件 ／ 文字列参照（GameRefs）{gameRefs.Count} 件\n\n");

            foreach (string type in allTypes)
            {
                sb.Append($"## {type}\n\n");
                if (byType.TryGetValue(type, out var members))
                {
                    foreach (var m in members)
                        sb.Append($"- `{m.Key}` ← {string.Join(", ", m.Value.Select(Short))}\n");
                }
                if (patches.TryGetValue(type, out var methods))
                {
                    foreach (var m in methods)
                        sb.Append($"- パッチ `{m.Key}` ← {string.Join(", ", m.Value.Select(Short))}\n");
                }
                sb.Append('\n');
            }

            sb.Append("## 文字列参照（GameRefs のハンドル）\n\n");
            foreach (var h in gameRefs)
                sb.Append($"- `{h.Key}` ← {string.Join(", ", h.Value.Select(Short))}\n");

            return sb.ToString();
        }

        private static void Add(SortedDictionary<string, SortedSet<string>> map, string key, string value)
        {
            if (!map.TryGetValue(key, out var set))
                map[key] = set = new SortedSet<string>(StringComparer.Ordinal);
            set.Add(value);
        }

        private static string Short(string fullName)
            => fullName.StartsWith("Werewolf.", StringComparison.Ordinal) ? fullName.Substring("Werewolf.".Length) : fullName;
    }
}
