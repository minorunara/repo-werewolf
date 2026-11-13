using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private void InvokeReturnToLobby()
        {
            try
            {
                if (!Session.HasRunManager)
                {
                    WLog.Line("return_lobby_skipped", secret: false, ("reason", "no_run_manager"));
                    return;
                }
                if (ScenarioSessionGate.ReturnsToTitle(ScenarioRun.Active))
                {
                    Session.AllPlayersDeadSet(false);
                    ScenarioRun.ExitToTitle("result_sequence", completed: true);
                    return;
                }

                WLog.Line("return_lobby", secret: false, ("via", "result_sequence"));
                Session.ReturnToLobbyMenu();
                Session.AllPlayersDeadSet(false);
            }
            catch (Exception e)
            {
                WLog.Line("return_lobby_error", secret: false, ("err", e.Message));
            }
        }

        private void ResetToLobby(string reason)
        {
            ScenarioRun.Clear();

            if (_hostRound != null)
            {
                WorldgenApplier.RestoreOnLobbyReturn();
            }

            DetachHostRoundState();

            Plugin.RefreshGameConfig();

            FakeBodies.Clear();

            _lastBombDeny.Clear();
            Runtime.WinCeremonyFeature.ResetHost();
            _enemyIgnoreRoster.ClearAll();
            _beaconEffect.CancelAll("reset");
            _beaconEffect.ResetSummonGate();
            TeardownBus();
            _reservations.Clear();
            ClearClientState();
            _clientPhase = GamePhase.Lobby;
            _resetArmed = false;
            _autoStartWait.Disarm();
            PreplayEnemySpawnHold.Cancel();
            _resultSequence.Cancel();

            _lifecycleGate.ResetForNextRound();

            Runtime.LobbySettingsFeature.ResetForLobbyReturn();

            ClearCosmeticGrantState(reason);

            Patches.WipeGuardPatch.ResetLogThrottle();

            Runtime.MovementFreezer.End();
            Runtime.EnemyFreezer.End();
            Patches.PlayerSpawnPatch.MeetingActive = false;

            foreach (IClientPanel panel in Runtime.RoundPanels) panel.Destroy();
            Runtime.BomberFeature.DestroyUi();
            Runtime.ShamanFeature.DestroyUi();
            Runtime.TutorialFeature.DestroyUi();
            Runtime.ScenarioFeature.DestroyUi();
            Runtime.ChatFeature.DestroyUi();
            Runtime.ResultFlowFeature.DestroyUi();
            Runtime.RoleRevealFeature.DestroyUi();
            Runtime.WinCeremonyFeature.DestroyUi();
            Runtime.ScatterFeature.DestroyUi();
            Runtime.RolesClientFeature.DestroyUi();
            Runtime.MeetingClientFeature.DestroyUi();
            _sfxPlayer.Destroy();

            WLog.Phase(GamePhase.GameOver, GamePhase.Lobby, reason);
            Debugging.StructuredLog.FlushDeferredSecrets("session_reset");
        }

        private void ClearClientState()
        {
            Runtime.ResultFlowFeature.Reset();
            _roundMirror.Reset();
            _cfgMirror.Reset();
            IdRoster.Reset();
            ReplaySampler.ResetAll();
            Runtime.ChatFeature.Reset();
            Runtime.VoiceDriver.ForceRestore("client_reset");
            Runtime.VoiceDriver.SanitizeAtRoundStart();
            _meetingClient.Reset();
            Runtime.MeetingClientFeature.Reset();
            Runtime.TutorialFeature.Reset();
            Runtime.ScenarioFeature.Reset();
            Runtime.TruckWarper.ResetWatchdog();
            Runtime.ScatterFeature.Reset();
            Runtime.VotePanel.StopScatterReveal();
            Runtime.RolesClientFeature.Reset();

            Runtime.RoleRevealFeature.Reset();

            Runtime.WinCeremonyFeature.Reset();

            _resultScreen.Hide();

            Runtime.BomberFeature.Reset();

            Runtime.ShamanFeature.Reset();
        }

    }
}
