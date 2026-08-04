using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class NaturalHealClockTests
    {
        private const long T0 = 1_000_000_000L;

        [Fact]
        public void Activation_DoesNotHealImmediately_ThenHealsAfterInterval()
        {
            var clock = new NaturalHealClock();

            Assert.False(clock.ShouldHeal(true, T0, 3));
            Assert.False(clock.ShouldHeal(true, T0 + 2_999, 3));
            Assert.True(clock.ShouldHeal(true, T0 + 3_000, 3));
            Assert.False(clock.ShouldHeal(true, T0 + 3_001, 3));
            Assert.True(clock.ShouldHeal(true, T0 + 6_000, 3));
        }

        [Fact]
        public void Deactivation_DiscardsAnchor_AndRestartsOnReactivation()
        {
            var clock = new NaturalHealClock();
            clock.ShouldHeal(true, T0, 3);

            Assert.False(clock.ShouldHeal(false, T0 + 2_000, 3));
            Assert.False(clock.ShouldHeal(true, T0 + 2_500, 3));
            Assert.False(clock.ShouldHeal(true, T0 + 5_000, 3));
            Assert.True(clock.ShouldHeal(true, T0 + 5_500, 3));
        }

        [Fact]
        public void IntervalBelowOne_IsClampedToOneSecond()
        {
            var clock = new NaturalHealClock();

            clock.ShouldHeal(true, T0, 0);
            Assert.False(clock.ShouldHeal(true, T0 + 999, 0));
            Assert.True(clock.ShouldHeal(true, T0 + 1_000, 0));

            clock.Reset();
            clock.ShouldHeal(true, T0, -5);
            Assert.True(clock.ShouldHeal(true, T0 + 1_000, -5));
        }

        [Fact]
        public void LongGap_DoesNotGrantCatchUpHeals()
        {
            var clock = new NaturalHealClock();
            clock.ShouldHeal(true, T0, 3);

            Assert.True(clock.ShouldHeal(true, T0 + 30_000, 3));
            Assert.False(clock.ShouldHeal(true, T0 + 30_001, 3));
            Assert.False(clock.ShouldHeal(true, T0 + 32_999, 3));
            Assert.True(clock.ShouldHeal(true, T0 + 33_000, 3));
        }

        [Fact]
        public void Reset_DiscardsAnchor()
        {
            var clock = new NaturalHealClock();
            clock.ShouldHeal(true, T0, 3);
            clock.Reset();

            Assert.False(clock.ShouldHeal(true, T0 + 3_000, 3));
            Assert.True(clock.ShouldHeal(true, T0 + 6_000, 3));
        }

        [Fact]
        public void HealActive_RequiresWolfModeAndUnlockedFlag()
        {
            var state = new RolesClientState();
            Assert.False(state.HealActive);

            state.ApplyGaugeSync(500, (byte)PerkFlags.NaturalHeal, 0, 0, 0);
            Assert.False(state.HealActive);

            Assert.True(state.TryToggleWolfMode(Role.Werewolf));
            Assert.True(state.HealActive);

            state.ApplyGaugeSync(500, (byte)PerkFlags.InfiniteStamina, 0, 0, 0);
            Assert.False(state.HealActive);
        }
    }
}
