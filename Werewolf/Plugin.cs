using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Debugging;
using Werewolf.Game;

namespace Werewolf
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "minorunara.werewolf";
        public const string PluginName = "Werewolf";
        public const string PluginVersion = "1.0.0";

        internal static new ManualLogSource Logger;

        internal static GameConfig GameConfig;

        internal static ConfigBindings Bindings;

        internal static ConfigEntry<KeyCode> WolfModeKey;

        internal static ConfigEntry<KeyCode> BeaconKey;

        internal static ConfigEntry<KeyCode> LobbySettingsPanelKey;

        internal static ConfigEntry<KeyCode> MeetingMapKey;

        internal static ConfigEntry<KeyCode> MeetingChatLogKey;

        internal static ConfigEntry<KeyCode> CorpseReportKey;

        internal static ConfigEntry<KeyCode> ResultReturnKey;

        internal static ConfigEntry<KeyCode> ManualKey;

        internal static void RefreshGameConfig()
        {
            if (Bindings == null) return;
            GameConfig = Bindings.Snapshot();
        }

        private void Awake()
        {
            Logger = base.Logger;

            if (!CosmeticLottery.ValidateWeights())
            {
                Logger.LogWarning(
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
            CorpseReportKey = Bindings.CorpseReportKey;
            ManualKey = Bindings.ManualKey;
            ResultReturnKey = Bindings.ResultReturnKey;

            WorldgenItemBindings.Install(Config, Bindings);

            WorldgenMapBinding.Install(Config, Bindings);

            LoadLanguageOverride();

            WLog.Line("init", false, ("plugin", PluginName), ("version", PluginVersion));

            if (!GameRefs.ResolveAll())
            {
                Logger.LogError($"{PluginName} {PluginVersion}: 本体フィールド参照の解決に失敗したため" +
                    "MODを無効化しました（本体アップデートでフィールドが変わった可能性。" +
                    "修復手順: docs/steering/tech.md「本体アップデート対応プレイブック」）");
                return;
            }

            var harmony = new Harmony(PluginGuid);
            if (!ApplyPatchesIsolated(harmony))
            {
                Logger.LogError($"{PluginName} {PluginVersion}: Harmonyパッチ適用に失敗したため" +
                    "MODを無効化しました（本体アップデートでパッチ対象が変わった可能性。" +
                    "修復手順: docs/steering/tech.md「本体アップデート対応プレイブック」）");
                return;
            }

            gameObject.AddComponent<WerewolfDirector>();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded! " +
                $"(werewolfCount={GameConfig.WerewolfCount}, " +
                $"blackCatChancePercent={GameConfig.BlackCatChancePercent}, " +
                $"bomberChancePercent={GameConfig.BomberChancePercent}, " +
                $"roundSeconds={GameConfig.RoundSeconds}, " +
                $"blackCatRevealDelaySec={GameConfig.BlackCatRevealDelaySec}, " +
                $"debugMode={GameConfig.DebugMode})");
        }

        private void OnDestroy()
        {
            StructuredLog.FlushDeferredSecrets("plugin_destroy");
        }

        private static bool ApplyPatchesIsolated(Harmony harmony)
        {
            var failures = new List<(string patchClass, Exception error)>();
            foreach (var type in AccessTools.GetTypesFromAssembly(typeof(Plugin).Assembly))
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    failures.Add((type.FullName, e));
                }
            }

            if (failures.Count == 0)
            {
                int patchedCount = 0;
                foreach (var _ in harmony.GetPatchedMethods()) patchedCount++;
                WLog.Line("harmony_patched", secret: false, ("methods", patchedCount));
                return true;
            }

            foreach (var (patchClass, error) in failures)
            {
                var cause = error.InnerException ?? error;
                WLog.Line("harmony_patch_failed", secret: false,
                    ("patchClass", patchClass), ("err", cause.Message));
                Logger.LogError($"Harmony patch failed: {patchClass}\n{error}");
            }
            harmony.UnpatchSelf();
            WLog.Line("harmony_disabled", secret: false, ("failedClasses", failures.Count));
            return false;
        }

        private void LoadLanguageOverride()
        {
            try
            {
                string value = Bindings?.Language?.Value;
                if (string.IsNullOrEmpty(value) || value == "日本語" || value == "ja") return;

                bool english = value == "English" || value == "en";
                if (english)
                {
                    Texts.Current = Language.English;
                    WLog.Line("lang_loaded", secret: false, ("code", "en"), ("source", "builtin"));
                }

                string code = english ? "en" : value;

                var dllPath = typeof(Plugin).Assembly.Location;
                var dir = string.IsNullOrEmpty(dllPath) ? null : Path.GetDirectoryName(dllPath);
                if (string.IsNullOrEmpty(dir))
                {
                    if (!english)
                    {
                        WLog.Line("lang_load_failed", secret: false, ("code", code), ("reason", "no_dll_dir"));
                    }
                    return;
                }

                var path = Path.Combine(dir, "Lang", code + ".txt");
                if (!File.Exists(path))
                {
                    if (!english)
                    {
                        WLog.Line("lang_load_failed", secret: false,
                            ("code", code), ("reason", "file_missing"), ("path", path));
                    }
                    return;
                }

                string content = File.ReadAllText(path, Encoding.UTF8);
                var table = LangFile.Parse(content);
                Texts.SetOverride(table);
                WLog.Line("lang_loaded", secret: false, ("code", code), ("entries", table.Count));
            }
            catch (Exception e)
            {
                WLog.Line("lang_load_failed", secret: false, ("reason", "exception"), ("detail", e.GetType().Name));
            }
        }
    }
}
