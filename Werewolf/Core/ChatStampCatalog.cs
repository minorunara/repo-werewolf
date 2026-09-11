using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum ChatStampCategory : byte
    {
        Guide = 0,

        Press = 1,

        Defense = 2,

        Claim = 3,

        Chatter = 4,
    }

    public readonly struct ChatStampDef
    {
        public ChatStampDef(byte id, ChatStampCategory category, string emoji, TextId textId)
        {
            Id = id;
            Category = category;
            Emoji = emoji ?? string.Empty;
            TextId = textId;
        }

        public byte Id { get; }

        public ChatStampCategory Category { get; }

        public string Emoji { get; }

        public TextId TextId { get; }
    }

    public readonly struct ChatStampCategoryDef
    {
        public ChatStampCategoryDef(ChatStampCategory category, TextId textId,
                                    string iconKey, float iconRotationZ = 0f)
        {
            Category = category;
            TextId = textId;
            IconKey = iconKey ?? string.Empty;
            IconRotationZ = iconRotationZ;
        }

        public ChatStampCategory Category { get; }

        public TextId TextId { get; }

        public string IconKey { get; }

        public float IconRotationZ { get; }
    }

    public static class ChatStampCatalog
    {
        private static readonly ChatStampDef[] _defs =
        {
            new ChatStampDef(1, ChatStampCategory.Guide, "", TextId.ChatStampGuideVoteSkip),
            new ChatStampDef(2, ChatStampCategory.Guide, "🎯", TextId.ChatStampGuideVoteSuspect),
            new ChatStampDef(3, ChatStampCategory.Guide, "💰", TextId.ChatStampGuideExtraction),
            new ChatStampDef(4, ChatStampCategory.Guide, "", TextId.ChatStampGuideFriendly),
            new ChatStampDef(5, ChatStampCategory.Press, "", TextId.ChatStampPressSuspicious),
            new ChatStampDef(6, ChatStampCategory.Press, "", TextId.ChatStampPressWhere),
            new ChatStampDef(7, ChatStampCategory.Press, "", TextId.ChatStampPressWithWhom),
            new ChatStampDef(8, ChatStampCategory.Press, "", TextId.ChatStampPressDoing),
            new ChatStampDef(9, ChatStampCategory.Press, "", TextId.ChatStampPressItems),
            new ChatStampDef(10, ChatStampCategory.Press, "🔨", TextId.ChatStampPressDropWeapon),
            new ChatStampDef(11, ChatStampCategory.Defense, "", TextId.ChatStampDefenseInnocent),
            new ChatStampDef(12, ChatStampCategory.Defense, "👻", TextId.ChatStampDefenseWasDead),
            new ChatStampDef(13, ChatStampCategory.Defense, "", TextId.ChatStampDefenseAccident),
            new ChatStampDef(14, ChatStampCategory.Defense, "", TextId.ChatStampDefenseDontKnow),
            new ChatStampDef(15, ChatStampCategory.Defense, "", TextId.ChatStampDefenseForgot),
            new ChatStampDef(16, ChatStampCategory.Defense, "", TextId.ChatStampDefenseNoIdea),
            new ChatStampDef(17, ChatStampCategory.Claim, "", TextId.ChatStampClaimAgree),
            new ChatStampDef(18, ChatStampCategory.Claim, "❌", TextId.ChatStampClaimDisagree),
            new ChatStampDef(19, ChatStampCategory.Claim, "", TextId.ChatStampClaimObjection),
            new ChatStampDef(20, ChatStampCategory.Claim, "👈", TextId.ChatStampClaimTheyDidIt),
            new ChatStampDef(21, ChatStampCategory.Claim, "", TextId.ChatStampClaimItWasMe),
            new ChatStampDef(22, ChatStampCategory.Claim, "👥", TextId.ChatStampClaimTogether),
            new ChatStampDef(23, ChatStampCategory.Chatter, "", TextId.ChatStampChatterMorning),
            new ChatStampDef(24, ChatStampCategory.Chatter, "", TextId.ChatStampChatterGg),
            new ChatStampDef(25, ChatStampCategory.Chatter, "", TextId.ChatStampChatterWantLive),
            new ChatStampDef(26, ChatStampCategory.Chatter, "👻", TextId.ChatStampChatterDied),
            new ChatStampDef(27, ChatStampCategory.Chatter, "", TextId.ChatStampChatterLookAtMe),
            new ChatStampDef(28, ChatStampCategory.Chatter, "", TextId.ChatStampChatterAsPlanned),
            new ChatStampDef(29, ChatStampCategory.Chatter, "💨", TextId.ChatStampChatterWhatever),
            new ChatStampDef(30, ChatStampCategory.Chatter, "", TextId.ChatStampChatterSorry),
            new ChatStampDef(31, ChatStampCategory.Chatter, "", TextId.ChatStampChatterHelp),
            new ChatStampDef(32, ChatStampCategory.Chatter, "😭", TextId.ChatStampChatterCry),
        };

        private static readonly ChatStampCategoryDef[] _categoryDefs =
        {
            new ChatStampCategoryDef(ChatStampCategory.Guide, TextId.ChatStampCategoryGuide,
                "emoji_speech_balloon"),
            new ChatStampCategoryDef(ChatStampCategory.Press, TextId.ChatStampCategoryPress,
                "emoji_dagger", iconRotationZ: 180f),
            new ChatStampCategoryDef(ChatStampCategory.Defense, TextId.ChatStampCategoryDefense,
                "emoji_shield"),
            new ChatStampCategoryDef(ChatStampCategory.Claim, TextId.ChatStampCategoryClaim,
                "emoji_right_anger_bubble"),
            new ChatStampCategoryDef(ChatStampCategory.Chatter, TextId.ChatStampCategoryChatter,
                "emoji_hot_beverage"),
        };

        private static readonly Dictionary<byte, ChatStampDef> _byId = BuildIndex();

        private static Dictionary<byte, ChatStampDef> BuildIndex()
        {
            var map = new Dictionary<byte, ChatStampDef>(_defs.Length);
            foreach (ChatStampDef def in _defs) map.Add(def.Id, def);
            return map;
        }

        private static readonly Dictionary<ChatStampCategory, ChatStampDef[]> _byCategory = BuildCategoryIndex();

        private static Dictionary<ChatStampCategory, ChatStampDef[]> BuildCategoryIndex()
        {
            var lists = new Dictionary<ChatStampCategory, List<ChatStampDef>>();
            foreach (ChatStampDef def in _defs)
            {
                if (!lists.TryGetValue(def.Category, out List<ChatStampDef> list))
                {
                    lists[def.Category] = list = new List<ChatStampDef>();
                }
                list.Add(def);
            }
            var map = new Dictionary<ChatStampCategory, ChatStampDef[]>(lists.Count);
            foreach (KeyValuePair<ChatStampCategory, List<ChatStampDef>> pair in lists)
            {
                map[pair.Key] = pair.Value.ToArray();
            }
            return map;
        }

        public static IReadOnlyList<ChatStampCategoryDef> CategoryDefs => _categoryDefs;

        public static IReadOnlyList<ChatStampDef> All => _defs;

        public static bool TryGet(byte id, out ChatStampDef def) => _byId.TryGetValue(id, out def);

        public static IReadOnlyList<ChatStampDef> ForCategory(ChatStampCategory category)
            => _byCategory.TryGetValue(category, out ChatStampDef[] defs)
                ? defs
                : Array.Empty<ChatStampDef>();

        public static string ChatText(byte id)
        {
            if (!TryGet(id, out ChatStampDef def)) return Texts.Get(TextId.ChatStampUnknown);
            string text = Texts.Get(def.TextId);
            return def.Emoji.Length > 0 ? def.Emoji + " " + text : text;
        }

        public static string ReplayText(byte id)
            => TryGet(id, out ChatStampDef def)
                ? Texts.Get(def.TextId)
                : Texts.Get(TextId.ChatStampUnknown);
    }
}
