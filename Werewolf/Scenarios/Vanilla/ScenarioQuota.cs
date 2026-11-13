#nullable enable
using UnityEngine;
using System.Diagnostics.CodeAnalysis;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioQuota
    {
        public static bool TryReadState(out int haulGoal, out int points, out int currentHaul, out int surplus)
        {
            RoundDirector director = RoundDirector.instance;
            if (director == null)
            {
                haulGoal = 0;
                points = 0;
                currentHaul = 0;
                surplus = 0;
                return false;
            }
            haulGoal = director.haulGoal;
            points = director.extractionPoints;
            currentHaul = director.currentHaul;
            surplus = director.extractionPointSurplus;
            return true;
        }

        public static bool TryReadExtraction(out int points, out int completed,
            [NotNullWhen(false)] out string? failReason)
        {
            RoundDirector director = RoundDirector.instance;
            if (director == null)
            {
                points = 0;
                completed = 0;
                failReason = "no_round_director";
                return false;
            }
            points = director.extractionPoints;
            completed = director.extractionPointsCompleted;
            failReason = null;
            return true;
        }

        public static bool TryReadBagDollars(GameObject? bag, out int dollars)
        {
            dollars = 0;
            ValuableObject? valuable = bag != null ? bag.GetComponent<ValuableObject>() : null;
            if (valuable == null) return false;
            float current = valuable.dollarValueCurrent;
            dollars = current > 0f ? (int)current : 0;
            return true;
        }
    }
}
