using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(MenuPageLobby), nameof(MenuPageLobby.ButtonStart))]
    internal static class ModIntegrityLobbyStartPatch
    {
        private static bool Prefix(MenuPageLobby __instance)
        {
            WerewolfDirector director = WerewolfDirector.Instance;
            return director == null || director.TryInterceptLobbyStart(__instance);
        }
    }

    [HarmonyPatch(typeof(MenuPlayerListed), "Update")]
    internal static class ModIntegrityPlayerBadgePatch
    {
        private static readonly AccessTools.FieldRef<MenuPlayerListed, PlayerAvatar> PlayerAvatarRef =
            GameRefs.MenuPlayerListed_playerAvatar;
        private static bool _errorLogged;

        private static void Postfix(MenuPlayerListed __instance)
        {
            try
            {
                if (!SemiFunc.RunIsLobbyMenu() || __instance.forceCrown)
                {
                    __instance.GetComponent<ModIntegrityPlayerBadge>()?.Hide();
                    return;
                }

                ModIntegrityPlayerBadge badge = ModIntegrityPlayerBadge.GetOrCreate(__instance);
                PlayerAvatar avatar = PlayerAvatarRef(__instance);
                int actor = avatar != null ? PlayerCrownPatch.ResolveActor(avatar) : -1;
                WerewolfDirector director = WerewolfDirector.Instance;
                if (director == null || actor <= 0 ||
                    !director.TryGetModIntegrityStatus(actor, out ModParticipantRecord record))
                {
                    badge?.Hide();
                    return;
                }
                badge?.SetRecord(record, director.ModIntegrityRevision);
            }
            catch (Exception e)
            {
                if (_errorLogged) return;
                _errorLogged = true;
                WLog.Line("patch_mod_integrity_badge_error", secret: false, ("err", e.Message));
            }
        }
    }
}
