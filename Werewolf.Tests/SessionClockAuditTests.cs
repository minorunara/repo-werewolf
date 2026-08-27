using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class SessionClockAuditTests
    {
        private static readonly HashSet<string> AllowedWallClockReaders = new HashSet<string>
        {
            "Werewolf.Net.RpcEndpoint",
            "Werewolf.Net.PhotonRpcBus",
            "Werewolf.Debugging.StructuredLog",
            "Werewolf.Debugging.SecretLogBuffer",
            "Werewolf.Vanilla.ReplaySampler",
        };

        private static readonly HashSet<string> WallClockGetters = new HashSet<string>
        {
            "System.DateTimeOffset::get_UtcNow",
            "System.DateTimeOffset::get_Now",
            "System.DateTime::get_UtcNow",
            "System.DateTime::get_Now",
        };

        private static readonly HashSet<string> AllowedPhotonClockReaders = new HashSet<string>
        {
            "Werewolf.Game.SessionClock",
            "Werewolf.Net.RpcEndpoint",
        };

        private static readonly HashSet<string> PhotonClockMembers = new HashSet<string>
        {
            "Photon.Pun.PhotonNetwork::get_ServerTimestamp",
            "Photon.Pun.PhotonNetwork::get_Time",
            "Photon.Pun.PhotonNetwork::FetchServerTimestamp",
            "Photon.Pun.PhotonMessageInfo::get_SentServerTimestamp",
        };

        [Fact]
        public void WallClock_IsReadOnlyInAllowedTypes()
        {
            using var module = OpenModModule();
            var offenders = FindCallers(module, WallClockGetters, AllowedWallClockReaders, out _);

            Assert.True(offenders.Count == 0,
                "壁時計を直接読んでいる箇所がある。試合進行の時刻は SessionClock.NowMs() を使うこと。" +
                "ローカル完結の用途なら本テストの AllowedWallClockReaders へ理由つきで追加する:\n" +
                string.Join("\n", offenders));
        }

        [Fact]
        public void PhotonClock_IsReadOnlyBySessionClockAndRpcEndpoint()
        {
            using var module = OpenModModule();
            var offenders = FindCallers(module, PhotonClockMembers, AllowedPhotonClockReaders,
                out HashSet<string> allowedHits);

            Assert.True(offenders.Count == 0,
                "Photon の時計を SessionClock 以外が直接読んでいる。試合進行の時刻は SessionClock.NowMs() を使うこと:\n" +
                string.Join("\n", offenders));
            Assert.Contains("Werewolf.Game.SessionClock", allowedHits);
            Assert.Contains("Werewolf.Net.RpcEndpoint", allowedHits);
        }

        private static ModuleDefinition OpenModModule()
        {
            var dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            return ModuleDefinition.ReadModule(dllPath);
        }

        private static List<string> FindCallers(ModuleDefinition module, HashSet<string> members,
            HashSet<string> allowedOwners, out HashSet<string> allowedHits)
        {
            var offenders = new List<string>();
            allowedHits = new HashSet<string>();
            foreach (var type in CecilTypes.AllTypes(module))
            {
                string owner = type.FullName.Split('/')[0];
                foreach (var method in type.Methods.Where(m => m.HasBody))
                {
                    foreach (var ins in method.Body.Instructions)
                    {
                        if (ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) continue;
                        if (!(ins.Operand is MethodReference mr)) continue;
                        string key = mr.DeclaringType.FullName + "::" + mr.Name;
                        if (!members.Contains(key)) continue;
                        if (allowedOwners.Contains(owner)) allowedHits.Add(owner);
                        else offenders.Add($"{type.FullName}::{method.Name} -> {key}");
                    }
                }
            }
            return offenders;
        }
    }
}
