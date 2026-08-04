using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class BombSessionTests
    {
        private const long T0 = 1_000_000L;
        private const int Bomber = 1;
        private const int V1 = 2;
        private const int V2 = 3;
        private const int Wolf = 4;
        private const int DefaultCooldownSec = 30;
        private const int DefaultInitialCooldownSec = 60;

        private static long InitialReadyAt(int cdSec = DefaultInitialCooldownSec)
            => T0 + cdSec * 1000L;

        private static GameConfig Cfg(int cdSec = DefaultCooldownSec, int refillPct = 30,
            int initialCdSec = DefaultInitialCooldownSec)
            => new GameConfig
            {
                BomberInitialCooldownSec = initialCdSec,
                BomberCooldownSec = cdSec,
                BomberAmmoRefillPct = refillPct,
            };

        private static WPlayer P(int actor, Role role, bool alive = true)
            => new WPlayer { ActorNumber = actor, Role = role, Alive = alive };

        private static List<WPlayer> Roster(bool bomberAlive = true)
            => new List<WPlayer>
            {
                P(Bomber, Role.Bomber, bomberAlive),
                P(V1, Role.Villager),
                P(V2, Role.Villager),
                P(Wolf, Role.Werewolf),
            };

        private static void Kill(List<WPlayer> roster, BombSession s, int actor)
        {
            foreach (var p in roster)
            {
                if (p.ActorNumber == actor) { p.Alive = false; break; }
            }
            s.OnPlayerDied(actor);
        }

        [Fact]
        public void Ctor_WithBomber_StartsWithOneAmmoAndDirty()
        {
            var s = new BombSession(Cfg(), Roster(), T0);
            Assert.Equal(Bomber, s.BomberActor);
            Assert.Equal((byte)1, s.Ammo);
            Assert.False(s.HasBomb);
            Assert.True(s.Dirty);

            var snap = s.BuildSnapshot();
            Assert.Equal(1, snap.Ammo);
            Assert.Equal(-1, snap.TargetActor);
            Assert.Equal(InitialReadyAt(), snap.PlantReadyUnixMs);
            Assert.Equal(0, snap.DetonateReadyUnixMs);
            Assert.False(s.Dirty);
        }

        [Fact]
        public void Ctor_StartCooldownRejectsPlantUntilConfiguredTime()
        {
            var s = new BombSession(Cfg(), Roster(), T0);

            Assert.Equal(BombDenyReason.PlantCooldown,
                s.TryPlant(Bomber, V1, InitialReadyAt() - 1));
            Assert.Equal(BombDenyReason.None,
                s.TryPlant(Bomber, V1, InitialReadyAt()));
        }

        [Fact]
        public void Ctor_NoBomberInRoster_AllMethodsAreNoop()
        {
            var roster = new List<WPlayer> { P(V1, Role.Villager), P(V2, Role.Villager) };
            var s = new BombSession(Cfg(), roster, T0);

            Assert.Equal(-1, s.BomberActor);
            Assert.Equal((byte)0, s.Ammo);

            int det;
            Assert.Equal(BombDenyReason.NotBomber, s.TryPlant(V1, V2, T0));
            Assert.Equal(BombDenyReason.NotBomber, s.TryDetonate(V1, T0, false, false, out det));

            s.OnGaugeChanged(100f);
            Assert.Equal((byte)0, s.Ammo);
        }

        [Fact]
        public void TryPlant_ConsumesAmmoAndStartsBothCooldowns()
        {
            var s = new BombSession(Cfg(cdSec: 30), Roster(), T0);
            long plantedAt = InitialReadyAt();
            var r = s.TryPlant(Bomber, V1, plantedAt);

            Assert.Equal(BombDenyReason.None, r);
            Assert.Equal((byte)0, s.Ammo);
            Assert.True(s.HasBomb);
            Assert.Equal(V1, s.TargetActor);

            var snap = s.BuildSnapshot();
            Assert.Equal(plantedAt + 30_000, snap.PlantReadyUnixMs);
            Assert.Equal(plantedAt + 30_000, snap.DetonateReadyUnixMs);

            Assert.Equal(BombDenyReason.PlantCooldown, s.TryPlant(Bomber, V2, plantedAt + 29_999));
            int det;
            Assert.Equal(BombDenyReason.DetonateCooldown,
                s.TryDetonate(Bomber, plantedAt + 29_999, false, false, out det));
        }

        [Fact]
        public void TryPlant_ReplantDoesNotConsumeAmmoButRestartsCooldowns()
        {
            var s = new BombSession(Cfg(cdSec: 30), Roster(), T0);
            long firstPlantAt = InitialReadyAt();
            s.TryPlant(Bomber, V1, firstPlantAt);
            Assert.Equal((byte)0, s.Ammo);

            long replantAt = firstPlantAt + 30_000;
            var r = s.TryPlant(Bomber, V2, replantAt);
            Assert.Equal(BombDenyReason.None, r);
            Assert.Equal((byte)0, s.Ammo);
            Assert.Equal(V2, s.TargetActor);

            var snap = s.BuildSnapshot();
            Assert.Equal(replantAt + 30_000, snap.PlantReadyUnixMs);
            Assert.Equal(replantAt + 30_000, snap.DetonateReadyUnixMs);
        }

        [Fact]
        public void TryPlant_SameTargetDoesNotRestartCooldowns()
        {
            var s = new BombSession(Cfg(cdSec: 30), Roster(), T0);
            long firstPlantAt = InitialReadyAt();
            Assert.Equal(BombDenyReason.None, s.TryPlant(Bomber, V1, firstPlantAt));
            var original = s.BuildSnapshot();

            long retryAt = firstPlantAt + 30_000;
            Assert.Equal(BombDenyReason.None, s.TryPlant(Bomber, V1, retryAt));

            var afterRetry = s.BuildSnapshot();
            Assert.Equal(V1, afterRetry.TargetActor);
            Assert.Equal(original.Ammo, afterRetry.Ammo);
            Assert.Equal(original.PlantReadyUnixMs, afterRetry.PlantReadyUnixMs);
            Assert.Equal(original.DetonateReadyUnixMs, afterRetry.DetonateReadyUnixMs);
        }

        [Fact]
        public void TryPlant_RejectsNoAmmoWhenFirstPlantWithZeroAmmo()
        {
            var s = new BombSession(Cfg(), Roster(), T0);
            s.OnGaugeChanged(0f);
            s.TryPlant(Bomber, V1, InitialReadyAt());
            s.OnPlayerDisconnected(V1);
            Assert.False(s.HasBomb);

            var r = s.TryPlant(Bomber, V2, InitialReadyAt() + 30_000);
            Assert.Equal(BombDenyReason.NoAmmo, r);
        }

        [Fact]
        public void TryPlant_RejectsInvalidTargets()
        {
            var roster = Roster();
            var s = new BombSession(Cfg(), roster, T0);

            long readyAt = InitialReadyAt();
            Assert.Equal(BombDenyReason.TargetInvalid, s.TryPlant(Bomber, Bomber, readyAt));
            Assert.Equal(BombDenyReason.TargetInvalid, s.TryPlant(Bomber, 999, readyAt));

            Kill(roster, s, V1);
            Assert.Equal(BombDenyReason.TargetInvalid, s.TryPlant(Bomber, V1, readyAt));
        }

        [Fact]
        public void TryPlant_RejectsNonBomberSender()
        {
            var s = new BombSession(Cfg(), Roster(), T0);
            Assert.Equal(BombDenyReason.NotBomber, s.TryPlant(V1, V2, T0));
        }

        [Fact]
        public void TryPlant_AllowsPlantOnWerewolfAndBlackCat()
        {
            var roster = new List<WPlayer>
            {
                P(Bomber, Role.Bomber),
                P(Wolf, Role.Werewolf),
                P(5, Role.BlackCat),
            };
            var s = new BombSession(Cfg(), roster, T0);
            long firstPlantAt = InitialReadyAt();

            Assert.Equal(BombDenyReason.None, s.TryPlant(Bomber, Wolf, firstPlantAt));
            Assert.Equal(BombDenyReason.None, s.TryPlant(Bomber, 5, firstPlantAt + 30_000));
        }

        [Fact]
        public void TryPlant_AllowsNegativeActorDebugBotAndCanDetonateIt()
        {
            const int bot = -101;
            var roster = Roster();
            roster.Add(new WPlayer
            {
                ActorNumber = bot,
                Name = "Bot",
                IsBot = true,
                Role = Role.Villager,
                Alive = true,
            });
            var s = new BombSession(Cfg(cdSec: 30), roster, T0);
            long plantedAt = InitialReadyAt();

            Assert.Equal(BombDenyReason.None, s.TryPlant(Bomber, bot, plantedAt));
            Assert.True(s.HasBomb);
            Assert.Equal(bot, s.TargetActor);

            int detonatedActor;
            Assert.Equal(BombDenyReason.None,
                s.TryDetonate(Bomber, plantedAt + 30_000, false, false, out detonatedActor));
            Assert.Equal(bot, detonatedActor);
            Assert.False(s.HasBomb);
        }

        [Fact]
        public void TryDetonate_AcceptsAndClearsBombWithCooldown()
        {
            var s = new BombSession(Cfg(cdSec: 30), Roster(), T0);
            long plantedAt = InitialReadyAt();
            s.TryPlant(Bomber, V1, plantedAt);

            long detonatedAt = plantedAt + 30_000;
            int det;
            var r = s.TryDetonate(Bomber, detonatedAt, false, false, out det);

            Assert.Equal(BombDenyReason.None, r);
            Assert.Equal(V1, det);
            Assert.False(s.HasBomb);

            var snap = s.BuildSnapshot();
            Assert.Equal(detonatedAt + 30_000, snap.PlantReadyUnixMs);
            Assert.Equal(0, snap.DetonateReadyUnixMs);

            Assert.Equal(BombDenyReason.NoBomb,
                s.TryDetonate(Bomber, detonatedAt + 1, false, false, out det));
        }

        [Fact]
        public void TryDetonate_TargetDeadTriggersDudWithCooldownAndBombRemoved()
        {
            var roster = Roster();
            var s = new BombSession(Cfg(cdSec: 30), roster, T0);
            long plantedAt = InitialReadyAt();
            s.TryPlant(Bomber, V1, plantedAt);

            Kill(roster, s, V1);
            Assert.True(s.HasBomb);

            long detonatedAt = plantedAt + 30_000;
            int det;
            var r = s.TryDetonate(Bomber, detonatedAt, false, false, out det);
            Assert.Equal(BombDenyReason.TargetDead, r);
            Assert.Equal(-1, det);
            Assert.False(s.HasBomb);
            Assert.Equal((byte)0, s.Ammo);

            var snap = s.BuildSnapshot();
            Assert.Equal(detonatedAt + 30_000, snap.PlantReadyUnixMs);
            Assert.Equal(0, snap.DetonateReadyUnixMs);
        }

        [Fact]
        public void TryDetonate_RejectsMeetingLockedAndTruckZone()
        {
            var s = new BombSession(Cfg(cdSec: 0, initialCdSec: 0), Roster(), T0);
            s.TryPlant(Bomber, V1, T0);

            int det;
            Assert.Equal(BombDenyReason.MeetingLocked,
                s.TryDetonate(Bomber, T0, meetingLocked: true, targetNearTruck: false, out det));
            Assert.Equal(BombDenyReason.TruckZone,
                s.TryDetonate(Bomber, T0, meetingLocked: false, targetNearTruck: true, out det));
            Assert.True(s.HasBomb);
        }

        [Fact]
        public void TryDetonate_RejectsWhenNoBomb()
        {
            var s = new BombSession(Cfg(cdSec: 0, initialCdSec: 0), Roster(), T0);
            int det;
            Assert.Equal(BombDenyReason.NoBomb, s.TryDetonate(Bomber, T0, false, false, out det));
        }

        [Fact]
        public void OnMeetingEnded_WithoutBombStartsPlantCooldownOnly()
        {
            var s = new BombSession(Cfg(cdSec: 30), Roster(), T0);
            s.BuildSnapshot();

            long meetingEndedAt = T0 + 10_000;
            s.OnMeetingEnded(meetingEndedAt);

            var snap = s.BuildSnapshot();
            Assert.Equal(meetingEndedAt + 30_000, snap.PlantReadyUnixMs);
            Assert.Equal(0, snap.DetonateReadyUnixMs);
        }

        [Fact]
        public void OnMeetingEnded_WithBombRestartsBothCooldowns()
        {
            var s = new BombSession(Cfg(cdSec: 30), Roster(), T0);
            long plantedAt = InitialReadyAt();
            s.TryPlant(Bomber, V1, plantedAt);

            long meetingEndedAt = plantedAt + 5_000;
            s.OnMeetingEnded(meetingEndedAt);

            var snap = s.BuildSnapshot();
            Assert.Equal(meetingEndedAt + 30_000, snap.PlantReadyUnixMs);
            Assert.Equal(meetingEndedAt + 30_000, snap.DetonateReadyUnixMs);
            Assert.True(s.HasBomb);
        }

        [Fact]
        public void OnGaugeChanged_RefillsPerThresholdWithoutUpperCap()
        {
            var s = new BombSession(Cfg(refillPct: 30), Roster(), T0);
            Assert.Equal((byte)1, s.Ammo);

            s.OnGaugeChanged(29f);
            Assert.Equal((byte)1, s.Ammo);

            s.OnGaugeChanged(30f);
            Assert.Equal((byte)2, s.Ammo);

            s.OnGaugeChanged(90f);
            Assert.Equal((byte)4, s.Ammo);

            s.OnGaugeChanged(300f);
            Assert.Equal((byte)11, s.Ammo);
        }

        [Fact]
        public void OnGaugeChanged_DoesNotDecreaseOnDetonate()
        {
            var s = new BombSession(Cfg(refillPct: 30), Roster(), T0);
            s.OnGaugeChanged(60f);
            Assert.Equal((byte)3, s.Ammo);

            s.TryPlant(Bomber, V1, InitialReadyAt());
            Assert.Equal((byte)2, s.Ammo);

            s.OnGaugeChanged(60f);
            Assert.Equal((byte)2, s.Ammo);

            s.OnGaugeChanged(90f);
            Assert.Equal((byte)3, s.Ammo);
        }

        [Fact]
        public void OnPlayerDied_BomberInvalidatesEverything()
        {
            var s = new BombSession(Cfg(), Roster(), T0);
            s.TryPlant(Bomber, V1, InitialReadyAt());

            s.OnPlayerDied(Bomber);
            Assert.Equal(-1, s.BomberActor);
            Assert.False(s.HasBomb);

            int det;
            Assert.Equal(BombDenyReason.NotBomber, s.TryDetonate(Bomber, T0 + 60_000, false, false, out det));
        }

        [Fact]
        public void OnPlayerDied_TargetDoesNotAffectBombState()
        {
            var s = new BombSession(Cfg(), Roster(), T0);
            s.TryPlant(Bomber, V1, InitialReadyAt());
            s.BuildSnapshot();

            s.OnPlayerDied(V1);
            Assert.True(s.HasBomb);
            Assert.Equal(V1, s.TargetActor);
            Assert.False(s.Dirty);
        }

        [Fact]
        public void OnPlayerDisconnected_TargetImmediatelyRemovesBomb()
        {
            var s = new BombSession(Cfg(), Roster(), T0);
            long plantedAt = InitialReadyAt();
            s.TryPlant(Bomber, V1, plantedAt);

            s.OnPlayerDisconnected(V1);
            Assert.False(s.HasBomb);
            Assert.True(s.Dirty);

            var snap = s.BuildSnapshot();
            Assert.Equal(plantedAt + 30_000, snap.PlantReadyUnixMs);
            Assert.Equal(0, snap.DetonateReadyUnixMs);
        }

        [Fact]
        public void OnPlayerDisconnected_BomberInvalidatesEverything()
        {
            var s = new BombSession(Cfg(), Roster(), T0);
            s.TryPlant(Bomber, V1, InitialReadyAt());

            s.OnPlayerDisconnected(Bomber);
            Assert.Equal(-1, s.BomberActor);
            Assert.False(s.HasBomb);

            var snap = s.BuildSnapshot();
            Assert.Equal(0, snap.PlantReadyUnixMs);
            Assert.Equal(0, snap.DetonateReadyUnixMs);
        }

        [Fact]
        public void BuildSnapshot_CarriesLastDenyAndClearsDirty()
        {
            var s = new BombSession(Cfg(), Roster(), T0);
            s.BuildSnapshot();
            Assert.False(s.Dirty);

            s.TryPlant(Bomber, Bomber, InitialReadyAt());
            Assert.True(s.Dirty);
            var snap = s.BuildSnapshot();
            Assert.Equal(BombDenyReason.TargetInvalid, snap.LastDeny);
            Assert.False(s.Dirty);
        }
    }
}
