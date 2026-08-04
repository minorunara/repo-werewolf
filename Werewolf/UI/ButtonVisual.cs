using UnityEngine;

namespace Werewolf.UI
{
    internal readonly struct ButtonPalette
    {
        public ButtonPalette(Color enabledBg, Color hoverBg, Color disabledBg, Color disabledLabel)
        {
            EnabledBg = enabledBg;
            HoverBg = hoverBg;
            DisabledBg = disabledBg;
            DisabledLabel = disabledLabel;
        }

        public Color EnabledBg { get; }
        public Color HoverBg { get; }
        public Color DisabledBg { get; }
        public Color DisabledLabel { get; }
    }

    internal static class ButtonVisual
    {
        public static readonly Color ArmedBg = new Color(0.78f, 0.55f, 0.08f, 0.95f);

        public static readonly Color ArmedHoverBg = new Color(0.92f, 0.7f, 0.18f, 1f);

        public static void Resolve(ButtonPalette palette, bool armed, bool hover, bool selected, bool enabled,
                                   out Color bg, out Color label)
        {
            if (armed)
            {
                bg = hover ? ArmedHoverBg : ArmedBg;
                label = Color.white;
            }
            else if (hover && enabled)
            {
                bg = palette.HoverBg;
                label = Color.white;
            }
            else if (selected)
            {
                bg = ArmedBg;
                label = Color.white;
            }
            else if (enabled)
            {
                bg = palette.EnabledBg;
                label = Color.white;
            }
            else
            {
                bg = palette.DisabledBg;
                label = palette.DisabledLabel;
            }
        }
    }
}
