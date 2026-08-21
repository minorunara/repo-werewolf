namespace Werewolf.Core
{
    public static class EnemyRespawnScale
    {
        public const int MinPercent = 0;

        public const int MaxPercent = 100;

        public static float CompensationSeconds(int scalePercent, float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return 0f;
            int p = scalePercent < MinPercent ? MinPercent
                : (scalePercent > MaxPercent ? MaxPercent : scalePercent);
            return deltaSeconds * (MaxPercent - p) / 100f;
        }
    }
}
