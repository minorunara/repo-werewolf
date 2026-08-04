using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game
{
    internal static class LastRunGate
    {
        private static readonly AccessTools.FieldRef<RoundDirector, int> PointsRef =
            GameRefs.RoundDirector_extractionPoints;
        private static readonly AccessTools.FieldRef<RoundDirector, int> PointsCompletedRef =
            GameRefs.RoundDirector_extractionPointsCompleted;
        private static readonly AccessTools.FieldRef<RoundDirector, bool> AllCompletedRef =
            GameRefs.RoundDirector_allExtractionPointsCompleted;

        public static bool IsLastRunActive()
        {
            try
            {
                RoundDirector rd = RoundDirector.instance;
                if (rd == null) return false;
                int points = PointsRef(rd);
                return points > 0 && PointsCompletedRef(rd) >= points - 1;
            }
            catch (Exception e)
            {
                WLog.Line("lastrun_probe_error", secret: false, ("err", e.Message));
                return false;
            }
        }

        public static bool IsAllExtractionCompleted()
        {
            try
            {
                RoundDirector rd = RoundDirector.instance;
                if (rd == null) return false;
                return AllCompletedRef(rd);
            }
            catch (Exception e)
            {
                WLog.Line("allextract_probe_error", secret: false, ("err", e.Message));
                return false;
            }
        }
    }
}
