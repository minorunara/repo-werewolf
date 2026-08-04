using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ValuableMapGateTests
    {

        [Theory]
        [InlineData(ValuableMapMode.Realtime, true, false)]
        [InlineData(ValuableMapMode.Realtime, false, false)]
        [InlineData(ValuableMapMode.MeetingSync, true, true)]
        [InlineData(ValuableMapMode.MeetingSync, false, false)]
        [InlineData(ValuableMapMode.Hidden, true, true)]
        [InlineData(ValuableMapMode.Hidden, false, false)]
        public void ShouldSuppressAdd_HiddenAndMeetingSyncWhileActive(ValuableMapMode mode, bool roundActive, bool expected)
        {
            Assert.Equal(expected, ValuableMapGate.ShouldSuppressAdd(mode, roundActive));
        }

        [Theory]
        [InlineData(ValuableMapMode.Realtime, true, false)]
        [InlineData(ValuableMapMode.MeetingSync, true, true)]
        [InlineData(ValuableMapMode.MeetingSync, false, false)]
        [InlineData(ValuableMapMode.Hidden, true, false)]
        public void ShouldSnapshotOnDiscover_OnlyMeetingSyncAndActive(ValuableMapMode mode, bool roundActive, bool expected)
        {
            Assert.Equal(expected, ValuableMapGate.ShouldSnapshotOnDiscover(mode, roundActive));
        }

        [Theory]
        [InlineData(ValuableMapMode.Realtime, true, false)]
        [InlineData(ValuableMapMode.MeetingSync, true, true)]
        [InlineData(ValuableMapMode.MeetingSync, false, false)]
        [InlineData(ValuableMapMode.Hidden, true, false)]
        public void ShouldRefreshSnapshotAtInventoryPoint_OnlyMeetingSyncAndActive(ValuableMapMode mode, bool roundActive, bool expected)
        {
            Assert.Equal(expected, ValuableMapGate.ShouldRefreshSnapshotAtInventoryPoint(mode, roundActive));
        }

        [Theory]
        [InlineData(ValuableMapMode.Realtime, false)]
        [InlineData(ValuableMapMode.MeetingSync, true)]
        [InlineData(ValuableMapMode.Hidden, true)]
        public void ShouldRestoreValuablesOnEnd_HiddenAndMeetingSync(ValuableMapMode mode, bool expected)
        {
            Assert.Equal(expected, ValuableMapGate.ShouldRestoreValuablesOnEnd(mode));
        }

        [Theory]
        [InlineData(ValuableMapMode.Realtime)]
        [InlineData(ValuableMapMode.MeetingSync)]
        [InlineData(ValuableMapMode.Hidden)]
        public void EncodeDecodeValuableMapMode_RoundTrips(ValuableMapMode mode)
        {
            byte encoded = RoomStateKeys.EncodeValuableMapMode(mode);
            ValuableMapMode decoded = RoomStateKeys.DecodeValuableMapMode(encoded);
            Assert.Equal(mode, decoded);
        }

        [Fact]
        public void DecodeValuableMapMode_FallsBackToMeetingSyncForUnknown()
        {
            Assert.Equal(ValuableMapMode.MeetingSync, RoomStateKeys.DecodeValuableMapMode(255));
        }

        [Fact]
        public void GameConfig_DefaultValuableMapMode_IsMeetingSync()
        {
            var cfg = new GameConfig();
            Assert.Equal(ValuableMapMode.MeetingSync, cfg.ValuableMapMode);
        }

        [Fact]
        public void CfgValuableMapMode_IsPresentInAllKeys()
        {
            Assert.Contains(RoomStateKeys.CfgValuableMapMode, RoomStateKeys.AllKeys);
        }
    }
}
