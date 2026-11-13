#nullable enable
using System;
using System.Collections.Generic;
using Photon.Pun;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Vanilla;

namespace Werewolf.Vanilla
{
    internal static class WorldgenApplier
    {
        private const string ChargeTotalStatKey = "chargingStationChargeTotal";

        private const string PowerCrystalItemKey = "Item Power Crystal";

        private const string ItemsPurchasedDictName = "itemsPurchased";

        private const string PlayerHealthDictName = "playerHealth";

        private const string HealthUpgradeName = WorldgenUpgrades.Health;

        private const int DefaultPlayerHealth = 100;

        internal static bool DebugLevelArmed { get; private set; }

        internal static void ClearDebugLevel()
        {
            if (!DebugLevelArmed) return;
            DebugLevelArmed = false;
            try
            {
                var run = RunManager.instance;
                if (run != null) run.debugLevel = null;
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_map_error", secret: false,
                    ("part", "clear_debug_level"), ("err", e.Message));
            }
        }

        internal static bool CustomEnvironmentActive { get; private set; }

        internal static string MarkedSaveFileName { get; private set; } = "";

        internal static void ClearSessionMarker()
        {
            CustomEnvironmentActive = false;
            MarkedSaveFileName = "";
        }

        internal static void TryApplyOnDeparture(RunManager.ChangeLevelType changeLevelType)
        {
            try
            {
                var run = RunManager.instance;
                if (run == null) { WLog.Line("worldgen_apply_skip", secret: false, ("reason", "no_runmanager")); return; }
                if (run.restarting) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
                bool isRunLevelTransition =
                    changeLevelType == RunManager.ChangeLevelType.RunLevel
                    || (changeLevelType == RunManager.ChangeLevelType.Normal
                        && run.levelCurrent == run.levelLobby);
                if (!isRunLevelTransition) return;

                HaulGoalOverride.Clear();
                EnemySetOverride.Clear();

                if (ScenarioRunState.Active)
                {
                    ScenarioDefinition? definition = ScenarioRunState.Definition;
                    if (definition != null) ScenarioEnvironment.Apply(run, definition.Environment);
                    else WLog.Line("worldgen_apply_skip", secret: false, ("reason", "no_scenario_definition"));
                    return;
                }

                Plugin.RefreshGameConfig();
                var config = Plugin.GameConfig;
                if (config == null) { WLog.Line("worldgen_apply_skip", secret: false, ("reason", "no_config")); return; }
                if (!config.WerewolfModeEnabled) return;

                Photon.Realtime.Room? room = PhotonNetwork.CurrentRoom;
                int playerCount = PhotonNetwork.InRoom && room != null ? room.PlayerCount : 1;
                if (!ScenarioSessionGate.PlayerCountSatisfied(playerCount, config.DebugMode, ScenarioRunState.Active))
                {
                    WLog.Line("worldgen_apply_skip", secret: false,
                        ("reason", "too_few_players"), ("players", playerCount),
                        ("required", PlayerCountGate.MinimumPlayers));
                    return;
                }

                if (ScenarioSessionGate.RequiresConfigValidation(config.DebugMode, ScenarioRunState.Active))
                {
                    var configIssues = ConfigValidator.Validate(config, playerCount);
                    if (configIssues.Count > 0)
                    {
                        WLog.Line("worldgen_apply_skip", secret: false,
                            ("reason", "invalid_config"),
                            ("issues", string.Join(",", configIssues)),
                            ("players", playerCount));
                        return;
                    }
                }

                var stats = StatsManager.instance;
                var pun = PunManager.instance;
                if (stats == null || pun == null)
                {
                    WLog.Line("worldgen_apply_error", secret: false,
                        ("err", stats == null ? "no_statsmanager" : "no_punmanager"));
                    return;
                }

                WorldgenPlan plan = WorldgenPlanner.BuildPlan(config, GameItemDatabase.Instance);

                run.levelsCompleted = plan.LevelsCompleted;
                run.UpdateMoonLevel();
                SemiFunc.StatSetRunLevel(plan.LevelsCompleted);

                ApplyForcedLevel(run, config.StartMapName, plan.ForcedLevelName);

                ApplyItemsAndCharge(stats, pun, plan);

                ApplyUpgrades(stats, pun, plan.Upgrades);

                HaulGoalOverride.Arm(plan.HaulGoalPct);

                EnemySetOverride.Arm(plan.EnemySets);

                CustomEnvironmentActive = true;
                MarkedSaveFileName = ReadSaveFileName(stats);

                WLog.Line("worldgen_applied", secret: false,
                    ("level", plan.LevelsCompleted + 1),
                    ("map", plan.ForcedLevelName ?? "random"),
                    ("items", plan.Items.Count),
                    ("charge", plan.ChargeTotal),
                    ("upgrades", plan.Upgrades.Count),
                    ("orb", plan.OrbDropMax),
                    ("haulGoalPct", plan.HaulGoalPct));
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_apply_error", secret: false, ("err", e.Message));
            }
        }

