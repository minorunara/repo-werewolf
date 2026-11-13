using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Debugging;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;

namespace Werewolf
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(MenuLibGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "minorunara.werewolf";
        public const string PluginName = "Werewolf";
        public const string PluginVersion = WerewolfBuildInfo.Version;

        internal const string MenuLibGuid = "nickklmao.menulib";

        internal static new ManualLogSource? Logger;

        internal static GameConfig? GameConfig;

        internal static ConfigBindings? Bindings;

        internal static ConfigEntry<KeyCode>? WolfModeKey;

        internal static ConfigEntry<KeyCode>? BeaconKey;

        internal static ConfigEntry<KeyCode>? LobbySettingsPanelKey;

        internal static ConfigEntry<KeyCode>? MeetingMapKey;

        internal static ConfigEntry<KeyCode>? MeetingChatLogKey;

        internal static ConfigEntry<KeyCode>? ReplayViewerKey;

        internal static ConfigEntry<KeyCode>? CorpseReportKey;

        internal static ConfigEntry<KeyCode>? ResultReturnKey;

        internal static ConfigEntry<KeyCode>? ManualKey;

        internal static ConfigEntry<KeyCode>? VoidMatchKey;

        internal static void RefreshGameConfig()
        {
            if (Bindings == null) return;
            GameConfig = Bindings.Snapshot();
            ScenarioDefinition? definition = ScenarioRun.Definition;
            if (ScenarioRun.Active && definition != null)
                GameConfig = ScenarioGameConfig.Build(GameConfig, definition);
        }

        private void Awake()
        {
            ManualLogSource logger = base.Logger;
            Logger = logger;

            if (!CosmeticLottery.ValidateWeights())
            {
                logger.LogWarning(
                    "CosmeticLottery.ValidateWeights failed: weight constants (Common/Uncommon/Rare/UltraRare) " +
                    "do not sum to TotalWeight. Cosmetic coin drop rates will be inconsistent.");
            }

            StructuredLog.Install();

            Bindings = new ConfigBindings(Config);
            GameConfig = Bindings.Snapshot();

            WolfModeKey = Bindings.WolfModeKey;
            BeaconKey = Bindings.BeaconKey;
            LobbySettingsPanelKey = Bindings.LobbySettingsPanelKey;
            MeetingMapKey = Bindings.MeetingMapKey;
            MeetingChatLogKey = Bindings.MeetingChatLogKey;
            ReplayViewerKey = Bindings.ReplayViewerKey;
            CorpseReportKey = Bindings.CorpseReportKey;
            ManualKey = Bindings.ManualKey;
            ResultReturnKey = Bindings.ResultReturnKey;
            VoidMatchKey = Bindings.VoidMatchKey;

            WorldgenItemBindings.Install(Config, Bindings);

            WorldgenMapBinding.Install(Config, Bindings);

            UI.AssetCatalog.SetAlternativeExecutionIcon(Bindings.UseAlternativeExecutionIcon.Value);
            if (Bindings.UseAlternativeExecutionIcon.Value)
            {
                WLog.Line("alternative_execution_icon", secret: false, ("enabled", true));
            }

            UI.AssetCatalog.SetStreamerSafeMode(Bindings.StreamerSafeMode.Value);
            if (Bindings.StreamerSafeMode.Value)
            {
                WLog.Line("streamer_safe_mode", secret: false, ("enabled", true));
            }

            Vanilla.EnemyMapIcons.TextureResolver = UI.AssetCatalog.GetTexture;
            Vanilla.MeetingButton.PrefabResolver = UI.AssetCatalog.GetPrefab;

            var harmony = new Harmony(PluginGuid);
            if (!ApplyPatchesIsolated(harmony, logger))
            {
                logger.LogError($"{PluginName} {PluginVersion}: Harmonyパッチ適用に失敗したため" +
                    "MODを無効化しました（本体アップデートでパッチ対象が変わった可能性。" +
                    "修復手順: docs/steering/tech.md「本体アップデート対応プレイブック」）");
                return;
            }

            WerewolfDirector director;
            try
            {
                director = gameObject.AddComponent<WerewolfDirector>();
            }
            catch (Exception e)
            {
                harmony.UnpatchSelf();
                logger.LogError($"{PluginName} {PluginVersion}: Directorの生成に失敗したため" +
                    $"MODを無効化しました\n{e}");
                return;
            }
            if (!director.IsReady)
            {
                harmony.UnpatchSelf();
                UnityEngine.Object.Destroy(director);
                logger.LogError($"{PluginName} {PluginVersion}: Directorの初期化に失敗したため" +
                    "MODを無効化しました");
                return;
            }

            ScenarioMenuEntry.TryInstall();
        }

        private static bool ApplyPatchesIsolated(Harmony harmony, ManualLogSource logger)
        {
            var failures = new List<(string patchClass, Exception error)>();
            int scenarioFailures = 0;
            foreach (var type in AccessTools.GetTypesFromAssembly(typeof(Plugin).Assembly))
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    string patchClass = type.FullName ?? type.Name;
                    failures.Add((patchClass, e));
                    if (PatchTierPolicy.IsScenarioTier(patchClass)) scenarioFailures++;
                }
            }

            foreach (var (patchClass, error) in failures)
            {
                var cause = error.InnerException ?? error;
                WLog.Line("harmony_patch_failed", secret: false,
                    ("patchClass", patchClass), ("err", cause.Message));
                logger.LogError($"Harmony patch failed: {patchClass}\n{error}");
            }

            PatchApplyOutcome outcome = PatchTierPolicy.Decide(
                failures.Count - scenarioFailures, scenarioFailures);
            if (outcome == PatchApplyOutcome.ModDisabled)
            {
                harmony.UnpatchSelf();
                WLog.Line("harmony_disabled", secret: false, ("failedClasses", failures.Count));
                return false;
            }

            if (outcome == PatchApplyOutcome.ScenariosDisabled)
            {
                ScenarioRun.DisableForFailedPatches(scenarioFailures);
            }

            return true;
        }
    }
}
