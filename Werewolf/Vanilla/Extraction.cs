#nullable enable

namespace Werewolf.Vanilla
{
    internal static class Extraction
    {
        public static bool RoundDirectorAlive => RoundDirector.instance != null;

        public static bool TryReadQuota(out int haulGoal, out int points, out int completed)
        {
            RoundDirector rd = RoundDirector.instance;
            if (rd == null)
            {
                haulGoal = 0;
                points = 0;
                completed = 0;
                return false;
            }
            haulGoal = rd.haulGoal;
            points = rd.extractionPoints;
            completed = rd.extractionPointsCompleted;
            return true;
        }

        public static void ReadExtractedAndGoal(out int extracted, out int haulGoal)
        {
            if (!TryReadQuota(out haulGoal, out int points, out int completed))
            {
                extracted = -1;
                haulGoal = -1;
                return;
            }
            extracted = points > 0 ? haulGoal / points * completed : 0;
        }

        public static void AddCosmeticExtracted(byte rarity)
        {
            RoundDirector.instance.CosmeticWorldObjectExtracted((SemiFunc.Rarity)rarity);
        }
    }
}
