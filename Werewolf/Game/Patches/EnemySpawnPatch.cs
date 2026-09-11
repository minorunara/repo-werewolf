using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(LevelGenerator), "EnemySetup")]
    internal static class PreplayEnemySetupPatch
    {
        private static void Prefix()
        {
            try
            {
                WerewolfDirector.Instance?.OnEnemySetupStarted();
            }
            catch (Exception e)
            {
                WLog.Line("patch_preplay_enemysetup_error", secret: false, ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(EnemyParent), "Spawn")]
    internal static class EnemySpawnPatch
    {
        internal static volatile bool SuppressSpawns;

        private static bool Prefix(EnemyParent __instance)
        {
            try
            {
                if (PreplayEnemySpawnHold.ShouldBlock(__instance, SessionClock.NowMs())) return false;
                if (SuppressSpawns) return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_enemyspawn_error", secret: false, ("err", e.Message));
            }
            return true;
        }
    }
}
