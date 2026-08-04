namespace Werewolf.Core
{
    public static class ModIntegrityGate
    {
        public static bool IsActive(
            bool inRoom,
            bool inLobbyMenu,
            bool isMaster,
            bool localModeEnabled,
            bool hostSignalReceived)
        {
            if (!IsInScope(inRoom, inLobbyMenu)) return false;
            return isMaster ? localModeEnabled : hostSignalReceived;
        }

        public static bool IsInScope(bool inRoom, bool inLobbyMenu)
        {
            return inRoom && inLobbyMenu;
        }

        public static bool IsHostSignal(bool isMaster, int masterActor, int senderActor)
        {
            return !isMaster && masterActor > 0 && senderActor == masterActor;
        }
    }
}
