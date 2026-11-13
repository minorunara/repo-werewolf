#nullable enable
using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum ConfigIssue : byte
    {
        WerewolfCountExceedsPlayers = 1,

        RoundSecondsNotPositive = 2,
    }

    public static class ConfigValidator
    {
        public static bool WerewolfCountExceedsPlayers(int werewolfCount, int playerCount)
        {
            return werewolfCount >= playerCount;
        }

        public static IReadOnlyList<ConfigIssue> Validate(GameConfig config, int playerCount)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var issues = new List<ConfigIssue>();

            if (WerewolfCountExceedsPlayers(config.WerewolfCount, playerCount))
            {
                issues.Add(ConfigIssue.WerewolfCountExceedsPlayers);
            }

            if (config.RoundSeconds <= 0)
            {
                issues.Add(ConfigIssue.RoundSecondsNotPositive);
            }

            return issues;
        }
    }
}
