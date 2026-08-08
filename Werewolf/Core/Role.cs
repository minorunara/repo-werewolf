using System;

namespace Werewolf.Core
{
    public enum Role : byte
    {
        Villager = 0,
        Werewolf = 1,
        BlackCat = 2,
        Bomber = 3,
        Shaman = 4,
    }

    public enum Team : byte
    {
        Villagers = 0,
        Werewolves = 1,
    }

    public static class TeamCodes
    {
        public const byte VoidMatch = 255;
    }

    public static class RoleDistribution
    {
        public static Team TeamOf(Role role)
        {
            switch (role)
            {
                case Role.Villager:
                case Role.Shaman:
                    return Team.Villagers;
                case Role.Werewolf:
                case Role.BlackCat:
                case Role.Bomber:
                    return Team.Werewolves;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "未知の役職です。");
            }
        }
    }
}
