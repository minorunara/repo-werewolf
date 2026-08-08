using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ClientResetPolicyTests
    {
        [Fact]
        public void ApplyRoomLeft_ClearsMeetingRows_SoNextSessionDoesNotShowStaleDisconnected()
        {
            var mc = new MeetingClientState();

            mc.ApplyPlayerLeft(1);
            Assert.Equal(RowStatus.Disconnected, mc.GetRowStatus(1));

            ClientResetPolicy.ApplyRoomLeft(mc, new IdRosterClient());

            mc.ApplyStartMeeting(caller: 1, warpUnixMs: 100, endUnixMs: 200);

            Assert.Equal(RowStatus.Alive, mc.GetRowStatus(1));
        }

        [Fact]
        public void ApplyRoomLeft_DoesNotThrow_WhenMeetingRowsAreEmpty()
        {
            var mc = new MeetingClientState();

            ClientResetPolicy.ApplyRoomLeft(mc, null);

            Assert.False(mc.MeetingActive);
            Assert.Equal(RowStatus.Alive, mc.GetRowStatus(1));
            Assert.Equal(RowStatus.Alive, mc.GetRowStatus(2));
        }

        [Fact]
        public void ApplyRoomLeft_ClearsAnnouncedDead_SoNextSessionAnnouncesFreshly()
        {
            var mc = new MeetingClientState();
            mc.ApplyPlayerDied(3, DeathCause.Vote);
            mc.MarkAllDeadAnnounced();
            Assert.False(mc.IsDeadUnannounced(3));

            ClientResetPolicy.ApplyRoomLeft(mc, new IdRosterClient());

            mc.ApplyPlayerDied(3, DeathCause.Vote);
            Assert.True(mc.IsDeadUnannounced(3));
        }

        [Fact]
        public void ApplyRoomLeft_ClearsIdRoster_SoNextRoomStartsUnnumbered()
        {
            var roster = new IdRosterClient();
            roster.Apply(new[] { 1, 2, 3 });

            ClientResetPolicy.ApplyRoomLeft(new MeetingClientState(), roster);

            Assert.False(roster.HasRoster);
            Assert.Equal(0, roster.IdOf(1));
        }
    }
}
