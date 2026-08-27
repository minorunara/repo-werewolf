using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Werewolf.Core;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Werewolf.Tests
{
    public class GameRefAuditTests
    {
        [Fact]
        public void ModDll_AllGameRefsResolve()
        {
            var dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath,
                new ReaderParameters { AssemblyResolver = AuditEnvironment.CreateResolver() });
            var failures = GameRefAudit.FindUnresolvedRefs(module, AuditEnvironment.InAuditScope);

            Assert.True(failures.Count == 0,
                $"未解決のゲーム参照 {failures.Count} 件（本体アップデートで消失・変更された可能性。" +
                "修復手順: docs/steering/tech.md「本体アップデート対応プレイブック」）:\n  " +
                string.Join("\n  ", failures));
        }

        [Fact]
        public void GameRefs_AreTieredByTheirUsersAndNoneAreDead()
        {
            var dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            using var module = ModuleDefinition.ReadModule(dllPath);

            var refs = module.GetType("Werewolf.GameRefs");
            Assert.True(refs != null, "Werewolf.GameRefs が見つからない");

            var declared = refs.Fields.Where(f => f.IsPublic && f.IsStatic).Select(f => f.Name).ToList();
            Assert.NotEmpty(declared);

            var essentialTier = AssignedFields(refs, "ResolveAll");
            var scenarioTier = AssignedFields(refs, "ResolveScenarioRefs");
            Assert.NotEmpty(essentialTier);

            var unresolved = declared.Where(n => !essentialTier.Contains(n) && !scenarioTier.Contains(n)).ToList();
            Assert.True(unresolved.Count == 0,
                "宣言されているが ResolveAll / ResolveScenarioRefs のどちらでも解決されないハンドル:\n  "
                + string.Join("\n  ", unresolved));
            var doubled = declared.Where(n => essentialTier.Contains(n) && scenarioTier.Contains(n)).ToList();
            Assert.True(doubled.Count == 0,
                "両方の層で解決しているハンドル（層の帰結が曖昧になる）:\n  " + string.Join("\n  ", doubled));

            var users = FieldUsersByTier(module, refs);

            var dead = declared.Where(n => !users.ContainsKey(n)).ToList();
            Assert.True(dead.Count == 0,
                "どこからも使われていないハンドル（削るか、使う側と一緒に足す）:\n  " + string.Join("\n  ", dead));

            var leaked = scenarioTier
                .Where(n => users.TryGetValue(n, out var u) && u.essential.Count > 0)
                .Select(n => $"{n} <- {string.Join(", ", users[n].essential)}")
                .ToList();
            Assert.True(leaked.Count == 0,
                "チュートリアル層のハンドルを本編層が使っている（本編層へ移すか、利用側をシナリオへ寄せる）:\n  "
                + string.Join("\n  ", leaked));

            var shouldBeScenario = essentialTier
                .Where(n => users.TryGetValue(n, out var u) && u.essential.Count == 0 && u.scenario.Count > 0)
                .Select(n => $"{n} <- {string.Join(", ", users[n].scenario)}")
                .ToList();
            Assert.True(shouldBeScenario.Count == 0,
                "チュートリアルからしか使われないのに本編層で解決しているハンドル"
                + "（ResolveScenarioRefs へ移す。本体更新で欠けたときに本編まで止まる）:\n  "
                + string.Join("\n  ", shouldBeScenario));
        }

        private static SortedSet<string> AssignedFields(TypeDefinition owner, string methodName)
        {
            var method = owner.Methods.SingleOrDefault(m => m.Name == methodName);
            Assert.True(method != null, $"{owner.FullName}.{methodName} が見つからない");

            var names = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var instr in method.Body.Instructions)
            {
                if (instr.OpCode.Code == Code.Stsfld
                    && instr.Operand is FieldReference field
                    && field.DeclaringType.FullName == owner.FullName)
                {
                    names.Add(field.Name);
                }
            }
            return names;
        }

        private static Dictionary<string, (SortedSet<string> essential, SortedSet<string> scenario)>
            FieldUsersByTier(ModuleDefinition module, TypeDefinition owner)
        {
            var users = new Dictionary<string, (SortedSet<string>, SortedSet<string>)>(StringComparer.Ordinal);
            foreach (var type in CecilTypes.AllTypes(module))
            {
                TypeDefinition root = type;
                while (root.DeclaringType != null) root = root.DeclaringType;
                if (root.FullName == owner.FullName) continue;

                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    foreach (var instr in method.Body.Instructions)
                    {
                        if (!(instr.Operand is FieldReference field)
                            || field.DeclaringType.FullName != owner.FullName)
                        {
                            continue;
                        }
                        if (!users.TryGetValue(field.Name, out var sets))
                        {
                            users[field.Name] = sets = (new SortedSet<string>(StringComparer.Ordinal),
                                new SortedSet<string>(StringComparer.Ordinal));
                        }
                        if (PatchTierPolicy.IsScenarioTier(root.FullName)) sets.Item2.Add(root.FullName);
                        else sets.Item1.Add(root.FullName);
                    }
                }
            }
            return users;
        }

        [Fact]
        public void Audit_DetectsBrokenMemberAndTypeRefs()
        {
            var resolver = AuditEnvironment.CreateResolver();
            var gamePath = Path.Combine(AuditEnvironment.Meta("Werewolf.GameManagedDir"), "Assembly-CSharp.dll");
            using var game = AssemblyDefinition.ReadAssembly(gamePath,
                new ReaderParameters { AssemblyResolver = resolver });
            var playerAvatar = game.MainModule.GetType("PlayerAvatar");
            Assert.NotNull(playerAvatar);

            var asm = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("AuditProbe", new Version(1, 0)), "AuditProbe", ModuleKind.Dll);
            var module = asm.MainModule;

            var avatarRef = module.ImportReference(playerAvatar);
            var bogusMethod = new MethodReference(
                "NoSuchMethod_AuditProbe", module.TypeSystem.Void, avatarRef) { HasThis = true };
            var bogusType = new TypeReference(
                "", "NoSuchType_AuditProbe", module, avatarRef.Scope);

            var holder = new TypeDefinition("Probe", "Holder",
                TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
            module.Types.Add(holder);
            holder.Fields.Add(new FieldDefinition("avatar", FieldAttributes.Public, avatarRef));
            holder.Fields.Add(new FieldDefinition("bogus", FieldAttributes.Public, bogusType));

            var poke = new MethodDefinition("Poke",
                MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            holder.Methods.Add(poke);
            var il = poke.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Callvirt, bogusMethod);
            il.Emit(OpCodes.Ret);

            var path = Path.Combine(Path.GetTempPath(), $"AuditProbe_{Guid.NewGuid():N}.dll");
            asm.Write(path);
            try
            {
                using var reread = ModuleDefinition.ReadModule(path,
                    new ReaderParameters { AssemblyResolver = resolver });
                var failures = GameRefAudit.FindUnresolvedRefs(reread, AuditEnvironment.InAuditScope);

                Assert.Equal(2, failures.Count);
                Assert.Contains(failures, f => f.Contains("NoSuchMethod_AuditProbe"));
                Assert.Contains(failures, f => f.Contains("NoSuchType_AuditProbe"));
                Assert.All(failures, f => Assert.Contains("<- Probe.Holder", f));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
