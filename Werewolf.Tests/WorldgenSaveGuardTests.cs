using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WorldgenSaveGuardTests
    {
        [Fact]
        public void ShouldDelete_AllConditionsMet_ReturnsTrue()
        {
            Assert.True(WorldgenSaveGuard.ShouldDelete(
                leaveGame: true, active: true,
                markedSaveFileName: "REPO_SAVE_2026", currentSaveFileName: "REPO_SAVE_2026"));
        }

        [Fact]
        public void ShouldDelete_LeaveGameFalse_ReturnsFalse()
        {
            Assert.False(WorldgenSaveGuard.ShouldDelete(
                leaveGame: false, active: true,
                markedSaveFileName: "REPO_SAVE_2026", currentSaveFileName: "REPO_SAVE_2026"));
        }

        [Fact]
        public void ShouldDelete_NotActive_ReturnsFalse()
        {
            Assert.False(WorldgenSaveGuard.ShouldDelete(
                leaveGame: true, active: false,
                markedSaveFileName: "REPO_SAVE_2026", currentSaveFileName: "REPO_SAVE_2026"));
        }

        [Fact]
        public void ShouldDelete_MarkedEmpty_ReturnsFalse()
        {
            Assert.False(WorldgenSaveGuard.ShouldDelete(
                leaveGame: true, active: true,
                markedSaveFileName: "", currentSaveFileName: "REPO_SAVE_2026"));
        }

        [Fact]
        public void ShouldDelete_CurrentEmpty_ReturnsFalse()
        {
            Assert.False(WorldgenSaveGuard.ShouldDelete(
                leaveGame: true, active: true,
                markedSaveFileName: "REPO_SAVE_2026", currentSaveFileName: ""));
        }

        [Fact]
        public void ShouldDelete_NullNames_ReturnsFalse()
        {
            Assert.False(WorldgenSaveGuard.ShouldDelete(
                leaveGame: true, active: true,
                markedSaveFileName: null, currentSaveFileName: "REPO_SAVE_2026"));
            Assert.False(WorldgenSaveGuard.ShouldDelete(
                leaveGame: true, active: true,
                markedSaveFileName: "REPO_SAVE_2026", currentSaveFileName: null));
        }

        [Fact]
        public void ShouldDelete_NamesMismatch_ReturnsFalse()
        {
            Assert.False(WorldgenSaveGuard.ShouldDelete(
                leaveGame: true, active: true,
                markedSaveFileName: "REPO_SAVE_2026_WOLF", currentSaveFileName: "REPO_SAVE_2025_NORMAL"));
        }

        [Fact]
        public void ShouldDelete_NamesOrdinalCaseSensitive()
        {
            Assert.False(WorldgenSaveGuard.ShouldDelete(
                leaveGame: true, active: true,
                markedSaveFileName: "REPO_SAVE", currentSaveFileName: "repo_save"));
        }
    }
}
