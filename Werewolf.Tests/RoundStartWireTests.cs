using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RoundStartWireTests
    {
        private static readonly int[] Roster = { 1, 2, -101 };

        private static RoundStartPublicData PublicData()
            => new RoundStartPublicData(600, 1, true, 45, true, Roster);

        [Fact]
        public void PreplayRoundTrip()
        {
            object[] payload = RoundStartWire.ToPreplay(PublicData(), 10_000, 20_000);

            Assert.True(RoundStartWire.TryReadPreplay(
                payload, out long start, out long end, out RoundStartPublicData data));
            Assert.Equal(10_000, start);
            Assert.Equal(20_000, end);
            AssertPublicData(data);
        }

        [Fact]
        public void GameStartRoundTrip()
        {
            object[] payload = RoundStartWire.ToGameStart(PublicData(), 610_000);

            Assert.True(RoundStartWire.TryReadGameStart(
                payload, out long roundEnd, out RoundStartPublicData data));
            Assert.Equal(610_000, roundEnd);
            AssertPublicData(data);
        }

        [Fact]
        public void PreplayRejectsNonCanonicalWindow()
        {
            object[] payload = RoundStartWire.ToPreplay(PublicData(), 10_000, 20_000);
            payload[1] = 19_999L;

            Assert.False(RoundStartWire.TryReadPreplay(payload, out _, out _, out _));
        }

        [Fact]
        public void ReadersRejectInvalidFlagsAndRoster()
        {
            object[] badFlag = RoundStartWire.ToGameStart(PublicData(), 610_000);
            badFlag[3] = (byte)2;
            Assert.False(RoundStartWire.TryReadGameStart(badFlag, out _, out _));

            object[] duplicateRoster = RoundStartWire.ToPreplay(PublicData(), 10_000, 20_000);
            duplicateRoster[7] = new[] { 1, 1, -101 };
            Assert.False(RoundStartWire.TryReadPreplay(duplicateRoster, out _, out _, out _));
        }

        [Fact]
        public void PublicDataCopiesRoster()
        {
            int[] roster = { 1, 2, -101 };
            var data = new RoundStartPublicData(600, 1, false, 30, false, roster);
            roster[0] = 99;

            Assert.Equal(new[] { 1, 2, -101 }, data.ActorsByIdOrder);
        }

        private static void AssertPublicData(RoundStartPublicData data)
        {
            Assert.Equal(600, data.RoundSeconds);
            Assert.Equal(1, data.WerewolfCount);
            Assert.True(data.BlackCatPossible);
            Assert.Equal(45, data.RevealDelaySec);
            Assert.True(data.DebugMode);
            Assert.Equal(Roster, data.ActorsByIdOrder);
        }
    }
}
