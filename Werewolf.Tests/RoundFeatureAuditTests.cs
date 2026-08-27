using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class RoundFeatureAuditTests
    {
        private const string DirectorTypeName = "Werewolf.Game.WerewolfDirector";
        private const string FeatureInterfaceName = "Werewolf.Game.IRoundFeature";

        private const int DirectorMutableFieldCeiling = 19;

        private static readonly (string TypeFullName, string ResetMethod)[] ScopedControllers =
        {
            ("Werewolf.Game.ModIntegrityController", "ResetSession"),
            ("Werewolf.Game.LobbySettingsController", "ResetForRoomLeave"),
            ("Werewolf.Game.ClientSettingsMirror", "Reset"),
            ("Werewolf.Game.ClientRoundMirror", "Reset"),
        };

        private static readonly Dictionary<string, string> ExemptFields = new Dictionary<string, string>
        {
            ["BomberClientController._bomberProximity"] =
                "EnsureBomberGauges が fullSec 変化で再生成。Reset は ResetAll で中身のみ掃除",
            ["BomberClientController._selfDefenseProximity"] =
                "EnsureBomberGauges が fullSec 変化で再生成。Reset は ResetAll で中身のみ掃除",
            ["BomberClientController._bomberProximityFullSec"] =
                "EnsureBomberGauges の再生成判定用スナップショット",
            ["ShamanClientController._shamanSense"] =
                "EnsureShamanSense が設定変化で再生成。Reset は Reset() で中身のみ掃除",
            ["ShamanClientController._shamanGazeFullSecSnapshot"] =
                "EnsureShamanSense の再生成判定用スナップショット",
            ["ShamanClientController._shamanCooldownSecSnapshot"] =
                "EnsureShamanSense の再生成判定用スナップショット",

            ["ModIntegrityController._modIntegrityEpochSeed"] =
                "エポックの単調増加をセッションを跨いで保つ（NextModIntegrityEpoch の下限）",
            ["LobbySettingsController._panelTickAtMs"] =
                "2秒スロットルの時刻。残っても次の観測で上書きされ持ち越し実害がない",

            ["MeetingScatterController._extractionScatter"] =
                "遅延生成（??=）の常駐ホルダ。計画は Reset 内の ClearPlan で掃除済み",

            ["MeetingClientController._meetingUiActive"] =
                "Reset（切断掃除）では倒さない＝次フレームの Tick 会議外分岐が投票行の畳み" +
                "（VotePanel.EndMeeting・チャット畳み）を必ず1回実施してから false へ戻す" +
                "（最終保証はロビー帰還の DestroyUi）",

            ["RolesClientController._toastQueue"] =
                "表示時間が Config 依存のためロビー帰還（DestroyUi）で null 化して次セッションで再生成する" +
                "（旧 Director 直下でも ResetToLobby のみで null 化＝ClearClientState では消さない挙動を維持）",

            ["WinCeremonyController._checkmate"] = "StartHosted で生成・ResetToLobby（ResetHost）で破棄",
            ["WinCeremonyController._checkmateScanPending"] = "ホスト寿命（BeginHostRound/ResetHost が初期化）",
            ["WinCeremonyController._checkmateNextScanUnixMs"] =
                "スキャンスロットル。_checkmate が null 化されると単独では駆動されない",
            ["WinCeremonyController._checkmateLineNextSyncUnixMs"] = "ホスト寿命（ResetHost が初期化）",
            ["WinCeremonyController._eradicationConfirmAtUnixMs"] = "ホスト寿命（BeginHostRound/ResetHost が初期化）",

            ["MeetingChatController._chatDebugDueUnixMs"] = "遅延注入の予定時刻（発火で自己クリア）",
            ["MeetingChatController._chatDebugPendingText"] = "遅延注入の本文（発火で自己クリア）",
            ["MeetingChatController._inputGateProbe"] = "入力ゲート監視トグル（診断専用）",
            ["MeetingChatController._inputGateLastFree"] = "入力ゲート監視の前回値（診断専用）",
            ["MeetingChatController._chatDebugAvatarFallback"] = "負荷試験用アバター代用トグル（診断専用）",
        };

        private static string Meta(string key)
        {
            return typeof(RoundFeatureAuditTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static ModuleDefinition LoadModModule()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");
            return ModuleDefinition.ReadModule(dllPath);
        }

        private static List<TypeDefinition> FeatureTypes(ModuleDefinition module)
        {
            var types = CecilTypes.AllTypes(module)
                .Where(t => t.HasInterfaces && t.Interfaces.Any(
                    i => i.InterfaceType.FullName == FeatureInterfaceName))
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();
            Assert.True(types.Count > 0,
                $"{FeatureInterfaceName} の実装型が MOD DLL に1つも見つからない（監査対象の解決に失敗している）。");
            return types;
        }

        private static IEnumerable<FieldDefinition> MutableInstanceFields(TypeDefinition type)
        {
            return type.Fields.Where(f =>
                !f.IsStatic &&
                !f.IsLiteral &&
                !f.IsInitOnly &&
                !f.Name.Contains('<'));
        }

        private static HashSet<string> WrittenFieldsFrom(TypeDefinition type, string entryMethodName)
        {
            var written = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);

            var queue = new Queue<MethodDefinition>();
            foreach (var m in type.Methods.Where(m => m.Name == entryMethodName && m.HasBody))
            {
                queue.Enqueue(m);
                visited.Add(m.FullName);
            }
            Assert.True(queue.Count > 0, $"{type.FullName}.{entryMethodName} が見つからない（IL Body 付き）。");

            while (queue.Count > 0)
            {
                MethodDefinition method = queue.Dequeue();
                foreach (Instruction ins in method.Body.Instructions)
                {
                    if ((ins.OpCode == OpCodes.Stfld || ins.OpCode == OpCodes.Ldflda) &&
                        ins.Operand is FieldReference fr &&
                        fr.DeclaringType.FullName == type.FullName)
                    {
                        written.Add(fr.Name);
                        continue;
                    }

                    if ((ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) ||
                        !(ins.Operand is MethodReference mr))
                        continue;
                    if (mr.DeclaringType.FullName != type.FullName) continue;
                    if (visited.Contains(mr.FullName)) continue;

                    MethodDefinition target = mr.Resolve();
                    if (target == null || !target.HasBody) continue;
                    visited.Add(mr.FullName);
                    queue.Enqueue(target);
                }
            }

            return written;
        }

        private static HashSet<string> FeatureCallsFrom(
            TypeDefinition director, string entryMethodName, HashSet<string> featureTypeFullNames)
        {
            var calls = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);

            var queue = new Queue<MethodDefinition>();
            foreach (var m in director.Methods.Where(m => m.Name == entryMethodName && m.HasBody))
            {
                queue.Enqueue(m);
                visited.Add(m.FullName);
            }
            Assert.True(queue.Count > 0, $"{DirectorTypeName}.{entryMethodName} が見つからない（IL Body 付き）。");

            while (queue.Count > 0)
            {
                MethodDefinition method = queue.Dequeue();
                foreach (Instruction ins in method.Body.Instructions)
                {
                    if ((ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) ||
                        !(ins.Operand is MethodReference mr))
                        continue;

                    if (featureTypeFullNames.Contains(mr.DeclaringType.FullName))
                    {
                        calls.Add(mr.DeclaringType.Name + "." + mr.Name);
                        continue;
                    }

                    if (mr.DeclaringType.FullName != DirectorTypeName) continue;
                    if (visited.Contains(mr.FullName)) continue;

                    MethodDefinition target = mr.Resolve();
                    if (target == null || !target.HasBody) continue;
                    visited.Add(mr.FullName);
                    queue.Enqueue(target);
                }
            }

            return calls;
        }

        private static List<(TypeDefinition Type, string ResetMethod)> AuditedControllers(ModuleDefinition module)
        {
            var result = FeatureTypes(module)
                .Select(t => (Type: t, ResetMethod: "Reset"))
                .ToList();
            foreach ((string typeFullName, string resetMethod) in ScopedControllers)
            {
                TypeDefinition type = module.GetType(typeFullName);
                Assert.True(type != null, $"ScopedControllers に実在しない型がある: {typeFullName}");
                result.Add((type, resetMethod));
            }
            return result;
        }

        [Fact]
        public void FeatureReset_WritesEveryMutableFeatureField()
        {
            using var module = LoadModModule();
            var failures = new List<string>();

            foreach ((TypeDefinition feature, string resetMethod) in AuditedControllers(module))
            {
                HashSet<string> written = WrittenFieldsFrom(feature, resetMethod);
                foreach (var missing in MutableInstanceFields(feature)
                             .Select(f => f.Name)
                             .Where(n => !written.Contains(n)
                                         && !ExemptFields.ContainsKey(feature.Name + "." + n))
                             .OrderBy(n => n, StringComparer.Ordinal))
                {
                    failures.Add($"  - {feature.Name}.{missing}");
                }
            }

            Assert.True(failures.Count == 0,
                $"Reset() から到達する経路で掃除されていないコントローラの可変フィールドが {failures.Count} 個ある:\n" +
                string.Join("\n", failures) + "\n" +
                "セッション境界で前の試合の状態が次の試合へ持ち越される（ADR-0044 と同型の事故）。" +
                "各コントローラの Reset() で初期値へ戻すこと。掃除が不要なフィールドは ExemptFields へ理由付きで登録する。");
        }

        [Theory]
        [InlineData("Update", "Tick")]
        [InlineData("ResetToLobby", "Reset")]
        [InlineData("ResetToLobby", "DestroyUi")]
        public void EveryFeature_IsWiredIntoDirector(string directorEntry, string featureMethod)
        {
            using var module = LoadModModule();
            var director = module.GetType(DirectorTypeName);
            Assert.NotNull(director);

            List<TypeDefinition> features = FeatureTypes(module);
            var featureFullNames = new HashSet<string>(
                features.Select(t => t.FullName), StringComparer.Ordinal);

            HashSet<string> calls = FeatureCallsFrom(director, directorEntry, featureFullNames);

            var unwired = features
                .Select(t => t.Name)
                .Where(n => !calls.Contains(n + "." + featureMethod))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.True(unwired.Count == 0,
                $"{DirectorTypeName}.{directorEntry} から {featureMethod}() が呼ばれていないコントローラが {unwired.Count} 個ある:\n" +
                string.Join("\n", unwired.Select(n => "  - " + n)) + "\n" +
                "IRoundFeature 実装は Director の駆動・掃除・UI破棄へ必ず配線すること（ADR-0105）。");
        }

        [Theory]
        [InlineData("Werewolf.Game.ModIntegrityController", "Update", "Tick")]
        [InlineData("Werewolf.Game.ModIntegrityController", "ApplyInbound", "TryHandleInbound")]
        [InlineData("Werewolf.Game.LobbySettingsController", "Update", "Tick")]
        [InlineData("Werewolf.Game.LobbySettingsController", "HandleLocalDisconnected", "ResetForRoomLeave")]
        [InlineData("Werewolf.Game.ClientSettingsMirror", "ResetToLobby", "Reset")]
        [InlineData("Werewolf.Game.ClientRoundMirror", "ResetToLobby", "Reset")]
        public void ScopedController_IsWiredIntoDirector(string typeFullName, string directorEntry, string method)
        {
            using var module = LoadModModule();
            var director = module.GetType(DirectorTypeName);
            Assert.NotNull(director);
            TypeDefinition type = module.GetType(typeFullName);
            Assert.True(type != null, $"実在しない型: {typeFullName}");

            HashSet<string> calls = FeatureCallsFrom(director, directorEntry,
                new HashSet<string>(new[] { typeFullName }, StringComparer.Ordinal));

            Assert.True(calls.Contains(type.Name + "." + method),
                $"{DirectorTypeName}.{directorEntry} から {type.Name}.{method}() が呼ばれていない。");
        }

        [Fact]
        public void ExemptFields_ContainsNoStaleEntries()
        {
            using var module = LoadModModule();
            var actual = new HashSet<string>(
                AuditedControllers(module).SelectMany(c =>
                    MutableInstanceFields(c.Type).Select(f => c.Type.Name + "." + f.Name)),
                StringComparer.Ordinal);

            var stale = ExemptFields.Keys
                .Where(n => !actual.Contains(n))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.True(stale.Count == 0,
                $"ExemptFields に実在しないフィールドが {stale.Count} 個残っている:\n" +
                string.Join("\n", stale.Select(n => "  - " + n)) + "\n" +
                "削除・改名したフィールドの除外登録は同じ変更セットで消すこと。");
        }

        [Fact]
        public void DirectorMutableFieldCount_DoesNotRegrow()
        {
            using var module = LoadModModule();
            var director = module.GetType(DirectorTypeName);
            Assert.NotNull(director);

            var controllerTypes = new HashSet<string>(
                AuditedControllers(module).Select(c => c.Type.FullName), StringComparer.Ordinal);

            List<string> fields = MutableInstanceFields(director)
                .Where(f => !controllerTypes.Contains(f.FieldType.FullName))
                .Select(f => f.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.True(fields.Count <= DirectorMutableFieldCeiling,
                $"Director 本体の可変インスタンスフィールドが {fields.Count} 個あり、上限 {DirectorMutableFieldCeiling} を超えた。\n" +
                "新しい状態フィールドは Director 直下ではなく機能コントローラ（IRoundFeature）へ置くこと（ADR-0105）。\n" +
                "コントローラへの移設で実数が減ったときは上限を実数まで引き下げる。現在のフィールド:\n" +
                string.Join("\n", fields.Select(n => "  - " + n)));
        }
    }
}
