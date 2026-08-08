using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal static class MarkerColors
    {
        public static readonly Color Werewolf = new Color(1f, 0.25f, 0.25f, 1f);
        public static readonly Color Bomber = new Color(1f, 0.6f, 0.3f, 1f);

        public const string WerewolfHex = "#FF4040";
        public const string BomberHex = "#FF994D";

        public static Color ForRole(Role? marked, Color fallback)
        {
            if (marked == Core.Role.Werewolf) return Werewolf;
            if (marked == Core.Role.Bomber) return Bomber;
            return fallback;
        }

        public static string HexFor(Role? marked)
        {
            if (marked == Core.Role.Werewolf) return WerewolfHex;
            if (marked == Core.Role.Bomber) return BomberHex;
            return null;
        }
    }
}
