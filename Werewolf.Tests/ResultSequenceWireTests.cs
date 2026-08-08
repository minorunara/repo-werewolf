using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class ResultSequenceWireTests
    {
        private static string Meta(string key)
        {
            return typeof(ResultSequenceWireTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        [Theory]
        [InlineData("ResetToLobby")]
        [InlineData("HandleLocalDisconnected")]
        [InlineData("StartHosted")]
        public void SessionBoundary_CallsResultSequenceCancel(string methodName)
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);

            var director = module.GetType("Werewolf.Game.WerewolfDirector");
            Assert.NotNull(director);

            var method = director.Methods.FirstOrDefault(m => m.Name == methodName);
            Assert.NotNull(method);
            Assert.True(method.HasBody, $"{methodName} に IL Body が無い");

            bool foundCall = method.Body.Instructions.Any(ins =>
                (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) &&
                ins.Operand is MethodReference mr &&
                mr.DeclaringType.FullName == "Werewolf.Core.ResultSequence" &&
                mr.Name == "Cancel");

            Assert.True(foundCall,
                $"WerewolfDirector.{methodName} 内で ResultSequence.Cancel が呼ばれていない。" +
                "前試合の帰還タイマーが次試合へ持ち越され「開始直後に勝手にロビーへ戻る」バグが再発する。" +
                "修復手順: セッション境界の掃除（ResetToLobby / HandleLocalDisconnected / StartHosted）で " +
                "_resultSequence.Cancel() を呼ぶこと。");
        }
    }
}
