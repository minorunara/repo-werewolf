using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioBlastRules
    {
        public const float PointBlankMeters = 4f;

        public static float PointBlankLimit(float blastRadiusMeters)
            => blastRadiusMeters < PointBlankMeters ? blastRadiusMeters : PointBlankMeters;

        public static BombDenyReason Resolve(ScenarioBlastRule rule, float distanceMeters,
                                             float blastRadiusMeters, bool targetNearTruck,
                                             bool targetMoving, bool bombOnWrongTarget = false)
        {
            if (bombOnWrongTarget) return BombDenyReason.ScenarioTarget;
            if (rule == ScenarioBlastRule.None) return BombDenyReason.None;
            if (targetMoving)
            {
                return rule == ScenarioBlastRule.AwaitTruckZone
                    ? BombDenyReason.ScenarioDistance
                    : BombDenyReason.ScenarioTargetMoving;
            }
            return Denies(rule, distanceMeters, blastRadiusMeters, targetNearTruck)
                ? BombDenyReason.ScenarioDistance
                : BombDenyReason.None;
        }

        public static bool Denies(ScenarioBlastRule rule, float distanceMeters, float blastRadiusMeters,
                                  bool targetNearTruck)
        {
            switch (rule)
            {
                case ScenarioBlastRule.OutsideBlast:
                    return blastRadiusMeters > 0f && distanceMeters <= blastRadiusMeters;
                case ScenarioBlastRule.PointBlank:
                    return blastRadiusMeters > 0f && distanceMeters > PointBlankLimit(blastRadiusMeters);
                case ScenarioBlastRule.AwaitTruckZone:
                    return !targetNearTruck;
                default:
                    return false;
            }
        }

        private static readonly TextId[] TooClose =
        {
            TextId.ScenarioBlastDenyTooClose1, TextId.ScenarioBlastDenyTooClose2,
        };

        private static readonly TextId[] TooFar =
        {
            TextId.ScenarioBlastDenyTooFar1, TextId.ScenarioBlastDenyTooFar2,
        };

        private static readonly TextId[] AwaitTruck =
        {
            TextId.ScenarioBlastDenyAwaitTruck1, TextId.ScenarioBlastDenyAwaitTruck2,
        };

        private static readonly TextId[] NoBomb = { TextId.ScenarioBlastDenyGeneric1 };

        private static readonly TextId[] Moving =
        {
            TextId.ScenarioBlastDenyMoving1, TextId.ScenarioBlastDenyMoving2,
        };

        public static IReadOnlyList<TextId> MovingLines => Moving;

        public static IReadOnlyList<TextId> DenyLines(ScenarioBlastRule rule)
        {
            switch (rule)
            {
                case ScenarioBlastRule.OutsideBlast: return TooClose;
                case ScenarioBlastRule.PointBlank: return TooFar;
                case ScenarioBlastRule.AwaitTruckZone: return AwaitTruck;
                default: return NoBomb;
            }
        }
    }
}
