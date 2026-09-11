using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ClickGateTests
    {
        [Fact]
        public void PressAndReleaseInPlace_IsClick()
        {
            var gate = new ClickGate();
            Assert.False(gate.Tick(down: true, up: false, 100f, 100f));
            Assert.False(gate.Tick(down: false, up: false, 101f, 100f));
            Assert.True(gate.Tick(down: false, up: true, 102f, 101f));
        }

        [Fact]
        public void PressDownAlone_IsNotClick()
        {
            var gate = new ClickGate();
            Assert.False(gate.Tick(down: true, up: false, 100f, 100f));
            Assert.True(gate.IsPressed);
        }

        [Fact]
        public void DragBeyondThreshold_IsNotClick()
        {
            var gate = new ClickGate();
            gate.Tick(down: true, up: false, 100f, 100f);
            gate.Tick(down: false, up: false, 100f, 100f + ClickGate.DragThreshold + 1f);
            Assert.False(gate.Tick(down: false, up: true, 100f, 100f + ClickGate.DragThreshold + 1f));
        }

        [Fact]
        public void DragOutAndBack_IsNotClick()
        {
            var gate = new ClickGate();
            gate.Tick(down: true, up: false, 100f, 100f);
            gate.Tick(down: false, up: false, 100f, 150f);
            Assert.False(gate.Tick(down: false, up: true, 100f, 100f));
        }

        [Fact]
        public void MoveWithinThreshold_IsClick()
        {
            var gate = new ClickGate();
            gate.Tick(down: true, up: false, 100f, 100f);
            gate.Tick(down: false, up: false, 100f + ClickGate.DragThreshold, 100f);
            Assert.True(gate.Tick(down: false, up: true, 100f + ClickGate.DragThreshold, 100f));
        }

        [Fact]
        public void ReleaseWithoutPress_IsNotClick()
        {
            var gate = new ClickGate();
            Assert.False(gate.Tick(down: false, up: true, 100f, 100f));
        }

        [Fact]
        public void SameFramePressAndRelease_IsClick()
        {
            var gate = new ClickGate();
            Assert.True(gate.Tick(down: true, up: true, 100f, 100f));
            Assert.False(gate.IsPressed);
        }

        [Fact]
        public void Reset_DropsPendingPress()
        {
            var gate = new ClickGate();
            gate.Tick(down: true, up: false, 100f, 100f);
            gate.Reset();
            Assert.False(gate.IsPressed);
            Assert.False(gate.Tick(down: false, up: true, 100f, 100f));
        }

        [Fact]
        public void NextStrokeAfterDrag_StartsClean()
        {
            var gate = new ClickGate();
            gate.Tick(down: true, up: false, 0f, 0f);
            gate.Tick(down: false, up: true, 0f, 100f);
            Assert.True(gate.Tick(down: true, up: true, 0f, 100f));
        }
    }
}
