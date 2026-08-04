using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Werewolf.Tests
{
    public class GameRefAuditTests
    {
        private static readonly string[] AuditScopePrefixes =
            { "Assembly-CSharp", "Photon", "Unity", "BepInEx", "0Harmony" };

        private static bool InAuditScope(string scopeName)
        {
            return AuditScopePrefixes.Any(p => scopeName.StartsWith(p, StringComparison.Ordinal));
        }

        private static string Meta(string key)
        {
            return typeof(GameRefAuditTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static DefaultAssemblyResolver CreateResolver()
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Meta("Werewolf.GameManagedDir"));
            resolver.AddSearchDirectory(Meta("Werewolf.BepInExCoreDir"));
            resolver.AddSearchDirectory(Path.GetDirectoryName(Meta("Werewolf.ModDllPath")));
            return resolver;
        }

        [Fact]
        public void ModDll_AllGameRefsResolve()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath,
                new ReaderParameters { AssemblyResolver = CreateResolver() });
            var failures = GameRefAudit.FindUnresolvedRefs(module, InAuditScope);

            Assert.True(failures.Count == 0,
                $"未解決のゲーム参照 {failures.Count} 件（本体アップデートで消失・変更された可能性。" +
                "修復手順: docs/steering/tech.md「本体アップデート対応プレイブック」）:\n  " +
                string.Join("\n  ", failures));
        }

        [Fact]
        public void Audit_DetectsBrokenMemberAndTypeRefs()
        {
            var resolver = CreateResolver();
            var gamePath = Path.Combine(Meta("Werewolf.GameManagedDir"), "Assembly-CSharp.dll");
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
                var failures = GameRefAudit.FindUnresolvedRefs(reread, InAuditScope);

                Assert.Equal(2, failures.Count);
                Assert.Contains(failures, f => f.Contains("NoSuchMethod_AuditProbe"));
                Assert.Contains(failures, f => f.Contains("NoSuchType_AuditProbe"));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
