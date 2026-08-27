using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum WinReason : byte
    {
        WerewolvesEradicated = 0,

        VillagersEradicated = 1,

        ExtractionCompleted = 2,

        TimerExpired = 3,

        ExtractionFailed = 4,

        ValueCheckmate = 5,

        PeacefulSurvived = 6,

        PeacefulCollapsed = 7,
    }

    public sealed class WinResult
    {
        public WinResult(Team winningTeam, WinReason reason)
        {
            WinningTeam = winningTeam;
            Reason = reason;
        }

        public Team WinningTeam { get; }
        public WinReason Reason { get; }

        public bool Peaceful
            => Reason == WinReason.PeacefulSurvived || Reason == WinReason.PeacefulCollapsed;

        public WinResult AsPeaceful()
        {
            if (Peaceful) return this;
            return WinningTeam == Team.Villagers
                ? new WinResult(Team.Villagers, WinReason.PeacefulSurvived)
                : new WinResult(Team.Werewolves, WinReason.PeacefulCollapsed);
        }
    }

    public static class WinJudge
    {
        public const int PeacefulMinAlive = 2;

        public static WinResult Judge(
            IReadOnlyList<WPlayer> players,
            bool extractionCompleted = false,
            bool timerExpired = false,
            bool peaceful = false)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));

            if (peaceful)
            {
                int alive = 0;
                foreach (var player in players)
                {
                    if (player.Alive && RoleDistribution.TeamOf(player.Role) == Team.Villagers) alive++;
                }
                if (alive < PeacefulMinAlive)
                {
                    return new WinResult(Team.Werewolves, WinReason.PeacefulCollapsed);
                }
                if (extractionCompleted)
                {
                    return new WinResult(Team.Villagers, WinReason.PeacefulSurvived);
                }
                if (timerExpired)
                {
                    return new WinResult(Team.Werewolves, WinReason.PeacefulCollapsed);
                }
                return null;
            }

            int aliveWerewolves = 0;
            int aliveVillagers = 0;
            foreach (var player in players)
            {
                if (!player.Alive) continue;
                if (player.Role == Role.Werewolf || player.Role == Role.Bomber) aliveWerewolves++;
                else if (RoleDistribution.TeamOf(player.Role) == Team.Villagers) aliveVillagers++;
            }

            if (aliveWerewolves == 0)
            {
                return new WinResult(Team.Villagers, WinReason.WerewolvesEradicated);
            }
            if (aliveVillagers == 0)
            {
                return new WinResult(Team.Werewolves, WinReason.VillagersEradicated);
            }
            if (extractionCompleted)
            {
                return new WinResult(Team.Villagers, WinReason.ExtractionCompleted);
            }
            if (timerExpired)
            {
                return new WinResult(Team.Werewolves, WinReason.TimerExpired);
            }

            return null;
        }
    }
}
