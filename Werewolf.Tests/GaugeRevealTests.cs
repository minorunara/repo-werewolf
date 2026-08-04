using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GaugeRevealTests
    {
        private const long T0 = 1_000_000_000L;

        private const long G0 = T0 + GaugeReveal.HoldMs;

        private static GaugeReveal Started(
            int fromPermille = 200, int toPermille = 600, int fromLoss = 2000, int toLoss = 6000)
        {
            var reveal = new GaugeReveal();
            reveal.Start(fromPermille, toPermille, fromLoss, toLoss, T0);
            return reveal;
        }

        [Fact]
        public void ShouldAnimate_OnlyOnPositiveDelta()
        {
            Assert.True(GaugeReveal.ShouldAnimate(0, 1));
            Assert.True(GaugeReveal.ShouldAnimate(200, 600));
            Assert.False(GaugeReveal.ShouldAnimate(600, 600));
            Assert.False(GaugeReveal.ShouldAnimate(600, 200));
        }

        [Fact]
        public void Delay_HoldsFromValueUntilGrowthStart()
        {
            GaugeReveal reveal = Started();

            Assert.Equal(0.0, reveal.Progress(T0));
            Assert.Equal(0.0, reveal.Progress(G0 - 1));
            Assert.Equal(200, reveal.CurrentPermille(G0 - 1));
            Assert.Equal(2000, reveal.CurrentLoss(G0 - 1));
        }

        [Fact]
        public void GrowthStarted_TurnsTrueAtDelayEnd()
        {
            GaugeReveal reveal = Started();

            Assert.False(reveal.GrowthStarted(G0 - 1));
            Assert.True(reveal.GrowthStarted(G0));
            Assert.True(reveal.GrowthStarted(G0 + GaugeReveal.DurationMs * 10));
        }

        [Fact]
        public void GrowthStarted_FalseWhenInactive()
        {
            var fresh = new GaugeReveal();
            Assert.False(fresh.GrowthStarted(T0));

            GaugeReveal stopped = Started();
            stopped.Stop();
            Assert.False(stopped.GrowthStarted(G0 + GaugeReveal.DurationMs));
        }

        [Fact]
        public void CurrentPermille_StartsAtFromAndEndsAtTo()
        {
            GaugeReveal reveal = Started();

            Assert.Equal(200, reveal.CurrentPermille(T0));
            Assert.Equal(600, reveal.CurrentPermille(G0 + GaugeReveal.DurationMs));
            Assert.Equal(600, reveal.CurrentPermille(G0 + GaugeReveal.DurationMs * 10));
        }

        [Fact]
        public void CurrentLoss_StartsAtFromAndEndsAtTo()
        {
            GaugeReveal reveal = Started(fromLoss: 2000, toLoss: 6000);

            Assert.Equal(2000, reveal.CurrentLoss(T0));
            Assert.Equal(6000, reveal.CurrentLoss(G0 + GaugeReveal.DurationMs));
        }

        [Fact]
        public void Progress_IsMonotonicNonDecreasing()
        {
            GaugeReveal reveal = Started();

            double previous = -1.0;
            for (long t = 0; t <= GaugeReveal.HoldMs + GaugeReveal.DurationMs; t += 50)
            {
                double progress = reveal.Progress(T0 + t);
                Assert.True(progress >= previous, $"t={t}: {progress} < {previous}");
                previous = progress;
            }
        }

        [Fact]
        public void Progress_IsFrontLoaded()
        {
            GaugeReveal reveal = Started();

            double atHalf = reveal.Progress(G0 + GaugeReveal.DurationMs / 2);
            Assert.True(atHalf > 0.5, $"half-time progress {atHalf} は前傾でない");
        }

        [Fact]
        public void Start_InvertedLossIsClampedToAvoidBackwardCounting()
        {
            GaugeReveal reveal = Started(fromLoss: 9000, toLoss: 6000);

            Assert.Equal(6000, reveal.CurrentLoss(T0));
            Assert.Equal(6000, reveal.CurrentLoss(G0 + GaugeReveal.DurationMs / 2));
        }

        [Fact]
        public void Done_TrueAfterDelayPlusDurationAndAfterStop()
        {
            GaugeReveal reveal = Started();

            Assert.False(reveal.Done(G0 + GaugeReveal.DurationMs - 1));
            Assert.True(reveal.Done(G0 + GaugeReveal.DurationMs));

            reveal.Stop();
            Assert.False(reveal.Active);
            Assert.True(reveal.Done(T0));
            Assert.Equal(1.0, reveal.Progress(T0));
        }
    }
}
