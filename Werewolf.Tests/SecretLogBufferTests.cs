using System;
using Werewolf.Debugging;
using Xunit;

namespace Werewolf.Tests
{
    public class SecretLogBufferTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 7, 17, 21, 3, 45, 123);

        [Fact]
        public void Add_AppendsCaptureTimestampSuffix()
        {
            var buffer = new SecretLogBuffer();
            buffer.Add("[WW] assign actor=3 role=BlackCat secret=1", T0);

            var lines = buffer.Flush(out _);

            Assert.Equal("[WW] assign actor=3 role=BlackCat secret=1 t=21:03:45.123", Assert.Single(lines));
        }

        [Fact]
        public void Flush_ReturnsLinesInCaptureOrder()
        {
            var buffer = new SecretLogBuffer();
            buffer.Add("first", T0);
            buffer.Add("second", T0.AddSeconds(1));
            buffer.Add("third", T0.AddSeconds(2));

            var lines = buffer.Flush(out int dropped);

            Assert.Equal(3, lines.Count);
            Assert.StartsWith("first", lines[0]);
            Assert.StartsWith("second", lines[1]);
            Assert.StartsWith("third", lines[2]);
            Assert.Equal(0, dropped);
        }

        [Fact]
        public void Flush_EmptiesBuffer_SecondFlushReturnsNothing()
        {
            var buffer = new SecretLogBuffer();
            buffer.Add("line", T0);
            buffer.Flush(out _);

            var second = buffer.Flush(out int dropped);

            Assert.Empty(second);
            Assert.Equal(0, dropped);
            Assert.Equal(0, buffer.Count);
        }

        [Fact]
        public void Add_OverCapacity_DropsOldestAndCountsDropped()
        {
            var buffer = new SecretLogBuffer(capacity: 2);
            buffer.Add("oldest", T0);
            buffer.Add("middle", T0);
            buffer.Add("newest", T0);

            var lines = buffer.Flush(out int dropped);

            Assert.Equal(2, lines.Count);
            Assert.StartsWith("middle", lines[0]);
            Assert.StartsWith("newest", lines[1]);
            Assert.Equal(1, dropped);
        }

        [Fact]
        public void Flush_ResetsDroppedCounter()
        {
            var buffer = new SecretLogBuffer(capacity: 1);
            buffer.Add("a", T0);
            buffer.Add("b", T0);

            buffer.Flush(out int firstDropped);
            Assert.Equal(1, firstDropped);

            buffer.Add("c", T0);
            buffer.Flush(out int secondDropped);
            Assert.Equal(0, secondDropped);
        }

        [Fact]
        public void Count_TracksHeldLines()
        {
            var buffer = new SecretLogBuffer();
            Assert.Equal(0, buffer.Count);

            buffer.Add("a", T0);
            buffer.Add("b", T0);

            Assert.Equal(2, buffer.Count);
        }
    }
}
