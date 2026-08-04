namespace Werewolf.Core
{
    public static class ValuableRecordGate
    {
        public static bool IsWerewolfTeam(Role? localRole)
            => localRole.HasValue && RoleDistribution.TeamOf(localRole.Value) == Team.Werewolves;

        public static bool ShouldSuppressDiscover(Role? localRole, bool alive, bool roundActive, bool recordOn)
            => roundActive && alive && !recordOn && IsWerewolfTeam(localRole);

        public static bool CanOperate(Role? localRole, bool alive, GamePhase phase, bool warpedInMeeting)
            => (phase == GamePhase.Play || phase == GamePhase.Meeting)
               && !warpedInMeeting && alive && IsWerewolfTeam(localRole);
    }
}
