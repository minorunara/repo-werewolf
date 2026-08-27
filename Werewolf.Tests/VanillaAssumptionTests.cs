using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Werewolf.Tests
{
    public class VanillaAssumptionTests
    {
        private sealed class Assumption
        {
            public string Type;
            public string Method;
            public string Field;
            public string Calls;
            public string Why;
        }

        private static readonly Assumption[] Assumptions =
        {
            new Assumption { Type = "PlayerHealth", Method = "Hurt", Field = "invincibleTimer",
                Why = "無敵化（Vanilla.LocalPlayer.SetInvincible / GameOverSafety）は Hurt が invincibleTimer>0 で早期returnすることに依る" },
            new Assumption { Type = "PlayerHealth", Method = "InvincibleSet", Field = "invincibleTimer",
                Why = "InvincibleSet が invincibleTimer を書くことで毎フレーム 0.5s 供給の無敵化が成立する" },
            new Assumption { Type = "SemiUI", Method = "Show", Field = "showTimer",
                Why = "SemiUI の Show ポンプ流儀（Vanilla.TutorialPresenter が毎フレーム Show() を呼ぶ）は showTimer 駆動に依る" },
            new Assumption { Type = "SemiUI", Method = "Update", Field = "showTimer",
                Why = "SemiUI.Update が showTimer を減らして自己隠蔽することが Show ポンプの前提" },
            new Assumption { Type = "MenuPlayerListed", Method = "Update", Field = "playerAvatar", Calls = "set_text",
                Why = "行プレハブの名前は Update が playerAvatar から毎フレーム書く（Vanilla.ListedAvatar が生成直後に直接設定する根拠）" },
            new Assumption { Type = "MenuPlayerListed", Method = "Update", Field = "listSpot",
                Why = "行位置は Update が listSpot から決める（ListedAvatar が位置を直接設定する根拠）" },
            new Assumption { Type = "SemiFunc", Method = "InputDisableMovement", Calls = "DisableMovement",
                Why = "会議中の移動凍結（Vanilla.MovementFreezer / Inputs.DisableMovement）は InputManager.DisableMovement 経由" },
            new Assumption { Type = "InputManager", Method = "DisableMovement", Field = "disableMovementTimer",
                Why = "移動凍結は disableMovementTimer の毎フレーム供給で成立する" },
            new Assumption { Type = "InputManager", Method = "KeyDown", Field = "disableMovementTimer",
                Why = "凍結中に KeyDown(Interact) が潰される前提（ScenarioController.ReadAdvanceKey は KeyHold からエッジを作る）" },
            new Assumption { Type = "PlayerVoiceChat", Method = "OverrideMute", Field = "overrideMuteTimer",
                Why = "死者の発話演出抑止（Vanilla.VoiceMixerDriver）は OverrideMute の毎フレーム注入＝overrideMuteTimer に依る" },
            new Assumption { Type = "CosmeticTokenUIElement", Method = "OnDestroy", Field = "currentState",
                Why = "レベルから直接 LeaveToMainMenu する前に currentState を Main から外す（Scenarios.Vanilla.ScenarioWorld.TryLeaveGameStateMain）根拠" },
            new Assumption { Type = "RoundDirector", Method = "CosmeticWorldObjectExtracted", Field = "cosmeticWorldObjectsExtracted",
                Why = "コスメティックコインの注入（Vanilla.Extraction.AddCosmeticExtracted）はこのリストへの追加でバニラの演出・加算・セーブが自走する" },
            new Assumption { Type = "ResultScreenUI", Method = null, Field = "cosmeticWorldObjectsExtracted",
                Why = "ResultScreenUI が cosmeticWorldObjectsExtracted を読んでコイン演出を駆動する（ADR-0053 の正経路）" },
            new Assumption { Type = "MenuManager", Method = "PlayerHeadRemove", Field = "playerHeads",
                Why = "頭部アバターの静止（Vanilla.ListedAvatar.Freeze）は PlayerHeadRemove が playerHeads から外すことに依る" },
            new Assumption { Type = "RunManager", Method = "ChangeLevel", Calls = "SetRunLevel",
                Why = "ロビー→本編の捕捉（Game.Patches.LevelPatch）は ChangeLevel 内の SetRunLevel 呼び出しに依る" },
            new Assumption { Type = "GameDirector", Method = "SetDisableEscMenu", Field = "disableEscMenuTimer",
                Why = "Esc メニュー抑止（Vanilla.Session.DisableEscMenu）は disableEscMenuTimer の供給で成立する" },
            new Assumption { Type = "EnemyDirector", Method = "AmountSetup", Field = "amountCurve1_1", Calls = "PickEnemies",
                Why = "階層別の敵セット数（Vanilla.EnemySetOverride）は AmountSetup が amountCurve を評価して PickEnemies を回すことに依る（ADR-0117）" },
            new Assumption { Type = "EnemyDirector", Method = "PickEnemies", Field = "levelsCompletedCondition",
                Why = "空プールの検査（EnemySetRules.IsEligible）は PickEnemies が levelsCompletedCondition で絞ることの写し" },
            new Assumption { Type = "EnemyParent", Method = "DespawnedTimerSet", Field = "DespawnedTimer",
                Why = "ビーコンの呼集（Vanilla.BeaconEffect）とデモの保留（Scenarios.Vanilla.ScenarioEnemyHold）は DespawnedTimerSet が DespawnedTimer を書くことに依る" },
        };

        [Fact]
        public void GameplayMechanismsTheGatewayReliesOn_StillHold()
        {
            string gamePath = Path.Combine(AuditEnvironment.Meta("Werewolf.GameManagedDir"), "Assembly-CSharp.dll");
            Assert.True(File.Exists(gamePath), $"本体 DLL が見つからない: {gamePath}");
            using var game = AssemblyDefinition.ReadAssembly(gamePath,
                new ReaderParameters { AssemblyResolver = AuditEnvironment.CreateResolver() });

            var broken = new List<string>();
            foreach (var a in Assumptions)
            {
                string reason = Verify(game.MainModule, a);
                if (reason != null) broken.Add($"{a.Type}.{a.Method ?? "*"}: {reason}\n      → {a.Why}");
            }

            Assert.True(broken.Count == 0,
                $"本体の流儀の前提が {broken.Count} 件崩れている（本体アップデートで仕組みが変わった可能性。"
                + "各行の接着モジュールと DnSpy/clean の差分を読み、前提テストも実態へ直す）:\n  "
                + string.Join("\n  ", broken));
        }

        [Fact]
        public void Verify_ReportsMissingFieldAndCall()
        {
            string gamePath = Path.Combine(AuditEnvironment.Meta("Werewolf.GameManagedDir"), "Assembly-CSharp.dll");
            using var game = AssemblyDefinition.ReadAssembly(gamePath,
                new ReaderParameters { AssemblyResolver = AuditEnvironment.CreateResolver() });

            Assert.Null(Verify(game.MainModule, Assumptions[0]));
            Assert.Contains("フィールド", Verify(game.MainModule,
                new Assumption { Type = "PlayerHealth", Method = "Hurt", Field = "noSuchField_Probe" }));
            Assert.Contains("呼んでいない", Verify(game.MainModule,
                new Assumption { Type = "RunManager", Method = "ChangeLevel", Calls = "NoSuchMethod_Probe" }));
            Assert.Equal("メソッドが無い", Verify(game.MainModule,
                new Assumption { Type = "RunManager", Method = "NoSuchMethod_Probe", Field = "levels" }));
            Assert.Equal("型が無い", Verify(game.MainModule,
                new Assumption { Type = "NoSuchType_Probe", Method = "Update", Field = "x" }));
        }

        private static string Verify(ModuleDefinition module, Assumption a)
        {
            var type = module.GetType(a.Type);
            if (type == null) return "型が無い";

            var methods = a.Method == null
                ? type.Methods.Where(m => m.HasBody).ToList()
                : type.Methods.Where(m => m.Name == a.Method && m.HasBody).ToList();
            if (methods.Count == 0) return a.Method == null ? "本体を持つメソッドが無い" : "メソッドが無い";

            var operands = methods.SelectMany(m => m.Body.Instructions).Select(i => i.Operand).ToList();
            if (a.Field != null && !operands.OfType<FieldReference>().Any(f => f.Name == a.Field))
                return $"フィールド {a.Field} を読み書きしていない";
            if (a.Calls != null && !operands.OfType<MethodReference>().Any(m => m.Name == a.Calls))
                return $"{a.Calls} を呼んでいない";
            return null;
        }
    }
}
