using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Debugging
{
    public readonly struct RepeatedLogDecision
    {
        public RepeatedLogDecision(bool emitCurrent, RepeatedLogSummary summary)
        {
            EmitCurrent = emitCurrent;
            Summary = summary;
        }

        public bool EmitCurrent { get; }
        public RepeatedLogSummary Summary { get; }
        public bool HasSummary => Summary.SuppressedCount > 0;
    }

    public readonly struct RepeatedLogSummary
    {
        public RepeatedLogSummary(string kind, WLogLevel level, bool secret, int suppressedCount)
        {
            Kind = kind;
            Level = level;
            Secret = secret;
            SuppressedCount = suppressedCount;
        }

        public string Kind { get; }
        public WLogLevel Level { get; }
        public bool Secret { get; }
        public int SuppressedCount { get; }

        public string ToLine()
        {
            string line = $"{WLog.Prefix} log_repeats_suppressed kind={Kind} count={SuppressedCount}";
            return Secret ? line + " secret=1" : line;
        }
    }

    public sealed class RepeatedLogLimiter
    {
        public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(10);

        private sealed class State
        {
            public DateTime LastEmittedAt;
            public int SuppressedCount;
            public string Kind;
            public WLogLevel Level;
            public bool Secret;
        }

        private readonly object _lock = new object();
        private readonly Dictionary<string, State> _states = new Dictionary<string, State>();
        private readonly TimeSpan _window;

        public RepeatedLogLimiter(TimeSpan? window = null)
        {
            TimeSpan requested = window ?? DefaultWindow;
            _window = requested > TimeSpan.Zero ? requested : DefaultWindow;
        }

        public RepeatedLogDecision Observe(
            string line,
            WLogLevel level,
            bool secret,
            DateTime observedAt)
        {
            string kind = WLog.KindOf(line);
            string signature = string.IsNullOrEmpty(line) ? kind : line;
            string key = ((int)level) + ":" + (secret ? "1:" : "0:") + signature;

            lock (_lock)
            {
                if (!_states.TryGetValue(key, out State state))
                {
                    _states.Add(key, new State
                    {
                        LastEmittedAt = observedAt,
                        Kind = kind,
                        Level = level,
                        Secret = secret,
                    });
                    return new RepeatedLogDecision(emitCurrent: true, default);
                }

                if (observedAt < state.LastEmittedAt || observedAt - state.LastEmittedAt >= _window)
                {
                    var summary = new RepeatedLogSummary(
                        state.Kind, state.Level, state.Secret, state.SuppressedCount);
                    state.LastEmittedAt = observedAt;
                    state.SuppressedCount = 0;
                    return new RepeatedLogDecision(emitCurrent: true, summary);
                }

                state.SuppressedCount++;
                return new RepeatedLogDecision(emitCurrent: false, default);
            }
        }

        public List<RepeatedLogSummary> DrainAndReset()
        {
            lock (_lock)
            {
                var summaries = new List<RepeatedLogSummary>();
                foreach (State state in _states.Values)
                {
                    if (state.SuppressedCount > 0)
                    {
                        summaries.Add(new RepeatedLogSummary(
                            state.Kind, state.Level, state.Secret, state.SuppressedCount));
                    }
                }
                _states.Clear();
                return summaries;
            }
        }
    }
}
