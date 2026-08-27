using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public readonly struct ScenarioPage
    {
        private static readonly TextId[] NoLines = new TextId[0];
        private static readonly ScenarioSignal[] NoSignals = new ScenarioSignal[0];
        private static readonly ScenarioEntryStep[] NoSteps = new ScenarioEntryStep[0];

        private readonly TextId[] _lines;
        private readonly ScenarioSignal[] _skipIf;
        private readonly ScenarioEntryStep[] _entrySteps;
        private readonly ScenarioSignal[] _forgets;
        private readonly TextId[] _nagLines;

        private ScenarioPage(TextId[] lines, ScenarioSignal gate, ScenarioAnchor anchor,
                             ScenarioSignal[] skipIf, ScenarioCue cue, ScenarioEntryStep[] entrySteps,
                             ScenarioMilestone milestone, ScenarioNeed need,
                             bool mustRead, ScenarioHighlight highlight, int meetingIndex,
                             ScenarioSignal[] forgets, TextId[] nagLines, ScenarioBlastRule blastRule,
                             int bombTarget, bool markBombTarget)
        {
            _lines = lines ?? NoLines;
            Gate = gate;
            Anchor = anchor;
            _skipIf = skipIf ?? NoSignals;
            Cue = cue;
            _entrySteps = entrySteps ?? NoSteps;
            Milestone = milestone;
            Need = need;
            MustRead = mustRead;
            Highlight = highlight;
            MeetingIndex = meetingIndex;
            _forgets = forgets ?? NoSignals;
            _nagLines = nagLines ?? NoLines;
            BlastRule = blastRule;
            BombTargetParticipantId = bombTarget;
            MarkBombTarget = markBombTarget;
        }

        public static ScenarioPage Say(ScenarioSignal gate, params TextId[] lines)
            => new ScenarioPage(lines, gate, ScenarioAnchor.BottomLeft, null,
                ScenarioCue.None, null, ScenarioMilestone.None,
                ScenarioNeed.None, false, ScenarioHighlight.None, 0, null, null,
                ScenarioBlastRule.None, 0, false);

        public static ScenarioPage Wait(ScenarioSignal gate)
            => Say(gate);

        public ScenarioPage At(ScenarioAnchor anchor) => With(anchor: anchor);

        public ScenarioPage SkipWhen(params ScenarioSignal[] signals) => With(skipIf: signals);

        public ScenarioPage WithCue(ScenarioCue cue)
        {
            if (cue == ScenarioCue.ActionNag)
            {
                throw new ArgumentException(
                    "ActionNag は WithNag(再案内文言) で立てる", nameof(cue));
            }

            return With(cue: cue);
        }

        public ScenarioPage WithNag(params TextId[] lines)
        {
            if (lines == null || lines.Length == 0)
            {
                throw new ArgumentException("再案内文言が空", nameof(lines));
            }

            return With(cue: ScenarioCue.ActionNag, nagLines: lines);
        }

        public ScenarioPage Does(ScenarioEntryAction action, int arg = 0)
        {
            ScenarioEntryStep[] current = _entrySteps ?? NoSteps;
            var steps = new ScenarioEntryStep[current.Length + 1];
            Array.Copy(current, steps, current.Length);
            steps[current.Length] = new ScenarioEntryStep(action, arg);
            return With(entrySteps: steps);
        }

        public ScenarioPage Detonation(ScenarioBlastRule rule) => With(blastRule: rule);

        public ScenarioPage BombTarget(int participantId) => With(bombTarget: participantId);

        public ScenarioPage MarksBombTarget() => With(markBombTarget: true);

        public ScenarioPage InMeeting(int meetingIndex) => With(meetingIndex: meetingIndex);

        public ScenarioPage Forgets(params ScenarioSignal[] signals) => With(forgets: signals);

        public ScenarioPage Marks(ScenarioMilestone milestone) => With(milestone: milestone);

        public ScenarioPage Requires(ScenarioNeed need) => With(need: need);

        public ScenarioPage PointsAt(ScenarioHighlight highlight) => With(highlight: highlight);

        public ScenarioPage MustBeRead() => With(mustRead: true);

        private ScenarioPage With(ScenarioAnchor? anchor = null, ScenarioSignal[] skipIf = null,
                                  ScenarioCue? cue = null, ScenarioEntryStep[] entrySteps = null,
                                  ScenarioMilestone? milestone = null, ScenarioNeed? need = null,
                                  bool? mustRead = null, ScenarioHighlight? highlight = null,
                                  int? meetingIndex = null, ScenarioSignal[] forgets = null,
                                  TextId[] nagLines = null, ScenarioBlastRule? blastRule = null,
                                  int? bombTarget = null, bool? markBombTarget = null)
            => new ScenarioPage(_lines, Gate, anchor ?? Anchor, skipIf ?? _skipIf, cue ?? Cue,
                entrySteps ?? _entrySteps,
                milestone ?? Milestone, need ?? Need,
                mustRead ?? MustRead, highlight ?? Highlight, meetingIndex ?? MeetingIndex,
                forgets ?? _forgets, nagLines ?? _nagLines, blastRule ?? BlastRule,
                bombTarget ?? BombTargetParticipantId, markBombTarget ?? MarkBombTarget);

        public IReadOnlyList<TextId> Lines => _lines ?? NoLines;

        public TextId Text => HasText ? _lines[0] : default;

        public ScenarioSignal Gate { get; }

        public bool HasText => _lines != null && _lines.Length > 0;

        public ScenarioAnchor Anchor { get; }

        public ScenarioCue Cue { get; }

        public IReadOnlyList<ScenarioEntryStep> EntrySteps => _entrySteps ?? NoSteps;

        public ScenarioBlastRule BlastRule { get; }

        public int BombTargetParticipantId { get; }

        public bool MarkBombTarget { get; }

        public bool Has(ScenarioEntryAction action, int? arg = null)
        {
            ScenarioEntryStep[] steps = _entrySteps ?? NoSteps;
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i].Action == action && (!arg.HasValue || steps[i].Arg == arg.Value)) return true;
            }
            return false;
        }

        public int MeetingIndex { get; }

        public IReadOnlyList<ScenarioSignal> ForgetsOnEntry => _forgets ?? NoSignals;

        public IReadOnlyList<TextId> NagLines => _nagLines ?? NoLines;

        public ScenarioMilestone Milestone { get; }

        public ScenarioNeed Need { get; }

        public ScenarioHighlight Highlight { get; }

        public IReadOnlyList<ScenarioSignal> SkipIf => _skipIf ?? NoSignals;

        public bool MustRead { get; }
    }
}
