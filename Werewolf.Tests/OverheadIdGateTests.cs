using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class OverheadIdGateTests
    {
        private const float Dt = 0.05f;

        private static float TickAtAngle(OverheadIdGate gate, int actor, bool bodyVisible,
            double angleDeg, float dt = Dt)
        {
            float x = (float)Math.Sin(angleDeg * Math.PI / 180.0);
            float z = (float)Math.Cos(angleDeg * Math.PI / 180.0);
            return gate.Tick(actor, bodyVisible, 0f, 0f, 1f, x, 0f, z, dt);
        }

        private static float RampToFull(OverheadIdGate gate, int actor)
        {
            float alpha = 0f;
            for (int i = 0; i < 20; i++) alpha = TickAtAngle(gate, actor, bodyVisible: true, angleDeg: 0);
            return alpha;
        }

        [Fact]
        public void InCone_RampsToFullAlpha()
        {
            var gate = new OverheadIdGate();

            float first = TickAtAngle(gate, 1, true, 0);
            float full = RampToFull(gate, 1);

            Assert.InRange(first, 0.01f, 1f);
            Assert.Equal(1f, full);
        }

        [Fact]
        public void OutOfCone_StaysHidden()
        {
            var gate = new OverheadIdGate();

            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(0f, TickAtAngle(gate, 1, true, 30));
            }
        }

        [Fact]
        public void BodyInvisible_StaysHidden_EvenWhenAimedAt()
        {
            var gate = new OverheadIdGate();

            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(0f, TickAtAngle(gate, 1, bodyVisible: false, angleDeg: 0));
            }
        }

        [Fact]
        public void ConeBoundary_InsideShows_OutsideDoesNot()
        {
            var gate = new OverheadIdGate();

            Assert.True(TickAtAngle(gate, 1, true, OverheadIdGate.ConeDegrees - 1) > 0f);
            Assert.Equal(0f, TickAtAngle(gate, 2, true, OverheadIdGate.ConeDegrees + 1));
        }

        [Fact]
        public void LeavingCone_LingersAtFullAlpha_ThenFadesOut()
        {
            var gate = new OverheadIdGate();
            RampToFull(gate, 1);

            float elapsed = 0f;
            float alpha = 1f;
            while (elapsed + 0.1f <= OverheadIdGate.LingerSeconds)
            {
                alpha = TickAtAngle(gate, 1, true, 90, dt: 0.1f);
                elapsed += 0.1f;
                Assert.Equal(1f, alpha);
            }

            alpha = TickAtAngle(gate, 1, true, 90, dt: 0.1f);
            Assert.True(alpha < 1f);

            for (int i = 0; i < 20; i++) alpha = TickAtAngle(gate, 1, true, 90, dt: 0.1f);
            Assert.Equal(0f, alpha);
        }

        [Fact]
        public void ReenterCone_ResetsLingerAndRampsBackUp()
        {
            var gate = new OverheadIdGate();
            RampToFull(gate, 1);

            for (int i = 0; i < 5; i++) TickAtAngle(gate, 1, true, 90, dt: 0.1f);
            float faded = TickAtAngle(gate, 1, true, 90, dt: 0.05f);
            Assert.True(faded < 1f);

            float back = TickAtAngle(gate, 1, true, 0);
            Assert.True(back > faded);
            Assert.Equal(1f, RampToFull(gate, 1));
        }

        [Fact]
        public void ZeroForwardVector_TreatedAsOutOfCone()
        {
            var gate = new OverheadIdGate();

            Assert.Equal(0f, gate.Tick(1, true, 0f, 0f, 0f, 0f, 0f, 1f, Dt));
        }

        [Fact]
        public void ActorsAreIndependent()
        {
            var gate = new OverheadIdGate();

            RampToFull(gate, 1);
            Assert.Equal(0f, TickAtAngle(gate, 2, true, 90));
            Assert.Equal(1f, TickAtAngle(gate, 1, true, 0));
        }

        [Fact]
        public void Reset_ClearsAllState()
        {
            var gate = new OverheadIdGate();
            RampToFull(gate, 1);

            gate.Reset();

            Assert.Equal(0f, TickAtAngle(gate, 1, true, 90));
        }
    }
}
