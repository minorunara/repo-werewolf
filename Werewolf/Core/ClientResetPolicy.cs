#nullable enable
using System;

namespace Werewolf.Core
{
    public static class ClientResetPolicy
    {
        public static void ApplyRoomLeft(MeetingClientState meetingClient, IdRosterClient idRoster)
        {
            if (meetingClient == null) throw new ArgumentNullException(nameof(meetingClient));
            if (idRoster == null) throw new ArgumentNullException(nameof(idRoster));
            meetingClient.Reset();
            idRoster.Reset();
        }
    }
}
