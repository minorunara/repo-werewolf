using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Werewolf.Core
{
    public enum ChatSpeaker : byte
    {
        Alive = 0,

        Dead = 1,
    }

    public enum ChatEntryKind : byte
    {
        Message = 0,

        Vote = 1,
    }

    public readonly struct ChatLogEntry
    {
        public ChatLogEntry(int actor, string name, string text, ChatSpeaker speaker, ChatEntryKind kind)
        {
            Actor = actor;
            Name = name;
            Text = text;
            Speaker = speaker;
            Kind = kind;
        }

        public int Actor { get; }

        public string Name { get; }

        public string Text { get; }

        public ChatSpeaker Speaker { get; }

        public ChatEntryKind Kind { get; }
    }

    public sealed class MeetingChatLog : IReadOnlyList<ChatLogEntry>
    {
        public const int MaxEntries = 10000;

        public const int MaxTextLength = 140;

        public const int MaxNameLength = 20;

        public const string UnknownName = "???";

        private readonly ChatLogEntry[] _entries = new ChatLogEntry[MaxEntries];
        private int _head;
        private int _count;

        public IReadOnlyList<ChatLogEntry> Entries => this;

        public int Count => _count;

        public ChatLogEntry this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
                return _entries[(_head + index) % MaxEntries];
            }
        }

        public int Revision { get; private set; }

        public long AppendedTotal { get; private set; }

        public long DroppedTotal { get; private set; }

        public bool Append(int actor, string name, string text, ChatSpeaker speaker)
            => AppendCore(actor, name, text, speaker, ChatEntryKind.Message);

        public bool AppendVote(int actor, string name, string text)
            => AppendCore(actor, name, text, ChatSpeaker.Alive, ChatEntryKind.Vote);

        private bool AppendCore(int actor, string name, string text, ChatSpeaker speaker, ChatEntryKind kind)
        {
            string body = Sanitize(text, MaxTextLength);
            if (body.Length == 0) return false;

            string who = Sanitize(name, MaxNameLength);
            if (who.Length == 0) who = UnknownName;

            var entry = new ChatLogEntry(actor, who, body, speaker, kind);
            if (_count < MaxEntries)
            {
                int tail = (_head + _count) % MaxEntries;
                _entries[tail] = entry;
                _count++;
            }
            else
            {
                _entries[_head] = entry;
                _head = (_head + 1) % MaxEntries;
                DroppedTotal++;
            }
            AppendedTotal++;
            Revision++;
            return true;
        }

        public void Clear()
        {
            if (_count == 0) return;
            DroppedTotal += _count;
            for (int i = 0; i < _count; i++)
            {
                _entries[(_head + i) % MaxEntries] = default;
            }
            _head = 0;
            _count = 0;
            Revision++;
        }

        public IEnumerator<ChatLogEntry> GetEnumerator()
        {
            for (int i = 0; i < _count; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static string Sanitize(string raw, int maxLength)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (c == '<') { sb.Append('＜'); continue; }
                if (c == '\n' || c == '\r' || c == '\t') { sb.Append(' '); continue; }
                if (char.IsControl(c)) continue;
                sb.Append(c);
            }

            string s = sb.ToString().Trim();
            if (maxLength > 0 && s.Length > maxLength)
            {
                s = s.Substring(0, maxLength) + "…";
            }
            return s;
        }
    }
}
