using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public readonly struct ScenarioCorpseHint
    {
        private static readonly TextId[] NoLines = new TextId[0];

        private readonly TextId[] _lines;

        public ScenarioCorpseHint(int participantId, ScenarioSignal after, ScenarioSignal until,
                                  long delayMs, TextId[] lines)
        {
            ParticipantId = participantId;
            After = after;
            Until = until;
            DelayMs = delayMs;
            _lines = lines ?? NoLines;
        }

        public int ParticipantId { get; }

        public ScenarioSignal After { get; }

        public ScenarioSignal Until { get; }

        public long DelayMs { get; }

        public IReadOnlyList<TextId> Lines => _lines ?? NoLines;

        public bool Enabled => ParticipantId != 0;
    }

    public sealed class ScenarioCorpseHintClock
    {
        private bool _armed;
        private long _armedAtMs;
        private bool _fired;
        private bool _done;

        public bool MarkerShown => _fired && !_done;

        public bool Done => _done;

        public bool Tick(long now, bool started, bool ended, long delayMs)
        {
            if (_done) return false;
            if (ended)
            {
                _done = true;
                return false;
            }
            if (!started) return false;

            if (!_armed)
            {
                _armed = true;
                _armedAtMs = now;
            }
            if (_fired || now - _armedAtMs < delayMs) return false;

            _fired = true;
            return true;
        }
    }
}
