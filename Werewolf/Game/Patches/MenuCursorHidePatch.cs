using HarmonyLib;
using Werewolf.UI;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(MenuCursor), nameof(MenuCursor.Show))]
    internal static class MenuCursorHidePatch
    {
        private static bool Prefix()
        {
            return !CursorMirror.SuppressVanillaCursor;
        }
    }
}
