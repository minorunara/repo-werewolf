using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class RoundHotPathAuditTests
    {
        private const string SamplerType = "Werewolf.Vanilla.ReplaySampler";
        private const string DirectorType = "Werewolf.Game.WerewolfDirector";
        private const string ResolverType = "Werewolf.Vanilla.MeetingButtonAssetResolver";

        [Fact]
        public void ReplaySample_DoesNotScanAllObjectsPerSample()
        {
            using var module = OpenModModule();
            MethodDefinition collect = Method(module, SamplerType, "CollectAndSample");

            var scans = Calls(collect)
                .Where(k => k.StartsWith("UnityEngine.Object::FindObjects")
                            || k.StartsWith("UnityEngine.Resources::FindObjects"))
                .Distinct()
                .ToList();

            Assert.True(scans.Count == 0,
                $"{SamplerType}.CollectAndSample が実体の全走査を呼んでいる（サンプル周期ごとに"
                + "レベル内の全オブジェクトを舐めることになり、ラウンド中だけフレーム時間が伸びる）。"
                + "走査は RefreshOneRoster の巡回へ足し、収集側はキャッシュ配列を読むこと:\n  "
                + string.Join("\n  ", scans));
        }

        [Fact]
        public void ReplayRosterRefresh_UsesUnsortedScan()
        {
            using var module = OpenModModule();
            var calls = Calls(Method(module, SamplerType, "FindAll")).ToList();

            Assert.Contains(calls, k => k == "UnityEngine.Object::FindObjectsByType");
            Assert.DoesNotContain(calls, k => k == "UnityEngine.Object::FindObjectsOfType");
        }

        [Fact]
        public void DirectorUpdate_WarmsMeetingButtonTemplate()
        {
            using var module = OpenModModule();
            var calls = Calls(Method(module, DirectorType, "Update")).ToList();

            Assert.True(calls.Contains(ResolverType + "::TryWarmup"),
                $"{DirectorType}.Update から {ResolverType}.TryWarmup の先読みが消えている。"
                + "会議ボタンのテンプレートは未キャッシュだと Resources.Load の同期ロードで秒単位止まるため、"
                + "ラウンド開始のフレームではなくタイトル画面のうちに解決すること。");
        }

        private static ModuleDefinition OpenModModule()
        {
            string dllPath = AuditEnvironment.Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            return ModuleDefinition.ReadModule(dllPath);
        }

        private static MethodDefinition Method(ModuleDefinition module, string typeName, string methodName)
        {
            TypeDefinition type = CecilTypes.AllTypes(module)
                .SingleOrDefault(t => t.FullName == typeName);
            Assert.True(type != null, $"監査対象の型が無い: {typeName}");
            MethodDefinition method = type.Methods.SingleOrDefault(m => m.Name == methodName && m.HasBody);
            Assert.True(method != null, $"監査対象のメソッドが無い: {typeName}::{methodName}");
            return method;
        }

        private static IEnumerable<string> Calls(MethodDefinition method)
        {
            foreach (Instruction ins in method.Body.Instructions)
            {
                if (ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) continue;
                if (!(ins.Operand is MethodReference mr)) continue;
                yield return mr.DeclaringType.FullName + "::" + mr.Name;
            }
        }
    }
}
