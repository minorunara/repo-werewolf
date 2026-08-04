using System;
using System.Collections.Generic;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(MenuPlayerListed), "Update")]
    internal static class MenuPlayerCrownPatch
    {
        private static readonly AccessTools.FieldRef<MenuPlayerListed, PlayerAvatar> PlayerAvatarRef =
            GameRefs.MenuPlayerListed_playerAvatar;

        private static bool _errorLogged;
        private static int _loggedVersion = -1;
        private static readonly HashSet<int> _loggedActors = new HashSet<int>();

        private static void Postfix(MenuPlayerListed __instance)
        {
            try
            {
                if (!CrownRoster.HasWinners) return;
                if (__instance.forceCrown) return;

                PlayerAvatar avatar = PlayerAvatarRef(__instance);
                if (avatar == null) return;

                int actor = PlayerCrownPatch.ResolveActor(avatar);
                if (!CrownRoster.IsWinner(actor)) return;

                bool changed = false;
                if (__instance.leftCrown != null && !__instance.leftCrown.activeSelf)
                {
                    __instance.leftCrown.SetActive(true);
                    changed = true;
                }
                if (__instance.rightCrown != null && !__instance.rightCrown.activeSelf)
                {
                    __instance.rightCrown.SetActive(true);
                    changed = true;
                }
                if (changed)
                {
                    if (_loggedVersion != CrownRoster.Version)
                    {
                        _loggedVersion = CrownRoster.Version;
                        _loggedActors.Clear();
                    }
                    if (_loggedActors.Add(actor))
                    {
                        WLog.Line("menu_crown_forced", secret: false, ("actor", actor));
                    }
                }
            }
            catch (Exception e)
            {
                if (_errorLogged) return;
                _errorLogged = true;
                WLog.Line("patch_menu_crown_error", secret: false, ("err", e.Message));
            }
        }
    }
}
