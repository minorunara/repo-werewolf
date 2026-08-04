using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(PhysGrabber), "DiscoverLogic")]
    internal static class ValuableDiscoverSuppressPatch
    {
        private static bool Prefix(PhysGrabber __instance, ValuableObject _valuableObject)
        {
            try
            {
                if (_valuableObject == null) return true;
                if (__instance == null || !__instance.isLocal) return true;

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.LocalValuableDiscoverSuppressed) return true;

                dir.MaybeShowTutorial(TutorialId.ValuableRecordSuppressed);
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("patch_valuable_discover_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