        internal static void ApplyForcedLevel(RunManager run, string configuredMapName, string? forcedLevelName)
        {
            if (forcedLevelName == null)
            {
                if (!string.IsNullOrWhiteSpace(configuredMapName))
                {
                    WLog.Line("worldgen_map_fallback", secret: false,
                        ("name", configuredMapName.Trim()), ("reason", "not_found"));
                }
                return;
            }

            Level? resolved = null;
            if (run.levels != null)
            {
                foreach (var level in run.levels)
                {
                    if (level != null && string.Equals(level.name, forcedLevelName, StringComparison.Ordinal))
                    {
                        resolved = level;
                        break;
                    }
                }
            }

            if (resolved == null)
            {
                WLog.Line("worldgen_map_fallback", secret: false,
                    ("name", forcedLevelName),
                    ("reason", "resolve_failed"));
                return;
            }

            run.debugLevel = resolved;
            DebugLevelArmed = true;
        }

        private static void ApplyItemsAndCharge(StatsManager stats, PunManager pun, WorldgenPlan plan)
        {
            var existingKeys = stats.itemsPurchased != null
                ? new List<string>(stats.itemsPurchased.Keys)
                : new List<string>();

            foreach (var key in existingKeys)
            {
                if (key == PowerCrystalItemKey) continue;
                if (plan.Items.ContainsKey(key)) continue;
                pun.UpdateStat(ItemsPurchasedDictName, key, 0);
            }

            foreach (var pair in plan.Items)
            {
                pun.UpdateStat(ItemsPurchasedDictName, pair.Key, pair.Value);
            }

            pun.UpdateStat(ItemsPurchasedDictName, PowerCrystalItemKey, plan.PowerCrystals);
            pun.SetRunStatSet(ChargeTotalStatKey, plan.ChargeTotal);
        }

        private static void ApplyUpgrades(StatsManager stats, PunManager pun,
                                          IReadOnlyDictionary<string, int> upgrades)
        {
            if (upgrades == null || upgrades.Count == 0) return;

            var dicts = stats.dictionaryOfDictionaries;
            if (dicts == null)
            {
                WLog.Line("worldgen_upgrades_error", secret: false, ("err", "dict_null"));
                return;
            }

            bool multiplayer = SemiFunc.IsMultiplayer();
            PhotonView? punView = pun.photonView;
            if (multiplayer && punView == null)
            {
                WLog.Line("worldgen_upgrades_error", secret: false, ("err", "photonview_unresolved"));
                return;
            }

            var director = GameDirector.instance;
            if (director == null || director.PlayerList == null)
            {
                WLog.Line("worldgen_upgrades_error", secret: false, ("err", "no_playerlist"));
                return;
            }

            foreach (PlayerAvatar avatar in director.PlayerList)
            {
                if (avatar == null) continue;
                string steamID = SemiFunc.PlayerGetSteamID(avatar);
                if (string.IsNullOrEmpty(steamID)) continue;

                var deltas = WorldgenUpgradeDeltas.Compute(upgrades, name =>
                {
                    Dictionary<string, int>? dict;
                    return dicts.TryGetValue("playerUpgrade" + name, out dict) && dict != null
                        ? dict.GetValueOrDefault(steamID, 0)
                        : 0;
                });

                foreach (var (name, delta) in deltas)
                {
                    int startHealth = -1;
                    if (name == HealthUpgradeName)
                    {
                        Dictionary<string, int>? hpDict;
                        int currentHp = dicts.TryGetValue(PlayerHealthDictName, out hpDict) && hpDict != null
                            ? hpDict.GetValueOrDefault(steamID, DefaultPlayerHealth)
                            : DefaultPlayerHealth;
                        startHealth = WorldgenStartHealth.Compute(
                            currentHp, delta, upgrades[HealthUpgradeName]);
                    }

                    if (multiplayer)
                    {
                        punView!.RPC("TesterUpgradeCommandRPC", RpcTarget.All,
                            new object[] { steamID, name, delta });
                    }
                    else
                    {
                        pun.TesterUpgradeCommandRPC(steamID, name, delta);
                    }

                    if (startHealth >= 0)
                    {
                        pun.UpdateStat(PlayerHealthDictName, steamID, startHealth);
                    }
                }
            }
        }

