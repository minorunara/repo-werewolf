using System;
using System.Collections.Generic;
using System.Globalization;

namespace Werewolf.Core
{
    public static class ChatEmoji
    {
        public const int VariationSelector16 = 0xFE0F;

        private static readonly Dictionary<int, string> _spriteKeys = new Dictionary<int, string>
        {
            [0x1F465] = "emoji_people",
            [0x1F47B] = "emoji_ghost",
            [0x274C]  = "emoji_cross_mark",
            [0x1F494] = "emoji_broken_heart",
            [0x1F528] = "emoji_hammer",
            [0x1F4B0] = "emoji_money_bag",
            [0x1F3AF] = "emoji_bullseye",
            [0x1F4E1] = "emoji_satellite_antenna",
            [0x1F448] = "emoji_backhand_index_left",
            [0x1F479] = "emoji_ogre",
            [0x1F4A8] = "emoji_dashing_away",
            [0x1F514] = "emoji_bell",
        };

        private static readonly Dictionary<TextId, string> _templates = new Dictionary<TextId, string>
        {
            [TextId.ChatLogScatterTitle] = "👥",
            [TextId.ChatLogMeetingNumberFormat] = "🔔 {0}",
            [TextId.RecapDeathsFormat] = "👻: {0}",
            [TextId.RecapDeathsNone] = "👻: ❌",
            [TextId.RecapLostFormat] = "💔🔨: -${0}",
            [TextId.RecapHaulFormat] = "💰: ${0} ／ 🎯 ${1}",
            [TextId.RecapBeaconFormat] = "📡👈👹💨: {0}",
            [TextId.RecapBeaconNone] = "📡👈👹💨: ❌",
        };

        public static IReadOnlyDictionary<int, string> SpriteKeys => _spriteKeys;

        public static IReadOnlyDictionary<TextId, string> Templates => _templates;

        public static string Template(TextId id)
            => _templates.TryGetValue(id, out string template) ? template : null;

        public static string Get(TextId id, bool emoji)
        {
            if (emoji && _templates.TryGetValue(id, out string template)) return template;
            return Texts.Get(id);
        }

        public static string Format(TextId id, bool emoji, params object[] args)
        {
            if (emoji && _templates.TryGetValue(id, out string template))
            {
                if (args == null || args.Length == 0) return template;
                try
                {
                    return string.Format(CultureInfo.InvariantCulture, template, args);
                }
                catch (FormatException)
                {
                }
            }
            return Texts.Format(id, args);
        }
    }
}
