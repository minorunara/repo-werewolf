using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private readonly LifecycleGate _lifecycleGate = new LifecycleGate();

        public void OnEnemySetupStarted()
        {
            if (!Session.IsHost || _session != null || !IsWerewolfRoundExpected()) return;
            PreplayEnemySpawnHold.Begin(SessionClock.NowMs());
        }

        public void OnLevelGenerated()
        {
            try
            {
                if (!Session.IsHost) return;
                if (Plugin.GameConfig == null) return;
                if (!ScenarioSessionGate.ShouldEngage(
                        Plugin.GameConfig.WerewolfModeEnabled, ScenarioRun.Active))
                {
                    return;
                }
                if (_session != null) return;

                bool isRunLevel = Session.InRunLevel;
                GamePhase phase = HostPhase;
                if (!_lifecycleGate.ShouldAutoStart(true, isRunLevel, phase)) return;

                _autoStartWait.Arm();
                CacheLoadWaitRosterNames();
                PreplayEnemySpawnHold.Begin(SessionClock.NowMs());
            }
            catch (Exception e)
            {
                WLog.Line("autostart_error", secret: false, ("err", e.Message));
            }
        }

        private void TickAutoStartWait(long now)
        {
            if (!_autoStartWait.Armed) return;

            if (_session != null || !Session.InRunLevel || !Session.IsHost)
            {
                _autoStartWait.Disarm();
                return;
            }

            if (!_autoStartWait.ShouldFire(AllPlayersLevelLoadCompleted())) return;
            StartResult result = StartHosted();
            if (result.Success) return;

            WLog.Line("autostart_rejected", secret: false, ("reason", result.Reason));
            PreplayAbortReason reason = result.Reason == StartRejectReason.TooFewPlayers
                ? PreplayAbortReason.TooFewPlayersAfterLoad
                : PreplayAbortReason.InvalidConfigAfterLoad;
            BeginPreplayAbortWithoutSession(reason, now);
        }

        private bool AllPlayersLevelLoadCompleted()
        {
            try
            {
                return Session.IsGameStateMain && Players.AllLoadAnimationsCompleted();
            }
            catch (Exception e)
            {
                WLog.Line("autostart_probe_error", secret: false, ("err", e.Message));
                return false;
            }
        }

        private void CacheLoadWaitRosterNames()
        {
            Dictionary<int, string> roomNames = Players.RoomRosterNames();
            List<WPlayer> players = Registry != null && Registry.Available
                ? Registry.BuildRealPlayers()
                : new List<WPlayer>();
            if (roomNames.Count == 0 && players.Count == 0) return;
            if (_roundMirror.DisplayNameCache == null)
                _roundMirror.DisplayNameCache = new Dictionary<int, string>();
            foreach (var pair in roomNames)
                _roundMirror.DisplayNameCache[pair.Key] = pair.Value;
            foreach (WPlayer player in players)
            {
                if (player != null && !string.IsNullOrEmpty(player.Name))
                    _roundMirror.DisplayNameCache[player.ActorNumber] = player.Name;
            }
        }

        private void CacheHostSessionRosterNames()
        {
            if (_session == null) return;
            if (_roundMirror.DisplayNameCache == null)
                _roundMirror.DisplayNameCache = new Dictionary<int, string>();
            foreach (WPlayer player in _session.Players)
            {
                if (player != null && !string.IsNullOrEmpty(player.Name))
                    _roundMirror.DisplayNameCache[player.ActorNumber] = player.Name;
            }
        }

        private void CachePreplayRosterIfNeeded()
        {
            if (_session != null || _roundMirror.DisplayNameCache != null) return;
            if (!Session.InRunLevel || !IsWerewolfRoundExpected()) return;
            CacheLoadWaitRosterNames();
        }

        private void BeginPreplayAbortWithoutSession(PreplayAbortReason reason, long nowUnixMs)
        {
            PreplayEnemySpawnHold.Begin(nowUnixMs);
            _lifecycleGate.MarkStarted();
            _clientPhase = GamePhase.PreplayAborted;
            _roomState.PublishPhase(GamePhase.PreplayAborted, 0);
            _bus?.SendToAll(MessageCodes.PreplayAborted, new object[] { (byte)reason });
            _resultSequence.Begin(nowUnixMs, _resultFlowFeature.RoundGameOverAutoReturnSec);
            _resetArmed = true;
            WLog.Line("preplay_aborted", secret: false, ("reason", reason));
        }
    }
}
