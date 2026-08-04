using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ModIntegrityGateTests
    {
        [Fact]
        public void IsActive_Host_UsesLocalModeFlag()
        {
            Assert.True(ModIntegrityGate.IsActive(
                inRoom: true, inLobbyMenu: true, isMaster: true,
                localModeEnabled: true, hostSignalReceived: false));
            Assert.False(ModIntegrityGate.IsActive(
                inRoom: true, inLobbyMenu: true, isMaster: true,
                localModeEnabled: false, hostSignalReceived: true));
        }

        [Fact]
        public void IsActive_Guest_IgnoresLocalModeFlagAndFollowsHostSignal()
        {
            Assert.True(ModIntegrityGate.IsActive(
                inRoom: true, inLobbyMenu: true, isMaster: false,
                localModeEnabled: false, hostSignalReceived: true));
            Assert.False(ModIntegrityGate.IsActive(
                inRoom: true, inLobbyMenu: true, isMaster: false,
                localModeEnabled: true, hostSignalReceived: false));
        }

        [Fact]
        public void IsActive_RequiresRoomAndLobbyMenuScope()
        {
            Assert.False(ModIntegrityGate.IsActive(
                inRoom: false, inLobbyMenu: true, isMaster: true,
                localModeEnabled: true, hostSignalReceived: true));
            Assert.False(ModIntegrityGate.IsActive(
                inRoom: true, inLobbyMenu: false, isMaster: false,
                localModeEnabled: true, hostSignalReceived: true));
        }

        [Fact]
        public void IsInScope_RequiresBothConditions()
        {
            Assert.True(ModIntegrityGate.IsInScope(true, true));
            Assert.False(ModIntegrityGate.IsInScope(true, false));
            Assert.False(ModIntegrityGate.IsInScope(false, true));
        }

        [Fact]
        public void IsHostSignal_OnlyForGuestReceivingFromMaster()
        {
            Assert.True(ModIntegrityGate.IsHostSignal(isMaster: false, masterActor: 1, senderActor: 1));
            Assert.False(ModIntegrityGate.IsHostSignal(isMaster: true, masterActor: 1, senderActor: 1));
            Assert.False(ModIntegrityGate.IsHostSignal(isMaster: false, masterActor: 1, senderActor: 2));
            Assert.False(ModIntegrityGate.IsHostSignal(isMaster: false, masterActor: 0, senderActor: 0));
        }
    }
}
