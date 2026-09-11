using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class RoomLeaveWireTests
    {
        private const string DirectorTypeName = "Werewolf.Game.WerewolfDirector";

        private static string Meta(string key)
        {
            return typeof(RoomLeaveWireTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        [Theory]
        [InlineData("Update", "TickRoomLeaveOnTitle")]
        [InlineData("TickRoomLeaveOnTitle", "CleanupAfterRoomLeave")]
        [InlineData("HandleLocalDisconnected", "CleanupAfterRoomLeave")]
        public void DirectorMethod_CallsRoomLeaveWiring(string callerName, string calleeName)
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);
            var director = module.GetType(DirectorTypeName);
            Assert.NotNull(director);

            var caller = director.Methods.FirstOrDefault(m => m.Name == callerName);
            Assert.NotNull(caller);
            Assert.True(caller.HasBody, $"{callerName} に IL Body が無い");

            bool foundCall = caller.Body.Instructions.Any(ins =>
                (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) &&
                ins.Operand is MethodReference mr &&
                mr.DeclaringType.FullName == DirectorTypeName &&
                mr.Name == calleeName);

            Assert.True(foundCall,
                $"WerewolfDirector.{callerName} 内で {calleeName} が呼ばれていない。" +
                "ルーム離脱の掃除がその経路で欠落し、前のルームの状態が次のルームへ持ち越される。");
        }
    }
}
