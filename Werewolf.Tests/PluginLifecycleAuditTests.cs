using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class PluginLifecycleAuditTests
    {
        private static readonly HashSet<string> ForbiddenPluginCallbacks = new HashSet<string>
        {
            "Start",
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnEnable",
            "OnDisable",
            "OnDestroy",
            "OnGUI",
            "OnApplicationQuit",
        };

        [Fact]
        public void BepInExPlugin_UsesOnlyAwakeForUnityLifecycle()
        {
            using var module = OpenModModule();
            TypeDefinition plugin = Type(module, "Werewolf.Plugin");

            var callbacks = plugin.Methods
                .Where(m => ForbiddenPluginCallbacks.Contains(m.Name))
                .Select(m => m.FullName)
                .ToList();

            Assert.True(callbacks.Count == 0,
                "BepInEx Plugin に Awake 以外の Unity ライフサイクル処理がある。" +
                "REPO 配布環境では起動直後に破棄され得るため、Harmony パッチまたは" +
                "ゲーム所有オブジェクト上の WerewolfDirector へ移すこと:\n" +
                string.Join("\n", callbacks));
        }

        [Fact]
        public void DirectorApplicationQuit_FlushesDeferredSecrets()
        {
            using var module = OpenModModule();
            MethodDefinition onQuit = Type(module, "Werewolf.Game.WerewolfDirector")
                .Methods.SingleOrDefault(m => m.Name == "OnApplicationQuit" && m.HasBody);

            Assert.True(onQuit != null, "WerewolfDirector.OnApplicationQuit が見つからない。");
            bool callsFlush = onQuit.Body.Instructions.Any(ins =>
                (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt)
                && ins.Operand is MethodReference mr
                && mr.DeclaringType.FullName == "Werewolf.Debugging.StructuredLog"
                && mr.Name == "FlushDeferredSecrets");

            Assert.True(callsFlush,
                "WerewolfDirector.OnApplicationQuit から保留 secret ログの回収が外れている。");
        }

        private static ModuleDefinition OpenModModule()
        {
            string dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            return ModuleDefinition.ReadModule(dllPath);
        }

        private static TypeDefinition Type(ModuleDefinition module, string fullName)
        {
            TypeDefinition type = CecilTypes.AllTypes(module)
                .SingleOrDefault(t => t.FullName == fullName);
            Assert.True(type != null, $"監査対象の型が無い: {fullName}");
            return type;
        }
    }
}
