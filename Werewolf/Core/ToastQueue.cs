using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class ToastEntry
    {
        internal ToastEntry(string message, long expiresAtUnixMs, long remainingMs)
        {
            Message = message;
            ExpiresAtUnixMs = expiresAtUnixMs;
            RemainingMs = remainingMs;
        }

        public string Message { get; }

        public long ExpiresAtUnixMs { get; }

        public long RemainingMs { get; }
    }

    public sealed class ToastQueue
    {
        private readonly List<(string Message, long ExpiresAtUnixMs)> _entries = new List<(string, long)>();

        private readonly long _durationMs;

        private const int MaxVisibleEntries = 5;

        public ToastQueue(int durationSec)
        {
            if (durationSec <= 0) throw new ArgumentOutOfRangeException(nameof(durationSec));
            _durationMs = durationSec * 1000L;
        }

        public void Push(string message, long nowUnixMs)
        {
            if (string.IsNullOrEmpty(message)) return;

            PruneExpired(nowUnixMs);
            _entries.Add((message, nowUnixMs + _durationMs));
        }

        public IReadOnlyList<ToastEntry> Visible(long nowUnixMs)
        {
            PruneExpired(nowUnixMs);

            var result = new List<ToastEntry>();
            for (int i = _entries.Count - 1; i >= 0 && result.Count < MaxVisibleEntries; i--)
            {
                var (message, expiresAt) = _entries[i];
                long remaining = expiresAt - nowUnixMs;
                if (remaining < 0) remaining = 0;
                result.Add(new ToastEntry(message, expiresAt, remaining));
            }
            return result;
        }

        private void PruneExpired(long nowUnixMs)
        {
            _entries.RemoveAll(e => e.ExpiresAtUnixMs <= nowUnixMs);
        }
    }
}
