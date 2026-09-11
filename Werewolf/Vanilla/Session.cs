namespace Werewolf.Vanilla
{
    internal static class Session
    {
        public static bool IsHost => SemiFunc.IsMasterClientOrSingleplayer();

        public static bool IsMultiplayer => SemiFunc.IsMultiplayer();

        public static string RoomId => Photon.Pun.PhotonNetwork.CurrentRoom?.Name;

        public static bool IsRoleUnsettled
            => Photon.Pun.PhotonNetwork.InRoom && !SemiFunc.IsMultiplayer();

        public static bool HasGameManager => GameManager.instance != null;

        public static int GameModeOrMinusOne
            => GameManager.instance != null ? GameManager.instance.gameMode : -1;

        public static bool IsSoloMode
            => GameManager.instance == null || GameManager.instance.gameMode == 0;

        public static bool InRunLevel => SemiFunc.RunIsLevel();

        public static bool InLobbyMenu => SemiFunc.RunIsLobbyMenu();

        public static bool InLobby => SemiFunc.RunIsLobby();

        public static bool InMainMenu => SemiFunc.IsMainMenu();

        public static bool InMenuLevel => SemiFunc.MenuLevel();

        public static bool IsTitleScreenBuilt => MenuPageMain.instance != null;

        public static bool IsGameStateMain
            => GameDirector.instance != null
               && GameDirector.instance.currentState == GameDirector.gameState.Main;

        public static bool LevelGenerated
            => LevelGenerator.Instance != null && LevelGenerator.Instance.Generated;

        public static void DisableEscMenu(float seconds)
        {
            GameDirector director = GameDirector.instance;
            if (director != null) director.SetDisableEscMenu(seconds);
        }

        public static bool IsEscMenuOpen
            => MenuManager.instance != null && MenuManager.instance.PageCheck(MenuPageIndex.Escape);

        public static bool HasRunManager => RunManager.instance != null;

        public static void ReturnToLobbyMenu()
        {
            RunManager rm = RunManager.instance;
            if (rm == null) return;
            rm.ChangeLevel(true, false, RunManager.ChangeLevelType.LobbyMenu);
        }

        public static void AllPlayersDeadSet(bool dead)
        {
            RunManager rm = RunManager.instance;
            if (rm == null) return;
            rm.AllPlayersDeadSet(dead);
        }

        public static bool TryReadCosmeticWorldObjectCooldown(out float seconds)
        {
            seconds = float.MaxValue;
            RunManager rm = RunManager.instance;
            if (rm == null || GameRefs.RunManager_cosmeticWorldObjectCooldown == null) return false;
            seconds = GameRefs.RunManager_cosmeticWorldObjectCooldown(rm);
            return true;
        }
    }
}
