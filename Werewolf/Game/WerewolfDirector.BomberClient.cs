using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private readonly BombClientState _bomberClient = new BombClientState();

        private ProximityGauge _bomberProximity;
        private ProximityGauge _selfDefenseProximity;
        private float _bomberProximityFullSec;

        private bool _warningShown;

        private readonly BomberStatusPanel _bomberHud = new BomberStatusPanel();
        private readonly BombIconPresenter _bombIconPresenter = new BombIconPresenter();
        private readonly BombWarningPresenter _bombWarningPresenter = new BombWarningPresenter();

        private float _bomberLastTickTime;
        private bool _bomberHasPlantedThisRound;

        internal void ApplyBomberState(int targetActor, byte ammo, byte lastDenyByte,
            long plantReadyUnixMs, long detonateReadyUnixMs)
        {
            bool previouslyHadBomb = _bomberClient.HasBomb;
            int previousTarget = _bomberClient.TargetActor;
            var snap = new BomberStateSnapshot(
                targetActor, ammo, plantReadyUnixMs, detonateReadyUnixMs,
                (BombDenyReason)lastDenyByte);
            _bomberClient.ApplyState(snap);

            if (_localRole == Role.Bomber
                && snap.LastDeny == BombDenyReason.None
                && snap.TargetActor != -1
                && (!previouslyHadBomb || previousTarget != snap.TargetActor))
            {
                _bomberHasPlantedThisRound = true;
                EnsurePanelBuilt(_bombIconPresenter);
                _bombIconPresenter.BeginPlantFlight(snap.TargetActor);
                MaybeShowTutorial(TutorialId.BombPlantedAsBomber);
            }

            if (snap.LastDeny != BombDenyReason.None)
            {
                string message = FormatBomberDenyToast(snap.LastDeny);
                if (!string.IsNullOrEmpty(message)) PushRawToast(message);
                _bomberClient.ConsumeLastDeny();
            }
            WLog.Line("recv_bomberstate", secret: true,
                ("target", targetActor), ("ammo", ammo), ("deny", snap.LastDeny));
        }

        internal void ApplyBombDetonation(int targetActor, long detonateAtUnixMs)
        {
            _bomberClient.ApplyPendingDetonation(targetActor, detonateAtUnixMs);
            _warningShown = false;
            WLog.Line("recv_bombdetonation", secret: true,
                ("target", targetActor), ("atUnixMs", detonateAtUnixMs));
        }

        private void TickBomberClient(long now)
        {
            try
            {
                if (!IsRoundActiveClient)
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

                bool localIsBomber = _localRole == Role.Bomber;
                bool bomberPossible = ClientBomberPossible;
                bool phaseSuspended = ClientPhase != GamePhase.Play;
                bool initialCooldown = !_bomberHasPlantedThisRound
                    && (localIsBomber
                        ? now < _bomberClient.PlantReadyUnixMs
                        : _gameStartUnixMsClient > 0
                            && now < _gameStartUnixMsClient + ClientBomberInitialCooldownSec * 1000L);
                bool afterMeetingCooldown = _lastMeetingEndUnixMsClient > 0
                    && now < _lastMeetingEndUnixMsClient + ClientBomberCooldownSec * 1000L;
                bool bomberGaugeSuspended = phaseSuspended || initialCooldown || afterMeetingCooldown;
                bool warpedInMeeting = _meetingClient.MeetingActive && _meetingClient.WarpDone(now);
                if (warpedInMeeting)
                {
                    _bomberProximity?.ResetAll();
                    _selfDefenseProximity?.ResetAll();
                }

                if (bomberPossible) BombExplosionDriver.WarmupOnce();
                int localActor = LocalActor;
                PlayerAvatar localAvatar = ResolveAvatar(localActor);
                Vector3? localPos = localAvatar != null ? (Vector3?)localAvatar.transform.position : null;

                var players = GameDirector.instance != null ? GameDirector.instance.PlayerList : null;
                float proximity = ClientBomberProximityMeters;
                if (proximity <= 0f) proximity = 8f;
                float selfDefenseProximity = proximity + GameConfig.SelfDefenseProximityMarginMeters;

                List<(int actor, Vector3 pos)> bodies = null;
                if (localAvatar != null
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
                        if (IsDeadActorClient(actor))
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
                        if (IsDeadActorClient(actor))
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
                        PushRawToast(Texts.Get(TextId.BomberProximityWarning),
                            NoticeSfx.BomberProximityWarningClipKey);
                        if (LocalIsVillagerTeam)
                        {
                            MaybeShowTutorial(TutorialId.BomberProximityWarnedAsVillager);
                        }
                    }
                }

                if (localIsBomber && Plugin.Bindings != null && InputGate.KeysFree)
                {
                    KeyCode plantKey = Plugin.Bindings.BomberPlantKey.Value;
                    KeyCode detonateKey = Plugin.Bindings.BomberDetonateKey.Value;

                    if (Input.GetKeyDown(plantKey))
                    {
                        int excludedTarget = _bomberClient.HasBomb
                            ? _bomberClient.TargetActor : -1;
                        int target = FindNearestFullTarget(localPos, bodies, excludedTarget);
                        if (target == -1)
                        {
                            PushRawToast(Texts.Get(TextId.BomberDenyNoFullTarget));
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

        private void DrivePendingDetonation(long now, int localActor, IReadOnlyList<PlayerAvatar> players)
        {
            if (!_bomberClient.HasPendingDetonation) return;

            int targetActor = _bomberClient.PendingTargetActor;
            long atMs = _bomberClient.PendingDetonateAtUnixMs;
            PlayerAvatar targetAvatar = ResolveAvatar(targetActor);

            if (!_warningShown)
            {
                _warningShown = true;
                EnsurePanelBuilt(_bombWarningPresenter);
                bool localIsTarget = targetActor == localActor;
                float radius = ClientBomberBlastRadiusMeters;
                Vector3? fakeAnchor = null;
                if (targetAvatar == null && FakeBodies.TryGetPosition(targetActor, out Vector3 fp))
                {
                    fakeAnchor = fp;
                }
                _bombWarningPresenter.Show(localIsTarget, targetAvatar, radius, fakeAnchor);
            }
            _bombWarningPresenter.Tick();

            if (now >= atMs)
            {
                PlayerAvatar epicenter = targetAvatar ?? ResolveAvatar(targetActor);
                bool localIsBomber = _localRole == Role.Bomber;
                int playerDamage = ClientBomberBlastPlayerDamage;
                int enemyDamage = ClientBomberBlastEnemyDamage;
                float radius = ClientBomberBlastRadiusMeters;
                if (epicenter != null)
                {
                    BombExplosionDriver.Detonate(epicenter, localIsBomber,
                        playerDamage, enemyDamage, radius);
                }
                else if (FakeBodies.TryGetPosition(targetActor, out Vector3 fakeEpicenter))
                {
                    BombExplosionDriver.DetonateAt(fakeEpicenter + Vector3.up * 0.5f, null,
                        localIsBomber, playerDamage, enemyDamage, radius);
                }
                else
                {
                    WLog.Line("bomb_explode_skip", secret: false,
                        ("reason", "epicenter_unresolved"), ("target", targetActor));
                }
                if (targetActor == localActor && LocalIsVillagerTeam)
                {
                    MaybeShowTutorial(TutorialId.SelfBombExplodedAsVillager);
                }
                _bomberClient.ClearPendingDetonation();
                _bombWarningPresenter.Hide();
                _warningShown = false;
            }
        }

        private void DriveBomberUi(long now, int localActor, bool localIsBomber,
            Dictionary<int, float> radialTargets, IReadOnlyList<PlayerAvatar> players)
        {
            EnsurePanelBuilt(_bomberHud);
            bool warpedInMeeting = _meetingClient.MeetingActive && _meetingClient.WarpDone(now);
            if (localIsBomber && !warpedInMeeting)
            {
                long plantRemMs = Math.Max(0L, _bomberClient.PlantReadyUnixMs - now);
                long detonateRemMs = _bomberClient.HasBomb
                    ? Math.Max(0L, _bomberClient.DetonateReadyUnixMs - now)
                    : 0L;
                int plantCdSec = (int)((plantRemMs + 999) / 1000);
                int detonateCdSec = (int)((detonateRemMs + 999) / 1000);
                bool phaseAllows = ClientPhase == GamePhase.Play;
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
                    long totalCdMs = Math.Max(1, ClientBomberCooldownSec) * 1000L;
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

            EnsurePanelBuilt(_bombIconPresenter);
            var iconActors = new HashSet<int>();
            if (localIsBomber && _bomberClient.HasBomb) iconActors.Add(_bomberClient.TargetActor);
            if (_bomberClient.HasPendingDetonation) iconActors.Add(_bomberClient.PendingTargetActor);
            bool showRadial = localIsBomber && radialTargets != null && radialTargets.Count > 0;
            _bombIconPresenter.Tick(showRadial || iconActors.Count > 0,
                showRadial ? radialTargets : null, iconActors, ResolveBodyWorldPos);
        }

        private Vector3? ResolveBodyWorldPos(int actor)
        {
            PlayerAvatar av = ResolveAvatar(actor);
            if (av != null && av.transform != null) return av.transform.position;
            if (FakeBodies.TryGetPosition(actor, out Vector3 pos)) return pos;
            return null;
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
            IReadOnlyList<PlayerAvatar> players, int localActor)
        {
            var bodies = new List<(int actor, Vector3 pos)>();
            if (players != null)
            {
                foreach (PlayerAvatar av in players)
                {
                    if (av == null || av.transform == null) continue;
                    int actor = Registry != null ? Registry.ResolveActor(av) : -1;
                    if (actor < 0 || actor == localActor) continue;
                    bodies.Add((actor, av.transform.position));
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
            if (_bus == null)
            {
                WLog.Line("bomber_send_fail", secret: true, ("subtype", subtype), ("reason", "no_bus"));
                return;
            }
            _bus.SendToMaster(MessageCodes.RoleAction, new object[] { subtype, arg, (byte)0 });
        }

        private void EnsureBomberGauges()
        {
            float fullSec = ClientBomberGaugeFullSec;
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

        private void ResetBomberClient()
        {
            _bomberClient.Reset();
            _bomberProximity?.ResetAll();
            _selfDefenseProximity?.ResetAll();
            _bomberLastTickTime = 0f;
            _bomberHasPlantedThisRound = false;
            _warningShown = false;
            HideBomberUi();
            BombExplosionDriver.ResetWarmup();
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

        private void PushRawToast(string message, string clipKey = NoticeSfx.DefaultClipKey,
            string logKind = "bomber", bool playSfx = true)
        {
            if (string.IsNullOrEmpty(message)) return;
            EnsureToastQueue();
            EnsureRolesUiBuilt();
            _toastQueue.Push(message, NowUnixMs());
            if (playSfx)
            {
                EnsureSfxBuilt();
                _sfxPlayer.Play(clipKey);
            }
            WLog.Line("toast_push_raw", secret: false, ("kind", logKind));
        }
    }
}
