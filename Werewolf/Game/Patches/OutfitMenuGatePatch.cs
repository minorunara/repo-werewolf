using System;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(MenuPageEsc), "ButtonEventChangeColor")]
    internal static class OutfitMenuGatePatch
    {
        private static bool Prefix()
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null) return true;

                if (!OutfitRules.ShouldBlockOutfitChange(dir.ClientPhase, dir.ClientOutfitChangeAllowed))
                    return true;

                MenuManager menu = MenuManager.instance;
                if (menu != null)
                {
                    menu.PagePopUp(
                        Texts.Get(TextId.OutfitBlockedHeader),
                        new Color(0.8f, 0.1f, 0.1f),
                        Texts.Get(TextId.OutfitBlockedBody),
                        Texts.Get(TextId.OutfitBlockedOk),
                        false);
                }

                WLog.Line("outfit_change_blocked", secret: false, ("phase", dir.ClientPhase));
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_outfit_menu_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
