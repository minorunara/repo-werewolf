namespace Werewolf.Core
{
    public enum WolfPerkVisual : byte
    {
        Locked = 0,

        Ready = 1,

        Active = 2,
    }

    public readonly struct WolfStatusState
    {
        public bool Visible { get; }

        public WolfPerkVisual Toggle { get; }

        public WolfPerkVisual Stamina { get; }

        public WolfPerkVisual Jump { get; }

        public int JumpCharges { get; }

        public WolfPerkVisual EnemyIgnore { get; }

        public WolfPerkVisual Heal { get; }

        public int BeaconCharges { get; }

        public int BeaconCooldownSec { get; }

        public float BeaconGrayFraction { get; }

        public WolfStatusState(bool visible, WolfPerkVisual toggle,
            WolfPerkVisual stamina, WolfPerkVisual jump, int jumpCharges,
            WolfPerkVisual enemyIgnore, WolfPerkVisual heal,
            int beaconCharges, int beaconCooldownSec, float beaconGrayFraction)
        {
            Visible = visible;
            Toggle = toggle;
            Stamina = stamina;
            Jump = jump;
            JumpCharges = jumpCharges;
            EnemyIgnore = enemyIgnore;
            Heal = heal;
            BeaconCharges = beaconCharges;
            BeaconCooldownSec = beaconCooldownSec;
            BeaconGrayFraction = beaconGrayFraction;
        }

        public static readonly WolfStatusState Hidden = new WolfStatusState(
            visible: false, toggle: WolfPerkVisual.Locked,
            stamina: WolfPerkVisual.Locked, jump: WolfPerkVisual.Locked, jumpCharges: -1,
            enemyIgnore: WolfPerkVisual.Locked, heal: WolfPerkVisual.Locked,
            beaconCharges: 0, beaconCooldownSec: 0, beaconGrayFraction: 0f);
    }

    public sealed class WolfStatusModel
    {
        private long _trackedReadyUnixMs;
        private long _anchorUnixMs;

        public WolfStatusState Compute(RolesClientState roles, Role? effectiveRole,
            GamePhase phase, bool warpedInMeeting, long nowUnixMs,
            int extraJumpLimit = -1, int jumpRefillsUsed = 0,
            bool injectedJumpAvailable = false)
        {
            bool sessionActive = phase == GamePhase.Play || phase == GamePhase.Meeting;
            if (!sessionActive || warpedInMeeting || roles == null
                || effectiveRole != Role.Werewolf || !roles.CanShowHud(effectiveRole))
            {
                return WolfStatusState.Hidden;
            }

            float cooldownFraction = CooldownFraction(roles.BeaconReadyUnixMs, nowUnixMs);
            long remainingMs = roles.BeaconReadyUnixMs - nowUnixMs;
            int cooldownSec = remainingMs > 0 ? (int)((remainingMs + 999) / 1000) : 0;
            float grayFraction = cooldownSec > 0
                ? cooldownFraction
                : (roles.BeaconCharges <= 0 ? 1f : 0f);

            bool wolfOn = roles.WolfMode;
            return new WolfStatusState(
                visible: true,
                toggle: roles.UnlockedFlags == PerkFlags.None
                    ? WolfPerkVisual.Locked
                    : (wolfOn ? WolfPerkVisual.Active : WolfPerkVisual.Ready),
                stamina: SlotVisual(roles.UnlockedFlags, PerkId.InfiniteStamina, wolfOn),
                jump: SlotVisual(roles.UnlockedFlags, PerkId.InfiniteJump, wolfOn),
                jumpCharges: JumpCharges(roles.UnlockedFlags, extraJumpLimit, jumpRefillsUsed,
                    injectedJumpAvailable),
                enemyIgnore: SlotVisual(roles.UnlockedFlags, PerkId.EnemyIgnore, wolfOn),
                heal: SlotVisual(roles.UnlockedFlags, PerkId.NaturalHeal, wolfOn),
                beaconCharges: roles.BeaconCharges,
                beaconCooldownSec: cooldownSec,
                beaconGrayFraction: grayFraction);
        }

        public void Reset()
        {
            _trackedReadyUnixMs = 0;
            _anchorUnixMs = 0;
        }

        private float CooldownFraction(long readyUnixMs, long nowUnixMs)
        {
            if (readyUnixMs != _trackedReadyUnixMs)
            {
                _trackedReadyUnixMs = readyUnixMs;
                _anchorUnixMs = nowUnixMs;
            }
            if (readyUnixMs <= nowUnixMs) return 0f;

            long total = readyUnixMs - _anchorUnixMs;
            if (total <= 0) return 0f;
            float fraction = (float)(readyUnixMs - nowUnixMs) / total;
            return fraction > 1f ? 1f : fraction;
        }

        private static WolfPerkVisual SlotVisual(PerkFlags unlocked, PerkId perk, bool wolfOn)
        {
            if (!PerkFlagsUtil.Has(unlocked, perk)) return WolfPerkVisual.Locked;
            return wolfOn ? WolfPerkVisual.Active : WolfPerkVisual.Ready;
        }

        private static int JumpCharges(PerkFlags unlocked, int extraJumpLimit, int jumpRefillsUsed,
            bool injectedJumpAvailable)
        {
            if (!PerkFlagsUtil.Has(unlocked, PerkId.InfiniteJump)) return -1;
            if (extraJumpLimit < 0) return -1;
            int remaining = extraJumpLimit - jumpRefillsUsed + (injectedJumpAvailable ? 1 : 0);
            if (remaining > extraJumpLimit) remaining = extraJumpLimit;
            return remaining > 0 ? remaining : 0;
        }
    }
}
