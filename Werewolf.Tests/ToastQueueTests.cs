using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ToastQueueTests
    {
        [Fact]
        public void Push_ThenVisible_ImmediatelyShowsEntry()
        {
            var queue = new ToastQueue(durationSec: 3);
            queue.Push("A", nowUnixMs: 1000);

            var visible = queue.Visible(nowUnixMs: 1000);

            Assert.Single(visible);
            Assert.Equal("A", visible[0].Message);
            Assert.Equal(3000, visible[0].RemainingMs);
        }

        [Fact]
        public void ConsecutivePushes_DoNotDismissEarlierEntry()
        {
            var queue = new ToastQueue(durationSec: 3);
            queue.Push("first", nowUnixMs: 1000);
            queue.Push("second", nowUnixMs: 1100);
            queue.Push("third", nowUnixMs: 1200);

            var visible = queue.Visible(nowUnixMs: 1200);

            Assert.Equal(3, visible.Count);
            Assert.Contains(visible, e => e.Message == "first");
            Assert.Contains(visible, e => e.Message == "second");
            Assert.Contains(visible, e => e.Message == "third");
        }

        [Fact]
        public void ConsecutivePushes_DoNotShortenEarlierEntryRemainingTime()
        {
            var queue = new ToastQueue(durationSec: 3);
            queue.Push("first", nowUnixMs: 1000);

            queue.Push("second", nowUnixMs: 1100);

            var visible = queue.Visible(nowUnixMs: 1100);
            var first = visible.Single(e => e.Message == "first");

            Assert.Equal(2900, first.RemainingMs);
        }

        [Fact]
        public void Visible_OrdersNewestFirst()
        {
            var queue = new ToastQueue(durationSec: 10);
            queue.Push("first", nowUnixMs: 0);
            queue.Push("second", nowUnixMs: 100);
            queue.Push("third", nowUnixMs: 200);

            var visible = queue.Visible(nowUnixMs: 200);

            Assert.Equal(new[] { "third", "second", "first" }, visible.Select(e => e.Message));
        }

        [Fact]
        public void Visible_ExpiredEntriesAreRemoved()
        {
            var queue = new ToastQueue(durationSec: 3);
            queue.Push("expiring", nowUnixMs: 0);
            queue.Push("fresh", nowUnixMs: 2000);

            var visible = queue.Visible(nowUnixMs: 4000);

            Assert.Single(visible);
            Assert.Equal("fresh", visible[0].Message);
        }

        [Fact]
        public void Visible_AtExactExpiryTime_EntryIsGone()
        {
            var queue = new ToastQueue(durationSec: 3);
            queue.Push("A", nowUnixMs: 1000);

            var visible = queue.Visible(nowUnixMs: 4000);

            Assert.Empty(visible);
        }

        [Fact]
        public void Push_EmptyOrNullMessage_IsIgnored()
        {
            var queue = new ToastQueue(durationSec: 3);
            queue.Push(null, nowUnixMs: 0);
            queue.Push("", nowUnixMs: 0);

            Assert.Empty(queue.Visible(nowUnixMs: 0));
        }
    }
}
