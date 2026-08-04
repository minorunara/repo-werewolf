using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RewarpGateTests
    {
        private const long T0 = 1_000_000L;
        private const long Sustain = 1500L;
        private const long Cooldown = 3000L;

        [Fact]
        public void 継続不足では発火しない()
        {
            var gate = new RewarpGate(Sustain, Cooldown);
            Assert.False(gate.Tick(1, true, T0));
            Assert.False(gate.Tick(1, true, T0 + Sustain - 1));
        }

        [Fact]
        public void sustain到達で1回だけ発火する()
        {
            var gate = new RewarpGate(Sustain, Cooldown);
            Assert.False(gate.Tick(1, true, T0));
            Assert.True(gate.Tick(1, true, T0 + Sustain));
            Assert.False(gate.Tick(1, true, T0 + Sustain + 1));
        }

        [Fact]
        public void 発火後クールダウン中は再継続でも発火しない()
        {
            var gate = new RewarpGate(Sustain, Cooldown);
            Assert.False(gate.Tick(1, true, T0));
            Assert.True(gate.Tick(1, true, T0 + Sustain));

            long t2 = T0 + Sustain + 1;
            Assert.False(gate.Tick(1, true, t2));
            Assert.False(gate.Tick(1, true, t2 + Sustain));
            Assert.False(gate.Tick(1, true, T0 + Sustain + Cooldown - 1));
        }

        [Fact]
        public void クールダウン明けと再継続で再発火する()
        {
            var gate = new RewarpGate(Sustain, Cooldown);
            Assert.False(gate.Tick(1, true, T0));
            Assert.True(gate.Tick(1, true, T0 + Sustain));

            long cooldownEnd = T0 + Sustain + Cooldown;
            Assert.False(gate.Tick(1, true, cooldownEnd));
            Assert.True(gate.Tick(1, true, cooldownEnd + Sustain));
        }

        [Fact]
        public void isFarFalseで継続がリセットされる()
        {
            var gate = new RewarpGate(Sustain, Cooldown);
            Assert.False(gate.Tick(1, true, T0));
            Assert.False(gate.Tick(1, false, T0 + 500));
            Assert.False(gate.Tick(1, true, T0 + Sustain));
            Assert.True(gate.Tick(1, true, T0 + Sustain + Sustain));
        }

        [Fact]
        public void key毎に独立して状態を持つ()
        {
            var gate = new RewarpGate(Sustain, Cooldown);
            Assert.False(gate.Tick(1, true, T0));
            Assert.True(gate.Tick(1, true, T0 + Sustain));

            Assert.False(gate.Tick(2, true, T0 + Sustain));
            Assert.True(gate.Tick(2, true, T0 + Sustain + Sustain));
        }

        [Fact]
        public void Resetで全状態がクリアされる()
        {
            var gate = new RewarpGate(Sustain, Cooldown);
            Assert.False(gate.Tick(1, true, T0));
            Assert.True(gate.Tick(1, true, T0 + Sustain));

            gate.Reset();

            Assert.False(gate.Tick(1, true, T0 + Sustain + 1));
            Assert.True(gate.Tick(1, true, T0 + Sustain + 1 + Sustain));
        }
    }
}
