using System;

namespace Werewolf.Core
{
    public enum PatchApplyOutcome : byte
    {
        AllApplied = 0,

        ScenariosDisabled = 1,

        ModDisabled = 2,
    }

    public static class PatchTierPolicy
    {
        public const string ScenarioNamespacePrefix = "Werewolf.Scenarios.";

        public static bool IsScenarioTier(string patchClassFullName)
            => patchClassFullName != null
               && patchClassFullName.StartsWith(ScenarioNamespacePrefix, StringComparison.Ordinal);

        public static PatchApplyOutcome Decide(int essentialFailures, int scenarioFailures)
        {
            if (essentialFailures > 0) return PatchApplyOutcome.ModDisabled;
            return scenarioFailures > 0
                ? PatchApplyOutcome.ScenariosDisabled
                : PatchApplyOutcome.AllApplied;
        }
    }
}
