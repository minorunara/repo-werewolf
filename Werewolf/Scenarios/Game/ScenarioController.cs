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

        private ScenarioFlow _flow;

        private ScenarioDefinition _definition;

        private ScenarioId _scenario;

        private bool _started;

        private readonly ScenarioInterruptState _interrupt = new ScenarioInterruptState();

        internal ScenarioController(WerewolfDirector director)
        {
            _d = director;
        }

        internal ScenarioFlow Flow => _flow;

        private bool ScriptRunning => _flow != null && !_flow.Finished && _definition != null;

        internal void FeedSignal(ScenarioSignal signal)
        {
            if (_flow == null || _flow.Finished) return;

            int from = _flow.Index;

            ScenarioNotices notices = _definition != null ? _definition.Notices : default;
            if (notices.EarlyStartSignal.HasValue && signal == notices.EarlyStartSignal.Value
                && !_flow.HasReached(notices.EarlyStartAfter))
            {
                _interrupt.Defer(ScenarioInterrupt.EarlyStart, notices.EarlyStartLines);
            }

            ScenarioPage pressed = _flow.Current;
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

            if (!_flow.Feed(signal)) return;

            _interrupt.DropPressResponses();

            WLog.Line("scenario_page", secret: false,
                ("scenario", _scenario), ("signal", signal),
                ("from", from), ("to", _flow.Index), ("pages", _flow.PageCount));

            if (_flow.Finished)
            {
                WLog.Line("scenario_flow_finished", secret: false, ("scenario", _scenario));
            }
        }

        public void Tick(long now)
        {
            if (_d.ClientScenarioSession) GuardVanillaEscMenu();

            if (!_started && _d.ClientScenarioSession && _d.IsHostSessionActive)
            {
                _started = true;
                _scenario = ScenarioRun.Scenario;
                _definition = ScenarioRun.Definition;
                _flow = _definition != null ? _definition.CreateFlow() : null;
                WLog.Line("scenario_flow_started", secret: false,
                    ("scenario", _scenario), ("pages", _flow != null ? _flow.PageCount : 0));
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
               || (_flow != null && _flow.HasEntered(ScenarioMilestone.CheckmateArmed));

        public void Reset()
        {
            if (_started)
            {
                WLog.Line("scenario_flow_ended", secret: false,
                    ("scenario", _scenario),
                    ("page", _flow != null ? _flow.Index : 0),
                    ("pages", _flow != null ? _flow.PageCount : 0));
            }

            _flow = null;
            _definition = null;
            _scenario = default;
            _started = false;

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
            => _definition != null ? _definition.Cast.ActorOf(participantId, _d.LocalActor) : 0;

        private bool TryBotActor(int participantId, out int actor)
        {
            actor = ParticipantActor(participantId);
            return actor != 0 && actor != _d.LocalActor;
        }

        private Vector3? BotFootPos(int participantId)
            => TryBotActor(participantId, out int actor) ? _d.ResolveBodyWorldPos(actor) : null;
    }
}
