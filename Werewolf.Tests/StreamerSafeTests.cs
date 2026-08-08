using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class StreamerSafeTests
    {
        [Theory]
        [InlineData("perk_bomb_plant")]
        [InlineData("perk_bomb_detonate")]
        [InlineData("sfx_execution")]
        [InlineData("sfx_execution_curse")]
        public void Enabled_SuppressedKeys_ResolveToNoAsset(string key)
        {
            Assert.True(StreamerSafe.TryResolve(true, key, out string replacement));
            Assert.Null(replacement);
        }

        [Fact]
        public void Enabled_ConveneChime_ReplacedByDefaultToastSound()
        {
            Assert.True(StreamerSafe.TryResolve(true, "sfx_notice_convene", out string replacement));
            Assert.Equal(NoticeSfx.DefaultClipKey, replacement);
        }

        [Theory]
        [InlineData("perk_bomb_plant")]
        [InlineData("sfx_notice_convene")]
        [InlineData("sfx_execution")]
        public void Disabled_NeverOverrides(string key)
        {
            Assert.False(StreamerSafe.TryResolve(false, key, out string replacement));
            Assert.Null(replacement);
        }

        [Theory]
        [InlineData("role_werewolf")]
        [InlineData("img_bomb")]
        [InlineData("sfx_toast")]
        [InlineData("sfx_meeting_end")]
        [InlineData("")]
        [InlineData(null)]
        public void UntargetedKeys_PassThrough(string key)
        {
            Assert.False(StreamerSafe.TryResolve(true, key, out string replacement));
            Assert.Null(replacement);
        }

        [Fact]
        public void Replacements_AreNotThemselvesOverridden()
        {
            Assert.True(StreamerSafe.TryResolve(true, "sfx_notice_convene", out string replacement));
            Assert.False(StreamerSafe.TryResolve(true, replacement, out _));
        }
    }
}
