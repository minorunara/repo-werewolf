using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class SharedSettingsPublishPolicyTests
    {
        [Fact]
        public void Claim_FirstPublishInRoom_IsAllowed()
        {
            var policy = new SharedSettingsPublishPolicy();

            Assert.True(policy.Claim("room-a", "blob-1"));
        }

        [Fact]
        public void Claim_SameRoomSameBlob_IsSuppressed()
        {
            var policy = new SharedSettingsPublishPolicy();
            policy.Claim("room-a", "blob-1");

            Assert.False(policy.Claim("room-a", "blob-1"));
            Assert.False(policy.Claim("room-a", "blob-1"));
        }

        [Fact]
        public void Claim_SameRoomChangedBlob_IsAllowedOnce()
        {
            var policy = new SharedSettingsPublishPolicy();
            policy.Claim("room-a", "blob-1");

            Assert.True(policy.Claim("room-a", "blob-2"));
            Assert.False(policy.Claim("room-a", "blob-2"));
        }

        [Fact]
        public void Claim_RecreatedRoomSameBlob_IsAllowedAgain()
        {
            var policy = new SharedSettingsPublishPolicy();
            policy.Claim("room-a", "blob-1");

            Assert.True(policy.Claim("room-b", "blob-1"));
            Assert.False(policy.Claim("room-b", "blob-1"));
        }

        [Fact]
        public void Claim_MatchStartPublish_SuppressesLobbyRepublishOfSameBlob()
        {
            var policy = new SharedSettingsPublishPolicy();
            policy.Claim("room-a", "lobby-blob");

            Assert.True(policy.Claim("room-a", "match-blob"));
            Assert.False(policy.Claim("room-a", "match-blob"));
        }

        [Fact]
        public void Claim_OutsideRoomOrEmptyBlob_IsRefusedWithoutRecording()
        {
            var policy = new SharedSettingsPublishPolicy();

            Assert.False(policy.Claim(null, "blob-1"));
            Assert.False(policy.Claim("", "blob-1"));
            Assert.False(policy.Claim("room-a", null));
            Assert.False(policy.Claim("room-a", ""));
            Assert.True(policy.Claim("room-a", "blob-1"));
        }

        [Fact]
        public void Reset_ForgetsRecord_SoNextClaimIsAllowed()
        {
            var policy = new SharedSettingsPublishPolicy();
            policy.Claim("room-a", "blob-1");

            policy.Reset();

            Assert.True(policy.Claim("room-a", "blob-1"));
        }
    }
}
