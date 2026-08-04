using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(EnemyParent), "Spawn")]
    internal static class EnemySpawnPatch
    {
        internal static volatile bool SuppressSpawns;

        private static bool Prefix()
        {
            try
            {
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
