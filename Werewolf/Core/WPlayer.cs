namespace Werewolf.Core
{
    public enum DeathCause : byte
    {
        Vote = 0,

        Other = 1,
    }

    public sealed class WPlayer
    {
        public int ActorNumber;

        public string Name;

        public string SteamId;

        public bool IsBot;

        public Role Role;

        public bool Alive = true;

        public DeathCause? DeathCause;
    }
}
