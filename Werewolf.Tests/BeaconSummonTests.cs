using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class BeaconSummonTests
    {
        [Theory]
        [InlineData(0, 5f)]
        [InlineData(1, 10f)]
        [InlineData(2, 15f)]
        [InlineData(3, 20f)]
        [InlineData(4, 20f)]
        [InlineData(10, 20f)]
        public void ClampSeconds_IsStaircaseWithCap(int index, float expected)
        {
            Assert.Equal(expected, BeaconSummonPlan.ClampSeconds(index));
        }

        [Fact]
        public void ClampSeconds_NegativeIndex_IsTreatedAsFirst()
        {
            Assert.Equal(BeaconSummonPlan.StepSeconds, BeaconSummonPlan.ClampSeconds(-1));
        }

        [Fact]
        public void Gate_FirstOpen_Succeeds()
        {
            var gate = new BeaconSummonGate();
            Assert.True(gate.TryOpen(1_000_000L, 60));
        }

        [Fact]
        public void Gate_WithinCooldown_IsClosedAndStateUnchanged()
        {
            var gate = new BeaconSummonGate();
            Assert.True(gate.TryOpen(1_000_000L, 60));
            Assert.False(gate.TryOpen(1_000_000L + 59_999, 60));
            Assert.False(gate.TryOpen(1_000_000L + 59_999, 60));
            Assert.True(gate.TryOpen(1_000_000L + 60_000, 60));
        }

        [Fact]
        public void Gate_Reset_ClearsCooldown()
        {
            var gate = new BeaconSummonGate();
            Assert.True(gate.TryOpen(1_000_000L, 60));
            gate.Reset();
            Assert.True(gate.TryOpen(1_000_000L + 1, 60));
        }
    }
}
