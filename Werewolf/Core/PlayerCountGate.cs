namespace Werewolf.Core
{
    public static class PlayerCountGate
    {
        public const int MinimumPlayers = 3;

        public static bool IsSatisfied(int playerCount, bool debugMode)
        {
            return debugMode || playerCount >= MinimumPlayers;
        }
    }
}
