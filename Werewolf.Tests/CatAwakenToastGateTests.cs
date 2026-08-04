using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CatAwakenToastGateTests
    {
        private const long Start = 1_000_000;
        private const int DelaySec = 60;
        private const long Due = Start + DelaySec * 1000L;

        [Fact]
        public void BeforeDue_DoesNotFire()
        {
            var gate = new CatAwakenToastGate();
            Assert.False(gate.ShouldFire(GamePhase.Play, true, Start, DelaySec, Due - 1));
        }

        [Fact]
        public void AtDue_FiresExactlyOnce()
        {
            var gate = new CatAwakenToastGate();

            Assert.True(gate.ShouldFire(GamePhase.Play, true, Start, DelaySec, Due));
            Assert.False(gate.ShouldFire(GamePhase.Play, true, Start, DelaySec, Due + 100));
            Assert.False(gate.ShouldFire(GamePhase.Play, true, Start, DelaySec, Due + 60_000));
        }

        [Fact]
        public void CatNotPossible_NeverFires()
        {
            var gate = new CatAwakenToastGate();
            Assert.False(gate.ShouldFire(GamePhase.Play, false, Start, DelaySec, Due + 1000));
        }

        [Fact]
        public void CatPossible_ArrivingAfterDue_FiresOnThatFrame()
        {
            var gate = new CatAwakenToastGate();

            Assert.False(gate.ShouldFire(GamePhase.Play, false, Start, DelaySec, Due + 1000));
            Assert.True(gate.ShouldFire(GamePhase.Play, true, Start, DelaySec, Due + 2000));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void OutsideSession_DoesNotFire(GamePhase phase)
        {
            var gate = new CatAwakenToastGate();
            Assert.False(gate.ShouldFire(phase, true, Start, DelaySec, Due + 1000));
        }

        [Fact]
        public void DuringMeeting_DefersUntilPlayResumes()
        {
            var gate = new CatAwakenToastGate();

            Assert.False(gate.ShouldFire(GamePhase.Meeting, true, Start, DelaySec, Due));
            Assert.False(gate.ShouldFire(GamePhase.Meeting, true, Start, DelaySec, Due + 30_000));
            Assert.True(gate.ShouldFire(GamePhase.Play, true, Start, DelaySec, Due + 60_000));
        }

        [Fact]
        public void WithoutGameStart_DoesNotFire()
        {
            var gate = new CatAwakenToastGate();
            Assert.False(gate.ShouldFire(GamePhase.Play, true, gameStartUnixMs: 0,
                revealDelaySec: DelaySec, nowUnixMs: long.MaxValue));
        }

        [Fact]
        public void ZeroDelay_FiresAtGameStart()
        {
            var gate = new CatAwakenToastGate();
            Assert.True(gate.ShouldFire(GamePhase.Play, true, Start, revealDelaySec: 0, nowUnixMs: Start));
        }

        [Fact]
        public void Reset_RearmsForNextRound()
        {
            var gate = new CatAwakenToastGate();
            Assert.True(gate.ShouldFire(GamePhase.Play, true, Start, DelaySec, Due));

            gate.Reset();

            long nextStart = Due + 300_000;
            long nextDue = nextStart + DelaySec * 1000L;
            Assert.False(gate.ShouldFire(GamePhase.Play, true, nextStart, DelaySec, nextDue - 1));
            Assert.True(gate.ShouldFire(GamePhase.Play, true, nextStart, DelaySec, nextDue));
        }
    }
}
