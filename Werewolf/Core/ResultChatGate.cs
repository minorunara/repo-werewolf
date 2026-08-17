namespace Werewolf.Core
{
    public static class ResultChatGate
    {
        public static bool IsOpen(GamePhase phase, bool resultScreenVisible, bool chatLogEnabled)
            => phase == GamePhase.GameOver && resultScreenVisible && chatLogEnabled;
    }
}
