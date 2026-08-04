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
        private static bool _firstTickLogged;

        private static void Postfix(RunManager __instance)
        {
            if (!_firstTickLogged)
            {
                _firstTickLogged = true;
                WLog.Line("director_watchdog_first_tick", secret: false,
                    ("directorAlive", WerewolfDirector.Instance != null),
                    ("scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            }

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
