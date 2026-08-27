using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(EnemyParent), "SpawnRPC")]
    internal static class EnemyMapSpawnPatch
    {
        private static void Postfix(EnemyParent __instance)
        {
            try
            {
                EnemyMapIcons.OnSpawn(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_enemymap_error", secret: false, ("via", "spawn"), ("err", e.Message));
            }
        }
    }

    [HarmonyPatch(typeof(EnemyParent), "DespawnRPC")]
    internal static class EnemyMapDespawnPatch
    {
        private static void Postfix(EnemyParent __instance)
        {
            try
            {
                EnemyMapIcons.OnDespawn(__instance);
            }
            catch (Exception e)
            {
                WLog.Line("patch_enemymap_error", secret: false, ("via", "despawn"), ("err", e.Message));
            }
        }
    }
}
