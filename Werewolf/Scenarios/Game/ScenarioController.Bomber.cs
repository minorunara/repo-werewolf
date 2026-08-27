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
        private Vector3? _standSite;
        private Vector3? _farSite;

        private long _fatalBlastArmedAtMs;

        private const long FatalBlastGraceMs = 4000L;

        private const float BodyTickMaxSec = 0.5f;

        private void TickFakeBodies()
        {
            if (!Session.IsHost) return;
            FakeBodies.Tick(Mathf.Min(Time.unscaledDeltaTime, BodyTickMaxSec));
        }

        private void TickFatalBlast(long now)
        {
            if (!ScriptRunning) return;
            if (!_flow.HasEntered(ScenarioMilestone.FatalBlastArmed))
            {
                _fatalBlastArmedAtMs = 0;
                return;
            }

            ScenarioRun.LiftSafety();
            if (_fatalBlastArmedAtMs == 0)
            {
                _fatalBlastArmedAtMs = now;
                return;
            }

            if (!_definition.Watch.BreakIfFatalBlastSurvived) return;
            if (now - _fatalBlastArmedAtMs < FatalBlastGraceMs) return;
            if (!_flow.IsLatched(ScenarioSignal.BombExploded)) return;
            if (_d.IsDeadActorClient(_d.LocalActor)) return;
            Break(ScenarioBreakKind.FatalBlastSurvived, now);
        }

        private ScenarioBlastRule CurrentBlastRule
            => ScriptRunning ? _flow.Current.BlastRule : ScenarioBlastRule.None;

        internal BombDenyReason DeniesDetonation(Vector3? bomberPos, Vector3? targetPos,
                                                 float blastRadiusMeters, bool targetNearTruck,
                                                 int targetActor)
        {
            ScenarioBlastRule rule = CurrentBlastRule;
            bool wrongTarget = targetActor != -1 && IsWrongBombTarget(targetActor);
            if (rule == ScenarioBlastRule.None && !wrongTarget) return BombDenyReason.None;

            float distance = bomberPos.HasValue && targetPos.HasValue
                ? Vector3.Distance(bomberPos.Value, targetPos.Value)
                : float.PositiveInfinity;
            bool targetMoving = targetActor != -1 && FakeBodies.IsMoving(targetActor);
            return ScenarioBlastRules.Resolve(rule, distance, blastRadiusMeters,
                targetNearTruck, targetMoving, wrongTarget);
        }

        internal bool IsWrongBombTarget(int targetActor)
            => TryBombTargetActor(out int actor) && actor != targetActor;

        private void MoveBody(int participantId, ScenarioEntryAction site)
        {
            if (!Session.IsHost) return;
            if (!TryBotActor(participantId, out int actor))
            {
                WLog.Line("scenario_body_skipped", secret: false,
                    ("participant", participantId), ("site", site));
                return;
            }

            if (!TryResolveSite(site, participantId, out Vector3 ground))
            {
                WLog.Line("scenario_body_unresolved", secret: false,
                    ("participant", participantId), ("site", site));
                return;
            }

            FakeBodies.MoveTo(actor, ground, ScenarioBodyLooks.Semibot);
        }

        private bool TryResolveSite(ScenarioEntryAction site, int participantId, out Vector3 ground)
        {
            ground = default;

            switch (site)
            {
                case ScenarioEntryAction.BodyToPlantSite:
                    return TryStandSite(out ground);

                case ScenarioEntryAction.BodyToTruck:
                    return ScenarioProps.TryResolveTruckCorpseGround(
                        TryLocalPlayerPosition(out Vector3 p) ? p : Vector3.zero, out ground);

                case ScenarioEntryAction.BodyToGatherSite:
                    if (!TryStandSite(out Vector3 stand)) return false;
                    ground = ScenarioProps.ResolveAround(stand, participantId);
                    return true;

                case ScenarioEntryAction.BodyToFarSite:
                    if (!TryFarSite(out Vector3 far)) return false;
                    ground = ScenarioProps.ResolveAround(far, participantId);
                    return true;

                default:
                    return false;
            }
        }

        private bool TryStandSite(out Vector3 ground)
        {
            if (_standSite.HasValue)
            {
                ground = _standSite.Value;
                return true;
            }
            if (!ScenarioProps.TryResolveStandSite(TruckSafeRadius(), out ground)) return false;
            _standSite = ground;
            return true;
        }

        private bool TryFarSite(out Vector3 ground)
        {
            ground = default;
            if (_farSite.HasValue)
            {
                ground = _farSite.Value;
                return true;
            }
            if (!TryStandSite(out Vector3 stand)
                || !ScenarioProps.TryResolveFarSite(stand, out ground))
            {
                return false;
            }
            _farSite = ground;
            return true;
        }

        private void ConveneAsParticipant(int participantId)
        {
            int actor = ParticipantActor(participantId);
            if (actor == 0) return;
            _d.HostConveneAsActor(actor);
        }

        private void KillParticipant(int participantId)
        {
            if (!TryBotActor(participantId, out int actor)) return;
            _d.HostRecordDeathByActor(actor, asVote: false);
            FakeBodies.Stop(actor);
            FakeBodies.SetLook(actor, ScenarioBodyLooks.DeathHead);
            WLog.Line("scenario_kill_participant", secret: false, ("participant", participantId));
        }

        private int CurrentBombTarget => ScriptRunning ? _flow.Current.BombTargetParticipantId : 0;

        private bool LocalNearBombTarget()
        {
            if (!TryBombTargetActor(out int actor)) return false;
            if (!TryLocalPlayerPosition(out Vector3 playerPos)) return false;
            if (!FakeBodies.TryGetPosition(actor, out Vector3 targetPos)) return false;
            return Vector3.Distance(playerPos, targetPos) <= ProximityMeters()
                   && VisionProbe.BodyToBodyClear(playerPos, targetPos);
        }

        internal bool BombOnBombTarget()
            => TryBombTargetActor(out int actor) && _d.BomberTargetActorClient == actor;

        private Vector3? BombTargetMarkPos()
            => ScriptRunning && _flow.Current.MarkBombTarget ? BotFootPos(CurrentBombTarget) : null;

        private bool TryBombTargetActor(out int actor)
        {
            actor = 0;
            int participantId = CurrentBombTarget;
            return participantId != 0 && TryBotActor(participantId, out actor);
        }

        private static float TruckSafeRadius()
        {
            GameConfig config = Plugin.GameConfig;
            float r = config != null ? config.BomberTruckSafeRadiusMeters : 0f;
            return r > 0f ? r : GameConfig.DefaultTruckSafeRadiusMeters;
        }

        private float BlastRadius()
        {
            float r = _d.ClientBomberBlastRadiusMeters;
            return r > 0f ? r : GameConfig.DefaultBomberBlastRadiusMeters;
        }

        private float ProximityMeters()
        {
            float r = _d.ClientBomberProximityMeters;
            return r > 0f ? r : GameConfig.DefaultBomberProximityMeters;
        }

        private void ResetBomber()
        {
            _standSite = null;
            _farSite = null;
            _fatalBlastArmedAtMs = 0;
        }
    }
}
