using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class SettingsPanelGateTests
    {
        [Theory]
        [InlineData(GamePhase.Play, true, true)]
        [InlineData(GamePhase.Meeting, true, true)]
        [InlineData(GamePhase.RoleReveal, true, true)]
        [InlineData(GamePhase.PreplayAborted, true, false)]
        [InlineData(GamePhase.Lobby, true, false)]
        [InlineData(GamePhase.GameOver, true, false)]
        [InlineData(GamePhase.Play, false, false)]
        public void MatchScope_IsPlayOrMeetingInsideRunLevel(GamePhase phase, bool inRunLevel, bool expected)
        {
            Assert.Equal(expected, SettingsPanelGate.IsMatchScope(phase, inRunLevel));
        }

        [Fact]
        public void Lobby_ShowsPanelWhenAllConditionsHold()
        {
            var view = SettingsPanelGate.Lobby(inLobby: true, hasData: true, modeEnabled: true, userHidden: false);
            Assert.True(view.Panel);
            Assert.False(view.Hint);
            Assert.False(view.Elevated);
        }

        [Fact]
        public void Lobby_UserHidden_ShowsBottomHintOnly()
        {
            var view = SettingsPanelGate.Lobby(inLobby: true, hasData: true, modeEnabled: true, userHidden: true);
            Assert.False(view.Panel);
            Assert.True(view.Hint);
            Assert.False(view.HintAtTop);
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void Lobby_MissingBaseCondition_HidesEverything(bool inLobby, bool hasData, bool modeEnabled)
        {
            var view = SettingsPanelGate.Lobby(inLobby, hasData, modeEnabled, userHidden: true);
            Assert.False(view.Panel);
            Assert.False(view.Hint);
        }

        [Fact]
        public void Match_EscMenuOpen_ShowsElevatedPanel()
        {
            var view = SettingsPanelGate.Match(escMenuOpen: true, hasData: true, manualOpen: false, userHidden: false);
            Assert.True(view.Panel);
            Assert.False(view.Hint);
            Assert.True(view.Elevated);
        }

        [Fact]
        public void Match_UserHidden_ShowsTopHintOnly()
        {
            var view = SettingsPanelGate.Match(escMenuOpen: true, hasData: true, manualOpen: false, userHidden: true);
            Assert.False(view.Panel);
            Assert.True(view.Hint);
            Assert.True(view.HintAtTop);
        }

        [Theory]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void Match_EscClosedOrNoDataOrManualOpen_HidesEverything(bool escMenuOpen, bool hasData, bool manualOpen)
        {
            var view = SettingsPanelGate.Match(escMenuOpen, hasData, manualOpen, userHidden: false);
            Assert.False(view.Panel);
            Assert.False(view.Hint);
        }
    }
}
