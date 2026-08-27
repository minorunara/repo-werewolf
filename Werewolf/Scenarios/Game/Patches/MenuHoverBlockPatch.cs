using HarmonyLib;
using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.Scenarios.UI;
using Werewolf.UI;

namespace Werewolf.Scenarios.Game.Patches
{
    [HarmonyPatch(typeof(SemiFunc), nameof(SemiFunc.UIMouseHover))]
    internal static class MenuHoverBlockPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!ScenarioSelectPanel.SuppressVanillaMenuHover) return true;
            __result = false;
            return false;
        }
    }
}
