namespace Werewolf.Core
{
    public static class RoleText
    {
        public static string Label(Role role)
        {
            switch (role)
            {
                case Role.Werewolf: return Texts.Get(TextId.RoleNameWerewolf);
                case Role.BlackCat: return Texts.Get(TextId.RoleNameBlackCat);
                case Role.Villager: return Texts.Get(TextId.RoleNameVillager);
                case Role.Bomber: return Texts.Get(TextId.RoleNameBomber);
                case Role.Shaman: return Texts.Get(TextId.RoleNameShaman);
                default: return role.ToString();
            }
        }
    }
}
