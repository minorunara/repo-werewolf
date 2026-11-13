#nullable enable
using System;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class LastRunGate
    {
        public static bool IsLastRunActive()
        {
            try
            {
                RoundDirector rd = RoundDirector.instance;
                if (rd == null) return false;
                int points = rd.extractionPoints;
                return points > 0 && rd.extractionPointsCompleted >= points - 1;
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
                return rd.allExtractionPointsCompleted;
            }
            catch (Exception e)
            {
                WLog.Line("allextract_probe_error", secret: false, ("err", e.Message));
                return false;
            }
        }
    }
}
