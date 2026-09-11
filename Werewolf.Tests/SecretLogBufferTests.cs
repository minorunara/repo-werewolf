using System;
using Werewolf.Core;
using Werewolf.Debugging;
using Xunit;

namespace Werewolf.Tests
{
    public class SecretLogBufferTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 7, 17, 21, 3, 45, 123);

        [Fact]
        public void Add_PreservesLevelAndAppendsCaptureTimestamp()
        {
            var buffer = new SecretLogBuffer();
            buffer.Add("[WW] assign actor=3 role=BlackCat secret=1", WLogLevel.Debug, T0);

            var entry = Assert.Single(buffer.Flush(out _));

            Assert.Equal(WLogLevel.Debug, entry.Level);
            Assert.Equal("[WW] assign actor=3 role=BlackCat secret=1 t=21:03:45.123", entry.Line);
        }

        [Fact]
        public void Flush_ReturnsEntriesInCaptureOrderWithOriginalLevels()
        {
            var buffer = new SecretLogBuffer();
            buffer.Add("first", WLogLevel.Debug, T0);
            buffer.Add("second", WLogLevel.Warning, T0.AddSeconds(1));
            buffer.Add("third", WLogLevel.Error, T0.AddSeconds(2));

            var entries = buffer.Flush(out int dropped);

            Assert.Equal(3, entries.Count);
            Assert.StartsWith("first", entries[0].Line);
            Assert.Equal(WLogLevel.Debug, entries[0].Level);
            Assert.StartsWith("second", entries[1].Line);
            Assert.Equal(WLogLevel.Warning, entries[1].Level);
            Assert.StartsWith("third", entries[2].Line);
            Assert.Equal(WLogLevel.Error, entries[2].Level);
            Assert.Equal(0, dropped);
        }

        [Fact]
        public void Flush_EmptiesBufferAndResetsDroppedCounter()
        {
            var buffer = new SecretLogBuffer(capacity: 1);
            buffer.Add("a", WLogLevel.Debug, T0);
            buffer.Add("b", WLogLevel.Debug, T0);

            var first = buffer.Flush(out int firstDropped);
            var second = buffer.Flush(out int secondDropped);

            Assert.Single(first);
            Assert.StartsWith("b", first[0].Line);
            Assert.Equal(1, firstDropped);
            Assert.Empty(second);
            Assert.Equal(0, secondDropped);
            Assert.Equal(0, buffer.Count);
        }

        [Fact]
        public void Add_OverCapacity_DropsOldest()
        {
            var buffer = new SecretLogBuffer(capacity: 2);
            buffer.Add("oldest", WLogLevel.Debug, T0);
            buffer.Add("middle", WLogLevel.Warning, T0);
            buffer.Add("newest", WLogLevel.Error, T0);

            var entries = buffer.Flush(out int dropped);

            Assert.Equal(2, entries.Count);
            Assert.StartsWith("middle", entries[0].Line);
            Assert.StartsWith("newest", entries[1].Line);
            Assert.Equal(1, dropped);
        }

        [Fact]
        public void CapacityBelowOne_IsClampedToOne()
        {
            var buffer = new SecretLogBuffer(capacity: 0);
            buffer.Add("first", WLogLevel.Debug, T0);
            buffer.Add("second", WLogLevel.Debug, T0);

            var entries = buffer.Flush(out int dropped);

            Assert.Single(entries);
            Assert.StartsWith("second", entries[0].Line);
            Assert.Equal(1, dropped);
        }

        [Fact]
        public void Count_TracksHeldEntries()
        {
            var buffer = new SecretLogBuffer();

            buffer.Add("first", WLogLevel.Debug, T0);
            buffer.Add("second", WLogLevel.Error, T0);

            Assert.Equal(2, buffer.Count);
        }
    }
}
