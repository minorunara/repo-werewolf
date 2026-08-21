using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private int _chatRecapLostBaseline;

        private int _chatMeetingNumber;

        private static bool MeetingChatLogEnabled
            => Plugin.Bindings == null || Plugin.Bindings.MeetingChatLog.Value;

        internal bool IsChatLogWindowOpenClient
            => MeetingChatGate.IsOpen(ClientPhase, IsMeetingDiscussionOpenClient) || IsResultChatActiveClient;

        private bool IsMeetingWarpDoneClient => _meetingClient.WarpDone(NowUnixMs());

        private bool IsMeetingDiscussionOpenClient => _meetingClient.DiscussionOpen;

        private Func<int, PlayerAvatar> ChatAvatarResolver
            => _chatDebugAvatarFallback
                ? (Func<int, PlayerAvatar>)ResolveAvatarForChatDebug
                : ResolveAvatar;

        private void ResetMeetingChatView()
        {
            _chatUnread.Clear();
            _chatRecapDeaths.Clear();
            _chatRecapBeaconUses = MeetingRecap.Unknown;
            _chatVoteBaselinePending = false;
            _chatSystemPosted = false;
            if (_chatPanel.Exists) _chatPanel.ResetView();
        }

        private void ResetMeetingChat()
        {
            _chatLog.Clear();
            ResetMeetingChatView();
        }

        private int ConsumeRecapLostDelta()
        {
            MeetingGaugeSnapshot gauge = RolesClient != null ? RolesClient.MeetingGauge : null;
            int total = gauge != null ? gauge.LostDollars : MeetingRecap.Unknown;
            int delta = MeetingRecap.LostSince(total, _chatRecapLostBaseline);
            if (total >= 0) _chatRecapLostBaseline = total;
            return delta;
        }

        private void PostMeetingChatSystemLines(List<List<int>> lastGroups, int lostSince, int meetingNumber)
        {
            try
            {
                string speaker = Texts.Get(TextId.ChatLogSystemName);
                bool emoji = EmojiSprites.Ready;
                string face = _chatRecapDeaths.Count > 0 ? "img_taxman_system" : "img_taxman_nodeath";
                MeetingGaugeSnapshot gauge = RolesClient != null ? RolesClient.MeetingGauge : null;
                var recap = new MeetingRecapData(
                    _chatRecapDeaths,
                    lostSince,
                    gauge != null ? gauge.ExtractedDollars : MeetingRecap.Unknown,
                    gauge != null ? gauge.HaulGoalDollars : MeetingRecap.Unknown,
                    _chatRecapBeaconUses);
                _chatLog.AppendSystem(speaker,
                    ChatEmoji.Format(TextId.ChatLogMeetingNumberFormat, emoji, meetingNumber),
                    string.Join("\n", MeetingRecap.BuildLines(recap, emoji).ToArray()), face,
                    section: true);

                if (lastGroups != null && lastGroups.Count >= 2)
                {
                    List<string> lines = ScatterGroupsText.FormatLines(
                        lastGroups, ScatterMemberChatLabel, TextId.ChatLogScatterLineFormat);
                    _chatLog.AppendSystem(speaker, ChatEmoji.Get(TextId.ChatLogScatterTitle, emoji),
                        string.Join("\n", lines.ToArray()), face);
                }
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_system_error", secret: false, ("err", e.Message));
            }
        }

        private void RecordMeetingVotesClient(int[] votedActors)
        {
            if (!MeetingChatLogEnabled || votedActors == null) return;
            try
            {
                if (_chatVoteBaselinePending)
                {
                    _chatVoteBaselinePending = false;
                    return;
                }

                string voted = Texts.Get(TextId.ChatLogVoted);
                IReadOnlyCollection<int> known = _meetingClient.VotedActors;
                foreach (int actor in votedActors)
                {
                    if (Contains(known, actor)) continue;
                    _chatLog.AppendVote(actor, ResolveDisplayName(actor), voted);
                }
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_vote_error", secret: false, ("err", e.Message));
            }
        }

        private static bool Contains(IReadOnlyCollection<int> values, int value)
        {
            foreach (int v in values)
            {
                if (v == value) return true;
            }
            return false;
        }

        public void RecordMeetingChatClient(PlayerAvatar speaker, string message)
        {
            if (!MeetingChatLogEnabled || speaker == null) return;
            try
            {
                int actor = Registry != null ? Registry.ResolveActor(speaker) : -1;
                AppendMeetingChatMessageClient(
                    actor, ResolveDisplayName(actor), message, ChatSpeakerKindFor(actor), playSfx: true);
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_record_error", secret: false, ("err", e.Message));
            }
        }

        public void RecordReplayChatClient(PlayerAvatar speaker, string message)
        {
            if (speaker == null) return;
            try
            {
                RecordReplayChatByActor(
                    Registry != null ? Registry.ResolveActor(speaker) : -1, message);
            }
            catch (Exception e)
            {
                WLog.Line("replay_chat_record_error", secret: false, ("err", e.Message));
            }
        }

        private bool RecordReplayChatByActor(int actor, string message)
        {
            bool alive = _meetingClient.GetRowStatus(actor) == RowStatus.Alive;
            if (!ReplayChatGate.ShouldRecord(ClientPhase, IsMeetingDiscussionOpenClient, alive)) return false;
            string text = ReplayChatText.SanitizeForRecord(message);
            if (text.Length == 0) return false;
            ReplaySampler.NoteChat(actor, text);
            return true;
        }

        private ChatSpeaker ChatSpeakerKindFor(int actor)
        {
            if (ClientPhase == GamePhase.GameOver) return ChatSpeaker.Alive;
            return _meetingClient.GetRowStatus(actor) == RowStatus.Alive
                ? ChatSpeaker.Alive
                : ChatSpeaker.Dead;
        }

        private bool AppendMeetingChatMessageClient(
            int actor, string name, string message, ChatSpeaker kind, bool playSfx)
        {
            bool added = _chatLog.Append(actor, name, message, kind);
            if (added && playSfx)
            {
                EnsureSfxBuilt();
                _sfxPlayer.Play(MeetingChatSfxClipKey, MeetingChatSfxVolumeScale);
                _chatUnread.OnMessageAppended(actor, LocalActor, _chatPanel.IsOpen);
            }
            return added;
        }

        private void TickMeetingChat()
        {
            if (!MeetingChatLogEnabled || !_chatPanel.Exists) return;

            _chatPanel.Tick(
                Plugin.MeetingChatLogKey != null ? Plugin.MeetingChatLogKey.Value : KeyCode.L,
                InputGate.KeysFree);

            _chatPanel.Render(_chatLog, LocalActor, ChatAvatarResolver,
                ParticipantIdFor,
                MarkedTeammateRole,
                !IsLocalAlive());

            if (_chatPanel.IsOpen) _chatUnread.Clear();
            _chatPanel.SetUnreadBadge(_chatUnread.HasUnread);
        }

    }
}
