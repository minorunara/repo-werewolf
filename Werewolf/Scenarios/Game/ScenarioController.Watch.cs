using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;
using Werewolf.Vanilla;
using Werewolf.Scenarios.Vanilla;

namespace Werewolf.Scenarios.Game
{
    internal sealed partial class ScenarioController
    {
        private bool _mapWasOpen;

        private ScenarioWolfWatch _wolfWatch = new ScenarioWolfWatch();

        private readonly List<ScenarioSignal> _wolfSignals = new List<ScenarioSignal>();

        private ScenarioShamanWatch _shamanWatch = new ScenarioShamanWatch();
        private readonly List<ScenarioSignal> _shamanSignals = new List<ScenarioSignal>();

        private ScenarioCorpseHintClock _corpseHintClock = new ScenarioCorpseHintClock();

        private int _prevExtractionsCompleted = -1;
        private long _extractionNextCheckAtMs;

        private bool _lastRunSeen;

        private long _grabbedSinceMs;
        private long _grabReleaseNextCheckAtMs;

        private const long PollIntervalMs = 500L;

        private void TickWolfWatch()
        {
            if (!ScriptRunning) return;

            RolesClientState roles = _d.RolesClient;
            if (roles == null) return;

            RolesSession session = _d.HostRolesSession;
            if (session == null) return;

            _wolfWatch.Observe(
                new ScenarioWolfSample(_d.HostValuablesDestroyed, roles.UnlockedFlags,
                    roles.WolfMode, roles.ValuableRecordOn, roles.BeaconCharges),
                _wolfSignals);

            for (int i = 0; i < _wolfSignals.Count; i++) FeedSignal(_wolfSignals[i]);
        }

        private void TickShamanWatch()
        {
            if (!ScriptRunning) return;

            _shamanWatch.Observe(_d.ShamanSenseSample, _shamanSignals);
            for (int i = 0; i < _shamanSignals.Count; i++) FeedSignal(_shamanSignals[i]);
        }

        private void TickCorpseHint(long now)
        {
            if (!ScriptRunning) return;

            ScenarioCorpseHint hint = _definition.CorpseHint;
            if (!hint.Enabled) return;

            bool fired = _corpseHintClock.Tick(now,
                _flow.IsLatched(hint.After), _flow.IsLatched(hint.Until), hint.DelayMs);
            if (!fired) return;

            _interrupt.Raise(ScenarioInterrupt.CorpseHint, now, hint.Lines);
            WLog.Line("scenario_corpse_hint", secret: false,
                ("participant", hint.ParticipantId), ("delayMs", hint.DelayMs));
        }

        private Vector3? CorpseHintMarkPos()
            => ScriptRunning && _corpseHintClock.MarkerShown
                ? BotFootPos(_definition.CorpseHint.ParticipantId)
                : null;

        private void TickExtractionWatch(long now)
        {
            if (!ScriptRunning) return;
            if (now < _extractionNextCheckAtMs) return;
            _extractionNextCheckAtMs = now + PollIntervalMs;

            if (!ScenarioQuota.TryReadExtraction(out int points, out int completed, out string failReason))
            {
                ScenarioVanillaProbe.Failed("extraction_watch", failReason);
                return;
            }
            if (points <= 0) return;

            bool lastRun = LastRunGate.IsLastRunActive();
            if (_prevExtractionsCompleted < 0)
            {
                _prevExtractionsCompleted = completed;
                _lastRunSeen = lastRun;
                return;
            }

            bool extracted = completed > _prevExtractionsCompleted;
            _prevExtractionsCompleted = completed;
            if (extracted) FeedSignal(ScenarioSignal.ExtractionCompleted);
            if (lastRun && !_lastRunSeen) FeedSignal(ScenarioSignal.LastRunReached);
            _lastRunSeen = lastRun;
            if (!extracted) return;

            ScenarioBreakWatch watch = _definition.Watch;
            if (watch.BreakOnAllExtractionsCompleted && completed >= points)
            {
                Break(ScenarioBreakKind.AllExtractionsCompleted, now);
                return;
            }
            if (watch.ExtractionBreaksBefore != ScenarioMilestone.None
                && !_flow.HasReached(watch.ExtractionBreaksBefore))
            {
                Break(ScenarioBreakKind.EarlyExtraction, now, watch.ExtractionBreaksBefore.ToString());
                return;
            }

            if (_definition.Notices.ExtractionNag.Count == 0) return;
            _interrupt.Raise(ScenarioInterrupt.ExtractionNag, now, _definition.Notices.ExtractionNag);
            WLog.Line("scenario_extraction_nag", secret: false,
                ("completed", completed), ("points", points));
        }

        private void TickGrabRelease(long now)
        {
            if (_definition == null || !_definition.Safety.GrabReleaseEnabled) return;
            if (now < _grabReleaseNextCheckAtMs) return;
            _grabReleaseNextCheckAtMs = now + PollIntervalMs;

            bool held = LocalPlayer.IsTumbling;
            if (!held)
            {
                _grabbedSinceMs = 0;
                return;
            }

            if (_grabbedSinceMs == 0)
            {
                _grabbedSinceMs = now;
                return;
            }
            if (now - _grabbedSinceMs < _definition.Safety.GrabReleaseMs) return;

            _grabbedSinceMs = 0;
            if (!Session.IsHost) return;
            _d.EnemyFreezerView.Begin();
            _d.EnemyFreezerView.End();
            WLog.Line("scenario_grab_released", secret: false, ("scenario", _scenario));
        }

        private void TickMapClose()
        {
            bool open = _d.MeetingMapOpen;
            if (_mapWasOpen && !open) FeedSignal(ScenarioSignal.MapClosed);
            _mapWasOpen = open;
        }

        private void ResetWatch()
        {
            _wolfWatch = new ScenarioWolfWatch();
            _wolfSignals.Clear();
            _shamanWatch = new ScenarioShamanWatch();
            _shamanSignals.Clear();
            _corpseHintClock = new ScenarioCorpseHintClock();
            _mapWasOpen = false;
            _prevExtractionsCompleted = -1;
            _lastRunSeen = false;
            _extractionNextCheckAtMs = 0;
            _grabbedSinceMs = 0;
            _grabReleaseNextCheckAtMs = 0;
        }
    }
}
