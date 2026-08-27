using UnityEngine;
using Werewolf.Core;
using Werewolf.Vanilla;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;
using Werewolf.Scenarios.Vanilla;

namespace Werewolf.Scenarios.Game
{
    internal sealed partial class ScenarioController
    {
        private int _cuePage = -1;
        private int _cueIndex;
        private long _cueNextAtMs;

        private void TickCue(long now)
        {
            bool onPage = _flow != null && !_flow.Finished;
            int page = onPage ? _flow.Index : -1;
            ScenarioCue cue = onPage ? _flow.Current.Cue : ScenarioCue.None;

            if (page != _cuePage)
            {
                _cuePage = page;
                _cueIndex = 0;
                _cueNextAtMs = now + (_definition != null ? _definition.Timing.StartDelayMs(cue) : 0L);
                if (onPage) RunEntryAction(_flow.Current, now);
            }

            switch (cue)
            {
                case ScenarioCue.ChatScript: TickChatScript(now); break;
                case ScenarioCue.BotVotes: TickBotVotes(now); break;
                case ScenarioCue.ActionNag: TickActionNag(now); break;
            }
        }

        private void RunEntryAction(ScenarioPage page, long now)
        {
            foreach (ScenarioEntryStep step in page.EntrySteps) RunEntryStep(step, now);
        }

        private void RunEntryStep(ScenarioEntryStep step, long now)
        {
            switch (step.Action)
            {
                case ScenarioEntryAction.GrantBeaconCharge:
                    _d.DebugRolesBeaconCharge(1);
                    WLog.Line("scenario_beacon_grant", secret: false, ("count", 1));
                    break;

                case ScenarioEntryAction.GrantMeetingRight:
                    _d.HostGrantMeetingRights(_d.LocalActor, 1);
                    break;

                case ScenarioEntryAction.PlaceFarCorpse:
                    PlaceCorpse(step.Arg, far: true);
                    break;

                case ScenarioEntryAction.PlaceTruckCorpse:
                    PlaceCorpse(step.Arg, far: false);
                    break;

                case ScenarioEntryAction.SpawnQuotaBag:
                    OpenQuotaBagWindow(now);
                    break;

                case ScenarioEntryAction.BodyToPlantSite:
                case ScenarioEntryAction.BodyToTruck:
                case ScenarioEntryAction.BodyToGatherSite:
                case ScenarioEntryAction.BodyToFarSite:
                    MoveBody(step.Arg, step.Action);
                    break;

                case ScenarioEntryAction.GrantBombAmmo:
                    _d.HostGrantBombAmmo(1);
                    break;

                case ScenarioEntryAction.ConveneAsParticipant:
                    ConveneAsParticipant(step.Arg);
                    break;

                case ScenarioEntryAction.KillParticipant:
                    KillParticipant(step.Arg);
                    break;

                case ScenarioEntryAction.HoldEnemiesDespawned:
                    ScenarioEnemyHold.Begin();
                    break;
            }
        }

        private void PlaceCorpse(int participantId, bool far)
        {
            if (!TryBotActor(participantId, out int actor))
            {
                WLog.Line("scenario_corpse_skipped", secret: false,
                    ("participant", participantId), ("reason", "not_a_bot"));
                return;
            }

            bool hasLocal = LocalPlayer.TryGetPositionAndForward(out Vector3 playerPos, out Vector3 localForward);
            if (!hasLocal) playerPos = Vector3.zero;

            bool resolved = far
                ? ScenarioProps.TryResolveFarCorpseGround(out Vector3 ground)
                : ScenarioProps.TryResolveTruckCorpseGround(playerPos, out ground);
            if (!resolved)
            {
                Vector3 forward = hasLocal ? localForward : Vector3.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                ground = playerPos + forward.normalized * 3f;
                WLog.Line("scenario_corpse_fallback", secret: false,
                    ("participant", participantId), ("far", far));
            }

            _d.HostPlaceScenarioCorpse(actor, ground, ScenarioBodyLooks.DeathHead);
        }

        private void TickActionNag(long now)
        {
            if (_cueIndex > 0 || now < _cueNextAtMs) return;
            _cueIndex++;
            FeedSignal(ScenarioSignal.ActionNag);
        }

        private void TickChatScript(long now)
        {
            if (_definition == null) return;

            var script = _definition.MeetingScript(_flow.Current.MeetingIndex).ChatScript;
            if (_cueIndex >= script.Count || now < _cueNextAtMs) return;

            ScenarioCast cast = _definition.Cast;
            ScenarioChatLine line = script[_cueIndex];
            string text = Texts.Get(line.Text);
            string name = cast.TryGetBot(line.ParticipantId, out var bot) ? bot.Name : null;
            int actor = cast.ActorOf(line.ParticipantId, _d.LocalActor);

            bool posted = _d.Chat.PostScenarioChat(actor, name, text);
            if (!posted)
            {
                WLog.Line("scenario_chat_drop", secret: false,
                    ("line", _cueIndex), ("actor", actor));
            }

            _cueIndex++;
            _cueNextAtMs = now + _definition.Timing.ChatDelayMs(text);

            if (_cueIndex >= script.Count) FeedSignal(ScenarioSignal.ChatScriptDone);
        }

        private void TickBotVotes(long now)
        {
            if (_definition == null) return;

            var votes = _definition.MeetingScript(_flow.Current.MeetingIndex).BotVotes;
            if (_cueIndex >= votes.Count || now < _cueNextAtMs) return;

            ScenarioCast cast = _definition.Cast;
            ScenarioVote vote = votes[_cueIndex];
            VoteRejectReason reason = _d.HostCastVoteAsActor(
                cast.ActorOf(vote.VoterParticipantId, _d.LocalActor),
                vote.TargetActor(cast, _d.LocalActor));
            if (reason != VoteRejectReason.None)
            {
                WLog.Line("scenario_bot_vote_rejected", secret: false,
                    ("index", _cueIndex), ("reason", reason));
                Break(ScenarioBreakKind.BotVoteRejected, now);
                return;
            }

            _cueIndex++;
            _cueNextAtMs = now + _definition.Timing.BotVoteIntervalMs;
        }

        private void ResetCue()
        {
            _cuePage = -1;
            _cueIndex = 0;
            _cueNextAtMs = 0;
        }
    }
}
