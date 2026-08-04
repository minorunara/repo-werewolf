using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class BombClientStateTests
    {
        [Fact]
        public void ApplyState_MirrorsAllFields()
        {
            var c = new BombClientState();
            c.ApplyState(new BomberStateSnapshot(
                targetActor: 7, ammo: 2,
                plantReadyUnixMs: 1000, detonateReadyUnixMs: 2000,
                lastDeny: BombDenyReason.PlantCooldown));

            Assert.Equal(7, c.TargetActor);
            Assert.True(c.HasBomb);
            Assert.Equal((byte)2, c.Ammo);
            Assert.Equal(1000L, c.PlantReadyUnixMs);
            Assert.Equal(2000L, c.DetonateReadyUnixMs);
            Assert.Equal(BombDenyReason.PlantCooldown, c.LastDeny);
        }

        [Fact]
        public void ApplyPendingDetonation_StoresTargetAndTime()
        {
            var c = new BombClientState();
            c.ApplyPendingDetonation(targetActor: 5, detonateAtUnixMs: 12345);

            Assert.True(c.HasPendingDetonation);
            Assert.Equal(5, c.PendingTargetActor);
            Assert.Equal(12345L, c.PendingDetonateAtUnixMs);
        }

        [Fact]
        public void ApplyState_NegativeDebugBotActorStillCountsAsBomb()
        {
            var c = new BombClientState();
            c.ApplyState(new BomberStateSnapshot(-101, 0, 1000, 2000, BombDenyReason.None));

            Assert.True(c.HasBomb);
            Assert.Equal(-101, c.TargetActor);
        }

        [Fact]
        public void ClearPendingDetonation_RemovesPendingState()
        {
            var c = new BombClientState();
            c.ApplyPendingDetonation(5, 1);
            c.ClearPendingDetonation();

            Assert.False(c.HasPendingDetonation);
            Assert.Equal(-1, c.PendingTargetActor);
        }

        [Fact]
        public void ConsumeLastDeny_ResetsToNone()
        {
            var c = new BombClientState();
            c.ApplyState(new BomberStateSnapshot(-1, 0, 0, 0, BombDenyReason.NoAmmo));
            Assert.Equal(BombDenyReason.NoAmmo, c.LastDeny);

            c.ConsumeLastDeny();
            Assert.Equal(BombDenyReason.None, c.LastDeny);
        }

        [Fact]
        public void Reset_ClearsAllStateAndPending()
        {
            var c = new BombClientState();
            c.ApplyState(new BomberStateSnapshot(9, 3, 100, 200, BombDenyReason.TruckZone));
            c.ApplyPendingDetonation(9, 500);

            c.Reset();

            Assert.Equal(-1, c.TargetActor);
            Assert.False(c.HasBomb);
            Assert.Equal((byte)0, c.Ammo);
            Assert.Equal(0L, c.PlantReadyUnixMs);
            Assert.Equal(0L, c.DetonateReadyUnixMs);
            Assert.Equal(BombDenyReason.None, c.LastDeny);
            Assert.False(c.HasPendingDetonation);
        }

        [Fact]
        public void ApplyPendingDetonation_DuplicateOverridesInPlace()
        {
            var c = new BombClientState();
            c.ApplyPendingDetonation(1, 100);
            c.ApplyPendingDetonation(2, 200);

            Assert.Equal(2, c.PendingTargetActor);
            Assert.Equal(200L, c.PendingDetonateAtUnixMs);
        }
    }
}
