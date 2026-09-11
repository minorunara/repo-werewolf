using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ChatStampCooldownTests
    {
        [Fact]
        public void FirstTake_Succeeds()
        {
            var cd = new ChatStampCooldown();
            Assert.True(cd.TryTake(1, 1000L));
        }

        [Fact]
        public void TakeWithinCooldown_Fails()
        {
            var cd = new ChatStampCooldown();
            Assert.True(cd.TryTake(1, 1000L));
            Assert.False(cd.TryTake(1, 1000L + ChatStampCooldown.CooldownMs - 1L));
        }

        [Fact]
        public void TakeAfterCooldown_Succeeds()
        {
            var cd = new ChatStampCooldown();
            Assert.True(cd.TryTake(1, 1000L));
            Assert.True(cd.TryTake(1, 1000L + ChatStampCooldown.CooldownMs));
        }

        [Fact]
        public void RejectedTake_DoesNotExtendCooldown()
        {
            var cd = new ChatStampCooldown();
            Assert.True(cd.TryTake(1, 1000L));
            Assert.False(cd.TryTake(1, 2999L));
            Assert.True(cd.TryTake(1, 1000L + ChatStampCooldown.CooldownMs));
        }

        [Fact]
        public void Actors_AreIndependent()
        {
            var cd = new ChatStampCooldown();
            Assert.True(cd.TryTake(1, 1000L));
            Assert.True(cd.TryTake(2, 1000L));
            Assert.True(cd.TryTake(-101, 1000L));
        }

        [Fact]
        public void IsReady_DoesNotConsume()
        {
            var cd = new ChatStampCooldown();
            Assert.True(cd.IsReady(1, 1000L));
            Assert.True(cd.IsReady(1, 1000L));
            Assert.True(cd.TryTake(1, 1000L));
            Assert.False(cd.IsReady(1, 1500L));
            Assert.True(cd.IsReady(1, 1000L + ChatStampCooldown.CooldownMs));
        }
    }
}
