using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
    internal static class LevelPatch
    {
        private static void Prefix(bool _completedLevel, bool _levelFailed, RunManager.ChangeLevelType _changeLevelType)
        {
            try
            {
                if (_changeLevelType == RunManager.ChangeLevelType.LobbyMenu) return;

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
                if (dir.HostPhase != GamePhase.Play && dir.HostPhase != GamePhase.Meeting) return;
                if (!SemiFunc.RunIsLevel()) return;

                dir.HostNotifyExtraction(_completedLevel, _levelFailed);
            }
            catch (Exception e)
            {
                WLog.Line("patch_level_error", secret: false, ("err", e.Message));
            }
        }
    }
}