        internal static void ApplyScenarioUpgrades(IReadOnlyDictionary<string, int> upgrades)
        {
            if (upgrades == null || upgrades.Count == 0) return;

            try
            {
                var stats = StatsManager.instance;
                var pun = PunManager.instance;
                if (stats == null || pun == null)
                {
                    WLog.Line("scenario_upgrades_error", secret: false,
                        ("err", stats == null ? "no_statsmanager" : "no_punmanager"));
                    return;
                }

                ApplyUpgrades(stats, pun, upgrades);
            }
            catch (Exception e)
            {
                WLog.Line("scenario_upgrades_error", secret: false, ("err", e.Message));
            }
        }

        internal static void RestoreOnLobbyReturn()
        {
            try
            {
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

                var run = RunManager.instance;
                if (run == null) return;

                run.levelsCompleted = 0;
                SemiFunc.StatSetRunLevel(0);

                var config = Plugin.GameConfig;
                if (config != null && config.WerewolfModeEnabled)
                {
                    TryResetSessionState();
                }

                ClearDebugLevel();
                HaulGoalOverride.Clear();
                EnemySetOverride.Clear();
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_restore_error", secret: false, ("err", e.Message));
            }
        }

        private static void TryResetSessionState()
        {
            var stats = StatsManager.instance;
            if (stats == null)
            {
                WLog.Line("worldgen_save_reset_skip", secret: false, ("reason", "no_statsmanager"));
                return;
            }

            string oldSaveName = ReadSaveFileName(stats);
            bool resetStats = false;
            bool deletedDisk = false;
            bool createdNew = false;

            try
            {
                stats.ResetAllStats();
                resetStats = true;
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_save_reset_error", secret: false,
                    ("step", "reset_all_stats"), ("err", e.Message));
            }

            try
            {
                SemiFunc.SaveFileDelete(oldSaveName);
                deletedDisk = true;
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_save_reset_error", secret: false,
                    ("step", "save_file_delete"), ("err", e.Message));
            }

            try
            {
                SemiFunc.SaveFileCreate();
                createdNew = true;
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_save_reset_error", secret: false,
                    ("step", "save_file_create"), ("err", e.Message));
            }

            string newSaveName = ReadSaveFileName(stats);
            WLog.Line("worldgen_save_reset", secret: false,
                ("old", oldSaveName),
                ("new", newSaveName),
                ("resetStats", resetStats ? 1 : 0),
                ("deletedDisk", deletedDisk ? 1 : 0),
                ("createdNew", createdNew ? 1 : 0));
        }

        internal static string GetCurrentSaveFileName()
        {
            try
            {
                var stats = StatsManager.instance;
                if (stats == null) return "";
                return stats.saveFileCurrent ?? "";
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_field_error", secret: false,
                    ("type", nameof(StatsManager)), ("field", "saveFileCurrent"), ("err", e.Message));
                return "";
            }
        }

        private static string ReadSaveFileName(StatsManager stats)
        {
            try
            {
                return stats.saveFileCurrent ?? "";
            }
            catch (Exception e)
            {
                WLog.Line("worldgen_field_error", secret: false,
                    ("type", nameof(StatsManager)), ("field", "saveFileCurrent"), ("err", e.Message));
                return "";
            }
        }
    }
}
