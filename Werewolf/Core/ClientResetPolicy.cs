namespace Werewolf.Core
{
    public static class ClientResetPolicy
    {
        public static void ApplyRoomLeft(MeetingClientState meetingClient)
        {
            if (meetingClient == null) return;
            meetingClient.Reset();
        }
    }
}
