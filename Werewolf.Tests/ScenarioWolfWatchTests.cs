using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioWolfWatchTests
    {
        private readonly ScenarioWolfWatch _watch = new ScenarioWolfWatch();
        private readonly List<ScenarioSignal> _signals = new List<ScenarioSignal>();

        private static ScenarioWolfSample Sample(int destroyed = 0, PerkFlags unlocked = PerkFlags.None,
                                                 bool wolfMode = false, bool recordOn = true,
                                                 int charges = 0)
            => new ScenarioWolfSample(destroyed, unlocked, wolfMode, recordOn, charges);

        private IReadOnlyList<ScenarioSignal> Observe(ScenarioWolfSample sample)
        {
            _watch.Observe(sample, _signals);
            return _signals;
        }

        [Fact]
        public void FirstObservation_OnlyPrimes()
        {
            Assert.Empty(Observe(Sample(destroyed: 3, unlocked: PerkFlags.InfiniteStamina,
                wolfMode: true, recordOn: false, charges: 2)));
            Assert.True(_watch.Primed);
        }

        [Fact]
        public void SteadyState_EmitsNothing()
        {
            Observe(Sample(destroyed: 1, charges: 1));

            Assert.Empty(Observe(Sample(destroyed: 1, charges: 1)));
        }

        [Fact]
        public void Destruction_SplitsByWhetherRecordingIsOn()
        {
            Observe(Sample());
            Assert.Equal(new[] { ScenarioSignal.ValuableDestroyed }, Observe(Sample(destroyed: 1)));

            Assert.Equal(
                new[] { ScenarioSignal.StealthDestroyed, ScenarioSignal.RecordTurnedOff },
                Observe(Sample(destroyed: 2, recordOn: false)));
        }

        [Fact]
        public void RecordingBackOn_IsNotASignal()
        {
            Observe(Sample(recordOn: true));
            Observe(Sample(recordOn: false));

            Assert.Empty(Observe(Sample(recordOn: true)));
        }

        [Fact]
        public void WolfForm_EmitsOnTheRisingEdgeOnly()
        {
            Observe(Sample(wolfMode: false));
            Assert.Equal(new[] { ScenarioSignal.WolfFormTurnedOn }, Observe(Sample(wolfMode: true)));
            Assert.Empty(Observe(Sample(wolfMode: true)));
            Assert.Empty(Observe(Sample(wolfMode: false)));
        }

        [Fact]
        public void Beacon_EmitsWhenTheChargeCountDrops()
        {
            Observe(Sample(charges: 1));

            Assert.Empty(Observe(Sample(charges: 2)));
            Assert.Equal(new[] { ScenarioSignal.BeaconUsed }, Observe(Sample(charges: 1)));
        }

        [Fact]
        public void PerkUnlocks_EmitOncePerPerkInThresholdOrder()
        {
            Observe(Sample());

            Assert.Equal(
                new[] { ScenarioSignal.PerkStaminaUnlocked, ScenarioSignal.PerkJumpUnlocked },
                Observe(Sample(unlocked: PerkFlags.InfiniteStamina | PerkFlags.InfiniteJump)));

            Assert.Equal(
                new[] { ScenarioSignal.PerkHealUnlocked },
                Observe(Sample(unlocked: PerkFlags.InfiniteStamina | PerkFlags.InfiniteJump
                                         | PerkFlags.NaturalHeal)));
        }

        [Fact]
        public void EveryPerkSignalIsReachable()
        {
            Observe(Sample());

            Assert.Equal(
                new[]
                {
                    ScenarioSignal.PerkStaminaUnlocked, ScenarioSignal.PerkJumpUnlocked,
                    ScenarioSignal.PerkEnemyIgnoreUnlocked, ScenarioSignal.PerkHealUnlocked,
                },
                Observe(Sample(unlocked: PerkFlags.InfiniteStamina | PerkFlags.InfiniteJump
                                         | PerkFlags.EnemyIgnore | PerkFlags.NaturalHeal)));
        }

        [Fact]
        public void EachObservationReplacesThePreviousSignals()
        {
            Observe(Sample());
            Observe(Sample(wolfMode: true));

            Assert.Empty(Observe(Sample(wolfMode: true)));
        }
    }
}
