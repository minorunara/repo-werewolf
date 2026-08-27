using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ToastRepeatGateTests
    {
        [Fact]
        public void FirstCall_Passes()
        {
            var gate = new ToastRepeatGate();

            Assert.True(gate.TryPass(nowUnixMs: 1000, suppressSec: 9));
        }

        [Fact]
        public void WithinSuppressWindow_Blocks()
        {
            var gate = new ToastRepeatGate();
            gate.TryPass(nowUnixMs: 1000, suppressSec: 9);

            Assert.False(gate.TryPass(nowUnixMs: 1000, suppressSec: 9));
            Assert.False(gate.TryPass(nowUnixMs: 5000, suppressSec: 9));
            Assert.False(gate.TryPass(nowUnixMs: 9999, suppressSec: 9));
        }

        [Fact]
        public void AfterSuppressWindow_PassesAndRearms()
        {
            var gate = new ToastRepeatGate();
            gate.TryPass(nowUnixMs: 1000, suppressSec: 9);

            Assert.True(gate.TryPass(nowUnixMs: 10000, suppressSec: 9));
            Assert.False(gate.TryPass(nowUnixMs: 18999, suppressSec: 9));
            Assert.True(gate.TryPass(nowUnixMs: 19000, suppressSec: 9));
        }

        [Fact]
        public void NonPositiveSuppressSec_PassesEveryTime()
        {
            var gate = new ToastRepeatGate();

            Assert.True(gate.TryPass(nowUnixMs: 1000, suppressSec: 0));
            Assert.True(gate.TryPass(nowUnixMs: 1000, suppressSec: 0));
            Assert.True(gate.TryPass(nowUnixMs: 1000, suppressSec: -1));
        }

        [Fact]
        public void Reset_ClearsSuppression()
        {
            var gate = new ToastRepeatGate();
            gate.TryPass(nowUnixMs: 1000, suppressSec: 9);

            gate.Reset();

            Assert.True(gate.TryPass(nowUnixMs: 1000, suppressSec: 9));
        }
    }
}
