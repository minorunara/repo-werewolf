using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    internal static class CosmeticBoxSpawnPatch
    {
        internal static bool Armed;

        private static bool _blockLogged;

        private static readonly AccessTools.FieldRef<RunManager, bool> RestartingRef =
            GameRefs.RunManager_restarting;

        private static bool IsRunLevelTransition(RunManager run, RunManager.ChangeLevelType changeLevelType)
        {
            return changeLevelType == RunManager.ChangeLevelType.RunLevel
                || (changeLevelType == RunManager.ChangeLevelType.Normal && run.levelCurrent == run.levelLobby);
        }

        internal static void LogBlockOnce()
        {
            if (_blockLogged) return;
            _blockLogged = true;
            WLog.Line("cosmetic_spawn_blocked", secret: false);
        }

        private static void ResetLogThrottle()
        {
            _blockLogged = false;
        }

        internal static void ReevaluateGate(RunManager run, RunManager.ChangeLevelType changeLevelType)
        {
            if (run == null) return;
            if (RestartingRef != null && RestartingRef(run)) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!IsRunLevelTransition(run, changeLevelType)) return;

            Plugin.RefreshGameConfig();
            var config = Plugin.GameConfig;
            Armed = config != null && config.WerewolfModeEnabled;
            ResetLogThrottle();
        }
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ChangeLevel))]
    internal static class CosmeticBoxGatePatch
    {
        private static void Prefix(RunManager __instance, RunManager.ChangeLevelType _changeLevelType)
        {
            try
            {
                CosmeticBoxSpawnPatch.ReevaluateGate(__instance, _changeLevelType);
            }
            catch (Exception e)
            {
                WLog.Line("cosmetic_gate_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(ValuableDirector), "SpawnCosmeticWorldObject")]
    internal static class CosmeticBoxSkipPatch
    {
        private static bool Prefix()
        {
            try
            {
                if (!CosmeticBoxSpawnPatch.Armed) return true;

                CosmeticBoxSpawnPatch.LogBlockOnce();
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("cosmetic_skip_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
