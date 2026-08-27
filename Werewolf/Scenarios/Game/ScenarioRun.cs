using System;
using Werewolf.Core;
using Werewolf.Vanilla;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;
using Werewolf.Scenarios.Vanilla;

namespace Werewolf.Scenarios.Game
{
    public static class ScenarioRun
    {
        public static bool Active { get; private set; }

        public static bool Available { get; private set; } = true;

        public static void DisableForFailedPatches(int failedClasses)
            => Disable("patch_failed", failedClasses);

        public static void DisableForFailedRefs(int failedFields)
            => Disable("ref_failed", failedFields);

        private static void Disable(string reason, int failed)
        {
            Available = false;
            WLog.Line("scenario_unavailable", secret: false, ("reason", reason), ("failed", failed));
        }

        public static ScenarioId Scenario { get; private set; }

        public static ScenarioDefinition Definition
            => Active && ScenarioCatalog.TryGetDefinition(Scenario, out var definition)
                ? definition
                : null;

        public static ScenarioSafetySpec Safety
        {
            get
            {
                ScenarioDefinition definition = Definition;
                return definition != null ? definition.Safety : ScenarioSafetySpec.Sheltered;
            }
        }

        public static bool EnemiesSuppressed => Active && Safety.SuppressEnemies;

        public static bool InvincibilityForced => Active && !SafetyLifted && Safety.Invincible;

        public static bool DamageClamped => Active && !SafetyLifted && Safety.ClampDamage;

        public static bool DeathGuardActive => Active && !SafetyLifted;

        public static bool SafetyLifted { get; private set; }

        public static void LiftSafety()
        {
            if (SafetyLifted) return;
            SafetyLifted = true;
            WLog.Line("scenario_safety_lifted", secret: false, ("scenario", Scenario));
        }

        public static bool Broken { get; private set; }

        public static void MarkBroken() => Broken = true;

        public static bool TryStart(ScenarioId id)
        {
            if (!Available)
            {
                WLog.Line("scenario_start_rejected", secret: false,
                    ("scenario", id), ("reason", "patch_failed"));
                return false;
            }

            if (!ScenarioCatalog.TryGetDefinition(id, out ScenarioDefinition startDefinition))
            {
                WLog.Line("scenario_start_rejected", secret: false,
                    ("scenario", id), ("reason", "not_implemented"));
                return false;
            }

            try
            {
                if (!ScenarioWorld.CheckStart(startDefinition.Environment, out string rejectReason))
                {
                    WLog.Line("scenario_start_rejected", secret: false, ("scenario", id), ("reason", rejectReason));
                    return false;
                }

                Active = true;
                Broken = false;
                SafetyLifted = false;
                ScenarioVanillaProbe.Reset();
                Scenario = id;

                ScenarioWorld.StartRunLevel(out bool saveCleared);

                WLog.Line("scenario_started", secret: false,
                    ("scenario", id), ("saveCleared", saveCleared));
                return true;
            }
            catch (Exception e)
            {
                Clear();
                WLog.Line("scenario_start_error", secret: false, ("scenario", id), ("err", e.Message));
                return false;
            }
        }

        public static void ExitToTitle(string reason, bool completed = false)
        {
            try
            {
                if (completed) MarkCompleted();
                if (!Session.HasRunManager)
                {
                    WLog.Line("scenario_exit_error", secret: false, ("reason", reason), ("err", "no_runmanager"));
                    Clear();
                    return;
                }

                WLog.Line("scenario_exit", secret: false, ("scenario", Scenario), ("reason", reason));
                Clear();
                LeaveGameStateMain();
                ScenarioSelection.RequestOpen();
                ScenarioWorld.LeaveToMainMenu();
            }
            catch (Exception e)
            {
                Clear();
                WLog.Line("scenario_exit_error", secret: false, ("reason", reason), ("err", e.Message));
            }
        }

        private static void LeaveGameStateMain()
        {
            if (!ScenarioWorld.TryLeaveGameStateMain()) return;
            WLog.Line("scenario_gamestate_left_main", secret: false);
        }

        public static void Clear()
        {
            Active = false;
            Broken = false;
            SafetyLifted = false;
        }

        private static void MarkCompleted()
        {
            if (!Active || Broken) return;
            try
            {
                Plugin.Bindings?.MarkScenarioCleared(Scenario);
                WLog.Line("scenario_completed", secret: false, ("scenario", Scenario));
            }
            catch (Exception e)
            {
                WLog.Line("scenario_complete_error", secret: false,
                    ("scenario", Scenario), ("err", e.Message));
            }
        }

    }
}
