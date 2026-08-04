using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ChangeLevel))]
    internal static class WipeGuardPatch
    {
        private static bool _blockLogged;

        private static bool _departBlockLogged;

        internal static void ResetLogThrottle()
        {
            _blockLogged = false;
            _departBlockLogged = false;
        }

        private static bool Prefix(bool _completedLevel, bool _levelFailed, RunManager.ChangeLevelType _changeLevelType)
        {
            try
            {
                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive)
                {
                    ResetLogThrottle();
                    return true;
                }

                if (!SemiFunc.RunIsLevel()) return true;

                if (_levelFailed)
                {
                    if (!_blockLogged)
                    {
                        _blockLogged = true;
                        WLog.Line("wipe_transition_blocked", secret: false);
                    }
                    return false;
                }

                if (_completedLevel && _changeLevelType == RunManager.ChangeLevelType.Normal)
                {
                    if (!_departBlockLogged)
                    {
                        _departBlockLogged = true;
                        WLog.Line("depart_transition_blocked", secret: false);
                    }
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                WLog.Line("wipe_guard_error", secret: false, ("err", e.Message));
                return true;
            }
        }
    }
}
