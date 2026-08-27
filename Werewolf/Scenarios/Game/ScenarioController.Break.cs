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
            if (!ScriptRunning) return;

            ScenarioBreakWatch watch = _definition.Watch;

            if (watch.RequireTallyBeforeResult
                && _flow.IsLatched(ScenarioSignal.ResultScreenShown)
                && !_flow.IsLatched(ScenarioSignal.TallyRevealed))
            {
                Break(ScenarioBreakKind.ResultBeforeTally, now);
                return;
            }

            bool meetingRequired = watch.MeetingRequiredFrom.HasValue
                                   && _flow.IsLatched(watch.MeetingRequiredFrom.Value)
                                   && !(watch.MeetingRequiredUntil.HasValue
                                        && _flow.IsLatched(watch.MeetingRequiredUntil.Value));
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

        private static long BrokenNoticeMs(string text)
        {
            long ms = BrokenNoticeMinMs + (text != null ? text.Length : 0) * BrokenNoticePerCharMs;
            return ms > BrokenNoticeMaxMs ? BrokenNoticeMaxMs : ms;
        }

        private string BrokenNoticeText(IReadOnlyList<TextId> lines)
            => lines != null && lines.Count > 0 ? JoinLines(lines) : Texts.Get(TextId.ScenarioBrokenNotice);

        private void Break(ScenarioBreakKind kind, long now, string detail = null)
        {
            IReadOnlyList<TextId> lines = _definition != null ? _definition.Notices.BreakLines(kind) : null;
            if (!_interrupt.Break(kind, lines, now + BrokenNoticeMs(BrokenNoticeText(lines)))) return;

            ScenarioRun.MarkBroken();
            WLog.Line("scenario_broken", secret: false,
                ("scenario", _scenario), ("reason", kind), ("detail", detail ?? ""),
                ("page", _flow != null ? _flow.Index : 0));
        }

        private void TickTruckLeash(long now)
        {
            if (!ScriptRunning) return;

            ScenarioEnvironmentSpec spec = _definition.Environment;
            if (!spec.TruckLeashEnabled) return;
            if (_d.MeetingActiveClient) return;
            if (!LeashWindowOpen(spec)) return;
            if (now < _leashNextCheckAtMs) return;
            _leashNextCheckAtMs = now + LeashCheckIntervalMs;

            if (!LocalPlayer.TryGetPosition(out Vector3 localPos)) return;

            if (!TruckWarper.TryNearestTruckSpawn(localPos, out Vector3 spawnPos, out Quaternion spawnRot,
                    out float nearestSqr)) return;

            float leash = spec.TruckLeashMeters;
            if (nearestSqr <= leash * leash) return;

            Werewolf.Game.Patches.PlayerSpawnPatch.ArmWarpGrace(LeashWarpGraceMs);
            TruckWarper.WarpLocalAlive(spawnPos, spawnRot);
            _interrupt.Raise(ScenarioInterrupt.LeashNag, now, _definition.Notices.LeashNag);
            _leashNextCheckAtMs = now + ScenarioInterruptState.NoticeDurationMs(ScenarioInterrupt.LeashNag);
            WLog.Line("scenario_truck_leash", secret: false,
                ("distanceM", Mathf.Sqrt(nearestSqr)), ("leashM", leash));
        }

        private bool LeashWindowOpen(ScenarioEnvironmentSpec spec)
        {
            if (spec.TruckLeashBefore != ScenarioMilestone.None
                && !_flow.HasReached(spec.TruckLeashBefore))
            {
                return true;
            }
            if (spec.TruckLeashAfter != ScenarioMilestone.None
                && !_flow.HasReached(spec.TruckLeashAfter))
            {
                return false;
            }
            return !spec.TruckLeashUntil.HasValue || !_flow.IsLatched(spec.TruckLeashUntil.Value);
        }

        private void ResetBreak()
        {
            _leashNextCheckAtMs = 0;
            _meetingMissingSinceMs = 0;
        }
    }
}
