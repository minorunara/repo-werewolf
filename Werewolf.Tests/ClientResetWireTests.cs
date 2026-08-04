using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class ClientResetWireTests
    {
        private static string Meta(string key)
        {
            return typeof(ClientResetWireTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        [Fact]
        public void TeardownBus_CallsClientResetPolicy_ApplyRoomLeft()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);

            var director = module.GetType("Werewolf.Game.WerewolfDirector");
            Assert.NotNull(director);

            var teardown = director.Methods.FirstOrDefault(m =>
                m.Name == "TeardownBus" && m.Parameters.Count == 0);
            Assert.NotNull(teardown);
            Assert.True(teardown.HasBody, "TeardownBus に IL Body が無い");

            bool foundCall = teardown.Body.Instructions.Any(ins =>
                (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) &&
                ins.Operand is MethodReference mr &&
                mr.DeclaringType.FullName == "Werewolf.Core.ClientResetPolicy" &&
                mr.Name == "ApplyRoomLeft");

            Assert.True(foundCall,
                "WerewolfDirector.TeardownBus 内で ClientResetPolicy.ApplyRoomLeft が呼ばれていない。" +
                "Photon Room 離脱時のクライアント状態掃除が欠落＝ADR-0044 の再発。" +
                "修復手順: WerewolfDirector.Reset.cs の TeardownBus() 冒頭で " +
                "ClientResetPolicy.ApplyRoomLeft(_meetingClient) を呼ぶこと。");
        }
    }
}
