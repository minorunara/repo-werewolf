using System;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioLevelSizeRules
    {
        public const int MinModuleAmount = 5;
        public const int MaxModuleAmount = 15;

        public const int MinExtractionPoints = 1;
        public const int MaxExtractionPoints = 5;

        public static int VanillaModuleAmount(int levelsCompleted)
        {
            int completed = levelsCompleted > 0 ? levelsCompleted : 0;
            int amount = Math.Min(5 + completed, 10);
            if (completed >= 10) amount += Math.Min(completed - 9, 5);
            return amount;
        }

        public static float SizeMultiplier(int levelsCompleted, int targetModuleAmount)
        {
            if (targetModuleAmount <= 0) return 1f;
            int vanilla = VanillaModuleAmount(levelsCompleted);
            int target = Clamp(targetModuleAmount, MinModuleAmount, MaxModuleAmount);
            if (target <= vanilla) return 1f;
            return (target - 0.5f) / vanilla;
        }

        public static int ScaledModuleAmount(int levelsCompleted, float multiplier)
        {
            float scaled = VanillaModuleAmount(levelsCompleted) * multiplier;
            return (int)Math.Ceiling(scaled);
        }

        public static int ExtractionRooms(int extractionPoints)
        {
            if (extractionPoints <= 0) return 0;
            return Clamp(extractionPoints, MinExtractionPoints, MaxExtractionPoints) - 1;
        }

        private static int Clamp(int value, int min, int max)
            => value < min ? min : value > max ? max : value;
    }
}
