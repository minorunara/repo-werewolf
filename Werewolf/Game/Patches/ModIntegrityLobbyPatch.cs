using System;
using HarmonyLib;
using Werewolf.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(MenuPageLobby), nameof(MenuPageLobby.ButtonStart))]
    internal static class ModIntegrityLobbyStartPatch
    {
        private static bool Prefix(MenuPageLobby __instance)
        {
            WerewolfDirector director = WerewolfDirector.Instance;
            return director == null
                || director.TryInterceptLobbyStart(__instance != null ? new LobbyStartPage(__instance) : null);
        }
    }

    [HarmonyPatch(typeof(MenuPlayerListed), "Update")]
    internal static class ModIntegrityPlayerBadgePatch
    {
        private static AccessTools.FieldRef<MenuPlayerListed, PlayerAvatar> PlayerAvatarRef =>
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

                ModIntegrityPlayerBadge badge = ModIntegrityPlayerBadge.GetOrCreate(__instance.gameObject,
                    __instance.playerName != null ? __instance.playerName.rectTransform : null);
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
