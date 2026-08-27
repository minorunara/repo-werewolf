using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioShamanWatchTests
    {
        private readonly ScenarioShamanWatch _watch = new ScenarioShamanWatch();
        private readonly List<ScenarioSignal> _signals = new List<ScenarioSignal>();

        private static ScenarioShamanSample Sample(bool armed = false, bool coolingDown = false,
                                                   bool ghost = false,
                                                   ShamanStormTier storm = ShamanStormTier.None)
            => new ScenarioShamanSample(armed, coolingDown, ghost, storm);

        private IReadOnlyList<ScenarioSignal> Observe(ScenarioShamanSample sample)
        {
            _watch.Observe(sample, _signals);
            return _signals;
        }

        [Fact]
        public void FirstObservation_OnlyPrimes()
        {
            Assert.Empty(Observe(Sample(armed: true, ghost: true, storm: ShamanStormTier.Strong)));
            Assert.True(_watch.Primed);
        }

        [Fact]
        public void SteadyState_EmitsNothing()
        {
            Observe(Sample(armed: true, storm: ShamanStormTier.Weak));
            Assert.Empty(Observe(Sample(armed: true, storm: ShamanStormTier.Weak)));
        }

        [Fact]
        public void TranceState_IsNotASignal()
        {
            Observe(Sample());
            Assert.Empty(Observe(Sample(armed: true)));
            Assert.Empty(Observe(Sample(armed: false)));
            Assert.Empty(Observe(Sample(armed: true)));
        }

        [Fact]
        public void GhostFired_FiresWhenTheBurstBecomesVisible()
        {
            Observe(Sample(armed: true));
            Assert.Equal(new[] { ScenarioSignal.ShamanGhostFired }, Observe(Sample(ghost: true)));
            Assert.Empty(Observe(Sample(ghost: true)));
            Assert.Empty(Observe(Sample()));
        }

        [Fact]
        public void StormEntered_FiresOnlyWhenATierAppearsFromNone()
        {
            Observe(Sample());
            Assert.Equal(new[] { ScenarioSignal.ShamanStormEntered },
                Observe(Sample(storm: ShamanStormTier.Weak)));
            Assert.Empty(Observe(Sample(storm: ShamanStormTier.Medium)));
            Assert.Empty(Observe(Sample(storm: ShamanStormTier.Strong)));
            Assert.Empty(Observe(Sample(storm: ShamanStormTier.None)));
            Assert.Equal(new[] { ScenarioSignal.ShamanStormEntered },
                Observe(Sample(storm: ShamanStormTier.Strong)));
        }

        [Fact]
        public void Observe_EmitsInAFixedOrderWhenSeveralEdgesCoincide()
        {
            Observe(Sample());
            Assert.Equal(
                new[] { ScenarioSignal.ShamanGhostFired, ScenarioSignal.ShamanStormEntered },
                Observe(Sample(armed: true, ghost: true, storm: ShamanStormTier.Weak)));
        }

        [Fact]
        public void Observe_ClearsTheListBeforeFilling()
        {
            _signals.Add(ScenarioSignal.Advance);
            Observe(Sample());
            Assert.Empty(_signals);
        }
    }
}
