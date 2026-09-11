using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class AutoStartWaitGateTests
    {
        [Fact]
        public void 未武装では発火しない()
        {
            var gate = new AutoStartWaitGate();
            Assert.False(gate.Armed);
            Assert.False(gate.ShouldFire(allPlayersLoaded: true));
        }

        [Fact]
        public void 武装後_全員ロード完了で発火し自動解除される()
        {
            var gate = new AutoStartWaitGate();
            gate.Arm();
            Assert.True(gate.Armed);

            Assert.False(gate.ShouldFire(false));
            Assert.True(gate.ShouldFire(true));

            Assert.False(gate.Armed);
            Assert.False(gate.ShouldFire(true));
        }

        [Fact]
        public void 未完了のままでは発火も解除もしない()
        {
            var gate = new AutoStartWaitGate();
            gate.Arm();

            Assert.False(gate.ShouldFire(false));
            Assert.False(gate.ShouldFire(false));
            Assert.True(gate.Armed);
        }

        [Fact]
        public void 武装中の再武装は状態を変えない()
        {
            var gate = new AutoStartWaitGate();
            gate.Arm();
            gate.Arm();

            Assert.True(gate.Armed);
            Assert.False(gate.ShouldFire(false));
        }

        [Fact]
        public void 明示解除後は発火しないが再Armで再度使える()
        {
            var gate = new AutoStartWaitGate();
            gate.Arm();
            gate.Disarm();
            Assert.False(gate.Armed);
            Assert.False(gate.ShouldFire(true));

            gate.Arm();
            Assert.True(gate.ShouldFire(true));
        }
    }
}
