using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class TransportAuditTests
    {
        private static string Meta(string key)
        {
            return typeof(TransportAuditTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        [Fact]
        public void ModDll_DoesNotCall_PhotonNetworkRaiseEvent()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);

            var offenders = new List<string>();
            foreach (TypeDefinition type in module.GetTypes())
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    foreach (Instruction ins in method.Body.Instructions)
                    {
                        if ((ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) &&
                            ins.Operand is MethodReference mr &&
                            mr.DeclaringType.FullName == "Photon.Pun.PhotonNetwork" &&
                            mr.Name == "RaiseEvent")
                        {
                            offenders.Add($"{type.FullName}.{method.Name}");
                        }
                    }
                }
            }

            Assert.True(offenders.Count == 0,
                "PhotonNetwork.RaiseEvent の呼び出しが検出された: " + string.Join(", ", offenders) +
                "。独自通信は固定ViewID RPC transport（PhotonRpcBus / RpcEndpoint）のみを使う" +
                "（ISSUE-13 / ADR-0083。custom event code帯は使用しない）。");
        }

        [Fact]
        public void ModDll_DoesNotImplement_IOnEventCallback()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);

            var offenders = module.GetTypes()
                .Where(t => t.HasInterfaces && t.Interfaces.Any(i =>
                    i.InterfaceType.FullName == "Photon.Realtime.IOnEventCallback"))
                .Select(t => t.FullName)
                .ToList();

            Assert.True(offenders.Count == 0,
                "IOnEventCallback 実装が検出された: " + string.Join(", ", offenders) +
                "。custom event code受信は旧transportの経路であり、受信は RpcEndpoint（[PunRPC]）へ" +
                "一本化されている（ISSUE-13 / ADR-0083）。");
        }

        [Fact]
        public void RpcEndpoint_VerifiesSenderIsMasterClient()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);

            TypeDefinition endpoint = module.GetTypes()
                .SingleOrDefault(t => t.FullName == "Werewolf.Net.RpcEndpoint");
            Assert.True(endpoint != null, "Werewolf.Net.RpcEndpoint が見つからない。");

            MethodDefinition rpc = endpoint.Methods
                .SingleOrDefault(m => m.Name == "WerewolfRpc");
            Assert.True(rpc != null && rpc.HasBody,
                "RpcEndpoint.WerewolfRpc（受信RPC本体）が見つからない。");

            bool callsAuthority = rpc.Body.Instructions.Any(ins =>
                (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) &&
                ins.Operand is MethodReference mr &&
                mr.DeclaringType.FullName == "Werewolf.Net.InboundAuthority" &&
                mr.Name == "IsAcceptable");

            Assert.True(callsAuthority,
                "RpcEndpoint.WerewolfRpc から InboundAuthority.IsAcceptable の呼び出しが消えている。" +
                "ホスト→全員/対象のコードは transport の入口で送信元がマスタークライアントか" +
                "検証する（ISSUE-14）。コード別のホワイトリストで代替してはいけない" +
                "（新コード追加時に守り忘れる形にしないため）。");
        }
    }
}
