using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Tests
{
    internal static class VanillaEmojiNames
    {
        public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            ":P", ":D", ":O", "angry", ":|", ":)", "$$", "$", "bang", "X", "check", "!", "?",
            "skull", ":/", "blush", "truck", "truck2", "arrowleft", "arrowright", "heart",
            "heartbreak", "thumbsup", "thumbsdown", ";)", ":'(", "hush", "dead", "deal", "$$$",
            "ghost", "headman", "headman2", "hearteyes", "think", "road2", "road",
            "cryinglaughing", "creepycrying", "blank", "drone_battery", "drone_feather", "dot",
            "drone_heal", "drone_indestructible", "drone_torque", "drone_zero_gravity",
            "orb_battery", "orb_feather", "orb_heal", "orb_torque", "orb_indestructible",
            "orb_zero_gravity", "poof", "poof2", "rocketfire", "rocketfire2", "fist",
            "thumbsup2", "thumbsdown2", "eyesright", "eyesleft", "eye", "eyecry", "mouthsmile",
            "mouthhappy", "mouthsad", "mouthsurprised", "eyeblank", "eyesmall", "eyeheart",
            "eyeclosed", "eyedead", "mouth", "pointright", "pointleft", "orb_magnet",
            "grenade_explosive", "grenade_stun", "weapon_baseball_bat", "weapon_sledgehammer",
            "weapon_frying_pan", "weapon_sword", "weapon_inflatable_hammer", "sleep", "yawn",
            "nap", "speechless", "shades", "fedup", "horror", ":(", "mellow", "player",
            "settings", "map", "arrow_right_blue", "right_click", "left_click", "mouse",
            "backpack", "on", "off", "box_left", "box_right", "invite", "send", "message_arrow",
            "message", "shoppingcart", "shop", "thief", "police", "jail", "handcuffs",
            "register", "burnmoney", "cart", "poop", "poopeyes", "fire", "point", "surplus",
            "moneyeyes", "deduct", "dorment", "inventoryempty", "item_health_pack_S",
            "item_health_pack_M", "item_health_pack_L", "item_gun_shotgun", "item_gun_handgun",
            "item_gun_tranq", "clock", "item_valuable_tracker", "item_grenade_human",
            "item_grenade_duct_taped", "item_rubber_duck", "item_extraction_tracker",
            "item_grenade_shockwave", "item_mine_explosive", "extraction", "pleading",
            "blush_smile", "dry", "smh", "disappointed", "kiss", "weary", "wave", "finger_wag",
            "pray", "bone", "sweat", "clown", "disappointed2", "sparkle", "give_right",
            "give_left", "evil_smile", "dead_clown", "scared", "slight_smile", "spiral",
            "realistic_eye", "squiggle", "grave", "blood", "void", "void_eye", "nasty_lips",
            "evil_mouth", "weird_eye", "weird_mouth", "semibots", "deathhead",
        };

        public static IEnumerable<string> Problems(string text)
        {
            foreach (string name in VanillaEmojiToken.Names(text))
            {
                if (name == null) yield return "[[ に対する ]] が無い";
                else if (!All.Contains(name)) yield return $"[[{name}]]";
            }
        }
    }
}
