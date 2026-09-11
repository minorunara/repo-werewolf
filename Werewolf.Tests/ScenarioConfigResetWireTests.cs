using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioConfigResetWireTests
    {
        [Fact]
        public void ResetToLobby_RestoresLiveConfigAfterWorldgenCleanup()
        {
            string dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);
            TypeDefinition director = module.GetType("Werewolf.Game.WerewolfDirector");
            Assert.NotNull(director);

            MethodDefinition reset = director.Methods.Single(m => m.Name == "ResetToLobby");
            int worldgenCleanup = CallIndex(reset, "Werewolf.Vanilla.WorldgenApplier", "RestoreOnLobbyReturn");
            int configRefresh = CallIndex(reset, "Werewolf.Plugin", "RefreshGameConfig");

            Assert.True(worldgenCleanup >= 0,
                "ResetToLobby から WorldgenApplier.RestoreOnLobbyReturn が呼ばれていない。");
            Assert.True(configRefresh > worldgenCleanup,
                "Plugin.RefreshGameConfig はシナリオ値を必要とする worldgen 後始末の後に呼ぶこと。" +
                "順序が逆、または呼び出しが無いと ScenarioGameConfig が通常ロビーへ残留する。");
        }

        private static int CallIndex(MethodDefinition method, string declaringType, string methodName)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                Instruction instruction = method.Body.Instructions[i];
                if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                    instruction.Operand is MethodReference called &&
                    called.DeclaringType.FullName == declaringType &&
                    called.Name == methodName)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
