using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Scenarios.Core;

namespace Werewolf.Scenarios.Game.Patches
{
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
    internal static class ScenarioDamageClampPatch
    {
        [HarmonyPriority(Priority.Low)]
        private static bool Prefix(PlayerHealth __instance, ref int damage)
        {
            try
            {
                if (!ScenarioRun.DamageClamped || damage <= 0) return true;
                if (!IsLocalPlayerHealth(__instance)) return true;
                if (GameRefs.PlayerHealth_health == null) return true;

                int current = GameRefs.PlayerHealth_health(__instance);
                int clamped = ScenarioSafetyRules.ClampDamage(current, damage);
                if (clamped <= 0) return false;
                if (clamped == damage) return true;

                WLog.Line("scenario_damage_clamped", secret: false,
                    ("health", current), ("damage", damage), ("clamped", clamped));
                damage = clamped;
                return true;
            }
            catch (Exception e)
            {
                WLog.Line("patch_scenario_damage_error", secret: false, ("err", e.Message));
                return true;
            }
        }

        private static bool IsLocalPlayerHealth(PlayerHealth health)
        {
            PlayerAvatar local = PlayerAvatar.instance;
            return health != null && local != null && local.playerHealth == health;
        }
    }
}
