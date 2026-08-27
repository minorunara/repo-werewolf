using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ServerClockTests
    {
        [Fact]
        public void NeverSampled_ReturnsLocalMonotonic()
        {
            var clock = new ServerClock();
            Assert.Equal(0L, clock.Read(0, 0));
            Assert.Equal(1500L, clock.Read(0, 1500));
            Assert.False(clock.ServerBacked);
        }

        [Fact]
        public void ServerValue_IsUsedAsIs_RegardlessOfLocal()
        {
            var clock = new ServerClock();
            Assert.Equal(1_000_000L, clock.Read(1_000_000, 42));
            Assert.True(clock.ServerBacked);
            Assert.Equal(1_000_250L, clock.Read(1_000_250, 99_999));
        }

        [Fact]
        public void NegativeInt_ReadsAsUnsigned_ContinuousAcrossIntWrap()
        {
            var clock = new ServerClock();
            long beforeWrap = clock.Read(int.MaxValue, 0);
            long afterWrap = clock.Read(int.MinValue, 1);
            Assert.Equal(2147483647L, beforeWrap);
            Assert.Equal(2147483648L, afterWrap);
            Assert.Equal(1L, afterWrap - beforeWrap);
            Assert.Equal(4294967295L, clock.Read(-1, 2));
        }

        [Fact]
        public void Unavailable_ExtendsLastSampleByLocalElapsed()
        {
            var clock = new ServerClock();
            clock.Read(500_000, 10_000);
            Assert.Equal(503_000L, clock.Read(0, 13_000));
            Assert.False(clock.ServerBacked);
            Assert.Equal(510_000L, clock.Read(0, 20_000));
            Assert.Equal(999_000L, clock.Read(999_000, 21_000));
            Assert.True(clock.ServerBacked);
            Assert.Equal(1_000_000L, clock.Read(0, 22_000));
        }
    }
}
