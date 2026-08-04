using System;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private ShamanSense _shamanSense;
        private float _shamanGazeFullSecSnapshot;
        private float _shamanCooldownSecSnapshot;

        private readonly ShamanSensePanel _shamanPanel = new ShamanSensePanel();

        private const float StormLoopVolumeWeak = 0.30f;
        private const float StormLoopVolumeMedium = 0.60f;
        private const float StormLoopVolumeStrong = 1f;

        private const int ShamanSobVariantCount = 11;
        private int _lastShamanSobIndex = -1;

        private const float ShamanStillSpeedMps = 0.5f;

        private readonly GazeConeLock _shamanGazeLock = new GazeConeLock();

        private bool _shamanStartCooldownArmed;

        private const float ShamanDripVolume = 0.7f;

        private Vector3? _shamanLastLocalPos;

        private float _shamanLastTickTime;

        private void TickShamanClient(long now)
        {
            try
            {
                if (!IsRoundActiveClient || _localRole != Role.Shaman)
                {
                    HideShamanUi();
                    _shamanLastTickTime = 0f;
                    _shamanStartCooldownArmed = false;
                    return;
                }

                float unscaledNow = Time.unscaledTime;
                float delta = _shamanLastTickTime > 0f ? unscaledNow - _shamanLastTickTime : 0f;
                _shamanLastTickTime = unscaledNow;
                if (delta < 0f) delta = 0f;
                if (delta > 0.5f) delta = 0.5f;

                EnsureShamanSense();

                if (!_shamanStartCooldownArmed)
                {
                    _shamanSense.BeginCooldown();
                    _shamanStartCooldownArmed = true;
                }

                bool suspend = ShamanSenseGate.ShouldSuspend(
                    ClientPhase,
                    IsLocalAlive(),
                    _meetingClient.MeetingActive,
                    _meetingClient.WarpDone(now),
                    _meetingClient.Kind);

                float? nearestDist = null;
                bool inView = false;
                bool stationary = false;
                if (!suspend)
                {
                    PlayerAvatar localAvatar = ResolveAvatar(LocalActor);
                    if (localAvatar != null && localAvatar.transform != null)
                    {
                        Vector3 localPos = localAvatar.transform.position;
                        bool posStill = false;
                        if (_shamanLastLocalPos != null && delta > 0f)
                        {
                            float speed = Vector3.Distance(localPos, _shamanLastLocalPos.Value) / delta;
                            posStill = speed < ShamanStillSpeedMps;
                        }
                        _shamanLastLocalPos = localPos;

                        bool viewHeld = false;
                        Camera cam = Camera.main;
                        if (cam != null)
                        {
                            Vector3 fwd = cam.transform.forward;
                            viewHeld = _shamanGazeLock.Update(fwd.x, fwd.y, fwd.z);
                        }
                        else
                        {
                            _shamanGazeLock.Reset();
                        }
                        stationary = posStill && viewHeld;

                        Vector3? nearest = FindNearestUnannouncedCorpse(localPos, out float dist);
                        if (nearest != null)
                        {
                            nearestDist = dist;
                            inView = IsInLocalViewFrustum(nearest.Value);
                        }
                    }
                    else
                    {
                        _shamanLastLocalPos = null;
                        _shamanGazeLock.Reset();
                    }
                }
                else
                {
                    _shamanLastLocalPos = null;
                    _shamanGazeLock.Reset();
                }

                ShamanStormTier tier = _shamanSense.TickStorm(nearestDist, suspend,
                    ClientShamanStormWeakMeters, ClientShamanStormMediumMeters,
                    ClientShamanStormStrongMeters);
                bool ghostFired = _shamanSense.TickGaze(inView, stationary, delta, suspend,
                    out bool dripFired);

                if (tier != ShamanStormTier.None)
                {
                    MaybeShowTutorial(TutorialId.ShamanStormEntered);
                }

                if (dripFired)
                {
                    EnsureSfxBuilt();
                    _sfxPlayer.Play("sfx_shaman_drip", ShamanDripVolume);
                    EnsurePanelBuilt(_shamanPanel);
                    _shamanPanel.PlayDripRipple();
                    MaybeShowTutorial(TutorialId.ShamanTranceEntered);
                }

                if (ghostFired)
                {
                    WLog.Line("shaman_ghost", secret: true,
                        ("dist", nearestDist != null ? (int)nearestDist.Value : -1));
                    EnsureSfxBuilt();
                    _sfxPlayer.Play("sfx_shaman_storm");
                    _sfxPlayer.Play(NextShamanSobKey());
                    MaybeShowTutorial(TutorialId.ShamanGhostSighted);
                }

                EnsurePanelBuilt(_shamanPanel);
                if (_shamanPanel.Exists)
                {
                    ShamanStormTier visualTier = LastRunGate.IsLastRunActive()
                        ? ShamanStormTier.None
                        : tier;
                    _shamanPanel.Tick(visualTier, _shamanSense.GhostVisible,
                        _shamanSense.GazeArmed, delta);
                }

                if (tier != ShamanStormTier.None)
                {
                    EnsureSfxBuilt();
                    _sfxPlayer.PlayLoop("sfx_shaman_storm", StormLoopVolume(tier));
                }
                else
                {
                    _sfxPlayer.StopLoop();
                }
            }
            catch (Exception e)
            {
                WLog.Line("shaman_client_tick_error", secret: false, ("err", e.Message));
            }
        }

        private Vector3? FindNearestUnannouncedCorpse(Vector3 fromPos, out float bestDist)
        {
            bestDist = float.MaxValue;
            Vector3? best = null;

            var director = GameDirector.instance;
            if (director != null && director.PlayerList != null
                && Registry != null && Registry.Available)
            {
                foreach (PlayerAvatar avatar in director.PlayerList)
                {
                    if (avatar == null) continue;
                    int actor = Registry.ResolveActor(avatar);
                    if (!_meetingClient.IsDeadUnannounced(actor)) continue;
                    if (!TruckWarper.TryGetDeathHeadPosition(avatar, out Vector3 headPos)) continue;
                    float dist = Vector3.Distance(fromPos, headPos);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = headPos;
                    }
                }
            }

            if (FakeBodies.Any)
            {
                foreach ((int actor, Vector3 bodyPos) in FakeBodies.Snapshot())
                {
                    if (!_meetingClient.IsDeadUnannounced(actor)) continue;
                    float dist = Vector3.Distance(fromPos, bodyPos);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = bodyPos;
                    }
                }
            }
            return best;
        }

        private static bool IsInLocalViewFrustum(Vector3 worldPos)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        }

        private void EnsureShamanSense()
        {
            float fullSec = ClientShamanGazeFullSec;
            if (fullSec <= 0f) fullSec = 5f;
            float cooldownSec = ClientShamanGhostCooldownSec;
            if (cooldownSec < 0f) cooldownSec = 0f;
            if (_shamanSense == null
                || Mathf.Abs(fullSec - _shamanGazeFullSecSnapshot) > 0.01f
                || Mathf.Abs(cooldownSec - _shamanCooldownSecSnapshot) > 0.01f)
            {
                _shamanSense = new ShamanSense(fullSec, cooldownSec);
                _shamanGazeFullSecSnapshot = fullSec;
                _shamanCooldownSecSnapshot = cooldownSec;
            }
        }

        private void HideShamanUi()
        {
            _shamanPanel?.Hide();
            _sfxPlayer.StopLoop();
        }

        private string NextShamanSobKey()
        {
            int index = UnityEngine.Random.Range(0, ShamanSobVariantCount);
            if (index == _lastShamanSobIndex) index = (index + 1) % ShamanSobVariantCount;
            _lastShamanSobIndex = index;
            return string.Format("sfx_shaman_sob_{0:00}", index + 1);
        }

        private static float StormLoopVolume(ShamanStormTier tier)
        {
            switch (tier)
            {
                case ShamanStormTier.Strong: return StormLoopVolumeStrong;
                case ShamanStormTier.Medium: return StormLoopVolumeMedium;
                case ShamanStormTier.Weak: return StormLoopVolumeWeak;
                default: return 0f;
            }
        }

        private void ResetShamanClient()
        {
            _shamanSense?.Reset();
            _shamanLastTickTime = 0f;
            _shamanStartCooldownArmed = false;
            _lastShamanSobIndex = -1;
            _shamanLastLocalPos = null;
            _shamanGazeLock.Reset();
            HideShamanUi();
        }
    }
}
