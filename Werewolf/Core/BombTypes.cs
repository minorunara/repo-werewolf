namespace Werewolf.Core
{
    public enum BombDenyReason : byte
    {
        None = 0,
        NotBomber = 1,
        NoAmmo = 2,

        NoFullTarget = 3,
        PlantCooldown = 4,
        DetonateCooldown = 5,
        NoBomb = 6,
        MeetingLocked = 7,
        TruckZone = 8,

        TargetDead = 9,
        TargetInvalid = 10,
    }

    public readonly struct BomberStateSnapshot
    {
        public BomberStateSnapshot(
            int targetActor, byte ammo, long plantReadyUnixMs, long detonateReadyUnixMs,
            BombDenyReason lastDeny)
        {
            TargetActor = targetActor;
            Ammo = ammo;
            PlantReadyUnixMs = plantReadyUnixMs;
            DetonateReadyUnixMs = detonateReadyUnixMs;
            LastDeny = lastDeny;
        }

        public int TargetActor { get; }

        public byte Ammo { get; }

        public long PlantReadyUnixMs { get; }

        public long DetonateReadyUnixMs { get; }

        public BombDenyReason LastDeny { get; }
    }

    public static class BombRoleActionSubtype
    {
        public const byte Plant = 3;

        public const byte Detonate = 4;
    }
}
