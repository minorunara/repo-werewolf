#nullable enable
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class AlternativeExecutionIconTests
    {
        [Fact]
        public void Enabled_ExecutedStatus_UsesAlternativeAsset()
        {
            Assert.Equal(
                AlternativeExecutionIcon.AlternativeAssetKey,
                AlternativeExecutionIcon.Resolve(true, AlternativeExecutionIcon.DefaultAssetKey));
        }

        [Theory]
        [InlineData(false, "icon_status_executed")]
        [InlineData(true, "icon_status_dead")]
        [InlineData(true, "icon_status_executed_alternative")]
        [InlineData(true, "")]
        [InlineData(true, null)]
        public void DisabledOrUntargeted_PassesThrough(bool enabled, string? key)
        {
            Assert.Equal(key, AlternativeExecutionIcon.Resolve(enabled, key));
        }
    }
}
