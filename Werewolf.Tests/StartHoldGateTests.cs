using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class StartHoldGateTests
    {
        private static bool Tick(
            StartHoldGate gate,
            bool inRunLevel = true,
            bool operable = true,
            bool werewolfExpected = true,
            bool gameStartReceived = false)
            => gate.Tick(inRunLevel, operable, werewolfExpected, gameStartReceived);

        [Fact]
        public void 操作可能到達でホールドを開始する()
        {
            var gate = new StartHoldGate();
            Assert.False(Tick(gate, operable: false));
            Assert.Equal(StartHoldPhase.Idle, gate.Phase);

            Assert.True(Tick(gate));
            Assert.Equal(StartHoldPhase.Holding, gate.Phase);
        }

        [Fact]
        public void GameStart受信で解除する()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate));

            Assert.False(Tick(gate, gameStartReceived: true));
            Assert.Equal(StartHoldPhase.Released, gate.Phase);
        }

        [Fact]
        public void 未受信のままでは解除しない()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate));
            Assert.True(Tick(gate));
            Assert.True(Tick(gate));
            Assert.Equal(StartHoldPhase.Holding, gate.Phase);
        }

        [Fact]
        public void ホールド中のレベル離脱は凍結を即時止める()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate));

            Assert.False(Tick(gate, inRunLevel: false));
            Assert.Equal(StartHoldPhase.Idle, gate.Phase);
        }

        [Fact]
        public void 解除後は同一レベルで再ホールドしない()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate));
            Assert.False(Tick(gate, gameStartReceived: true));

            Assert.False(Tick(gate, gameStartReceived: true));
            Assert.False(Tick(gate, gameStartReceived: false));
            Assert.Equal(StartHoldPhase.Released, gate.Phase);
        }

        [Fact]
        public void 操作可能到達時点で受信済みならホールドしない()
        {
            var gate = new StartHoldGate();
            Assert.False(Tick(gate, gameStartReceived: true));
            Assert.Equal(StartHoldPhase.Released, gate.Phase);
        }

        [Fact]
        public void 人狼モード外ではホールドしない()
        {
            var gate = new StartHoldGate();
            Assert.False(Tick(gate, werewolfExpected: false));
            Assert.Equal(StartHoldPhase.Released, gate.Phase);

            Assert.False(Tick(gate, werewolfExpected: true));
        }

        [Fact]
        public void レベルを離れると次レベルで再ホールドできる()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate));
            Assert.False(Tick(gate, gameStartReceived: true));

            Assert.False(Tick(gate, inRunLevel: false));
            Assert.Equal(StartHoldPhase.Idle, gate.Phase);

            Assert.True(Tick(gate));
            Assert.Equal(StartHoldPhase.Holding, gate.Phase);
        }
    }
}
