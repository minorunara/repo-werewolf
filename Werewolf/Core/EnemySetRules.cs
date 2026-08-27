namespace Werewolf.Core
{
    public readonly struct EnemySetCounts
    {
        public static readonly EnemySetCounts Untouched =
            new EnemySetCounts(EnemySetRules.Vanilla, EnemySetRules.Vanilla, EnemySetRules.Vanilla);

        public EnemySetCounts(int danger1, int danger2, int danger3)
        {
            Danger1 = danger1;
            Danger2 = danger2;
            Danger3 = danger3;
        }

        public int Danger1 { get; }
        public int Danger2 { get; }
        public int Danger3 { get; }

        public bool AnyOverridden
            => Danger1 != EnemySetRules.Vanilla
               || Danger2 != EnemySetRules.Vanilla
               || Danger3 != EnemySetRules.Vanilla;

        public override string ToString() => $"{Danger1}/{Danger2}/{Danger3}";
    }

    public static class EnemySetRules
    {
        public const int Vanilla = -1;

        public const int MinSets = 0;
        public const int MaxSets = 20;

        public static int Clamp(int sets)
        {
            if (sets < MinSets) return Vanilla;
            return sets > MaxSets ? MaxSets : sets;
        }

        public static EnemySetCounts Clamp(EnemySetCounts counts)
            => new EnemySetCounts(Clamp(counts.Danger1), Clamp(counts.Danger2), Clamp(counts.Danger3));

        public static bool IsEligible(int levelsCompleted, int runsPlayed,
                                      bool levelsCompletedCondition, int levelsCompletedMin, int levelsCompletedMax,
                                      int runsPlayedRequired)
        {
            if (levelsCompletedCondition)
            {
                if (levelsCompleted < levelsCompletedMin) return false;
                if (levelsCompletedMax != 0 && levelsCompleted > levelsCompletedMax) return false;
            }
            return runsPlayed >= runsPlayedRequired;
        }

        public static int? ResolveTier(int requestedSets, bool poolHasEligible)
        {
            int sets = Clamp(requestedSets);
            if (sets == Vanilla) return null;
            return poolHasEligible ? sets : MinSets;
        }
    }
}
