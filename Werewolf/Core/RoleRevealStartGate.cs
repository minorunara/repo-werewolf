namespace Werewolf.Core
{
    public static class RoleRevealStartGate
    {
        public static bool IsReady(
            Role? localRole,
            int[] knownWerewolves,
            int werewolfCount,
            GamePhase phase,
            long revealStartUnixMs,
            long revealEndUnixMs)
        {
            if (localRole == null || phase != GamePhase.RoleReveal) return false;
            if (revealStartUnixMs <= 0 || revealEndUnixMs <= revealStartUnixMs) return false;

            Role role = localRole.Value;
            bool waitsForTeammates = role == Role.Werewolf || role == Role.Bomber;
            return !waitsForTeammates || knownWerewolves != null || werewolfCount == 1;
        }
    }
}
