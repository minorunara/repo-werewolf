using System;

namespace Werewolf.Core
{
    public enum PerkId : byte
    {
        InfiniteStamina = 0,

        InfiniteJump = 1,

        EnemyIgnore = 2,

        NaturalHeal = 3,
    }

    [Flags]
    public enum PerkFlags : byte
    {
        None = 0,
        InfiniteStamina = 1,
        InfiniteJump = 2,
        EnemyIgnore = 4,
        NaturalHeal = 8,
    }

    public static class PerkFlagsUtil
    {
        public static PerkFlags ToFlag(PerkId perk)
        {
            switch (perk)
            {
                case PerkId.InfiniteStamina: return PerkFlags.InfiniteStamina;
                case PerkId.InfiniteJump:    return PerkFlags.InfiniteJump;
                case PerkId.EnemyIgnore:     return PerkFlags.EnemyIgnore;
                case PerkId.NaturalHeal:     return PerkFlags.NaturalHeal;
                default:                     return PerkFlags.None;
            }
        }

        public static bool Has(PerkFlags flags, PerkId perk) => (flags & ToFlag(perk)) != 0;
    }

    public enum GaugeEventKind : byte
    {
        PerkUnlocked = 0,

        BeaconCharged = 1,

        InformantReady = 2,
    }

    public sealed class GaugeEvent
    {
        private GaugeEvent(GaugeEventKind kind, PerkId perk, int beaconChargeCount)
        {
            Kind = kind;
            Perk = perk;
            BeaconChargeCount = beaconChargeCount;
        }

        public GaugeEventKind Kind { get; }

        public PerkId Perk { get; }

        public int BeaconChargeCount { get; }

        public static GaugeEvent Unlocked(PerkId perk)
            => new GaugeEvent(GaugeEventKind.PerkUnlocked, perk, 0);

        public static GaugeEvent Charged(int count)
            => new GaugeEvent(GaugeEventKind.BeaconCharged, default, count);

        public static GaugeEvent Informant()
            => new GaugeEvent(GaugeEventKind.InformantReady, default, 0);
    }

    public enum BeaconStatus : byte
    {
        Ok = 0,

        Suppressed = 1,

        Cooldown = 2,

        NoCharge = 3,

        MeetingActive = 4,
    }

    public static class WWRolesCodes
    {
        public const byte BeaconAudit = 167;

        public const byte SyncPerkGauge = 171;

        public const byte RoleAction = 174;

        public const byte RoleState = 175;

        public const byte CurseCandidates = 179;
    }

    public static class RoleActionSubtype
    {
        public const byte CurseDesignate = 0;

        public const byte BeaconUse = 1;

        public const byte WolfModeSync = 2;
    }

    public static class RoleStateSubtype
    {
        public const byte CurseStarted = 0;

        public const byte CurseResolved = 1;

        public const byte MeetingGauge = 2;
    }
}
