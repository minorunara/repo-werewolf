using System;

namespace Werewolf.Core
{
    public static class VoteTallyTimeline
    {
        public const long DurationCapMs = 2500;

        public const long MaxStepMs = 350;

        public const long BannerDelayMs = 600;

        public const int MaxChips = 12;

        public const long MinChipSfxIntervalMs = 60;

        public static long StepMs(int maxCount)
        {
            if (maxCount <= 0) return 0;
            long step = Math.Min(MaxStepMs, DurationCapMs / maxCount);
            return Math.Max(1, step);
        }

        public static long TallyEndMs(int maxCount)
            => maxCount <= 0 ? 0 : StepMs(maxCount) * maxCount;

        public static int Landed(int finalCount, long elapsedMs, long stepMs)
        {
            if (finalCount <= 0) return 0;
            if (stepMs <= 0) return finalCount;
            if (elapsedMs <= 0) return 0;
            long landed = elapsedMs / stepMs;
            return landed >= finalCount ? finalCount : (int)landed;
        }

        public static bool BannerReady(int maxCount, long elapsedMs)
            => elapsedMs >= TallyEndMs(maxCount) + BannerDelayMs;

        public static long CeremonyDelayMs(int[] voteCounts)
        {
            int max = 0;
            if (voteCounts != null)
            {
                foreach (int c in voteCounts)
                {
                    if (c > max) max = c;
                }
            }
            return TallyEndMs(max) + BannerDelayMs;
        }

        public static int VisibleChips(int landed) => Math.Min(landed, MaxChips);

        public static bool TopChipVisible(int finalCount, int landed, long elapsedMs, long stepMs)
        {
            if (landed <= MaxChips || landed >= finalCount || stepMs <= 0) return true;
            return (elapsedMs % stepMs) * 2 >= stepMs;
        }
    }
}
