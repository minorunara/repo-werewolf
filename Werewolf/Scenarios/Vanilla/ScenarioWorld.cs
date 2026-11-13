#nullable enable
using System.Diagnostics.CodeAnalysis;
using Werewolf.Scenarios.Core;

namespace Werewolf.Scenarios.Vanilla
{
    internal static class ScenarioWorld
    {
        public static bool CheckStart(ScenarioEnvironmentSpec environment,
            [NotNullWhen(false)] out string? rejectReason)
        {
            RunManager run = RunManager.instance;
            GameManager game = GameManager.instance;
            StatsManager stats = StatsManager.instance;
            if (run == null || game == null || stats == null)
            {
                rejectReason = run == null ? "no_runmanager" : game == null ? "no_gamemanager" : "no_statsmanager";
                return false;
            }
            if (!ScenarioVanillaCheck.VerifyForStart(run, environment))
            {
                rejectReason = "level_missing";
                return false;
            }
            rejectReason = null;
            return true;
        }

        public static void StartRunLevel()
        {
            RunManager run = RunManager.instance;
            GameManager game = GameManager.instance;
            StatsManager stats = StatsManager.instance;
            run.ResetProgress();
            game.SetConnectRandom(false);
            game.SetLobbyType(GameManager.LobbyTypes.Private);
            stats.saveFileCurrent = "";
            run.ChangeLevel(true, false, RunManager.ChangeLevelType.RunLevel);
        }

        public static void LeaveToMainMenu()
        {
            RunManager run = RunManager.instance;
            run.StartCoroutine(run.LeaveToMainMenu());
        }

        public static bool TryLeaveGameStateMain()
        {
            GameDirector director = GameDirector.instance;
            if (director == null || director.currentState != GameDirector.gameState.Main) return false;
            director.currentState = GameDirector.gameState.Result;
            return true;
        }

        public static bool TryClosePauseMenu()
        {
            MenuManager menu = MenuManager.instance;
            if (menu == null || !menu.PageCheck(MenuPageIndex.Escape)) return false;
            menu.PageClose(MenuPageIndex.Escape);
            return true;
        }

    }
}
