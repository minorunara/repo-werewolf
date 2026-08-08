using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(WorldSpaceUIPlayerName), "Update")]
    internal static class NameMarkerPatch
    {

        private static void Postfix(WorldSpaceUIPlayerName __instance)
        {
            try
            {
                if (__instance == null) return;

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null) return;
                Role? localRole = dir.LocalRoleClient;
                if (localRole != Role.BlackCat && localRole != Role.Werewolf && localRole != Role.Bomber) return;

                PlayerAvatar avatar = ResolvePlayerAvatar(__instance);
                if (avatar == null) return;

                Role? marked = dir.MarkedTeammateRoleForAvatar(avatar);
                if (marked == null) return;

                TextMeshProUGUI text = __instance.text;
                if (text == null) return;

                Color current = text.color;
                Color tint = marked == Role.Bomber ? MarkerColors.Bomber : MarkerColors.Werewolf;
                tint.a = current.a;
                text.color = tint;
            }
            catch (Exception e)
            {
                WLog.Line("patch_name_marker_error", secret: false, ("err", e.Message));
            }
        }

        private static PlayerAvatar ResolvePlayerAvatar(WorldSpaceUIPlayerName instance)
        {
            return GameRefs.WorldSpaceUIPlayerName_playerAvatar(instance);
        }
    }
}
