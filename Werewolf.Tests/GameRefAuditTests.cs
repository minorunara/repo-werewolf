using System;
using System.IO;
using System.Linq;
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
        [Fact]
        public void ModDll_AllGameMemberRefsResolve()
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
        public void ModDll_PublicizesOnlyTheBuildReferenceWithoutRuntimeToolDependency()
        {
            using var module = ModuleDefinition.ReadModule(AuditEnvironment.Meta("Werewolf.ModDllPath"));

            Assert.DoesNotContain(module.AssemblyReferences, reference =>
                reference.Name.Contains("AssemblyPublicizer", StringComparison.Ordinal)
                || reference.Name.StartsWith("AsmResolver", StringComparison.Ordinal));

            var ignoredAssemblies = module.Assembly.CustomAttributes
                .Where(attribute => attribute.AttributeType.FullName
                    == "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute")
                .Select(attribute => (string)attribute.ConstructorArguments[0].Value)
                .ToList();
            Assert.Equal(new[] { "Assembly-CSharp" }, ignoredAssemblies);
        }

        [Fact]
        public void PluginAwake_InjectsDependenciesBeforePatchingAndExecution()
        {
            var dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            using var module = ModuleDefinition.ReadModule(dllPath);

            var plugin = module.GetType("Werewolf.Plugin");
            Assert.NotNull(plugin);
            var awake = plugin.Methods.Single(method => method.Name == "Awake");
            var instructions = awake.Body.Instructions;

            int enemyMapInjection = InstructionIndex(instructions, instruction =>
                instruction.OpCode.Code == Code.Stsfld
                && instruction.Operand is FieldReference field
                && field.DeclaringType.FullName == "Werewolf.Vanilla.EnemyMapIcons"
                && field.Name == "TextureResolver");
            int meetingButtonInjection = InstructionIndex(instructions, instruction =>
                instruction.OpCode.Code == Code.Stsfld
                && instruction.Operand is FieldReference field
                && field.DeclaringType.FullName == "Werewolf.Vanilla.MeetingButton"
                && field.Name == "PrefabResolver");
            int patching = InstructionIndex(instructions, instruction =>
                instruction.Operand is MethodReference method
                && method.DeclaringType.FullName == "Werewolf.Plugin"
                && method.Name == "ApplyPatchesIsolated");
            int executionStart = InstructionIndex(instructions, instruction =>
                instruction.Operand is GenericInstanceMethod method
                && method.Name == "AddComponent"
                && method.GenericArguments.Any(argument =>
                    argument.FullName == "Werewolf.Game.WerewolfDirector"));

            Assert.True(enemyMapInjection >= 0 && meetingButtonInjection >= 0,
                "接着層の依存注入が見つからない");
            Assert.True(patching > enemyMapInjection && patching > meetingButtonInjection,
                "Harmonyパッチ適用が依存注入より前にある");
            Assert.True(executionStart > patching,
                "WerewolfDirector の生成が依存注入・パッチ適用より前にある");
        }

        [Fact]
        public void EnemyMapGate_CommitsStateAfterAttachOrClearSucceeds()
        {
            var dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            using var module = ModuleDefinition.ReadModule(dllPath);

            var enemyMap = module.GetType("Werewolf.Vanilla.EnemyMapIcons");
            Assert.NotNull(enemyMap);
            var tick = enemyMap.Methods.Single(method => method.Name == "Tick");
            var instructions = tick.Body.Instructions;

            int attach = InstructionIndex(instructions, instruction =>
                instruction.Operand is MethodReference method
                && method.DeclaringType.FullName == enemyMap.FullName
                && method.Name == "AttachAll");
            int clear = InstructionIndex(instructions, instruction =>
                instruction.Operand is MethodReference method
                && method.DeclaringType.FullName == enemyMap.FullName
                && method.Name == "ClearAll");
            var stateStores = instructions
                .Select((instruction, index) => (instruction, index))
                .Where(pair => pair.instruction.OpCode.Code == Code.Stsfld
                    && pair.instruction.Operand is FieldReference field
                    && field.DeclaringType.FullName == enemyMap.FullName
                    && field.Name == "_active")
                .Select(pair => pair.index)
                .ToList();

            Assert.True(attach >= 0 && clear >= 0 && stateStores.Count == 2,
                "EnemyMapIcons.Tick の付与・掃除・状態確定が一意に見つからない");
            Assert.True(attach < stateStores[0] && clear < stateStores[1],
                "EnemyMapIcons が付与または掃除の成功前にゲート状態を確定している");
        }

        private static int InstructionIndex(Mono.Collections.Generic.Collection<Instruction> instructions,
                                            Func<Instruction, bool> predicate)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                if (predicate(instructions[i])) return i;
            }
            return -1;
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
