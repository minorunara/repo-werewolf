using System;
using System.Collections.Generic;
using System.Globalization;

namespace Werewolf.Debugging
{
    public sealed class SecretLogBuffer
    {
        public const int DefaultCapacity = 4000;

        private readonly object _lock = new object();
        private readonly Queue<string> _lines = new Queue<string>();
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

        public void Add(string line, DateTime capturedAt)
        {
            string stamped = line + " t=" +
                capturedAt.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            lock (_lock)
            {
                _lines.Enqueue(stamped);
                if (_lines.Count > _capacity)
                {
                    _lines.Dequeue();
                    _dropped++;
                }
            }
        }

        public List<string> Flush(out int dropped)
        {
            lock (_lock)
            {
                dropped = _dropped;
                _dropped = 0;
                var result = new List<string>(_lines);
                _lines.Clear();
                return result;
            }
        }
    }
}
