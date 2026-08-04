using System;

namespace Werewolf.Core
{
    public sealed class BeaconState
    {
        private readonly GameConfig _config;
        private long _suppressedUntilUnixMs;
        private long _cooldownUntilUnixMs;

        public BeaconState(GameConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public int Charges { get; private set; }

        public void AddCharges(int count)
        {
            if (count > 0) Charges += count;
        }

        public void Suppress(long untilUnixMs)
        {
            if (untilUnixMs > _suppressedUntilUnixMs) _suppressedUntilUnixMs = untilUnixMs;
        }

        public long ReadyUnixMs =>
            _suppressedUntilUnixMs > _cooldownUntilUnixMs ? _suppressedUntilUnixMs : _cooldownUntilUnixMs;

        public void DebugClearRestrictions()
        {
            _suppressedUntilUnixMs = 0;
            _cooldownUntilUnixMs = 0;
        }

        public BeaconStatus TryUse(long nowUnixMs)
        {
            if (nowUnixMs < _suppressedUntilUnixMs) return BeaconStatus.Suppressed;
            if (nowUnixMs < _cooldownUntilUnixMs) return BeaconStatus.Cooldown;
            if (Charges <= 0) return BeaconStatus.NoCharge;

            Charges--;
            _cooldownUntilUnixMs = nowUnixMs + _config.BeaconCooldownSec * 1000L;
            return BeaconStatus.Ok;
        }
    }
}
