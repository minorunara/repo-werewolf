using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ResultChatGateTests
    {
        [Theory]
        [InlineData(GamePhase.GameOver, true, true, true)]
        [InlineData(GamePhase.GameOver, true, false, false)]
        [InlineData(GamePhase.GameOver, false, true, false)]
        [InlineData(GamePhase.GameOver, false, false, false)]
        [InlineData(GamePhase.Lobby, true, true, false)]
        [InlineData(GamePhase.Play, true, true, false)]
        [InlineData(GamePhase.Meeting, true, true, false)]
        public void IsOpen_MatrixOfAllInputs(
            GamePhase phase, bool resultScreenVisible, bool chatLogEnabled, bool expected)
        {
            Assert.Equal(expected, ResultChatGate.IsOpen(phase, resultScreenVisible, chatLogEnabled));
        }
    }
}
