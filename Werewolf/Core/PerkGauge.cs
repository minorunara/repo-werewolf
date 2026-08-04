using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class PerkGauge
    {
        private static readonly IReadOnlyList<GaugeEvent> Empty = Array.Empty<GaugeEvent>();

        private readonly GameConfig _config;
        private float _lostDollars;
        private PerkFlags _unlocked = PerkFlags.None;
        private bool _informantFired;
        private int _beaconChargesGranted;

        public PerkGauge(GameConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool BaseFrozen { get; private set; }

        public float BaseDollars { get; private set; }

        public float LostDollars => _lostDollars;

        public PerkFlags UnlockedFlags => _unlocked;

        public bool InformantFired => _informantFired;

        public int DisplayPermille
        {
            get
            {
                if (!BaseFrozen || BaseDollars <= 0f) return 0;
                int permille = (int)(_lostDollars / BaseDollars * 1000f);
                return permille > 1000 ? 1000 : permille;
            }
        }

        public void FreezeBase(float totalDollars)
        {
            if (BaseFrozen)
            {
                WLog.Line("gauge_freeze_ignored", secret: true,
                    ("reason", "already_frozen"), ("total", totalDollars));
                return;
            }
            if (totalDollars <= 0f)
            {
                WLog.Line("gauge_freeze_ignored", secret: true,
                    ("reason", "non_positive"), ("total", totalDollars));
                return;
            }

            BaseFrozen = true;
            BaseDollars = totalDollars;
            WLog.Line("gauge_freeze", secret: true, ("baseDollars", totalDollars));
        }

        public IReadOnlyList<GaugeEvent> AddLoss(float lostDollars)
        {
            if (!BaseFrozen)
            {
                WLog.Line("gauge_add_ignored", secret: true,
                    ("reason", "base_not_frozen"), ("lost", lostDollars));
                return Empty;
            }
            if (lostDollars <= 0f) return Empty;

            _lostDollars += lostDollars;
            WLog.Line("gauge_add", secret: true,
                ("lost", lostDollars), ("totalLost", _lostDollars), ("permille", DisplayPermille));

            float pct = _lostDollars / BaseDollars * 100f;
            List<GaugeEvent> events = null;

            TryUnlock(ref events, pct, _config.StaminaUnlockPct, PerkId.InfiniteStamina);
            TryUnlock(ref events, pct, _config.JumpUnlockPct, PerkId.InfiniteJump);
            TryUnlock(ref events, pct, _config.EnemyIgnoreUnlockPct, PerkId.EnemyIgnore);
            TryUnlock(ref events, pct, _config.HealUnlockPct, PerkId.NaturalHeal);

            if (_config.BeaconChargePct > 0)
            {
                int reachable = (int)(pct / _config.BeaconChargePct);
                if (reachable > _beaconChargesGranted)
                {
                    int grant = reachable - _beaconChargesGranted;
                    _beaconChargesGranted = reachable;
                    (events ?? (events = new List<GaugeEvent>())).Add(GaugeEvent.Charged(grant));
                }
            }

            if (!_informantFired && pct >= _config.InformantThresholdPct)
            {
                _informantFired = true;
                (events ?? (events = new List<GaugeEvent>())).Add(GaugeEvent.Informant());
            }

            return events ?? Empty;
        }

        public static float ComputeRealLoss(float dollarBeforeLoss, float valueLost, float dollarOriginal)
        {
            float after = dollarBeforeLoss - valueLost;
            bool fullyBroken = after < dollarOriginal * 0.15f;
            return fullyBroken ? dollarBeforeLoss : valueLost;
        }

        public bool DebugForceUnlock(PerkId perk)
        {
            if (PerkFlagsUtil.Has(_unlocked, perk)) return false;
            _unlocked |= PerkFlagsUtil.ToFlag(perk);
            WLog.Line("perk_unlocked", secret: true, ("perk", perk), ("via", "debug"));
            return true;
        }

        public bool DebugForceInformant()
        {
            if (_informantFired) return false;
            _informantFired = true;
            return true;
        }

        private void TryUnlock(ref List<GaugeEvent> events, float pct, int thresholdPct, PerkId perk)
        {
            if (PerkFlagsUtil.Has(_unlocked, perk)) return;
            if (pct < thresholdPct) return;

            _unlocked |= PerkFlagsUtil.ToFlag(perk);
            (events ?? (events = new List<GaugeEvent>())).Add(GaugeEvent.Unlocked(perk));
            WLog.Line("perk_unlocked", secret: true, ("perk", perk), ("pct", pct));
        }
    }
}
