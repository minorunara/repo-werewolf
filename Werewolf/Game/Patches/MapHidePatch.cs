using System;
using System.Collections.Generic;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game.Patches
{
    internal static class MapHidePatch
    {
        private static readonly AccessTools.FieldRef<PlayerAvatar, PlayerDeathHead> DeathHeadRef =
            GameRefs.PlayerAvatar_playerDeathHead;

        internal static bool GateOpen()
        {
            WerewolfDirector dir = WerewolfDirector.Instance;
            if (dir == null) return false;
            return MapHideGate.ShouldSuppress(dir.IsRoundActiveClient, dir.ClientMinimapHideEnabled);
        }

        internal static void Tick()
        {
            try
            {
                if (!GateOpen()) return;

                GameDirector gd = GameDirector.instance;
                if (gd == null) return;
                List<PlayerAvatar> players = gd.PlayerList;
                if (players == null) return;

                for (int i = 0; i < players.Count; i++)
                {
                    PlayerAvatar avatar = players[i];
                    if (avatar == null) continue;
                    PlayerDeathHead head = DeathHeadRef(avatar);
                    if (head == null) continue;
                    MapCustom mc = head.mapCustom;
                    if (mc == null) continue;
                    mc.Hide();
                }
            }
            catch (Exception e)
            {
                WLog.Line("maphide_tick_error", secret: false, ("err", e.Message));
            }
        }
    }
}
