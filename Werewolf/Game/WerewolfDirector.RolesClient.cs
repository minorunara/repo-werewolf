using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private void EnsureSfxBuilt()
        {
            if (_sfxPlayer.Exists) return;
            _sfxPlayer.Build(gameObject);
        }

        private void ShowResultScreen(byte winningTeam, int[] actors, byte[] roles)
        {
            try
            {
                if (actors == null || roles == null)
                {
                    WLog.Line("result_screen_skip", secret: false, ("reason", "null_payload"));
                    return;
                }

                List<int> disconnected = null;
                foreach (var pair in _meetingClient.Rows)
                {
                    if (pair.Value != RowStatus.Disconnected) continue;
                    (disconnected ??= new List<int>()).Add(pair.Key);
                }

                IReadOnlyList<ResultRow> rows = ResultModel.Build(
                    winningTeam, actors, roles, _roundMirror.DeathMirror, ResolveDisplayName, disconnected);

                SetCrownRosterFromRows(rows);

                EnsurePanelBuilt(_resultScreen);
                if (!_resultScreen.Exists)
                {
                    WLog.Line("result_screen_skip", secret: false, ("reason", "not_built"));
                    return;
                }

                List<string> digestLines = _resultFlowFeature.ClientDigestEntries != null
                    ? ResultDigestText.FormatLines(_resultFlowFeature.ClientDigestEntries, ResolveDisplayName)
                    : null;
                _resultScreen.Show(winningTeam, rows, Players.Resolve,
                    digestLines, ResultFlowController.BuildResultFooterText(), ParticipantIdFor,
                    _resultFlowFeature.MatchWinReason());
                _resultFlowFeature.CaptureResultChatContext(actors, roles);
                PlayResultSfx(winningTeam);
                FeedScenarioSignal(ScenarioSignal.ResultScreenShown);
            }
            catch (Exception e)
            {
                WLog.Line("result_screen_error", secret: false, ("err", e.Message));
            }
        }

        private void PlayResultSfx(byte outcome)
        {
            string clipKey;
            switch (outcome)
            {
                case (byte)Team.Werewolves:
                case TeamCodes.PeacefulCollapsed: clipKey = "sfx_result_wolves_win"; break;
                case (byte)Team.Villagers:
                case TeamCodes.PeacefulSurvived: clipKey = "sfx_result_villagers_win"; break;
                default: return;
            }
            EnsureSfxBuilt();
            _sfxPlayer.Play(clipKey);
        }

        private static void SetCrownRosterFromRows(IReadOnlyList<ResultRow> rows)
        {
            var winners = new List<int>();
            foreach (ResultRow row in rows)
            {
                if (row.IsWinningSide) winners.Add(row.ActorNumber);
            }
            CrownRoster.SetWinners(winners);
            WLog.Line("crown_roster_set", secret: false, ("count", winners.Count));
        }

        internal string DisplayNameForActor(int actorNumber) => ResolveDisplayName(actorNumber);

        internal string ResolveDisplayName(int actorNumber)
        {
            if (_session != null)
            {
                foreach (WPlayer p in _session.Players)
                {
                    if (p != null && p.ActorNumber == actorNumber && !string.IsNullOrEmpty(p.Name))
                    {
                        return p.Name;
                    }
                }
                return "#" + actorNumber;
            }

            if (_roundMirror.DisplayNameCache != null && _roundMirror.DisplayNameCache.TryGetValue(actorNumber, out string cached))
            {
                return cached;
            }
            if (_roundMirror.DisplayNameCache == null) _roundMirror.DisplayNameCache = new Dictionary<int, string>();
            foreach (WPlayer p in Registry.BuildRealPlayers())
            {
                if (p != null && !string.IsNullOrEmpty(p.Name))
                {
                    _roundMirror.DisplayNameCache[p.ActorNumber] = p.Name;
                }
            }
            if (_roundMirror.DisplayNameCache.TryGetValue(actorNumber, out string resolved))
            {
                return resolved;
            }
            return "#" + actorNumber;
        }

    }
}
