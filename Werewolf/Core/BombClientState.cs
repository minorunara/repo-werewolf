namespace Werewolf.Core
{
    public sealed class BombClientState
    {
        public int TargetActor { get; private set; } = -1;

        public byte Ammo { get; private set; }

        public long PlantReadyUnixMs { get; private set; }

        public long DetonateReadyUnixMs { get; private set; }

        public BombDenyReason LastDeny { get; private set; }

        public bool HasPendingDetonation { get; private set; }

        public int PendingTargetActor { get; private set; }

        public long PendingDetonateAtUnixMs { get; private set; }

        public bool HasBomb => TargetActor != -1;

        public void ApplyState(BomberStateSnapshot snapshot)
        {
            TargetActor = snapshot.TargetActor;
            Ammo = snapshot.Ammo;
            PlantReadyUnixMs = snapshot.PlantReadyUnixMs;
            DetonateReadyUnixMs = snapshot.DetonateReadyUnixMs;
            LastDeny = snapshot.LastDeny;
        }

        public void ApplyPendingDetonation(int targetActor, long detonateAtUnixMs)
        {
            HasPendingDetonation = true;
            PendingTargetActor = targetActor;
            PendingDetonateAtUnixMs = detonateAtUnixMs;
        }

        public void ClearPendingDetonation()
        {
            HasPendingDetonation = false;
            PendingTargetActor = -1;
            PendingDetonateAtUnixMs = 0;
        }

        public void ConsumeLastDeny()
        {
            LastDeny = BombDenyReason.None;
        }

        public void Reset()
        {
            TargetActor = -1;
            Ammo = 0;
            PlantReadyUnixMs = 0;
            DetonateReadyUnixMs = 0;
            LastDeny = BombDenyReason.None;
            ClearPendingDetonation();
        }
    }
}
