using System.Collections.Generic;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class InboundAuthorityTests
    {
        private const int Master = 1;
        private const int Cheater = 7;

        [Theory]
        [InlineData(MessageCodes.CastVote)]
        [InlineData(MessageCodes.RequestMeeting)]
        [InlineData(MessageCodes.RoleAction)]
        [InlineData(MessageCodes.ModManifestReport)]
        [InlineData(MessageCodes.ModIntegrityDetailRequest)]
        public void MasterInboundCodes_PassFromAnySender(byte code)
        {
            Assert.True(InboundAuthority.IsAcceptable(code, Cheater, Master));
            Assert.True(InboundAuthority.IsAcceptable(code, Master, Master));
        }

        [Theory]
        [InlineData(MessageCodes.GameOver)]
        [InlineData(MessageCodes.GameStart)]
        [InlineData(MessageCodes.PlayerDied)]
        [InlineData(MessageCodes.PhaseChanged)]
        [InlineData(MessageCodes.StartMeeting)]
        [InlineData(MessageCodes.MeetingResult)]
        [InlineData(MessageCodes.AssignRole)]
        [InlineData(MessageCodes.RevealTeammates)]
        [InlineData(MessageCodes.CosmeticGrant)]
        [InlineData(MessageCodes.BombDetonation)]
        [InlineData(MessageCodes.ScatterGuardWindow)]
        [InlineData(MessageCodes.ModIntegritySnapshot)]
        public void HostBroadcastCodes_DropFromNonMaster(byte code)
        {
            Assert.False(InboundAuthority.IsAcceptable(code, Cheater, Master));
        }

        [Theory]
        [InlineData(MessageCodes.GameOver)]
        [InlineData(MessageCodes.GameStart)]
        [InlineData(MessageCodes.PhaseChanged)]
        public void HostBroadcastCodes_PassFromMaster(byte code)
        {
            Assert.True(InboundAuthority.IsAcceptable(code, Master, Master));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void UnknownMaster_FailsOpen(int masterActor)
        {
            Assert.True(InboundAuthority.IsAcceptable(MessageCodes.GameOver, Cheater, masterActor));
        }

        [Fact]
        public void EveryCode_OutsideMasterInbound_IsRejectedFromNonMaster()
        {
            var leaked = new List<int>();
            for (int code = 0; code <= byte.MaxValue; code++)
            {
                bool accepted = InboundAuthority.IsAcceptable((byte)code, Cheater, Master);
                if (accepted != MessageCodes.IsMasterInbound((byte)code)) leaked.Add(code);
            }

            Assert.True(leaked.Count == 0,
                "非マスターからの受信が通るコードが IsMasterInbound 以外に存在する: " +
                string.Join(", ", leaked));
        }

        [Fact]
        public void DropThrottle_AllowsFirstThenSuppressesWithinWindow()
        {
            var throttle = new InboundDropThrottle();

            Assert.True(throttle.TryTake(1_000L, out int firstSuppressed));
            Assert.Equal(0, firstSuppressed);

            Assert.False(throttle.TryTake(2_000L, out int s1));
            Assert.Equal(0, s1);
            Assert.False(throttle.TryTake(1_000L + InboundDropThrottle.WindowMs - 1L, out int s2));
            Assert.Equal(0, s2);

            Assert.True(throttle.TryTake(1_000L + InboundDropThrottle.WindowMs, out int reported));
            Assert.Equal(2, reported);

            Assert.False(throttle.TryTake(1_000L + InboundDropThrottle.WindowMs + 1L, out _));
            Assert.True(throttle.TryTake(1_000L + InboundDropThrottle.WindowMs * 2L, out int next));
            Assert.Equal(1, next);
        }

        [Fact]
        public void DropThrottle_ResetClearsWindowAndCount()
        {
            var throttle = new InboundDropThrottle();
            Assert.True(throttle.TryTake(1_000L, out _));
            Assert.False(throttle.TryTake(1_500L, out _));

            throttle.Reset();

            Assert.True(throttle.TryTake(1_600L, out int suppressed));
            Assert.Equal(0, suppressed);
        }
    }
}
