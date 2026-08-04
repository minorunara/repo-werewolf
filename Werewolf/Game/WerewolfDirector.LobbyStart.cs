using System;
using Photon.Pun;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private readonly LobbyStartAttempt _lobbyStartAttempt = new LobbyStartAttempt();
        private readonly LobbyStartWarning _lobbyStartWarning = new LobbyStartWarning();
        private MenuPageLobby _lobbyStartHeldPage;

        internal bool TryInterceptLobbyStart(MenuPageLobby lobbyPage)
        {
            try
            {
                if (lobbyPage == null || !IsModIntegrityLobbyActive() || !IsCurrentMaster()) return true;
                int masterActor = CurrentMasterActor();
                long now = NowUnixMs();
                if (_modIntegrityHostState.IsInitialized)
                {
                    _modIntegrityHostState.SyncRoster(CurrentRoomActors(), now);
                    ApplyHostSnapshot();
                }
                if (_lobbyStartAttempt.TryConsumeBypass(lobbyPage.GetInstanceID(), masterActor, now))
                    return true;
                if (_lobbyStartAttempt.IsFinalCheckPending) return false;

                LobbyStartDecision decision = CurrentStartDecision();
                if (decision.HasPending)
                {
                    if (_lobbyStartAttempt.TryBeginFinalCheck(true, now))
                    {
                        _lobbyStartHeldPage = lobbyPage;
                        SendManifestRequest(_modIntegrityHostState.GetRetryTargets(now, true), now, force: true);
                        WLog.Line("lobby_start_warning", secret: false,
                            ("state", "final_check"), ("pending", decision.PendingCount));
                    }
                    return false;
                }
                if (decision.WarningKind == LobbyStartWarningKind.None) return true;

                _lobbyStartHeldPage = lobbyPage;
                ShowLobbyStartWarning(decision);
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "prefix_failed"), ("err", e.Message));
                return true;
            }
        }

        private void TickLobbyStartHold(long nowUnixMs)
        {
            if (!_lobbyStartAttempt.IsFinalCheckPending) return;
            try
            {
                LobbyStartDecision current = CurrentStartDecision();
                if (!_lobbyStartAttempt.ShouldCompleteFinalCheck(current.HasPending, nowUnixMs)) return;
                if (_lobbyStartHeldPage == null) return;

                current = CurrentStartDecision();
                if (current.WarningKind == LobbyStartWarningKind.None)
                    ReinvokeLobbyStart(_lobbyStartHeldPage, nowUnixMs, "resolved");
                else
                    ShowLobbyStartWarning(current);
            }
            catch (Exception e)
            {
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "hold_failed"), ("err", e.Message));
                FailOpenLobbyStart(nowUnixMs);
            }
        }

        private void ShowLobbyStartWarning(LobbyStartDecision decision)
        {
            EnsureModIntegrityUiBuilt();
            if (!_lobbyStartWarning.Exists) throw new InvalidOperationException("warning_build");
            _lobbyStartWarning.Show(
                decision,
                onBack: CancelLobbyStartWarning,
                onDetails: OpenModIntegrityDetailsFromWarning,
                onContinue: ContinueLobbyStart);
            WLog.Line("lobby_start_warning", secret: false,
                ("state", "shown"), ("kind", decision.WarningKind),
                ("players", decision.PlayerCount), ("required", decision.RequiredPlayerCount),
                ("shortfall", decision.HasPlayerShortfall ? 1 : 0),
                ("werewolfCount", decision.WerewolfCount),
                ("overflow", decision.HasTeamOverflow ? 1 : 0),
                ("difference", decision.DifferenceCount),
                ("unavailable", decision.UnavailableCount), ("pending", decision.PendingCount));
        }

        private void CancelLobbyStartWarning()
        {
            _lobbyStartAttempt.Clear();
            _lobbyStartHeldPage = null;
        }

        private void OpenModIntegrityDetailsFromWarning()
        {
            MenuPageLobby page = _lobbyStartHeldPage;
            try
            {
                _lobbyStartAttempt.Clear();
                _modIntegrityPanel.Open(preferNeedsReview: true);
                _lobbyStartHeldPage = null;
            }
            catch (Exception e)
            {
                _lobbyStartHeldPage = page;
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "details_failed"), ("err", e.Message));
                FailOpenLobbyStart(NowUnixMs());
            }
        }

        private void ContinueLobbyStart()
        {
            MenuPageLobby page = _lobbyStartHeldPage;
            try
            {
                if (page != null) ReinvokeLobbyStart(page, NowUnixMs(), "explicit_continue");
            }
            catch (Exception e)
            {
                _lobbyStartHeldPage = page;
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "continue_failed"), ("err", e.Message));
                FailOpenLobbyStart(NowUnixMs());
            }
        }

        private void ReinvokeLobbyStart(MenuPageLobby page, long nowUnixMs, string reason)
        {
            if (page == null || !PhotonNetwork.InRoom || !IsCurrentMaster())
            {
                _lobbyStartAttempt.Clear();
                _lobbyStartHeldPage = null;
                return;
            }
            int masterActor = CurrentMasterActor();
            _lobbyStartAttempt.ArmOneShotBypass(page.GetInstanceID(), masterActor, nowUnixMs);
            _lobbyStartHeldPage = null;
            WLog.Line("lobby_start_warning", secret: false, ("state", "reinvoke"), ("reason", reason));
            page.ButtonStart();
        }

        private void FailOpenLobbyStart(long nowUnixMs)
        {
            MenuPageLobby page = _lobbyStartHeldPage;
            _lobbyStartHeldPage = null;
            if (page != null && PhotonNetwork.InRoom && IsCurrentMaster())
                ReinvokeLobbyStart(page, nowUnixMs, "fail_open");
            else
                _lobbyStartAttempt.Clear();
        }

        private LobbyStartDecision CurrentStartDecision()
        {
            ModIntegritySnapshot snapshot = _modIntegrityHostState.IsInitialized
                ? _modIntegrityHostState.BuildSnapshot()
                : null;
            return LobbyStartGate.Evaluate(
                snapshot,
                _modManifestCollector.State == CollectorState.Ready && _modIntegrityHostState.IsInitialized,
                LobbyStartPlayerCount(),
                LiveWerewolfCount(),
                LiveDebugMode());
        }

        private static bool LiveWerewolfModeEnabled()
        {
            if (Plugin.Bindings != null) return Plugin.Bindings.WerewolfModeEnabled.Value;
            return Plugin.GameConfig != null && Plugin.GameConfig.WerewolfModeEnabled;
        }

        private static bool LiveDebugMode()
        {
            if (Plugin.Bindings != null) return Plugin.Bindings.DebugMode.Value;
            return Plugin.GameConfig != null && Plugin.GameConfig.DebugMode;
        }

        private static int LiveWerewolfCount()
        {
            if (Plugin.Bindings != null) return Plugin.Bindings.WerewolfCount.Value;
            return Plugin.GameConfig != null ? Plugin.GameConfig.WerewolfCount : 1;
        }

        private int LobbyStartPlayerCount()
        {
            int room = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                ? PhotonNetwork.CurrentRoom.PlayerCount
                : 1;
            return room + PendingBotCount;
        }
    }
}
