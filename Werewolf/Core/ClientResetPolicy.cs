namespace Werewolf.Core
{
    public static class ClientResetPolicy
    {
        public static void ApplyRoomLeft(MeetingClientState meetingClient, IdRosterClient idRoster)
        {
            meetingClient?.Reset();
            idRoster?.Reset();
        }
    }
}
