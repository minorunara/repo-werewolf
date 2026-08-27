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
    public class ClientStateResetCoverageTests
    {
        private const string DirectorTypeName = "Werewolf.Game.WerewolfDirector";

        private const string ScenarioControllerTypeName = "Werewolf.Scenarios.Game.ScenarioController";

        private const string ScenarioEntryMethodName = "Reset";

        private const string EntryMethodName = "ResetToLobby";

        private const string ReasonPermanentHolder =
            "Awake で1回生成し全ラウンド再利用する常駐ホルダ。中身の掃除は ResetToLobby が" +
            "各オブジェクトのメソッド（Destroy/End/ForceRestore/ResetWatchdog 等）で行い、参照自体は残す";

        private const string ReasonLobbyScoped =
            "ロビー滞在スコープ（試合セッションではない）。ロビー scope を外れたフレームで" +
            "ResetModIntegrity / TickLobbySettings 側の専用経路がクリアする";

        private const string ReasonDiagnosticOnly =
            "観測・ログスロットル専用でゲーム進行に影響しない（残っても次の観測で上書き・自然失効する）";

        private const string ReasonOwnerNulled =
            "所有オブジェクト側が ResetToLobby で null 化されるため、このスケジュール値だけが残っても駆動されない";

        private static readonly Dictionary<string, string> ExemptFields = new Dictionary<string, string>
        {
            ["_uiManager"] = ReasonPermanentHolder,
            ["_votePanel"] = ReasonPermanentHolder,
            ["_roundPanels"] = ReasonPermanentHolder,
            ["_movementFreezer"] = ReasonPermanentHolder,
            ["_enemyFreezer"] = ReasonPermanentHolder,
            ["_truckWarper"] = ReasonPermanentHolder,
            ["_voiceDriver"] = ReasonPermanentHolder,
            ["_voiceIsDeadActor"] = ReasonPermanentHolder,
            ["_voiceIsEavesdropTarget"] = ReasonPermanentHolder,
            ["_bomberFeature"] = ReasonPermanentHolder,
            ["_shamanFeature"] = ReasonPermanentHolder,
            ["_modIntegrityFeature"] = ReasonPermanentHolder,
            ["_tutorialFeature"] = ReasonPermanentHolder,
            ["_scenarioFeature"] = ReasonPermanentHolder,
            ["_chatFeature"] = ReasonPermanentHolder,
            ["_resultFlowFeature"] = ReasonPermanentHolder,
            ["_lobbySettingsFeature"] = ReasonPermanentHolder,
            ["_roleRevealFeature"] = ReasonPermanentHolder,
            ["_winCeremonyFeature"] = ReasonPermanentHolder,
            ["_scatterFeature"] = ReasonPermanentHolder,
            ["_rolesClientFeature"] = ReasonPermanentHolder,
            ["_meetingClientFeature"] = ReasonPermanentHolder,

            ["_busWaitLogAtMs"] = ReasonDiagnosticOnly,

        };

        private static string Meta(string key)
        {
            return typeof(ClientStateResetCoverageTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static IEnumerable<FieldDefinition> MutableInstanceFields(TypeDefinition owner)
        {
            return owner.Fields.Where(f =>
                !f.IsStatic &&
                !f.IsLiteral &&
                !f.IsInitOnly &&
                !f.Name.Contains('<'));
        }

        private static HashSet<string> WrittenFieldsFrom(TypeDefinition owner, string entryMethodName)
        {
            var written = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);

            var queue = new Queue<MethodDefinition>();
            foreach (var m in owner.Methods.Where(m => m.Name == entryMethodName && m.HasBody))
            {
                queue.Enqueue(m);
                visited.Add(m.FullName);
            }
            Assert.True(queue.Count > 0, $"{owner.FullName}.{entryMethodName} が見つからない（IL Body 付き）。");

            while (queue.Count > 0)
            {
                MethodDefinition method = queue.Dequeue();
                foreach (Instruction ins in method.Body.Instructions)
                {
                    if ((ins.OpCode == OpCodes.Stfld || ins.OpCode == OpCodes.Ldflda) &&
                        ins.Operand is FieldReference fr &&
                        fr.DeclaringType.FullName == owner.FullName)
                    {
                        written.Add(fr.Name);
                        continue;
                    }

                    if ((ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) ||
                        !(ins.Operand is MethodReference mr))
                        continue;

                    if (mr.DeclaringType.FullName != owner.FullName) continue;
                    if (visited.Contains(mr.FullName)) continue;

                    MethodDefinition target = mr.Resolve();
                    if (target == null || !target.HasBody) continue;
                    visited.Add(mr.FullName);
                    queue.Enqueue(target);
                }
            }

            return written;
        }

        private static void AssertTeardownWritesEveryMutableField(
            string typeName, string entryMethodName,
            IReadOnlyDictionary<string, string> exempt, string fixHint)
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);
            TypeDefinition owner = module.GetType(typeName);
            Assert.NotNull(owner);

            HashSet<string> written = WrittenFieldsFrom(owner, entryMethodName);

            var missing = MutableInstanceFields(owner)
                .Select(f => f.Name)
                .Where(n => !written.Contains(n) && !exempt.ContainsKey(n))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.True(missing.Count == 0,
                $"{typeName}: {entryMethodName} から到達する経路で掃除されていない可変フィールドが " +
                $"{missing.Count} 個ある:\n" +
                string.Join("\n", missing.Select(n => "  - " + n)) + "\n" +
                "境界の前後で状態が持ち越される（ADR-0044 と同型の事故）。修復手順: " + fixHint);
        }

        [Fact]
        public void SessionTeardown_WritesEveryMutableDirectorField()
        {
            AssertTeardownWritesEveryMutableField(DirectorTypeName, EntryMethodName, ExemptFields,
                $"WerewolfDirector.Reset.cs の {EntryMethodName}()（またはそこから呼ばれる Reset*/Clear*/Hide* ）で" +
                "初期値へ戻すこと。掃除が不要なフィールドは ExemptFields へ理由付きで登録する。");
        }

        [Fact]
        public void ScenarioReset_WritesEveryMutableScenarioControllerField()
        {
            AssertTeardownWritesEveryMutableField(ScenarioControllerTypeName, ScenarioEntryMethodName,
                new Dictionary<string, string>(),
                $"状態を足した partial の Reset*()（ScenarioController.{ScenarioEntryMethodName}() が呼ぶ）で" +
                "初期値へ戻すこと。生成時に確定して掃除の要らない参照は readonly で持つ。");
        }

        [Fact]
        public void ExemptFields_ContainsNoStaleEntries()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);
            var director = module.GetType(DirectorTypeName);
            Assert.NotNull(director);

            var actual = new HashSet<string>(
                MutableInstanceFields(director).Select(f => f.Name), StringComparer.Ordinal);

            var stale = ExemptFields.Keys
                .Where(n => !actual.Contains(n))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.True(stale.Count == 0,
                $"ExemptFields に実在しないフィールドが {stale.Count} 個残っている:\n" +
                string.Join("\n", stale.Select(n => "  - " + n)) + "\n" +
                "削除・改名したフィールドの除外登録は同じ変更セットで消すこと" +
                "（残すと同名の新フィールドが監査をすり抜ける）。");
        }
    }
}
