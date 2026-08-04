using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class AutoStartWaitGateTests
    {
        private const long T0 = 1_000_000L;

        [Fact]
        public void 未武装では発火しない()
        {
            var gate = new AutoStartWaitGate();
            Assert.False(gate.Armed);
            Assert.Equal(AutoStartFireReason.None, gate.ShouldFire(allPlayersLoaded: true, T0));
        }

        [Fact]
        public void 武装後_全員ロード完了で発火し自動解除される()
        {
            var gate = new AutoStartWaitGate();
            gate.Arm(T0);
            Assert.True(gate.Armed);

            Assert.Equal(AutoStartFireReason.None, gate.ShouldFire(false, T0 + 1000));
            Assert.Equal(AutoStartFireReason.AllLoaded, gate.ShouldFire(true, T0 + 2000));

            Assert.False(gate.Armed);
            Assert.Equal(AutoStartFireReason.None, gate.ShouldFire(true, T0 + 3000));
        }

        [Fact]
        public void 武装後_タイムアウトで発火する()
        {
            var gate = new AutoStartWaitGate(timeoutSec: 60);
            gate.Arm(T0);

            Assert.Equal(AutoStartFireReason.None, gate.ShouldFire(false, T0 + 59_999));
            Assert.Equal(AutoStartFireReason.Timeout, gate.ShouldFire(false, T0 + 60_000));
            Assert.False(gate.Armed);
        }

        [Fact]
        public void 同フレームで両条件成立なら全員ロード完了を優先する()
        {
            var gate = new AutoStartWaitGate(timeoutSec: 60);
            gate.Arm(T0);
            Assert.Equal(AutoStartFireReason.AllLoaded, gate.ShouldFire(true, T0 + 120_000));
        }

        [Fact]
        public void 再武装は無視されタイムアウト起点は最初のArmを保つ()
        {
            var gate = new AutoStartWaitGate(timeoutSec: 60);
            gate.Arm(T0);
            gate.Arm(T0 + 50_000);

            Assert.Equal(AutoStartFireReason.Timeout, gate.ShouldFire(false, T0 + 60_000));
        }

        [Fact]
        public void 明示解除後は発火しないが再Armで再度使える()
        {
            var gate = new AutoStartWaitGate();
            gate.Arm(T0);
            gate.Disarm();
            Assert.False(gate.Armed);
            Assert.Equal(AutoStartFireReason.None, gate.ShouldFire(true, T0 + 1000));

            gate.Arm(T0 + 2000);
            Assert.Equal(AutoStartFireReason.AllLoaded, gate.ShouldFire(true, T0 + 3000));
        }

        [Fact]
        public void 経過ミリ秒は武装中のみ計上される()
        {
            var gate = new AutoStartWaitGate();
            Assert.Equal(0, gate.WaitedMs(T0));
            gate.Arm(T0);
            Assert.Equal(1500, gate.WaitedMs(T0 + 1500));
            gate.Disarm();
            Assert.Equal(0, gate.WaitedMs(T0 + 2000));
        }

        [Fact]
        public void 不正なタイムアウト指定は既定値へ丸める()
        {
            var gate = new AutoStartWaitGate(timeoutSec: 0);
            gate.Arm(T0);
            long defaultMs = AutoStartWaitGate.DefaultTimeoutSec * 1000L;
            Assert.Equal(AutoStartFireReason.None, gate.ShouldFire(false, T0 + defaultMs - 1));
            Assert.Equal(AutoStartFireReason.Timeout, gate.ShouldFire(false, T0 + defaultMs));
        }
    }
}
