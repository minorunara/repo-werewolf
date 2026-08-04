using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(PlayerCrown), "FetchLogic")]
    internal static class PlayerCrownPatch
    {
        private static readonly AccessTools.FieldRef<PlayerCrown, PlayerAvatar> PlayerAvatarRef =
            GameRefs.PlayerCrown_playerAvatar;

        private static readonly AccessTools.FieldRef<PlayerCrown, bool> ActiveRef =
            GameRefs.PlayerCrown_active;

        private static bool _errorLogged;

        private static int _loggedVersion = -1;
        private static readonly System.Collections.Generic.HashSet<int> _loggedActors =
            new System.Collections.Generic.HashSet<int>();

        private static void Postfix(PlayerCrown __instance)
        {
            try
            {
                if (!CrownRoster.HasWinners) return;

                PlayerAvatar avatar = PlayerAvatarRef(__instance);
                if (avatar == null) return;

                int actor = ResolveActor(avatar);
                if (!CrownRoster.IsWinner(actor)) return;

                ActiveRef(__instance) = true;
                if (__instance.crownMesh != null && !__instance.crownMesh.gameObject.activeSelf)
                {
                    __instance.crownMesh.gameObject.SetActive(true);
                    if (_loggedVersion != CrownRoster.Version)
                    {
                        _loggedVersion = CrownRoster.Version;
                        _loggedActors.Clear();
                    }
                    if (_loggedActors.Add(actor))
                    {
                        WLog.Line("crown_forced", secret: false, ("actor", actor));
                    }
                }
            }
            catch (Exception e)
            {
                if (_errorLogged) return;
                _errorLogged = true;
                WLog.Line("patch_crown_error", secret: false, ("err", e.Message));
            }
        }

        internal static int ResolveActor(PlayerAvatar avatar)
        {
            if (!SemiFunc.IsMultiplayer()) return 1;
            if (avatar.photonView != null && avatar.photonView.Owner != null)
            {
                return avatar.photonView.Owner.ActorNumber;
            }
            return -1;
        }
    }
}
