using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ProximityGaugeTests
    {
        [Fact]
        public void Tick_AccumulatesToFullOverFullSeconds()
        {
            var g = new ProximityGauge(fullSeconds: 20f);
            for (int i = 0; i < 20; i++) g.Tick(1, within: true, deltaSeconds: 1f, suspend: false);

            Assert.True(g.IsFull(1));
            Assert.Equal(1f, g.Ratio(1), 3);
        }

        [Fact]
        public void Tick_OutsideDecaysAtDoubleSpeed()
        {
            var g = new ProximityGauge(fullSeconds: 20f, decayMultiplier: 2f);
            for (int i = 0; i < 20; i++) g.Tick(1, within: true, deltaSeconds: 1f, suspend: false);
            Assert.True(g.IsFull(1));

            for (int i = 0; i < 5; i++) g.Tick(1, within: false, deltaSeconds: 1f, suspend: false);
            Assert.Equal(0.5f, g.Ratio(1), 3);
        }

        [Fact]
        public void Tick_SuspendFreezesBothAccumulationAndDecay()
        {
            var g = new ProximityGauge(fullSeconds: 20f);
            for (int i = 0; i < 10; i++) g.Tick(1, within: true, deltaSeconds: 1f, suspend: false);
            float before = g.Ratio(1);

            for (int i = 0; i < 10; i++) g.Tick(1, within: true, deltaSeconds: 1f, suspend: true);
            for (int i = 0; i < 10; i++) g.Tick(1, within: false, deltaSeconds: 1f, suspend: true);
            Assert.Equal(before, g.Ratio(1), 3);
        }

        [Fact]
        public void ResetAll_ClearsAllActorsAndRearmsHysteresis()
        {
            var g = new ProximityGauge(fullSeconds: 5f);
            for (int i = 0; i < 5; i++) g.Tick(1, true, 1f, false);
            for (int i = 0; i < 5; i++) g.Tick(2, true, 1f, false);
            bool _;
            g.TryGetNotifyEdge(out _);

            g.ResetAll();

            Assert.Equal(0f, g.Ratio(1));
            Assert.Equal(0f, g.Ratio(2));

            for (int i = 0; i < 5; i++) g.Tick(1, true, 1f, false);
            Assert.True(g.TryGetNotifyEdge(out _));
        }

        [Fact]
        public void Remove_SetsActorToZeroImmediately()
        {
            var g = new ProximityGauge(fullSeconds: 5f);
            for (int i = 0; i < 5; i++) g.Tick(1, true, 1f, false);
            Assert.True(g.IsFull(1));

            g.Remove(1);
            Assert.Equal(0f, g.Ratio(1));
            Assert.False(g.IsFull(1));
        }

        [Fact]
        public void Gauges_AreIndependentPerActor()
        {
            var g = new ProximityGauge(fullSeconds: 10f);
            for (int i = 0; i < 10; i++) g.Tick(1, true, 1f, false);
            for (int i = 0; i < 3; i++) g.Tick(2, true, 1f, false);

            Assert.True(g.IsFull(1));
            Assert.Equal(0.3f, g.Ratio(2), 3);
        }

        [Fact]
        public void NotifyEdge_FiresOnceOnFull_ThenRearmsAfterAllEmpty()
        {
            var g = new ProximityGauge(fullSeconds: 2f);
            bool armed;

            for (int i = 0; i < 2; i++) g.Tick(1, true, 1f, false);
            Assert.True(g.TryGetNotifyEdge(out armed));
            Assert.False(armed);

            g.Tick(1, true, 1f, false);
            Assert.False(g.TryGetNotifyEdge(out armed));
            Assert.False(armed);

            for (int i = 0; i < 2; i++) g.Tick(2, true, 1f, false);
            Assert.False(g.TryGetNotifyEdge(out armed));
            Assert.False(armed);

            for (int i = 0; i < 4; i++) g.Tick(1, false, 1f, false);
            for (int i = 0; i < 4; i++) g.Tick(2, false, 1f, false);
            Assert.False(g.TryGetNotifyEdge(out armed));
            Assert.True(armed);

            for (int i = 0; i < 2; i++) g.Tick(1, true, 1f, false);
            Assert.True(g.TryGetNotifyEdge(out _));
        }
    }
}
