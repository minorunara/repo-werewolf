using System;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private long _chatDebugDueUnixMs = -1;
        private string _chatDebugPendingText;

        private bool _inputGateProbe;
        private bool _inputGateLastFree;

        private bool _chatDebugAvatarFallback;

        public bool DebugInjectChat(int actor, string name, string text)
            => DebugInjectChatCore(actor, name, text, playSfx: true);

        private bool DebugInjectChatCore(int actor, string name, string text, bool playSfx)
        {
            if (!MeetingChatLogEnabled)
            {
                WLog.Line("chat_debug_inject", secret: false, ("result", "disabled_by_cfg"));
                return false;
            }
            if (ClientPhase != GamePhase.Meeting)
            {
                WLog.Line("chat_debug_inject", secret: false,
                    ("result", "not_meeting"), ("phase", ClientPhase));
                return false;
            }
            if (!ShouldShowDeadTextClient(actor))
            {
                WLog.Line("chat_debug_inject", secret: false,
                    ("result", "blocked_dead_text"), ("actor", actor), ("localAlive", IsLocalAlive()));
                return false;
            }

            ChatSpeaker kind = _meetingClient.GetRowStatus(actor) == RowStatus.Alive
                ? ChatSpeaker.Alive
                : ChatSpeaker.Dead;
            bool added = AppendMeetingChatMessageClient(
                actor, name ?? ResolveDisplayName(actor), text, kind, playSfx);
            WLog.Line("chat_debug_inject", secret: false,
                ("result", added ? "logged" : "rejected_empty"), ("actor", actor), ("speaker", kind));
            return added;
        }

        public bool DebugInjectVoteLine(int actor)
        {
            if (!MeetingChatLogEnabled || ClientPhase != GamePhase.Meeting) return false;
            bool added = _chatLog.AppendVote(actor, ResolveDisplayName(actor),
                Texts.Get(TextId.ChatLogVoted));
            WLog.Line("chat_debug_vote_line", secret: false, ("actor", actor), ("added", added));
            return added;
        }

        public void DebugArmVoteBaseline()
        {
            _chatVoteBaselinePending = true;
            WLog.Line("chat_debug_vote_baseline_armed", secret: false);
        }

        public int DebugSpamChat(int count)
        {
            int[] speakers = BuildDebugSpeakerActors();
            int added = 0;
            for (int i = 0; i < count; i++)
            {
                int actor = speakers[(i / 2) % speakers.Length];
                if (DebugInjectChatCore(actor, null, $"テスト発言 {i + 1}", playSfx: false)) added++;
            }
            WLog.Line("chat_debug_spam", secret: false,
                ("requested", count), ("added", added), ("total", _chatLog.Count));
            return added;
        }

        private int[] BuildDebugSpeakerActors()
        {
            var list = new System.Collections.Generic.List<int> { LocalActor };
            if (_session != null)
            {
                foreach (WPlayer p in _session.Players)
                {
                    if (p.ActorNumber != LocalActor) list.Add(p.ActorNumber);
                }
            }
            return list.ToArray();
        }

        public void DebugScheduleChat(long delayMs, string text)
        {
            _chatDebugDueUnixMs = NowUnixMs() + Math.Max(0L, delayMs);
            _chatDebugPendingText = text;
            WLog.Line("chat_debug_scheduled", secret: false, ("delayMs", delayMs));
        }

        public bool DebugToggleInputGateProbe()
        {
            _inputGateProbe = !_inputGateProbe;
            _inputGateLastFree = InputGate.KeysFree;
            WLog.Line("chat_debug_input_gate_probe", secret: false,
                ("state", _inputGateProbe ? "on" : "off"), ("keysFree", _inputGateLastFree));
            return _inputGateProbe;
        }

        public bool DebugToggleChatAvatarFallback()
        {
            _chatDebugAvatarFallback = !_chatDebugAvatarFallback;
            if (_chatPanel.Exists) _chatPanel.ResetView();
            WLog.Line("chat_debug_avatar_fallback", secret: false,
                ("state", _chatDebugAvatarFallback ? "on" : "off"));
            return _chatDebugAvatarFallback;
        }

        private PlayerAvatar ResolveAvatarForChatDebug(int actor)
        {
            PlayerAvatar avatar = ResolveAvatar(actor);
            return avatar != null ? avatar : PlayerAvatar.instance;
        }

        public int DebugLocalActor => LocalActor;

        public void DebugClearChat()
        {
            _chatLog.Clear();
            if (_chatPanel.Exists) _chatPanel.ResetView();
            WLog.Line("chat_debug_clear", secret: false);
        }

        public void DebugDumpChatState()
        {
            WLog.Line("chat_debug_state", secret: false,
                ("enabled", MeetingChatLogEnabled),
                ("phase", ClientPhase),
                ("count", _chatLog.Count),
                ("appended", _chatLog.AppendedTotal),
                ("dropped", _chatLog.DroppedTotal),
                ("panelBuilt", _chatPanel.Exists),
                ("panelOpen", _chatPanel.Exists && _chatPanel.IsOpen),
                ("localAlive", IsLocalAlive()),
                ("keysFree", InputGate.KeysFree));
        }

        private void TickChatDebug(long now)
        {
            if (_chatDebugDueUnixMs >= 0 && now >= _chatDebugDueUnixMs)
            {
                long due = _chatDebugDueUnixMs;
                string text = _chatDebugPendingText;
                _chatDebugDueUnixMs = -1;
                _chatDebugPendingText = null;
                bool logged = DebugInjectChat(LocalActor, null, text);
                WLog.Line("chat_debug_scheduled_fired", secret: false,
                    ("phase", ClientPhase), ("logged", logged), ("lateMs", now - due));
            }

            if (!_inputGateProbe) return;
            bool free = InputGate.KeysFree;
            if (free == _inputGateLastFree) return;
            _inputGateLastFree = free;
            WLog.Line("chat_debug_input_gate", secret: false, ("keysFree", free));
        }
    }
}
