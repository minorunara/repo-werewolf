using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioVanillaProbe
    {
        private static readonly HashSet<string> LoggedSites = new HashSet<string>(StringComparer.Ordinal);

        internal static void Reset() => LoggedSites.Clear();

        internal static void Failed(string site, Exception e)
            => Failed(site, e != null ? e.GetType().Name : "null");

        internal static void Failed(string site, string err)
        {
            if (!LoggedSites.Add(site)) return;
            WLog.Line("scenario_vanilla_probe_failed", secret: false, ("site", site), ("err", err));
        }

        internal static bool InputDown(GameKey key, string site)
        {
            try
            {
                return Inputs.Down(key);
            }
            catch (Exception e)
            {
                Failed(site, e);
                return false;
            }
        }

        internal static bool InputHold(GameKey key, string site)
        {
            try
            {
                return Inputs.Hold(key);
            }
            catch (Exception e)
            {
                Failed(site, e);
                return false;
            }
        }

        internal static bool NoTextInputsActive(string site)
        {
            try
            {
                return Inputs.NoTextInputsActive;
            }
            catch (Exception e)
            {
                Failed(site, e);
                return true;
            }
        }
    }
}
