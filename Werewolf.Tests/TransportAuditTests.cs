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

        [Fact]
        public void RpcEndpoint_LogsUnhandledReceiveException()
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

            ExceptionHandler handler = rpc.Body.ExceptionHandlers
                .SingleOrDefault(h => h.HandlerType == ExceptionHandlerType.Catch &&
                    h.CatchType?.FullName == "System.Exception");
            Assert.True(handler != null,
                "RpcEndpoint.WerewolfRpc に System.Exception の最終防衛catchがない。");

            int start = rpc.Body.Instructions.IndexOf(handler.HandlerStart);
            int end = handler.HandlerEnd == null
                ? rpc.Body.Instructions.Count
                : rpc.Body.Instructions.IndexOf(handler.HandlerEnd);
            Assert.True(start >= 0 && end > start, "RpcEndpointのcatch範囲を解決できない。");

            var catchInstructions = rpc.Body.Instructions.Skip(start).Take(end - start).ToList();
            Assert.Contains(catchInstructions, ins =>
                ins.OpCode == OpCodes.Ldstr && (string)ins.Operand == "rpc_endpoint_error");
            Assert.Contains(catchInstructions, ins =>
                Calls(ins, "Werewolf.Core.WLog", "Exception"));
        }

        [Fact]
        public void PhotonRpcBus_RegisterAndUnregisterKeepSubscriptionsSymmetric()
        {
            using var module = ModuleDefinition.ReadModule(Meta("Werewolf.ModDllPath"));
            TypeDefinition bus = module.GetTypes()
                .Single(t => t.FullName == "Werewolf.Net.PhotonRpcBus");

            MethodDefinition register = bus.Methods.Single(m => m.Name == "Register");
            Assert.Contains(register.Body.Instructions, ins =>
                Calls(ins, "Werewolf.Net.PhotonRoomCallbackAdapter", "Register"));
            Assert.Contains(register.Body.Instructions, ins =>
                Calls(ins, "Werewolf.Net.RpcEndpoint", "add_OnMessage"));
            Assert.Contains(register.Body.Instructions, ins =>
                Calls(ins, "Werewolf.Net.PhotonRpcBus", "EnsureEndpoint"));

            MethodDefinition unregister = bus.Methods.Single(m => m.Name == "Unregister");
            Assert.Contains(unregister.Body.Instructions, ins =>
                Calls(ins, "Werewolf.Net.RpcEndpoint", "remove_OnMessage"));
            Assert.Contains(unregister.Body.Instructions, ins =>
                Calls(ins, "Werewolf.Net.PhotonRoomCallbackAdapter", "Unregister"));
            Assert.Contains(unregister.Body.Instructions, ins =>
                Calls(ins, "Werewolf.Net.PhotonRpcBus", "DestroyEndpoint"));
        }

        [Theory]
        [InlineData("SendToAll")]
        [InlineData("SendToActors")]
        [InlineData("SendToMaster")]
        public void PhotonRpcBus_SendPathsUseSharedPreparationAndPhotonRpc(string methodName)
        {
            using var module = ModuleDefinition.ReadModule(Meta("Werewolf.ModDllPath"));
            TypeDefinition bus = module.GetTypes()
                .Single(t => t.FullName == "Werewolf.Net.PhotonRpcBus");
            MethodDefinition send = bus.Methods.Single(m => m.Name == methodName);

            Assert.Contains(send.Body.Instructions, ins =>
                Calls(ins, "Werewolf.Net.PhotonRpcBus", "TryPrepare"));
            Assert.Contains(send.Body.Instructions, ins =>
                Calls(ins, "Photon.Pun.PhotonView", "RPC"));
        }

        [Theory]
        [InlineData("OnPlayerLeftRoom")]
        [InlineData("OnRoomPropertiesUpdate")]
        [InlineData("OnDisconnected")]
        public void PhotonRoomCallbacks_LogSubscriberFailures(string methodName)
        {
            using var module = ModuleDefinition.ReadModule(Meta("Werewolf.ModDllPath"));
            TypeDefinition adapter = module.GetTypes()
                .Single(t => t.FullName == "Werewolf.Net.PhotonRoomCallbackAdapter");
            MethodDefinition callback = adapter.Methods.Single(m => m.Name == methodName);
            ExceptionHandler handler = callback.Body.ExceptionHandlers.Single(h =>
                h.HandlerType == ExceptionHandlerType.Catch &&
                h.CatchType?.FullName == "System.Exception");

            int start = callback.Body.Instructions.IndexOf(handler.HandlerStart);
            int end = handler.HandlerEnd == null
                ? callback.Body.Instructions.Count
                : callback.Body.Instructions.IndexOf(handler.HandlerEnd);
            Assert.Contains(callback.Body.Instructions.Skip(start).Take(end - start), ins =>
                Calls(ins, "Werewolf.Core.WLog", "Exception"));
        }

        private static bool Calls(Instruction instruction, string declaringType, string methodName)
        {
            return (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference method &&
                method.DeclaringType.FullName == declaringType &&
                method.Name == methodName;
        }
    }
}
