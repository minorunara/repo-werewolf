using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Vanilla;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.Scenarios.Vanilla;
using Werewolf.UI;

namespace Werewolf.Scenarios.Game
{
    internal sealed partial class ScenarioController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private ScenarioScriptRun? _script;

        private readonly ScenarioInterruptState _interrupt = new ScenarioInterruptState();

        internal ScenarioController(WerewolfDirector director)
        {
            _d = director;
        }

        internal ScenarioFlow? Flow => _script?.Flow;

        private bool ScriptRunning => _script != null && !_script.Flow.Finished;

        private bool TryGetRunningScript([NotNullWhen(true)] out ScenarioScriptRun? script)
        {
            script = _script;
            return script != null && !script.Flow.Finished;
        }

        internal void FeedSignal(ScenarioSignal signal)
        {
            ScenarioScriptRun? script = _script;
            if (script == null || script.Flow.Finished) return;

            ScenarioFlow flow = script.Flow;

            int from = flow.Index;

            ScenarioNotices notices = script.Definition.Notices;
            if (notices.EarlyStartSignal.HasValue && signal == notices.EarlyStartSignal.Value
                && !flow.HasReached(notices.EarlyStartAfter))
            {
                _interrupt.Defer(ScenarioInterrupt.EarlyStart, notices.EarlyStartLines);
            }

            ScenarioPage pressed = flow.Current;
            switch (signal)
            {
                case ScenarioSignal.BombOnWrongTarget:
                    if (notices.WrongTargetLines.Count > 0)
                    {
                        _interrupt.Defer(ScenarioInterrupt.WrongTarget, notices.WrongTargetLines,
                            pressed.BombTargetParticipantId);
                    }
                    break;
                case ScenarioSignal.BombAlreadyOnTarget:
                    if (notices.AlreadyOnTargetLines.Count > 0)
                    {
                        _interrupt.Defer(ScenarioInterrupt.AlreadyOnTarget,
                            notices.AlreadyOnTargetLines, pressed.BombTargetParticipantId);
                    }
                    break;
                case ScenarioSignal.BombDeniedDistance:
                    _interrupt.Defer(ScenarioInterrupt.BlastDenied,
                        ScenarioBlastRules.DenyLines(pressed.BlastRule),
                        pressed.BombTargetParticipantId);
                    break;
                case ScenarioSignal.BombDeniedTargetMoving:
                    _interrupt.Defer(ScenarioInterrupt.BlastDeniedMoving,
                        ScenarioBlastRules.MovingLines, pressed.BombTargetParticipantId);
                    break;
            }

            if (!flow.Feed(signal)) return;

            _interrupt.DropPressResponses();

            WLog.Line("scenario_page", secret: false,
                ("scenario", script.Scenario), ("signal", signal),
                ("from", from), ("to", flow.Index), ("pages", flow.PageCount));

            if (flow.Finished)
            {
                WLog.Line("scenario_flow_finished", secret: false, ("scenario", script.Scenario));
            }
        }

        public void Tick(long now)
        {
            if (_d.ClientScenarioSession) GuardVanillaEscMenu();

            if (_script == null && _d.ClientScenarioSession && _d.IsHostSessionActive)
            {
                ScenarioId scenario = ScenarioRun.Scenario;
                ScenarioDefinition? definition = ScenarioRun.Definition;
                if (definition == null)
                {
                    WLog.Line("scenario_flow_start_error", secret: false,
                        ("scenario", scenario), ("reason", "no_definition"));
                    ScenarioRun.ExitToTitle("no_definition");
                }
                else
                {
                    try
                    {
                        var script = new ScenarioScriptRun(scenario, definition);
                        _script = script;
                        WLog.Line("scenario_flow_started", secret: false,
                            ("scenario", script.Scenario), ("pages", script.Flow.PageCount));
                    }
                    catch (System.Exception e)
                    {
                        WLog.Line("scenario_flow_start_error", secret: false,
                            ("scenario", scenario), ("reason", "invalid_script"), ("err", e.Message));
                        ScenarioRun.ExitToTitle("invalid_script");
                    }
                }
            }

            TickGuide(now);

            if (!_d.ClientScenarioSession) return;

            if (_interrupt.Broken)
            {
                if (now >= _interrupt.BrokenExitAtMs)
                {
                    ScenarioRun.ExitToTitle(_interrupt.BrokenKind.ToString());
                }
                return;
            }

            TickMapClose();
            TickWolfWatch();
            TickShamanWatch();
            TickCorpseHint(now);
            TickExtractionWatch(now);
            TickGrabRelease(now);
            TickFakeBodies();
            TickCue(now);
            TickQuotaBagWatch(now);
            TickTruckLeash(now);
            TickFatalBlast(now);
            TickBreakWatchdog(now);
        }

        private static void GuardVanillaEscMenu()
        {
            try
            {
                Session.DisableEscMenu(1f);
                if (ScenarioWorld.TryClosePauseMenu())
                {
                    WLog.Line("scenario_pause_menu_closed", secret: false, ("scenario", ScenarioRun.Scenario));
                }
            }
            catch (System.Exception e)
            {
                ScenarioVanillaProbe.Failed("esc_menu_guard", e);
            }
        }

        internal bool CheckmateArmed
            => !_d.ClientScenarioSession
               || (_script != null && _script.Flow.HasEntered(ScenarioMilestone.CheckmateArmed));

        public void Reset()
        {
            ScenarioScriptRun? script = _script;
            if (script != null)
            {
                WLog.Line("scenario_flow_ended", secret: false,
                    ("scenario", script.Scenario),
                    ("page", script.Flow.Index),
                    ("pages", script.Flow.PageCount));
            }

            _script = null;

            _interrupt.Reset();
            ScenarioEnemyHold.Release();
            ResetWatch();
            ResetCue();
            ResetQuotaBag();
            ResetBreak();
            ResetBomber();
            ResetGuide();
            ResetDecode();
        }

        private static bool TryLocalPlayerPosition(out Vector3 position)
            => LocalPlayer.TryGetPosition(out position);

        private int ParticipantActor(int participantId)
            => _script != null ? _script.Definition.Cast.ActorOf(participantId, _d.LocalActor) : 0;

        private bool TryBotActor(int participantId, out int actor)
        {
            actor = ParticipantActor(participantId);
            return actor != 0 && actor != _d.LocalActor;
        }

        private Vector3? BotFootPos(int participantId)
            => TryBotActor(participantId, out int actor) ? _d.ResolveBodyWorldPos(actor) : null;
    }
}
