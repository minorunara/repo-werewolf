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

        System = 2,
    }

    public readonly struct ChatLogEntry
    {
        public ChatLogEntry(int actor, string name, string text, ChatSpeaker speaker, ChatEntryKind kind,
                            string title = null, string icon = null)
        {
            Actor = actor;
            Name = name;
            Text = text;
            Speaker = speaker;
            Kind = kind;
            Title = title ?? string.Empty;
            Icon = icon ?? string.Empty;
        }

        public int Actor { get; }

        public string Name { get; }

        public string Text { get; }

        public ChatSpeaker Speaker { get; }

        public ChatEntryKind Kind { get; }

        public string Title { get; }

        public string Icon { get; }
    }

    public sealed class MeetingChatLog : IReadOnlyList<ChatLogEntry>
    {
        public const int MaxEntries = 10000;

        public const int MaxTextLength = 140;

        public const int MaxSystemTextLength = 600;

        public const int MaxTitleLength = 40;

        public const int MaxNameLength = 20;

        public const int SystemActor = int.MinValue + 1;

        public const string UnknownName = "???";

        private readonly ChatLogEntry[] _entries = new ChatLogEntry[MaxEntries];
        private int _head;
        private int _count;

        private readonly List<long> _sectionSeqs = new List<long>();

        public IReadOnlyList<ChatLogEntry> Entries => this;

        public IReadOnlyList<long> SectionSeqs => _sectionSeqs;

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

        public bool AppendSystem(string name, string title, string text, string icon = null,
                                 bool section = false)
        {
            bool added = AppendCore(SystemActor, name, text, ChatSpeaker.Alive, ChatEntryKind.System, title, icon);
            if (added && section) _sectionSeqs.Add(AppendedTotal - 1);
            return added;
        }

        private bool AppendCore(int actor, string name, string text, ChatSpeaker speaker, ChatEntryKind kind,
                                string title = null, string icon = null)
        {
            bool system = kind == ChatEntryKind.System;
            string body = system
                ? SanitizeMultiline(text, MaxSystemTextLength)
                : Sanitize(text, MaxTextLength);
            if (body.Length == 0) return false;

            string who = Sanitize(name, MaxNameLength);
            if (who.Length == 0) who = UnknownName;

            var entry = new ChatLogEntry(actor, who, body, speaker, kind,
                system ? Sanitize(title, MaxTitleLength) : null,
                system ? icon : null);
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
                while (_sectionSeqs.Count > 0 && _sectionSeqs[0] < DroppedTotal)
                {
                    _sectionSeqs.RemoveAt(0);
                }
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
            _sectionSeqs.Clear();
            Revision++;
        }

        public IEnumerator<ChatLogEntry> GetEnumerator()
        {
            for (int i = 0; i < _count; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static string Sanitize(string raw, int maxLength)
            => SanitizeCore(raw, maxLength, keepLineBreaks: false);

        public static string SanitizeMultiline(string raw, int maxLength)
            => SanitizeCore(raw, maxLength, keepLineBreaks: true);

        private static string SanitizeCore(string raw, int maxLength, bool keepLineBreaks)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (c == '<') { sb.Append('＜'); continue; }
                if (c == '\n' && keepLineBreaks) { sb.Append('\n'); continue; }
                if (c == '\r' && keepLineBreaks) continue;
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
