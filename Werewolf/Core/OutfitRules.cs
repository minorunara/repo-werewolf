namespace Werewolf.Core
{
    public static class OutfitRules
    {
        public static bool ShouldBlockOutfitChange(GamePhase phase, bool allowedByRoomSetting)
        {
            return CombatRules.IsMatchLive(phase) && !allowedByRoomSetting;
        }
    }
}
