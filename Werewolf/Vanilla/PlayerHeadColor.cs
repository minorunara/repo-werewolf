using System;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Core.Replay;

namespace Werewolf.Vanilla
{
    internal static class PlayerHeadColor
    {
        private static bool _errorLogged;

        internal static string HexOfLocal() => HexOf(PlayerAvatar.instance);

        internal static string HexOf(PlayerAvatar avatar)
        {
            try
            {
                if (avatar == null || avatar.playerCosmetics == null || MetaManager.instance == null) return null;
                return Hex(SemiFunc.PlayerGetColorMain(avatar));
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    WLog.Line("head_color_error", secret: false, ("err", e.Message));
                }
                return null;
            }
        }

        internal static string HexForBot(int actor)
        {
            if (actor >= 0) return null;
            return Hex(BotColors.ColorAt(BotColors.IndexFor(actor)));
        }

        private static string Hex(Color c) => ReplayMarkerPalette.ToHex(c.r, c.g, c.b);
    }
}
