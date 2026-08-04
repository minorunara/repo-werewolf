using System;

namespace Werewolf.Core
{
    public static class CosmeticHandoff
    {
        public const long MinMatchDurationMs = 180_000;

        public static bool ShouldGrant(long startUnixMs, long nowUnixMs)
        {
            if (startUnixMs <= 0) return true;
            return nowUnixMs - startUnixMs >= MinMatchDurationMs;
        }

        public enum Route : byte
        {
            Inject,
            DirectAdd,
        }

        public static Route Decide(bool departedToLobbyMenu, bool roundDirectorAlive,
                                   bool cooldownKnown, float cooldownSeconds,
                                   out string fallbackReason)
        {
            if (!departedToLobbyMenu)
            {
                fallbackReason = "not_lobby_menu_departure";
                return Route.DirectAdd;
            }
            if (!roundDirectorAlive)
            {
                fallbackReason = "no_round_director";
                return Route.DirectAdd;
            }
            if (!cooldownKnown)
            {
                fallbackReason = "cooldown_unknown";
                return Route.DirectAdd;
            }
            if (cooldownSeconds > 0f)
            {
                fallbackReason = "cooldown_active";
                return Route.DirectAdd;
            }
            fallbackReason = null;
            return Route.Inject;
        }

        public static int[] SubtractLeading(int[] countsByRarity, int injectedCount)
        {
            if (countsByRarity == null) throw new ArgumentNullException(nameof(countsByRarity));
            var remaining = (int[])countsByRarity.Clone();
            for (int r = 0; r < remaining.Length && injectedCount > 0; r++)
            {
                int take = Math.Min(remaining[r], injectedCount);
                remaining[r] -= take;
                injectedCount -= take;
            }
            return remaining;
        }
    }
}
