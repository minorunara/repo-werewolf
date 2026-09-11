using System;
using System.Collections.Generic;
using System.Globalization;
using Werewolf.Core;

namespace Werewolf.Debugging
{
    public readonly struct SecretLogEntry
    {
        public SecretLogEntry(WLogLevel level, string line)
        {
            Level = level;
            Line = line;
        }

        public WLogLevel Level { get; }
        public string Line { get; }
    }

    public sealed class SecretLogBuffer
    {
        public const int DefaultCapacity = 4000;

        private readonly object _lock = new object();
        private readonly Queue<SecretLogEntry> _lines = new Queue<SecretLogEntry>();
        private readonly int _capacity;
        private int _dropped;

        public SecretLogBuffer(int capacity = DefaultCapacity)
        {
            _capacity = capacity < 1 ? 1 : capacity;
        }

        public int Count
        {
            get { lock (_lock) return _lines.Count; }
        }

        public void Add(string line, WLogLevel level, DateTime capturedAt)
        {
            string stamped = line + " t=" +
                capturedAt.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            lock (_lock)
            {
                _lines.Enqueue(new SecretLogEntry(level, stamped));
                if (_lines.Count > _capacity)
                {
                    _lines.Dequeue();
                    _dropped++;
                }
            }
        }

        public List<SecretLogEntry> Flush(out int dropped)
        {
            lock (_lock)
            {
                dropped = _dropped;
                _dropped = 0;
                var result = new List<SecretLogEntry>(_lines);
                _lines.Clear();
                return result;
            }
        }
    }
}
