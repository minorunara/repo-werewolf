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

        private const string EntryMethodName = "ResetToLobby";

        private const string ReasonPermanentHolder =
            "Awake で1回生成し全ラウンド再利用する常駐ホルダ。中身の掃除は ResetToLobby が" +
            "各オブジェクトのメソッド（Destroy/End/ForceRestore/ResetWatchdog 等）で行い、参照自体は残す";

        private const string ReasonMeetingScoped =
            "会議スコープの保留値。会議外フレームの TickMeetingClient else 分岐が毎フレーム初期値へ戻すため、" +
            "セッション境界を跨いだ持ち越しが構造的に起きない";

        private const string ReasonLobbyScoped =
            "ロビー滞在スコープ（試合セッションではない）。ロビー scope を外れたフレームで" +
            "ResetModIntegrity / TickLobbySettings 側の専用経路がクリアする";

        private const string ReasonRoundStartSnapshot =
            "ラウンド開始時にホスト設定から取り直すスナップショット。設定値と食い違ったフレームで自己再生成する";

        private const string ReasonDiagnosticOnly =
            "観測・ログスロットル専用でゲーム進行に影響しない（残っても次の観測で上書き・自然失効する）";

        private const string ReasonOwnerNulled =
            "所有オブジェクト側が ResetToLobby で null 化されるため、このスケジュール値だけが残っても駆動されない";

        private static readonly Dictionary<string, string> ExemptFields = new Dictionary<string, string>
        {
            ["_uiManager"] = ReasonPermanentHolder,
            ["_votePanel"] = ReasonPermanentHolder,
            ["_roundPanels"] = ReasonPermanentHolder,
            ["_meetingButton"] = ReasonPermanentHolder,
            ["_movementFreezer"] = ReasonPermanentHolder,
            ["_enemyFreezer"] = ReasonPermanentHolder,
            ["_truckWarper"] = ReasonPermanentHolder,
            ["_voiceDriver"] = ReasonPermanentHolder,
            ["_voiceIsDeadActor"] = ReasonPermanentHolder,
            ["_voiceIsEavesdropTarget"] = ReasonPermanentHolder,
            ["_extractionScatter"] = ReasonPermanentHolder,

            ["_pendingBeaconAudit"] = ReasonMeetingScoped,
            ["_pendingMeetingTutorial"] = ReasonMeetingScoped,
            ["_resultCeremonyAtMs"] = ReasonMeetingScoped,
            ["_pendingCurseCatActor"] = ReasonMeetingScoped,
            ["_pendingCurseDeadlineMs"] = ReasonMeetingScoped,

            ["_modIntegritySessionActive"] = ReasonLobbyScoped,
            ["_modIntegrityHostActor"] = ReasonLobbyScoped,
            ["_modIntegrityHostSignal"] = ReasonLobbyScoped,
            ["_modIntegrityEpochSeed"] = ReasonLobbyScoped,
            ["_modIntegrityPendingRequest"] = ReasonLobbyScoped,
            ["_modIntegrityRosterTickAtMs"] = ReasonLobbyScoped,
            ["_modIntegrityUiEpoch"] = ReasonLobbyScoped,
            ["_modIntegrityUiRevision"] = ReasonLobbyScoped,
            ["_lastPanelBlob"] = ReasonLobbyScoped,
            ["_lastPanelModeEnabled"] = ReasonLobbyScoped,
            ["_lastPublishedBlob"] = ReasonLobbyScoped,
            ["_lobbyBlobMirror"] = ReasonLobbyScoped,
            ["_lobbyTickAtMs"] = ReasonLobbyScoped,
            ["_lobbyPanelUserHidden"] = ReasonLobbyScoped,
            ["_lobbyStartHeldPage"] = ReasonLobbyScoped,
            ["_debugInjectedBlob"] = ReasonLobbyScoped,

            ["_bomberProximity"] = ReasonRoundStartSnapshot,
            ["_selfDefenseProximity"] = ReasonRoundStartSnapshot,
            ["_bomberProximityFullSec"] = ReasonRoundStartSnapshot,
            ["_shamanSense"] = ReasonRoundStartSnapshot,
            ["_shamanCooldownSecSnapshot"] = ReasonRoundStartSnapshot,
            ["_shamanGazeFullSecSnapshot"] = ReasonRoundStartSnapshot,

            ["_warpVerifyTarget"] = ReasonDiagnosticOnly,
            ["_warpVerifyDeadlineMs"] = ReasonDiagnosticOnly,
            ["_busWaitLogAtMs"] = ReasonDiagnosticOnly,
            ["_inputGateLastFree"] = ReasonDiagnosticOnly,
            ["_inputGateProbe"] = ReasonDiagnosticOnly,
            ["_chatDebugAvatarFallback"] = ReasonDiagnosticOnly,
            ["_chatDebugDueUnixMs"] = ReasonDiagnosticOnly,
            ["_chatDebugPendingText"] = ReasonDiagnosticOnly,

            ["_checkmateNextScanUnixMs"] = ReasonOwnerNulled,
        };

        private static string Meta(string key)
        {
            return typeof(ClientStateResetCoverageTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static IEnumerable<FieldDefinition> MutableInstanceFields(TypeDefinition director)
        {
            return director.Fields.Where(f =>
                !f.IsStatic &&
                !f.IsLiteral &&
                !f.IsInitOnly &&
                !f.Name.Contains('<'));
        }

        private static HashSet<string> WrittenFieldsFrom(TypeDefinition director, string entryMethodName)
        {
            var written = new HashSet<string>(StringComparer.Ordinal);
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
                    if ((ins.OpCode == OpCodes.Stfld || ins.OpCode == OpCodes.Ldflda) &&
                        ins.Operand is FieldReference fr &&
                        fr.DeclaringType.FullName == DirectorTypeName)
                    {
                        written.Add(fr.Name);
                        continue;
                    }

                    if ((ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) ||
                        !(ins.Operand is MethodReference mr))
                        continue;

                    if (mr.DeclaringType.FullName != DirectorTypeName) continue;
                    if (visited.Contains(mr.FullName)) continue;

                    MethodDefinition target = mr.Resolve();
                    if (target == null || !target.HasBody) continue;
                    visited.Add(mr.FullName);
                    queue.Enqueue(target);
                }
            }

            return written;
        }

        [Fact]
        public void SessionTeardown_WritesEveryMutableDirectorField()
        {
            var dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);
            var director = module.GetType(DirectorTypeName);
            Assert.NotNull(director);

            HashSet<string> written = WrittenFieldsFrom(director, EntryMethodName);

            var missing = MutableInstanceFields(director)
                .Select(f => f.Name)
                .Where(n => !written.Contains(n) && !ExemptFields.ContainsKey(n))
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.True(missing.Count == 0,
                $"{EntryMethodName} から到達する経路で掃除されていない可変フィールドが {missing.Count} 個ある:\n" +
                string.Join("\n", missing.Select(n => "  - " + n)) + "\n" +
                "セッション境界で前の試合の状態が次の試合へ持ち越される（ADR-0044 と同型の事故）。" +
                $"修復手順: WerewolfDirector.Reset.cs の {EntryMethodName}()（またはそこから呼ばれる Reset*/Clear*/Hide* ）で" +
                "初期値へ戻すこと。掃除が不要なフィールドは ExemptFields へ理由付きで登録する。");
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
