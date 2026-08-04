using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class LifecycleGateTests
    {

        [Theory]
        [InlineData(true,  true,  GamePhase.Lobby,    true)]
        [InlineData(true,  true,  GamePhase.Play,     false)]
        [InlineData(true,  true,  GamePhase.Meeting,  false)]
        [InlineData(true,  true,  GamePhase.GameOver, false)]
        [InlineData(true,  false, GamePhase.Lobby,    false)]
        [InlineData(true,  false, GamePhase.Play,     false)]
        [InlineData(false, true,  GamePhase.Lobby,    false)]
        [InlineData(false, true,  GamePhase.Play,     false)]
        [InlineData(false, false, GamePhase.Lobby,    false)]
        [InlineData(false, false, GamePhase.GameOver, false)]
        public void ShouldAutoStart_MatrixOfAllInputs(
            bool modeEnabled, bool isRunLevel, GamePhase phase, bool expected)
        {
            var gate = new LifecycleGate();

            Assert.Equal(expected, gate.ShouldAutoStart(modeEnabled, isRunLevel, phase));
        }

        [Fact]
        public void MarkStarted_BlocksSubsequentAutoStart_EvenWhenAllInputsOk()
        {
            var gate = new LifecycleGate();

            Assert.True(gate.ShouldAutoStart(true, true, GamePhase.Lobby));

            gate.MarkStarted();

            Assert.False(gate.ShouldAutoStart(true, true, GamePhase.Lobby));
        }

        [Fact]
        public void MarkStarted_BlocksAutoStart_AcrossAllInputCombinations()
        {
            var gate = new LifecycleGate();
            gate.MarkStarted();

            Assert.False(gate.ShouldAutoStart(true, true, GamePhase.Lobby));
            Assert.False(gate.ShouldAutoStart(true, true, GamePhase.Play));
            Assert.False(gate.ShouldAutoStart(true, false, GamePhase.Lobby));
            Assert.False(gate.ShouldAutoStart(false, true, GamePhase.Lobby));
            Assert.False(gate.ShouldAutoStart(false, false, GamePhase.GameOver));
        }

        [Fact]
        public void ResetForNextRound_RestoresAutoStartCapability()
        {
            var gate = new LifecycleGate();
            gate.MarkStarted();
            Assert.False(gate.ShouldAutoStart(true, true, GamePhase.Lobby));

            gate.ResetForNextRound();

            Assert.True(gate.ShouldAutoStart(true, true, GamePhase.Lobby));
        }

        [Fact]
        public void MarkStarted_IsIdempotent_ResetForNextRoundIsIdempotent()
        {
            var gate = new LifecycleGate();

            gate.MarkStarted();
            gate.MarkStarted();
            Assert.False(gate.ShouldAutoStart(true, true, GamePhase.Lobby));

            gate.ResetForNextRound();
            gate.ResetForNextRound();
            Assert.True(gate.ShouldAutoStart(true, true, GamePhase.Lobby));
        }

        [Fact]
        public void ResetForNextRound_WhenNotStarted_KeepsInitialState()
        {
            var gate = new LifecycleGate();

            gate.ResetForNextRound();

            Assert.True(gate.ShouldAutoStart(true, true, GamePhase.Lobby));
        }
    }
}
