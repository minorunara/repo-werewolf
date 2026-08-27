using System;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class ShamanClientController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private ShamanSense _shamanSense;
        private float _shamanGazeFullSecSnapshot;
        private float _shamanCooldownSecSnapshot;

        private readonly ShamanSensePanel _shamanPanel = new ShamanSensePanel();
        private readonly ShamanHauntingPulse _hauntingPulse = new ShamanHauntingPulse();

        private const float StormLoopVolumeWeak = 0.30f;
        private const float StormLoopVolumeMedium = 0.60f;
        private const float StormLoopVolumeStrong = 1f;

        private const int ShamanSobVariantCount = 11;
        private int _lastShamanSobIndex = -1;

        private const float ShamanStillSpeedMps = 0.5f;

        private readonly GazeConeLock _shamanGazeLock = new GazeConeLock();

        private bool _shamanStartCooldownArmed;

        private const float ShamanDripVolume = 0.7f;

        private const string ShamanHauntingPulseClipKey = "sfx_shaman_haunting_pulse";
        private const float ShamanHauntingPulseVolume = 0.6f;

        private Vector3? _shamanLastLocalPos;

        private float _shamanLastTickTime;

        internal ShamanClientController(WerewolfDirector director)
        {
            _d = director;
        }

        internal bool SenseArmed { get; private set; }

        internal bool SenseCoolingDown { get; private set; }

        internal bool GhostVisible { get; private set; }

        internal ShamanStormTier StormTier { get; private set; }

        public void Tick(long now)
        {
            try
            {
                if (!_d.IsRoundActiveClient || _d.LocalRoleClient != Role.Shaman)
                {
                    HideShamanUi();
                    _shamanLastTickTime = 0f;
                    _shamanStartCooldownArmed = false;
                    ClearSenseView();
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
                    _d.ClientPhase,
                    _d.IsLocalAlive(),
                    _d.MeetingClientView.MeetingActive,
                    _d.MeetingClientView.WarpDone(now),
                    _d.MeetingClientView.Kind);

                float? nearestDist = null;
                bool inView = false;
                bool stationary = false;
                if (!suspend)
                {
                    if (Players.TryGetPosition(_d.LocalActor, out Vector3 localPos))
                    {
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
                    _d.ClientShamanStormWeakMeters, _d.ClientShamanStormMediumMeters,
                    _d.ClientShamanStormStrongMeters);
                bool ghostFired = _shamanSense.TickGaze(inView, stationary, delta, suspend,
                    out bool dripFired);
                SenseArmed = _shamanSense.GazeArmed;
                SenseCoolingDown = _shamanSense.CoolingDown;
                GhostVisible = _shamanSense.GhostVisible;
                StormTier = tier;

                bool lastRunActive = LastRunGate.IsLastRunActive();

                bool warpedInMeeting = _d.MeetingClientView.MeetingActive
                    && _d.MeetingClientView.WarpDone(now);
                CorpseReportHudMode reportHudMode = CorpseReportHudGate.Compute(
                    _d.ClientPhase, _d.IsLocalAlive(), _d.MeetingClientView.MeetingActive,
                    warpedInMeeting, lastRunActive);
                bool rippleAllowed = ShamanHauntingRippleGate.CanShow(reportHudMode, suspend);

                bool detectorOverloaded = nearestDist != null
                    && CorpseReportProximity.IsWithin(nearestDist.Value);

                bool pulseStopped = suspend || detectorOverloaded || tier == ShamanStormTier.None;
                bool hauntingPulseFired = _hauntingPulse.Tick(
                    tier, delta, suspend, detectorOverloaded);
                if (pulseStopped || !rippleAllowed)
                {
                    _d.ClearShamanHauntingRipples();
                }
                if (pulseStopped)
                {
                    _d.StopStoppableSfx(ShamanHauntingPulseClipKey);
                }
                if (hauntingPulseFired)
                {
                    _d.PlayStoppableSfx(ShamanHauntingPulseClipKey, ShamanHauntingPulseVolume);
                    if (rippleAllowed) _d.PlayShamanHauntingRipple(tier);
                }

                if (tier != ShamanStormTier.None)
                {
                    _d.MaybeShowTutorial(TutorialId.ShamanStormEntered);
                }

                if (dripFired)
                {
                    _d.PlaySfx("sfx_shaman_drip", ShamanDripVolume);
                    _d.EnsurePanelBuilt(_shamanPanel);
                    _shamanPanel.PlayDripRipple();
                    _d.MaybeShowTutorial(TutorialId.ShamanTranceEntered);
                }

                if (ghostFired)
                {
                    WLog.Line("shaman_ghost", secret: true,
                        ("dist", nearestDist != null ? (int)nearestDist.Value : -1));
                    _d.PlaySfx("sfx_shaman_storm");
                    _d.PlaySfx(NextShamanSobKey());
                    _d.MaybeShowTutorial(TutorialId.ShamanGhostSighted);
                }

                _d.EnsurePanelBuilt(_shamanPanel);
                if (_shamanPanel.Exists)
                {
                    ShamanStormTier visualTier = lastRunActive
                        ? ShamanStormTier.None
                        : tier;
                    _shamanPanel.Tick(visualTier, _shamanSense.GhostVisible,
                        _shamanSense.GazeArmed, delta);
                }

                if (tier != ShamanStormTier.None)
                {
                    _d.PlaySfxLoop("sfx_shaman_storm", StormLoopVolume(tier));
                }
                else
                {
                    _d.StopSfxLoop("sfx_shaman_storm");
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

            if (_d.Registry != null && _d.Registry.Available)
            {
                foreach (PlayerHandle player in Players.All())
                {
                    if (!_d.MeetingClientView.IsDeadUnannounced(player.Actor)) continue;
                    if (!Players.TryGetDeathHeadPosition(player, out Vector3 headPos)) continue;
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
                    if (!_d.MeetingClientView.IsDeadUnannounced(actor)) continue;
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
            float fullSec = _d.ClientShamanGazeFullSec;
            if (fullSec <= 0f) fullSec = 5f;
            float cooldownSec = _d.ClientShamanGhostCooldownSec;
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
            _d.StopSfxLoop("sfx_shaman_storm");
            _d.StopStoppableSfx(ShamanHauntingPulseClipKey);
            _hauntingPulse.Reset();
            _d.ClearShamanHauntingRipples();
        }

        private void ClearSenseView()
        {
            SenseArmed = false;
            SenseCoolingDown = false;
            GhostVisible = false;
            StormTier = ShamanStormTier.None;
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

        public void Reset()
        {
            _shamanSense?.Reset();
            _shamanLastTickTime = 0f;
            _shamanStartCooldownArmed = false;
            _lastShamanSobIndex = -1;
            _hauntingPulse.Reset();
            _shamanLastLocalPos = null;
            _shamanGazeLock.Reset();
            ClearSenseView();
            HideShamanUi();
        }

        public void DestroyUi()
        {
        }
    }
}
