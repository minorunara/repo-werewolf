using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.Game
{
    internal sealed partial class ScenarioController
    {
        private long _leashNextCheckAtMs;

        private long _meetingMissingSinceMs;

        private const long BrokenNoticeMinMs = 5000L;
        private const long BrokenNoticeMaxMs = 10000L;
        private const long BrokenNoticePerCharMs = 60L;

        private const long LeashWarpGraceMs = 1000L;

        private const long LeashCheckIntervalMs = 1000L;

        private const long MeetingMissingBreakMs = 5000L;

        private void TickBreakWatchdog(long now)
        {
            if (!TryGetRunningScript(out ScenarioScriptRun? script)) return;
            ScenarioFlow flow = script.Flow;
            ScenarioDefinition definition = script.Definition;

            ScenarioBreakWatch watch = definition.Watch;

            if (watch.RequireTallyBeforeResult
                && flow.IsLatched(ScenarioSignal.ResultScreenShown)
                && !flow.IsLatched(ScenarioSignal.TallyRevealed))
            {
                Break(ScenarioBreakKind.ResultBeforeTally, now);
                return;
            }

            bool meetingRequired = watch.MeetingRequiredFrom.HasValue
                                   && flow.IsLatched(watch.MeetingRequiredFrom.Value)
                                   && !(watch.MeetingRequiredUntil.HasValue
                                         && flow.IsLatched(watch.MeetingRequiredUntil.Value));
            if (!meetingRequired || _d.MeetingActiveClient)
            {
                _meetingMissingSinceMs = 0;
                return;
            }

            if (_meetingMissingSinceMs == 0)
            {
                _meetingMissingSinceMs = now;
                return;
            }
            if (now - _meetingMissingSinceMs < MeetingMissingBreakMs) return;

            Break(ScenarioBreakKind.MeetingLost, now);
        }

        private static long BrokenNoticeMs(string? text)
        {
            long ms = BrokenNoticeMinMs + (text != null ? text.Length : 0) * BrokenNoticePerCharMs;
            return ms > BrokenNoticeMaxMs ? BrokenNoticeMaxMs : ms;
        }

        private string BrokenNoticeText(IReadOnlyList<TextId>? lines)
            => lines != null && lines.Count > 0 ? JoinLines(lines) : Texts.Get(TextId.ScenarioBrokenNotice);

        private void Break(ScenarioBreakKind kind, long now, string? detail = null)
        {
            ScenarioScriptRun? script = _script;
            IReadOnlyList<TextId>? lines = script?.Definition.Notices.BreakLines(kind);
            if (!_interrupt.Break(kind, lines, now + BrokenNoticeMs(BrokenNoticeText(lines)))) return;

            ScenarioRun.MarkBroken();
            WLog.Line("scenario_broken", secret: false,
                ("scenario", script != null ? script.Scenario : ScenarioRun.Scenario),
                ("reason", kind), ("detail", detail ?? ""),
                ("page", script != null ? script.Flow.Index : 0));
        }

        private void TickTruckLeash(long now)
        {
            if (!TryGetRunningScript(out ScenarioScriptRun? script)) return;
            ScenarioFlow flow = script.Flow;
            ScenarioDefinition definition = script.Definition;

            ScenarioEnvironmentSpec spec = definition.Environment;
            if (!spec.TruckLeashEnabled) return;
            if (_d.MeetingActiveClient) return;
            if (!LeashWindowOpen(flow, spec)) return;
            if (now < _leashNextCheckAtMs) return;
            _leashNextCheckAtMs = now + LeashCheckIntervalMs;

            if (!LocalPlayer.TryGetPosition(out Vector3 localPos)) return;

            if (!TruckWarper.TryNearestTruckSpawn(localPos, out Vector3 spawnPos, out Quaternion spawnRot,
                    out float nearestSqr)) return;

            float leash = spec.TruckLeashMeters;
            if (nearestSqr <= leash * leash) return;

            Werewolf.Game.Patches.PlayerSpawnPatch.ArmWarpGrace(LeashWarpGraceMs);
            TruckWarper.WarpLocalAlive(spawnPos, spawnRot);
            _interrupt.Raise(ScenarioInterrupt.LeashNag, now, definition.Notices.LeashNag);
            _leashNextCheckAtMs = now + ScenarioInterruptState.NoticeDurationMs(ScenarioInterrupt.LeashNag);
            WLog.Line("scenario_truck_leash", secret: false,
                ("distanceM", Mathf.Sqrt(nearestSqr)), ("leashM", leash));
        }

        private static bool LeashWindowOpen(ScenarioFlow flow, ScenarioEnvironmentSpec spec)
        {
            if (spec.TruckLeashBefore != ScenarioMilestone.None
                && !flow.HasReached(spec.TruckLeashBefore))
            {
                return true;
            }
            if (spec.TruckLeashAfter != ScenarioMilestone.None
                && !flow.HasReached(spec.TruckLeashAfter))
            {
                return false;
            }
            return !spec.TruckLeashUntil.HasValue || !flow.IsLatched(spec.TruckLeashUntil.Value);
        }

        private void ResetBreak()
        {
            _leashNextCheckAtMs = 0;
            _meetingMissingSinceMs = 0;
        }
    }
}
