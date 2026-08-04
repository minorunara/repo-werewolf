using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class NoticeSfxTests
    {
        [Fact]
        public void ConveneStarted_UsesDedicatedClipKey()
        {
            var notice = SessionNotice.ForConveneStarted("Alice");
            Assert.Equal(NoticeSfx.ConveneStartedClipKey, NoticeSfx.Resolve(notice));
            Assert.Equal("sfx_notice_convene", NoticeSfx.Resolve(notice));
        }

        [Fact]
        public void Null_FallsBackToDefault()
        {
            Assert.Equal(NoticeSfx.DefaultClipKey, NoticeSfx.Resolve(null));
            Assert.Equal("sfx_toast", NoticeSfx.Resolve(null));
        }

        [Theory]
        [InlineData(NoticeKind.BeaconAudit)]
        [InlineData(NoticeKind.NoExecution)]
        [InlineData(NoticeKind.Executed)]
        [InlineData(NoticeKind.BlackCatRevealed)]
        [InlineData(NoticeKind.CurseVictim)]
        [InlineData(NoticeKind.ConveneDenied)]
        [InlineData(NoticeKind.CatAwakened)]
        [InlineData(NoticeKind.ConveneHoldHint)]
        public void UnmappedKinds_FallBackToDefault(NoticeKind kind)
        {
            SessionNotice notice = BuildFor(kind);
            Assert.Equal(NoticeSfx.DefaultClipKey, NoticeSfx.Resolve(notice));
        }

        [Fact]
        public void ConveneStarted_KeyDiffersFromDefault()
        {
            Assert.NotEqual(NoticeSfx.DefaultClipKey, NoticeSfx.ConveneStartedClipKey);
        }

        [Fact]
        public void BomberProximityWarning_UsesDedicatedClipKey()
        {
            Assert.Equal("sfx_bomb_proximity_warning",
                NoticeSfx.BomberProximityWarningClipKey);
            Assert.NotEqual(NoticeSfx.DefaultClipKey,
                NoticeSfx.BomberProximityWarningClipKey);
        }

        [Fact]
        public void PlayerDisconnected_UsesDedicatedClipKey()
        {
            var notice = SessionNotice.ForPlayerDisconnected("Eve");
            Assert.Equal(NoticeSfx.PlayerDisconnectedClipKey, NoticeSfx.Resolve(notice));
            Assert.Equal("sfx_player_disconnected", NoticeSfx.Resolve(notice));
            Assert.NotEqual(NoticeSfx.DefaultClipKey, NoticeSfx.PlayerDisconnectedClipKey);
        }

        [Fact]
        public void AllKinds_ReturnNonEmptyString()
        {
            foreach (NoticeKind kind in System.Enum.GetValues(typeof(NoticeKind)))
            {
                SessionNotice notice = BuildFor(kind);
                string key = NoticeSfx.Resolve(notice);
                Assert.False(string.IsNullOrEmpty(key));
            }
        }

        private static SessionNotice BuildFor(NoticeKind kind)
        {
            switch (kind)
            {
                case NoticeKind.ConveneStarted:   return SessionNotice.ForConveneStarted("A");
                case NoticeKind.BeaconAudit:      return SessionNotice.ForBeaconAudit(1);
                case NoticeKind.NoExecution:      return SessionNotice.ForNoExecution();
                case NoticeKind.Executed:         return SessionNotice.ForExecuted("A");
                case NoticeKind.BlackCatRevealed: return SessionNotice.ForBlackCatRevealed("A");
                case NoticeKind.CurseVictim:      return SessionNotice.ForCurseVictim("A");
                case NoticeKind.ConveneDenied:    return SessionNotice.ForConveneDenied(ConveneRejectReason.NoRight);
                case NoticeKind.CatAwakened:      return SessionNotice.ForCatAwakened();
                case NoticeKind.PlayerDisconnected: return SessionNotice.ForPlayerDisconnected("A");
                case NoticeKind.ConveneHoldHint:  return SessionNotice.ForConveneHoldHint();
                default: return SessionNotice.ForBeaconAudit(1);
            }
        }
    }
}
