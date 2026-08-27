using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class BomberClientController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private readonly BombClientState _bomberClient = new BombClientState();

        private ProximityGauge _bomberProximity;
        private ProximityGauge _selfDefenseProximity;
        private float _bomberProximityFullSec;

        private bool _warningShown;

        private readonly ToastRepeatGate _truckZoneDenyGate = new ToastRepeatGate();

        private readonly BomberStatusPanel _bomberHud = new BomberStatusPanel();
        private readonly BombIconPresenter _bombIconPresenter = new BombIconPresenter();
        private readonly BombWarningPresenter _bombWarningPresenter = new BombWarningPresenter();

        private float _bomberLastTickTime;
        private bool _bomberHasPlantedThisRound;

        internal BomberClientController(WerewolfDirector director)
        {
            _d = director;
        }

        internal ProximityGauge DebugProximityGauge => _bomberProximity;

        internal void ApplyBomberState(int targetActor, byte ammo, byte lastDenyByte,
            long plantReadyUnixMs, long detonateReadyUnixMs)
        {
            bool previouslyHadBomb = _bomberClient.HasBomb;
            int previousTarget = _bomberClient.TargetActor;
            var snap = new BomberStateSnapshot(
                targetActor, ammo, plantReadyUnixMs, detonateReadyUnixMs,
                (BombDenyReason)lastDenyByte);
            _bomberClient.ApplyState(snap);

            if (_d.LocalRoleClient == Role.Bomber
                && snap.LastDeny == BombDenyReason.None
                && snap.TargetActor != -1
                && (!previouslyHadBomb || previousTarget != snap.TargetActor))
            {
                _bomberHasPlantedThisRound = true;
                _d.EnsurePanelBuilt(_bombIconPresenter);
                _bombIconPresenter.BeginPlantFlight(snap.TargetActor);
                _d.MaybeShowTutorial(TutorialId.BombPlantedAsBomber);
                _d.FeedScenarioSignal(_d.ScenarioWrongBombTarget(snap.TargetActor)
                    ? ScenarioSignal.BombOnWrongTarget
                    : ScenarioSignal.BombPlanted);
            }

            if (snap.LastDeny != BombDenyReason.None)
            {
                bool scenarioOnly = snap.LastDeny == BombDenyReason.ScenarioDistance
                                    || snap.LastDeny == BombDenyReason.ScenarioTarget
                                    || snap.LastDeny == BombDenyReason.ScenarioTargetMoving;
                string message = scenarioOnly ? null : FormatBomberDenyToast(snap.LastDeny);
                if (!string.IsNullOrEmpty(message) && TryPassDenyToast(snap.LastDeny))
                {
                    _d.PushRawToast(message);
                }
                switch (snap.LastDeny)
                {
                    case BombDenyReason.TruckZone:
                        _d.FeedScenarioSignal(ScenarioSignal.BombDeniedTruckZone);
                        break;
                    case BombDenyReason.ScenarioTarget:
                        _d.FeedScenarioSignal(ScenarioSignal.BombOnWrongTarget);
                        break;
                    case BombDenyReason.ScenarioDistance:
                        _d.FeedScenarioSignal(ScenarioSignal.BombDeniedDistance);
                        break;
                    case BombDenyReason.ScenarioTargetMoving:
                        _d.FeedScenarioSignal(ScenarioSignal.BombDeniedTargetMoving);
                        break;
                }
                _bomberClient.ConsumeLastDeny();
            }
            WLog.Line("recv_bomberstate", secret: true,
                ("target", targetActor), ("ammo", ammo), ("deny", snap.LastDeny));
        }

        private bool TryPassDenyToast(BombDenyReason reason)
            => reason != BombDenyReason.TruckZone
               || _truckZoneDenyGate.TryPass(SessionClock.NowMs(),
                      RolesClientController.ToastDurationSec());

        internal void ApplyBombDetonation(int targetActor, long detonateAtUnixMs)
        {
            _bomberClient.ApplyPendingDetonation(targetActor, detonateAtUnixMs);
            _warningShown = false;
            WLog.Line("recv_bombdetonation", secret: true,
                ("target", targetActor), ("atUnixMs", detonateAtUnixMs));
            _d.FeedScenarioSignal(ScenarioSignal.BombDetonationAccepted);
        }

        internal int TargetActorClient => _bomberClient.TargetActor;

        public void Tick(long now)
        {
            try
            {
                if (!_d.IsRoundActiveClient)
                {
                    HideBomberUi();
                    _bomberLastTickTime = 0f;
                    return;
                }

                float unscaledNow = Time.unscaledTime;
                float delta = _bomberLastTickTime > 0f ? unscaledNow - _bomberLastTickTime : 0f;
                _bomberLastTickTime = unscaledNow;
                if (delta < 0f) delta = 0f;
                if (delta > 0.5f) delta = 0.5f;

                EnsureBomberGauges();

                bool localIsBomber = _d.LocalRoleClient == Role.Bomber;
                bool bomberPossible = _d.ClientBomberPossible;
                bool phaseSuspended = _d.ClientPhase != GamePhase.Play;
                bool initialCooldown = !_bomberHasPlantedThisRound
                    && (localIsBomber
                        ? now < _bomberClient.PlantReadyUnixMs
                        : _d.GameStartUnixMsClient > 0
                            && now < _d.GameStartUnixMsClient + _d.ClientBomberInitialCooldownSec * 1000L);
                bool afterMeetingCooldown = _d.LastMeetingEndUnixMsClient > 0
                    && now < _d.LastMeetingEndUnixMsClient + _d.ClientBomberCooldownSec * 1000L;
                bool bomberGaugeSuspended = phaseSuspended || initialCooldown || afterMeetingCooldown;
                bool warpedInMeeting = _d.MeetingClientView.MeetingActive
                    && _d.MeetingClientView.WarpDone(now);
                if (warpedInMeeting)
                {
                    _bomberProximity?.ResetAll();
                    _selfDefenseProximity?.ResetAll();
                }

                if (bomberPossible) BombExplosionDriver.WarmupOnce();
                int localActor = _d.LocalActor;
                PlayerHandle localAvatar = Players.Resolve(localActor);
                Vector3? localPos = localAvatar.Position;

                List<PlayerHandle> players = Players.All();
                float proximity = _d.ClientBomberProximityMeters;
                if (proximity <= 0f) proximity = 8f;
                float selfDefenseProximity = proximity + GameConfig.SelfDefenseProximityMarginMeters;

                List<(int actor, Vector3 pos)> bodies = null;
                if (localAvatar.IsValid
                    && ((localIsBomber && _bomberProximity != null)
                        || (bomberPossible && _selfDefenseProximity != null)))
                {
                    bodies = CollectOtherBodies(players, localActor);
                }

                Dictionary<int, float> radialTargets = null;
                if (localIsBomber && _bomberProximity != null && bodies != null)
                {
                    radialTargets = new Dictionary<int, float>();
                    foreach ((int actor, Vector3 bodyPos) in bodies)
                    {
                        if (_d.IsDeadActorClient(actor))
                        {
                            _bomberProximity.Remove(actor);
                            continue;
                        }
                        float dist = Vector3.Distance(localPos.Value, bodyPos);
                        bool within = dist <= proximity
                            && VisionProbe.BodyToBodyClear(localPos.Value, bodyPos);
                        _bomberProximity.Tick(actor, within, delta, bomberGaugeSuspended);
                        float ratio = _bomberProximity.Ratio(actor);
                        if (ratio > 0f) radialTargets[actor] = ratio;
                    }
                }

                if (!localIsBomber && bomberPossible && _selfDefenseProximity != null && bodies != null)
                {
                    foreach ((int actor, Vector3 bodyPos) in bodies)
                    {
                        if (_d.IsDeadActorClient(actor))
                        {
                            _selfDefenseProximity.Remove(actor);
                            continue;
                        }
                        float dist = Vector3.Distance(localPos.Value, bodyPos);
                        bool within = dist <= selfDefenseProximity
                            && VisionProbe.BodyToBodyClear(localPos.Value, bodyPos);
                        _selfDefenseProximity.Tick(actor, within, delta, bomberGaugeSuspended);
                    }
                    if (_selfDefenseProximity.TryGetNotifyEdge(out _))
                    {
                        _d.PushRawToast(Texts.Get(TextId.BomberProximityWarning),
                            NoticeSfx.BomberProximityWarningClipKey);
                        if (_d.LocalIsVillagerTeam)
                        {
                            _d.MaybeShowTutorial(TutorialId.BomberProximityWarnedAsVillager);
                        }
                    }
                }

                if (localIsBomber && Plugin.Bindings != null && InputGate.KeysFree)
                {
                    KeyCode plantKey = Plugin.Bindings.BomberPlantKey.Value;
                    KeyCode detonateKey = Plugin.Bindings.BomberDetonateKey.Value;

                    if (Input.GetKeyDown(plantKey) && _d.ScenarioBombAlreadyOnTarget)
                    {
                        _d.FeedScenarioSignal(ScenarioSignal.BombAlreadyOnTarget);
                    }
                    else if (Input.GetKeyDown(plantKey))
                    {
                        int excludedTarget = _bomberClient.HasBomb
                            ? _bomberClient.TargetActor : -1;
                        int target = FindNearestFullTarget(localPos, bodies, excludedTarget);
                        if (target == -1)
                        {
                            _d.PushRawToast(Texts.Get(TextId.BomberDenyNoFullTarget));
                        }
                        else
                        {
                            SendBomberRoleAction(BombRoleActionSubtype.Plant, target);
                        }
                    }
                    if (Input.GetKeyDown(detonateKey))
                    {
                        SendBomberRoleAction(BombRoleActionSubtype.Detonate, 0);
                    }
                }

                DrivePendingDetonation(now, localActor, players);

                DriveBomberUi(now, localActor, localIsBomber, radialTargets, players);

            }
            catch (Exception e)
            {
                WLog.Line("bomber_client_tick_error", secret: false, ("err", e.Message));
            }
        }

        private void DrivePendingDetonation(long now, int localActor, IReadOnlyList<PlayerHandle> players)
        {
            if (!_bomberClient.HasPendingDetonation) return;

            int targetActor = _bomberClient.PendingTargetActor;
            long atMs = _bomberClient.PendingDetonateAtUnixMs;
            PlayerHandle targetAvatar = Players.Resolve(targetActor);

            if (!_warningShown)
            {
                _warningShown = true;
                _d.EnsurePanelBuilt(_bombWarningPresenter);
                bool localIsTarget = targetActor == localActor;
                float radius = _d.ClientBomberBlastRadiusMeters;
                Vector3? fakeAnchor = null;
                if (!targetAvatar.IsValid && FakeBodies.TryGetPosition(targetActor, out Vector3 fp))
                {
                    fakeAnchor = fp;
                }
                _bombWarningPresenter.Show(localIsTarget, targetAvatar, radius, fakeAnchor);
            }
            _bombWarningPresenter.Tick();

            if (now >= atMs)
            {
                PlayerHandle epicenter = targetAvatar.IsValid ? targetAvatar : Players.Resolve(targetActor);
                bool localIsBomber = _d.LocalRoleClient == Role.Bomber;
                int playerDamage = _d.ClientBomberBlastPlayerDamage;
                int enemyDamage = _d.ClientBomberBlastEnemyDamage;
                float radius = _d.ClientBomberBlastRadiusMeters;
                if (epicenter.IsValid)
                {
                    BombExplosionDriver.Detonate(epicenter, localIsBomber,
                        playerDamage, enemyDamage, radius);
                }
                else if (FakeBodies.TryGetPosition(targetActor, out Vector3 fakeEpicenter))
                {
                    BombExplosionDriver.DetonateAt(fakeEpicenter + Vector3.up * 0.5f,
                        localIsBomber, playerDamage, enemyDamage, radius);
                }
                else
                {
                    WLog.Line("bomb_explode_skip", secret: false,
                        ("reason", "epicenter_unresolved"), ("target", targetActor));
                }
                if (targetActor == localActor && _d.LocalIsVillagerTeam)
                {
                    _d.MaybeShowTutorial(TutorialId.SelfBombExplodedAsVillager);
                }
                _bomberClient.ClearPendingDetonation();
                _bombWarningPresenter.Hide();
                _warningShown = false;
                _d.FeedScenarioSignal(ScenarioSignal.BombExploded);
            }
        }

        private void DriveBomberUi(long now, int localActor, bool localIsBomber,
            Dictionary<int, float> radialTargets, IReadOnlyList<PlayerHandle> players)
        {
            _d.EnsurePanelBuilt(_bomberHud);
            bool warpedInMeeting = _d.MeetingClientView.MeetingActive
                && _d.MeetingClientView.WarpDone(now);
            if (localIsBomber && !warpedInMeeting)
            {
                long plantRemMs = Math.Max(0L, _bomberClient.PlantReadyUnixMs - now);
                long detonateRemMs = _bomberClient.HasBomb
                    ? Math.Max(0L, _bomberClient.DetonateReadyUnixMs - now)
                    : 0L;
                int plantCdSec = (int)((plantRemMs + 999) / 1000);
                int detonateCdSec = (int)((detonateRemMs + 999) / 1000);
                bool phaseAllows = _d.ClientPhase == GamePhase.Play;
                int excludedTarget = _bomberClient.HasBomb ? _bomberClient.TargetActor : -1;
                bool hasPlantResource = _bomberClient.HasBomb || _bomberClient.Ammo > 0;
                float plantFraction = BomberHudModel.PlantFraction(phaseAllows, plantCdSec,
                    hasPlantResource, radialTargets, excludedTarget);
                float detonateBrightFrac;
                if (!_bomberClient.HasBomb)
                {
                    detonateBrightFrac = 0f;
                }
                else if (detonateRemMs <= 0)
                {
                    detonateBrightFrac = 1f;
                }
                else
                {
                    long totalCdMs = Math.Max(1, _d.ClientBomberCooldownSec) * 1000L;
                    detonateBrightFrac = 1f - Math.Min(1f, detonateRemMs / (float)totalCdMs);
                }
                string plantKey = Plugin.Bindings != null
                    ? Plugin.Bindings.BomberPlantKey.Value.ToString() : "?";
                string detonateKey = Plugin.Bindings != null
                    ? Plugin.Bindings.BomberDetonateKey.Value.ToString() : "?";
                _bomberHud.Tick(true, plantFraction, plantCdSec, _bomberClient.Ammo,
                    detonateBrightFrac, detonateCdSec, plantKey, detonateKey);
            }
            else
            {
                _bomberHud.Tick(false, 0f, 0, 0, 0f, 0, null, null);
            }

            _d.EnsurePanelBuilt(_bombIconPresenter);
            var iconActors = new HashSet<int>();
            if (localIsBomber && _bomberClient.HasBomb) iconActors.Add(_bomberClient.TargetActor);
            if (_bomberClient.HasPendingDetonation) iconActors.Add(_bomberClient.PendingTargetActor);
            bool showRadial = localIsBomber && radialTargets != null && radialTargets.Count > 0;
            _bombIconPresenter.Tick(showRadial || iconActors.Count > 0,
                showRadial ? radialTargets : null, iconActors, _d.ResolveBodyWorldPos);
        }

        private int FindNearestFullTarget(Vector3? localPos,
            IReadOnlyList<(int actor, Vector3 pos)> bodies, int excludedActor)
        {
            if (_bomberProximity == null || localPos == null || bodies == null) return -1;
            int bestActor = -1;
            float bestDist = float.MaxValue;
            foreach ((int actor, Vector3 bodyPos) in bodies)
            {
                if (actor == excludedActor) continue;
                if (!_bomberProximity.IsFull(actor)) continue;
                float dist = Vector3.Distance(localPos.Value, bodyPos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestActor = actor;
                }
            }
            return bestActor;
        }

        private List<(int actor, Vector3 pos)> CollectOtherBodies(
            IReadOnlyList<PlayerHandle> players, int localActor)
        {
            var bodies = new List<(int actor, Vector3 pos)>();
            if (players != null)
            {
                foreach (PlayerHandle av in players)
                {
                    if (!av.TryGetPosition(out Vector3 avatarPos)) continue;
                    int actor = _d.Registry != null ? av.Actor : -1;
                    if (actor < 0 || actor == localActor) continue;
                    bodies.Add((actor, avatarPos));
                }
            }
            if (FakeBodies.Any)
            {
                foreach ((int actor, Vector3 pos) in FakeBodies.Snapshot())
                {
                    if (actor == localActor) continue;
                    bodies.Add((actor, pos));
                }
            }
            return bodies;
        }

        private void SendBomberRoleAction(byte subtype, int arg)
        {
            INetBus bus = _d.ClientBus;
            if (bus == null)
            {
                WLog.Line("bomber_send_fail", secret: true, ("subtype", subtype), ("reason", "no_bus"));
                return;
            }
            bus.SendToMaster(MessageCodes.RoleAction, new object[] { subtype, arg, (byte)0 });
        }

        private void EnsureBomberGauges()
        {
            float fullSec = _d.ClientBomberGaugeFullSec;
            if (fullSec <= 0f) fullSec = 20f;
            if (_bomberProximity == null || Mathf.Abs(fullSec - _bomberProximityFullSec) > 0.01f)
            {
                _bomberProximity = new ProximityGauge(fullSec);
                _selfDefenseProximity = new ProximityGauge(fullSec);
                _bomberProximityFullSec = fullSec;
            }
        }

        private void HideBomberUi()
        {
            _bomberHud?.Hide();
            _bombIconPresenter?.Hide();
            if (_warningShown)
            {
                _bombWarningPresenter?.Hide();
                _warningShown = false;
            }
        }

        public void Reset()
        {
            _bomberClient.Reset();
            _bomberProximity?.ResetAll();
            _selfDefenseProximity?.ResetAll();
            _bomberLastTickTime = 0f;
            _bomberHasPlantedThisRound = false;
            _warningShown = false;
            _truckZoneDenyGate.Reset();
            HideBomberUi();
            BombExplosionDriver.ResetWarmup();
        }

        public void DestroyUi()
        {
            _bomberHud.Destroy();
            _bombIconPresenter.Destroy();
            _bombWarningPresenter.Destroy();
        }

        private static string FormatBomberDenyToast(BombDenyReason reason)
        {
            switch (reason)
            {
                case BombDenyReason.NoAmmo: return Texts.Get(TextId.BomberDenyNoAmmo);
                case BombDenyReason.NoFullTarget: return Texts.Get(TextId.BomberDenyNoFullTarget);
                case BombDenyReason.PlantCooldown: return Texts.Get(TextId.BomberDenyPlantCooldown);
                case BombDenyReason.DetonateCooldown: return Texts.Get(TextId.BomberDenyDetonateCooldown);
                case BombDenyReason.NoBomb: return Texts.Get(TextId.BomberDenyNoBomb);
                case BombDenyReason.MeetingLocked: return Texts.Get(TextId.BomberDenyMeetingLocked);
                case BombDenyReason.TruckZone: return Texts.Get(TextId.BomberDenyTruckZone);
                case BombDenyReason.TargetDead: return Texts.Get(TextId.BomberDudTargetDead);
                default: return null;
            }
        }
    }
}
