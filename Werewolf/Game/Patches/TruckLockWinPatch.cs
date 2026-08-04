using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    [HarmonyPatch(typeof(TruckScreenText), "PlayerChatBoxStateUpdateRPC")]
    internal static class TruckLockWinPatch
    {
        private static void Postfix(TruckScreenText.PlayerChatBoxState _state)
        {
            try
            {
                if (_state != TruckScreenText.PlayerChatBoxState.LockedStartingTruck) return;

                WerewolfDirector dir = WerewolfDirector.Instance;
                if (dir == null || !dir.IsHostSessionActive) return;
                if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
                if (dir.HostPhase != GamePhase.Play && dir.HostPhase != GamePhase.Meeting) return;
                if (!SemiFunc.RunIsLevel()) return;

                dir.HostNotifyMailDeparture();
                WLog.Line("truck_lock_win", secret: false,
                    ("via", "chat_box_state_rpc"), ("phase", dir.HostPhase));
            }
            catch (Exception e)
            {
                WLog.Line("patch_truck_lock_error", secret: false, ("err", e.Message));
            }
        }
    }
}
