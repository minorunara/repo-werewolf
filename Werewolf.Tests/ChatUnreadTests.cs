using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ChatUnreadTests
    {
        private const int Local = 1;
        private const int Other = 3;

        [Fact]
        public void OtherActorWhileClosed_SetsUnread()
        {
            var unread = new ChatUnread();

            unread.OnMessageAppended(Other, Local, panelOpen: false);

            Assert.True(unread.HasUnread);
        }

        [Fact]
        public void WhilePanelOpen_DoesNotSet()
        {
            var unread = new ChatUnread();

            unread.OnMessageAppended(Other, Local, panelOpen: true);

            Assert.False(unread.HasUnread);
        }

        [Fact]
        public void OwnMessageWhileClosed_DoesNotSet()
        {
            var unread = new ChatUnread();

            unread.OnMessageAppended(Local, Local, panelOpen: false);

            Assert.False(unread.HasUnread);
        }

        [Fact]
        public void OpenFrame_DoesNotClearExistingUnread()
        {
            var unread = new ChatUnread();
            unread.OnMessageAppended(Other, Local, panelOpen: false);

            unread.OnMessageAppended(Other, Local, panelOpen: true);

            Assert.True(unread.HasUnread);
        }

        [Fact]
        public void Clear_ResetsAndRearms()
        {
            var unread = new ChatUnread();
            unread.OnMessageAppended(Other, Local, panelOpen: false);

            unread.Clear();
            Assert.False(unread.HasUnread);

            unread.Clear();
            Assert.False(unread.HasUnread);

            unread.OnMessageAppended(Other, Local, panelOpen: false);
            Assert.True(unread.HasUnread);
        }
    }
}
