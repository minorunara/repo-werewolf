using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(RunManager), "Update")]
    internal static class DirectorRevivePatch
    {
        private const int MaxRevives = 10;

        private static int _reviveCount;
        private static bool _disabled;

        private static void Postfix(RunManager __instance)
        {
            if (_disabled || WerewolfDirector.Instance != null) return;
            try
            {
                if (_reviveCount >= MaxRevives)
                {
                    _disabled = true;
                    WLog.Line("director_revive_stopped", secret: false, ("count", _reviveCount));
                    return;
                }
                _reviveCount++;
                __instance.gameObject.AddComponent<WerewolfDirector>();
                WLog.Line("director_revived", secret: false,
                    ("host", "RunManager"), ("count", _reviveCount));
            }
            catch (Exception e)
            {
                _disabled = true;
                WLog.Line("director_revive_error", secret: false, ("err", e.Message));
            }
        }
    }
}
