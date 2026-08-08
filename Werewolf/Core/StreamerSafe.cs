using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class StreamerSafe
    {
        private static readonly Dictionary<string, string> Overrides = new Dictionary<string, string>
        {
            { "perk_bomb_plant", null },
            { "perk_bomb_detonate", null },
            { "sfx_notice_convene", NoticeSfx.DefaultClipKey },
            { "sfx_execution", null },
            { "sfx_execution_curse", null },
        };

        public static bool TryResolve(bool safeMode, string assetKey, out string replacementKey)
        {
            replacementKey = null;
            if (!safeMode || string.IsNullOrEmpty(assetKey)) return false;
            return Overrides.TryGetValue(assetKey, out replacementKey);
        }
    }
}
