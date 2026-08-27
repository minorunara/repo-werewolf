using UnityEngine;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioQuota
    {
        public static bool TryReadState(out int haulGoal, out int points, out int currentHaul, out int surplus)
        {
            RoundDirector director = RoundDirector.instance;
            if (director == null || GameRefs.RoundDirector_haulGoal == null
                || GameRefs.RoundDirector_extractionPoints == null
                || GameRefs.RoundDirector_currentHaul == null
                || GameRefs.RoundDirector_extractionPointSurplus == null)
            {
                haulGoal = 0;
                points = 0;
                currentHaul = 0;
                surplus = 0;
                return false;
            }
            haulGoal = GameRefs.RoundDirector_haulGoal(director);
            points = GameRefs.RoundDirector_extractionPoints(director);
            currentHaul = GameRefs.RoundDirector_currentHaul(director);
            surplus = GameRefs.RoundDirector_extractionPointSurplus(director);
            return true;
        }

        public static bool TryReadExtraction(out int points, out int completed, out string failReason)
        {
            RoundDirector director = RoundDirector.instance;
            if (director == null || GameRefs.RoundDirector_extractionPoints == null
                || GameRefs.RoundDirector_extractionPointsCompleted == null)
            {
                points = 0;
                completed = 0;
                failReason = director == null ? "no_round_director" : "fieldref_null";
                return false;
            }
            points = GameRefs.RoundDirector_extractionPoints(director);
            completed = GameRefs.RoundDirector_extractionPointsCompleted(director);
            failReason = null;
            return true;
        }

        public static bool TryReadBagDollars(GameObject bag, out int dollars)
        {
            dollars = 0;
            ValuableObject valuable = bag != null ? bag.GetComponent<ValuableObject>() : null;
            if (valuable == null || GameRefs.ValuableObject_dollarValueCurrent == null) return false;
            float current = GameRefs.ValuableObject_dollarValueCurrent(valuable);
            dollars = current > 0f ? (int)current : 0;
            return true;
        }
    }
}
