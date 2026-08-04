using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
    internal static class PlayerHurtPatch
    {
        private static bool Prefix(ref bool savingGrace, int damage)
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null) return true;

                bool overridden = CombatRules.OverrideSavingGrace(dir.ClientPhase, savingGrace);
                if (overridden != savingGrace && savingGrace)
                {
                    WLog.Line("savinggrace_off", secret: false,
                        ("phase", dir.ClientPhase), ("damage", damage));
                }

                savingGrace = overridden;
                return true;
            }
            catch (Exception e)
            {
                WLog.Line("patch_playerhurt_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
