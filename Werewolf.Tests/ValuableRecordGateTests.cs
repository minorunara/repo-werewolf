using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ValuableRecordGateTests
    {
        [Theory]
        [InlineData(Role.Werewolf, true)]
        [InlineData(Role.Bomber, true)]
        [InlineData(Role.BlackCat, true)]
        [InlineData(Role.Villager, false)]
        [InlineData(Role.Shaman, false)]
        public void IsWerewolfTeam_MatchesTeamOf(Role role, bool expected)
        {
            Assert.Equal(expected, ValuableRecordGate.IsWerewolfTeam(role));
        }

        [Fact]
        public void IsWerewolfTeam_UnknownRoleIsFalse()
        {
            Assert.False(ValuableRecordGate.IsWerewolfTeam(null));
        }

        [Theory]
        [InlineData(Role.Werewolf, true)]
        [InlineData(Role.Bomber, true)]
        [InlineData(Role.BlackCat, true)]
        [InlineData(Role.Villager, false)]
        [InlineData(Role.Shaman, false)]
        public void ShouldSuppressDiscover_OnlyWerewolfTeam(Role role, bool expected)
        {
            Assert.Equal(expected, ValuableRecordGate.ShouldSuppressDiscover(
                role, alive: true, roundActive: true, recordOn: false));
        }

        [Fact]
        public void ShouldSuppressDiscover_RequiresAllConditions()
        {
            Assert.True(ValuableRecordGate.ShouldSuppressDiscover(Role.Werewolf, true, true, false));
            Assert.False(ValuableRecordGate.ShouldSuppressDiscover(Role.Werewolf, true, true, true));
            Assert.False(ValuableRecordGate.ShouldSuppressDiscover(Role.Werewolf, true, false, false));
            Assert.False(ValuableRecordGate.ShouldSuppressDiscover(Role.Werewolf, false, true, false));
            Assert.False(ValuableRecordGate.ShouldSuppressDiscover(null, true, true, false));
        }

        [Fact]
        public void CanOperate_CoversPlayAndMeetingCountdownButNotWarpedMeeting()
        {
            Assert.True(ValuableRecordGate.CanOperate(Role.Werewolf, true, GamePhase.Play, false));
            Assert.True(ValuableRecordGate.CanOperate(Role.Werewolf, true, GamePhase.Meeting, false));
            Assert.False(ValuableRecordGate.CanOperate(Role.Werewolf, true, GamePhase.Meeting, true));
            Assert.False(ValuableRecordGate.CanOperate(Role.Werewolf, true, GamePhase.Play, true));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void CanOperate_NonSessionPhasesAreHidden(GamePhase phase)
        {
            Assert.False(ValuableRecordGate.CanOperate(Role.Werewolf, true, phase, false));
        }

        [Fact]
        public void CanOperate_RequiresAliveWerewolfTeam()
        {
            Assert.False(ValuableRecordGate.CanOperate(Role.Werewolf, false, GamePhase.Play, false));
            Assert.False(ValuableRecordGate.CanOperate(Role.Villager, true, GamePhase.Play, false));
            Assert.False(ValuableRecordGate.CanOperate(Role.Shaman, true, GamePhase.Play, false));
            Assert.False(ValuableRecordGate.CanOperate(null, true, GamePhase.Play, false));
        }

        [Fact]
        public void ToggleValuableRecord_StartsOnAndFlipsForWerewolfTeam()
        {
            var state = new RolesClientState();
            Assert.True(state.ValuableRecordOn);

            Assert.True(state.ToggleValuableRecord(Role.Werewolf));
            Assert.False(state.ValuableRecordOn);
            Assert.True(state.ToggleValuableRecord(Role.Werewolf));
            Assert.True(state.ValuableRecordOn);
        }

        [Fact]
        public void DefaultState_DoesNotSuppressWerewolfDiscover()
        {
            var state = new RolesClientState();
            Assert.False(ValuableRecordGate.ShouldSuppressDiscover(
                Role.Werewolf, alive: true, roundActive: true, recordOn: state.ValuableRecordOn));
        }

        [Theory]
        [InlineData(Role.Bomber)]
        [InlineData(Role.BlackCat)]
        public void ToggleValuableRecord_WorksForOtherWerewolfTeamRoles(Role role)
        {
            var state = new RolesClientState();
            Assert.True(state.ToggleValuableRecord(role));
            Assert.False(state.ValuableRecordOn);
        }

        [Theory]
        [InlineData(Role.Villager)]
        [InlineData(Role.Shaman)]
        public void ToggleValuableRecord_NoOpForVillagerTeam(Role role)
        {
            var state = new RolesClientState();
            Assert.False(state.ToggleValuableRecord(role));
            Assert.True(state.ValuableRecordOn);
        }

        [Fact]
        public void ToggleValuableRecord_NoOpWhenRoleUnknown()
        {
            var state = new RolesClientState();
            Assert.False(state.ToggleValuableRecord(null));
            Assert.True(state.ValuableRecordOn);
        }

        [Fact]
        public void ForceValuableRecordOff_ReportsTransitionOnce()
        {
            var state = new RolesClientState();
            Assert.True(state.ForceValuableRecordOff());
            Assert.False(state.ValuableRecordOn);
            Assert.False(state.ForceValuableRecordOff());
        }

        [Fact]
        public void Reset_RestoresRecordToggleToOn()
        {
            var state = new RolesClientState();
            state.ToggleValuableRecord(Role.Werewolf);
            Assert.False(state.ValuableRecordOn);
            state.Reset();
            Assert.True(state.ValuableRecordOn);
        }
    }
}
