using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class BotColors
    {
        private const float MinSaturation = 0.6f;
        private const float MinValue = 0.8f;

        private const float StartHue = 0f;

        private static List<SemiColor> _rankedPalette;
        private static int[] _ranked = new int[0];

        internal static int IndexFor(int actor)
        {
            List<SemiColor> colors = Palette();
            if (colors == null || colors.Count == 0) return 0;
            int[] ranked = Ranked(colors);
            int rank = Mathf.Abs(WPlayer.FirstBotActor - actor);
            return ranked.Length > 0 ? ranked[rank % ranked.Length] : rank % colors.Count;
        }

        internal static Color ColorAt(int colorIndex)
        {
            List<SemiColor> colors = Palette();
            if (colors == null || colorIndex < 0 || colorIndex >= colors.Count) return Color.white;
            return colors[colorIndex].color;
        }

        private static List<SemiColor> Palette()
        {
            return MetaManager.instance != null ? MetaManager.instance.colors : null;
        }

        private static int[] Ranked(List<SemiColor> colors)
        {
            if (ReferenceEquals(colors, _rankedPalette)) return _ranked;

            var indices = new List<int>();
            var hues = new List<float>();
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] == null) continue;
                Color.RGBToHSV(colors[i].color, out float h, out float s, out float v);
                if (s < MinSaturation || v < MinValue) continue;
                indices.Add(i);
                hues.Add(h * 360f);
            }

            int[] order = HueSpread.Order(hues, StartHue);
            var ranked = new int[order.Length];
            for (int n = 0; n < order.Length; n++) ranked[n] = indices[order[n]];

            _rankedPalette = colors;
            _ranked = ranked;
            return ranked;
        }
    }
}
