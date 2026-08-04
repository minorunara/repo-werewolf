using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class StartHoldGateTests
    {
        private const long T0 = 1_000_000L;

        private static bool Tick(
            StartHoldGate gate, long now,
            bool inRunLevel = true, bool operable = true,
            bool werewolfExpected = true, bool gameStartReceived = false)
        {
            return gate.Tick(inRunLevel, operable, werewolfExpected, gameStartReceived, now, out _);
        }

        [Fact]
        public void 操作可能到達でホールドを開始する()
        {
            var gate = new StartHoldGate();
            Assert.False(Tick(gate, T0, operable: false));
            Assert.Equal(StartHoldPhase.Idle, gate.Phase);

            Assert.True(Tick(gate, T0 + 100));
            Assert.Equal(StartHoldPhase.Holding, gate.Phase);
            Assert.Equal(500, gate.HeldMs(T0 + 600));
        }

        [Fact]
        public void GameStart受信で解除し理由が載る()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate, T0));

            bool freeze = gate.Tick(true, true, true, gameStartReceived: true, T0 + 2000, out StartHoldRelease released);
            Assert.False(freeze);
            Assert.Equal(StartHoldRelease.GameStart, released);
            Assert.Equal(StartHoldPhase.Released, gate.Phase);
        }

        [Fact]
        public void フェイルセーフ満了で解除する()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate, T0));
            Assert.True(Tick(gate, T0 + 19_999));

            bool freeze = gate.Tick(true, true, true, false, T0 + 20_000, out StartHoldRelease released);
            Assert.False(freeze);
            Assert.Equal(StartHoldRelease.Failsafe, released);
            Assert.Equal(StartHoldPhase.Released, gate.Phase);
        }

        [Fact]
        public void フェイルセーフ解除後の170遅着は一度だけギャップが載る()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate, T0));
            gate.Tick(true, true, true, false, T0 + 20_000, out _);
            Assert.Equal(-1, gate.LateGameStartGapMs);

            Assert.False(Tick(gate, T0 + 25_000, gameStartReceived: true));
            Assert.Equal(5_000, gate.LateGameStartGapMs);

            Assert.False(Tick(gate, T0 + 26_000, gameStartReceived: true));
            Assert.Equal(-1, gate.LateGameStartGapMs);
        }

        [Fact]
        public void 正常なGameStart解除では遅着を検出しない()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate, T0));
            gate.Tick(true, true, true, true, T0 + 1000, out _);

            Assert.False(Tick(gate, T0 + 2000, gameStartReceived: true));
            Assert.Equal(-1, gate.LateGameStartGapMs);
        }

        [Fact]
        public void 遅着観測はレベルを離れると打ち切られる()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate, T0));
            gate.Tick(true, true, true, false, T0 + 20_000, out _);

            Assert.False(Tick(gate, T0 + 21_000, inRunLevel: false));
            Assert.False(Tick(gate, T0 + 22_000, gameStartReceived: true));
            Assert.Equal(-1, gate.LateGameStartGapMs);
        }

        [Fact]
        public void 解除後は同一レベルで再ホールドしない()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate, T0));
            gate.Tick(true, true, true, true, T0 + 1000, out _);

            Assert.False(Tick(gate, T0 + 2000, gameStartReceived: true));
            Assert.False(Tick(gate, T0 + 3000, gameStartReceived: false));
            Assert.Equal(StartHoldPhase.Released, gate.Phase);
        }

        [Fact]
        public void 操作可能到達時点で受信済みならホールドせず即解除扱い()
        {
            var gate = new StartHoldGate();
            Assert.False(Tick(gate, T0, gameStartReceived: true));
            Assert.Equal(StartHoldPhase.Released, gate.Phase);
        }

        [Fact]
        public void 人狼モード外ではホールドしない()
        {
            var gate = new StartHoldGate();
            Assert.False(Tick(gate, T0, werewolfExpected: false));
            Assert.Equal(StartHoldPhase.Released, gate.Phase);

            Assert.False(Tick(gate, T0 + 1000, werewolfExpected: true));
        }

        [Fact]
        public void レベルを離れるとIdleへ戻り次レベルで再ホールドできる()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate, T0));
            gate.Tick(true, true, true, true, T0 + 1000, out _);
            Assert.Equal(StartHoldPhase.Released, gate.Phase);

            Assert.False(Tick(gate, T0 + 2000, inRunLevel: false));
            Assert.Equal(StartHoldPhase.Idle, gate.Phase);

            Assert.True(Tick(gate, T0 + 3000));
            Assert.Equal(StartHoldPhase.Holding, gate.Phase);
        }

        [Fact]
        public void ホールド中のレベル離脱は凍結を即時止める()
        {
            var gate = new StartHoldGate();
            Assert.True(Tick(gate, T0));
            Assert.False(Tick(gate, T0 + 500, inRunLevel: false));
            Assert.Equal(StartHoldPhase.Idle, gate.Phase);
        }

        [Fact]
        public void HeldMsはホールド中のみ計上される()
        {
            var gate = new StartHoldGate();
            Assert.Equal(0, gate.HeldMs(T0));
            Tick(gate, T0);
            Assert.Equal(1500, gate.HeldMs(T0 + 1500));
            gate.Tick(true, true, true, true, T0 + 2000, out _);
            Assert.Equal(0, gate.HeldMs(T0 + 3000));
        }
    }
}
