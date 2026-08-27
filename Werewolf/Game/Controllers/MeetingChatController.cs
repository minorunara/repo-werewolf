using System;
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Werewolf.Debugging;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class MeetingChatController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private const string MeetingChatSfxClipKey = "sfx_chat_message";
        private const float MeetingChatSfxVolumeScale = 1.5f;

        private readonly MeetingChatLog _chatLog = new MeetingChatLog();

        private readonly MeetingChatPanel _chatPanel = new MeetingChatPanel();

        private readonly MeetingChatPanel _resultChatPanel = new MeetingChatPanel(ChatPanelSurface.Result);

        private readonly ChatUnread _chatUnread = new ChatUnread();
        private readonly List<string> _chatRecapDeaths = new List<string>();
        private int _chatRecapBeaconUses = MeetingRecap.Unknown;

        private bool _chatVoteBaselinePending;

        private bool _chatSystemPosted;

        private int _chatRecapLostBaseline;

        private int _chatMeetingNumber;

        private long _chatDebugDueUnixMs = -1;
        private string _chatDebugPendingText;

        private bool _inputGateProbe;
        private bool _inputGateLastFree;

        private bool _chatDebugAvatarFallback;

        private readonly DebugChatAuto _chatAuto = new DebugChatAuto();

        internal MeetingChatController(WerewolfDirector director)
        {
            _d = director;
        }

        internal static bool LogEnabled
            => ScenarioSessionGate.ChatLogEnabled(
                Plugin.Bindings == null || Plugin.Bindings.MeetingChatLog.Value, ScenarioRun.Active);

        internal bool IsWindowOpen
            => MeetingChatGate.IsOpen(_d.ClientPhase, IsDiscussionOpen) || _d.IsResultChatActiveClient;

        private bool IsWarpDone => _d.MeetingClientView.WarpDone(SessionClock.NowMs());

        private bool IsDiscussionOpen => _d.MeetingClientView.DiscussionOpen;

        internal bool MeetingOpeningPosted => _chatSystemPosted;

        private Func<int, PlayerHandle> ChatAvatarResolver
            => _chatDebugAvatarFallback
                ? (Func<int, PlayerHandle>)ResolveAvatarForChatDebug
                : Players.Resolve;

        internal void ResetMeetingView()
        {
            _chatUnread.Clear();
            _chatRecapDeaths.Clear();
            _chatRecapBeaconUses = MeetingRecap.Unknown;
            _chatVoteBaselinePending = false;
            _chatSystemPosted = false;
            if (_chatPanel.Exists) _chatPanel.ResetView();
        }

        private void ResetAllLog()
        {
            _chatLog.Clear();
            ResetMeetingView();
        }

        internal void ResetForNewMatch()
        {
            ResetAllLog();
            _chatMeetingNumber = 0;
        }

        internal void ResetForRestoredMeeting()
        {
            ResetAllLog();
            _chatVoteBaselinePending = true;
            _chatSystemPosted = true;
        }

        internal void NoteBeaconAudit(int uses)
        {
            _chatRecapBeaconUses = uses;
        }

        internal void SetRecapDeaths(IReadOnlyList<string> labels)
        {
            _chatRecapDeaths.Clear();
            if (labels != null) _chatRecapDeaths.AddRange(labels);
        }

        internal void PostMeetingOpening(List<List<int>> lastGroups)
        {
            int lostSince = ConsumeRecapLostDelta();
            _chatMeetingNumber++;
            if (LogEnabled) PostMeetingChatSystemLines(lastGroups, lostSince, _chatMeetingNumber);
            _chatSystemPosted = true;
        }

        private int ConsumeRecapLostDelta()
        {
            MeetingGaugeSnapshot gauge = _d.RolesClient != null ? _d.RolesClient.MeetingGauge : null;
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
                bool emoji = EmojiSprites.Ready && EmojiSprites.VanillaReady;
                string face = _chatRecapDeaths.Count > 0 ? ChatEmoji.RecapFaceDeaths : ChatEmoji.RecapFaceNoDeaths;
                MeetingGaugeSnapshot gauge = _d.RolesClient != null ? _d.RolesClient.MeetingGauge : null;
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
                        lastGroups, _d.ParticipantIdLabel, TextId.ChatLogScatterLineFormat);
                    _chatLog.AppendSystem(speaker, ChatEmoji.Get(TextId.ChatLogScatterTitle, emoji),
                        string.Join("\n", lines.ToArray()), face);
                }
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_system_error", secret: false, ("err", e.Message));
            }
        }

        internal void RecordVoteProgress(int[] votedActors)
        {
            if (!LogEnabled || votedActors == null) return;
            try
            {
                if (_chatVoteBaselinePending)
                {
                    _chatVoteBaselinePending = false;
                    return;
                }

                string voted = Texts.Get(TextId.ChatLogVoted);
                IReadOnlyCollection<int> known = _d.MeetingClientView.VotedActors;
                foreach (int actor in votedActors)
                {
                    if (Contains(known, actor)) continue;
                    _chatLog.AppendVote(actor, _d.ResolveDisplayName(actor), voted);
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

        internal void RecordChat(int speakerActor, string message)
        {
            if (!LogEnabled) return;
            try
            {
                int actor = _d.Registry != null ? speakerActor : -1;
                AppendChatMessage(
                    actor, _d.ResolveDisplayName(actor), message, ChatSpeakerKindFor(actor), playSfx: true);
            }
            catch (Exception e)
            {
                WLog.Line("chat_log_record_error", secret: false, ("err", e.Message));
            }
        }

        internal void RecordReplayChat(int speakerActor, string message)
        {
            try
            {
                RecordReplayChatByActor(
                    _d.Registry != null ? speakerActor : -1, message);
            }
            catch (Exception e)
            {
                WLog.Line("replay_chat_record_error", secret: false, ("err", e.Message));
            }
        }

        private bool RecordReplayChatByActor(int actor, string message)
        {
            bool alive = _d.MeetingClientView.GetRowStatus(actor) == RowStatus.Alive;
            if (!ReplayChatGate.ShouldRecord(_d.ClientPhase, IsDiscussionOpen, alive)) return false;
            string text = ReplayChatText.SanitizeForRecord(message);
            if (text.Length == 0) return false;
            ReplaySampler.NoteChat(actor, text);
            return true;
        }

        private ChatSpeaker ChatSpeakerKindFor(int actor)
        {
            if (_d.ClientPhase == GamePhase.GameOver) return ChatSpeaker.Alive;
            return _d.MeetingClientView.GetRowStatus(actor) == RowStatus.Alive
                ? ChatSpeaker.Alive
                : ChatSpeaker.Dead;
        }

        private bool AppendChatMessage(
            int actor, string name, string message, ChatSpeaker kind, bool playSfx)
        {
            bool added = _chatLog.Append(actor, name, message, kind);
            if (added && playSfx)
            {
                _d.PlaySfx(MeetingChatSfxClipKey, MeetingChatSfxVolumeScale);
                _chatUnread.OnMessageAppended(actor, _d.LocalActor, _chatPanel.IsOpen);
            }
            return added;
        }

        internal void EnsureMeetingPanelBuilt()
        {
            if (LogEnabled) _d.EnsurePanelBuilt(_chatPanel);
        }

        internal bool IsPointerOverMeetingPanel(Vector2 p)
            => _chatPanel.Exists && _chatPanel.IsPointerOverPanel(p);

        internal bool IsMeetingPanelOpen => _chatPanel.Exists && _chatPanel.IsOpen;

        internal bool TryGetRecapLineScreenRect(int lineIndex, out UnityEngine.Rect rect)
        {
            if (!_chatPanel.Exists)
            {
                rect = default;
                return false;
            }
            return _chatPanel.TryGetRecapLineScreenRect(lineIndex, out rect);
        }

        internal bool PostScenarioChat(int actor, string name, string text)
            => DebugInjectChatCore(actor, name, text, playSfx: true, recordReplay: true,
                logKey: "scenario_chat", logSuccess: false);

        internal void TickMeetingPanel()
        {
            if (!LogEnabled || !_chatPanel.Exists) return;

            _chatPanel.Tick(
                Plugin.MeetingChatLogKey != null ? Plugin.MeetingChatLogKey.Value : KeyCode.L,
                InputGate.KeysFree);

            _chatPanel.Render(_chatLog, _d.LocalActor, ChatAvatarResolver,
                _d.ParticipantIdFor,
                _d.MarkedTeammateRole,
                !_d.IsLocalAlive());

            if (_chatPanel.IsOpen) _chatUnread.Clear();
            _chatPanel.SetUnreadBadge(_chatUnread.HasUnread);
        }

        internal void TickResultChat(bool active, Func<int, Role?> markedRole)
        {
            if (active) _d.EnsurePanelBuilt(_resultChatPanel);
            if (_resultChatPanel.Exists) _resultChatPanel.SetVisible(active);
            if (!active || !_resultChatPanel.Exists) return;

            UiKit.KeepCursorFree();
            _resultChatPanel.Tick(
                Plugin.MeetingChatLogKey != null ? Plugin.MeetingChatLogKey.Value : KeyCode.L,
                InputGate.KeysFree);
            _resultChatPanel.Render(_chatLog, _d.LocalActor, ChatAvatarResolver,
                _d.ParticipantIdFor, markedRole, localDead: false);

            if (_resultChatPanel.IsOpen) _chatUnread.Clear();
            _resultChatPanel.SetUnreadBadge(_chatUnread.HasUnread);
        }

        internal bool IsPointerOverResultPanel(Vector2 p)
            => _resultChatPanel.Exists && _resultChatPanel.IsPointerOverPanel(p);

        internal void EnsureResultPanelTopSibling()
        {
            if (_resultChatPanel.Exists) _resultChatPanel.EnsureTopSibling();
        }

        internal void ResetForResultChat()
        {
            _chatLog.Clear();
            _chatUnread.Clear();
            if (_resultChatPanel.Exists) _resultChatPanel.ResetView();
        }

        internal bool DebugInjectChat(int actor, string name, string text)
            => DebugInjectChatCore(actor, name, text, playSfx: true, recordReplay: true);

        private bool DebugInjectChatCore(
            int actor, string name, string text, bool playSfx, bool recordReplay,
            string logKey = "chat_debug_inject", bool logSuccess = true)
        {
            if (!IsWindowOpen)
            {
                WLog.Line(logKey, secret: false,
                    ("result", "out_of_window"), ("phase", _d.ClientPhase),
                    ("warpDone", IsWarpDone),
                    ("discussionOpen", IsDiscussionOpen));
                return false;
            }
            bool recorded = recordReplay && RecordReplayChatByActor(actor, text);

            if (!LogEnabled)
            {
                WLog.Line(logKey, secret: false,
                    ("result", "disabled_by_cfg"), ("replay", recorded));
                return false;
            }
            if (!_d.ShouldShowDeadTextClient(actor))
            {
                WLog.Line(logKey, secret: false,
                    ("result", "blocked_dead_text"), ("actor", actor),
                    ("localAlive", _d.IsLocalAlive()), ("replay", recorded));
                return false;
            }

            ChatSpeaker kind = ChatSpeakerKindFor(actor);
            bool added = AppendChatMessage(
                actor, name ?? _d.ResolveDisplayName(actor), text, kind, playSfx);
            if (logSuccess || !added)
            {
                WLog.Line(logKey, secret: false,
                    ("result", added ? "logged" : "rejected_empty"), ("actor", actor),
                    ("speaker", kind), ("replay", recorded));
            }
            return added;
        }

        internal bool DebugInjectVoteLine(int actor)
        {
            if (!LogEnabled || _d.ClientPhase != GamePhase.Meeting) return false;
            bool added = _chatLog.AppendVote(actor, _d.ResolveDisplayName(actor),
                Texts.Get(TextId.ChatLogVoted));
            WLog.Line("chat_debug_vote_line", secret: false, ("actor", actor), ("added", added));
            return added;
        }

        internal void DebugArmVoteBaseline()
        {
            _chatVoteBaselinePending = true;
            WLog.Line("chat_debug_vote_baseline_armed", secret: false);
        }

        internal int DebugSpamChat(int count)
        {
            int[] speakers = BuildDebugSpeakerActors();
            int added = 0;
            for (int i = 0; i < count; i++)
            {
                int actor = speakers[(i / 2) % speakers.Length];
                if (DebugInjectChatCore(actor, null, $"テスト発言 {i + 1}",
                        playSfx: false, recordReplay: false)) added++;
            }
            WLog.Line("chat_debug_spam", secret: false,
                ("requested", count), ("added", added), ("total", _chatLog.Count));
            return added;
        }

        internal bool DebugToggleChatAuto(int count)
        {
            if (_chatAuto.Active)
            {
                _chatAuto.Stop();
                WLog.Line("chat_debug_auto", secret: false,
                    ("state", "off"), ("reason", "toggle"), ("posted", _chatAuto.Posted));
                return false;
            }

            _chatAuto.Start(SessionClock.NowMs(), count);
            WLog.Line("chat_debug_auto", secret: false,
                ("state", "on"), ("count", count > 0 ? count : -1),
                ("steps", DebugChatAuto.StepCount), ("speakers", BuildDebugSpeakerActors().Length));
            return true;
        }

        private int[] BuildDebugSpeakerActors()
        {
            var list = new List<int> { _d.LocalActor };
            IReadOnlyList<WPlayer> players = _d.HostSessionPlayers;
            if (players != null)
            {
                foreach (WPlayer p in players)
                {
                    if (p.ActorNumber != _d.LocalActor) list.Add(p.ActorNumber);
                }
            }
            return list.ToArray();
        }

        internal void DebugScheduleChat(long delayMs, string text)
        {
            _chatDebugDueUnixMs = SessionClock.NowMs() + Math.Max(0L, delayMs);
            _chatDebugPendingText = text;
            WLog.Line("chat_debug_scheduled", secret: false, ("delayMs", delayMs));
        }

        internal bool DebugToggleInputGateProbe()
        {
            _inputGateProbe = !_inputGateProbe;
            _inputGateLastFree = InputGate.KeysFree;
            WLog.Line("chat_debug_input_gate_probe", secret: false,
                ("state", _inputGateProbe ? "on" : "off"), ("keysFree", _inputGateLastFree));
            return _inputGateProbe;
        }

        internal bool DebugToggleChatAvatarFallback()
        {
            _chatDebugAvatarFallback = !_chatDebugAvatarFallback;
            if (_chatPanel.Exists) _chatPanel.ResetView();
            WLog.Line("chat_debug_avatar_fallback", secret: false,
                ("state", _chatDebugAvatarFallback ? "on" : "off"));
            return _chatDebugAvatarFallback;
        }

        private PlayerHandle ResolveAvatarForChatDebug(int actor)
        {
            PlayerHandle player = Players.Resolve(actor);
            return player.IsValid ? player : Players.Local;
        }

        internal int DebugLocalActor => _d.LocalActor;

        internal void DebugClearChat()
        {
            _chatLog.Clear();
            _chatUnread.Clear();
            if (_chatPanel.Exists) _chatPanel.ResetView();
            WLog.Line("chat_debug_clear", secret: false);
        }

        internal void DebugDumpChatState()
        {
            WLog.Line("chat_debug_state", secret: false,
                ("enabled", LogEnabled),
                ("phase", _d.ClientPhase),
                ("warpDone", IsWarpDone),
                ("discussionOpen", IsDiscussionOpen),
                ("count", _chatLog.Count),
                ("appended", _chatLog.AppendedTotal),
                ("dropped", _chatLog.DroppedTotal),
                ("panelBuilt", _chatPanel.Exists),
                ("panelOpen", _chatPanel.Exists && _chatPanel.IsOpen),
                ("localAlive", _d.IsLocalAlive()),
                ("keysFree", InputGate.KeysFree),
                ("auto", _chatAuto.Active),
                ("autoPosted", _chatAuto.Posted),
                ("autoRemaining", _chatAuto.Remaining));
        }

        public void Tick(long now)
        {
            TickChatAuto(now);

            if (_chatDebugDueUnixMs >= 0 && now >= _chatDebugDueUnixMs)
            {
                long due = _chatDebugDueUnixMs;
                string text = _chatDebugPendingText;
                _chatDebugDueUnixMs = -1;
                _chatDebugPendingText = null;
                bool logged = DebugInjectChat(_d.LocalActor, null, text);
                WLog.Line("chat_debug_scheduled_fired", secret: false,
                    ("phase", _d.ClientPhase), ("logged", logged), ("lateMs", now - due));
            }

            if (!_inputGateProbe) return;
            bool free = InputGate.KeysFree;
            if (free == _inputGateLastFree) return;
            _inputGateLastFree = free;
            WLog.Line("chat_debug_input_gate", secret: false, ("keysFree", free));
        }

        private void TickChatAuto(long now)
        {
            if (!_chatAuto.IsDue(now)
                || !MeetingChatGate.IsOpen(_d.ClientPhase, IsDiscussionOpen)) return;

            int[] speakers = BuildDebugSpeakerActors();
            for (int i = 0; i < DebugChatAuto.MaxPostsPerFrame; i++)
            {
                if (!_chatAuto.TryTakeDue(now, speakers.Length, out int slot, out string text)) break;
                DebugInjectChatCore(speakers[slot], null, text, playSfx: true, recordReplay: true);
            }

            if (_chatAuto.Active) return;
            WLog.Line("chat_debug_auto", secret: false,
                ("state", "off"), ("reason", "count_reached"), ("posted", _chatAuto.Posted));
        }

        public void Reset()
        {
            ResetAllLog();
            _chatRecapLostBaseline = 0;
            _chatMeetingNumber = 0;
        }

        public void DestroyUi()
        {
            _chatPanel.Destroy();
            _resultChatPanel.Destroy();
        }
    }
}
