using System;
using System.Collections.Generic;

namespace Werewolf.Scenarios.Core
{
    public sealed class ScenarioFlow
    {
        private static readonly int SignalCount = Enum.GetValues(typeof(ScenarioSignal)).Length;

        private readonly IReadOnlyList<ScenarioPage> _pages;
        private readonly bool[] _latched;
        private int _index;

        public ScenarioFlow(IReadOnlyList<ScenarioPage> pages)
        {
            if (pages == null) throw new ArgumentNullException(nameof(pages));
            if (pages.Count == 0) throw new ArgumentException("pages", nameof(pages));
            _pages = pages;
            _latched = new bool[SignalCount];
        }

        public int Index => _index;

        public int PageCount => _pages.Count;

        public bool Finished => _index >= _pages.Count;

        public ScenarioPage Current => _pages[Finished ? _pages.Count - 1 : _index];

        public bool IsLatched(ScenarioSignal signal) => _latched[(int)signal];

        public bool HasReached(ScenarioMilestone milestone)
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i].Milestone != milestone) continue;
                return _index >= i;
            }
            return true;
        }

        public bool HasMilestone(ScenarioMilestone milestone)
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i].Milestone == milestone) return true;
            }
            return false;
        }

        public bool HasEntered(ScenarioMilestone milestone)
            => HasMilestone(milestone) && HasReached(milestone);

        public bool AdvancesOnInput
        {
            get
            {
                if (Finished) return false;

                int next = _index + 1;
                if (next >= _pages.Count) return true;
                if (IsSkipped(_pages[_index])) return true;
                if (CanBypassEntryGate(_index, next)) return true;

                ScenarioSignal gate = _pages[next].Gate;
                return gate == ScenarioSignal.Advance || _latched[(int)gate];
            }
        }

        public bool Feed(ScenarioSignal signal)
        {
            if (Finished) return false;

            bool advance = signal == ScenarioSignal.Advance;
            bool oneShot = IsOneShot(signal);
            if (!advance && !oneShot) _latched[(int)signal] = true;

            bool pending = oneShot;

            bool moved = false;
            while (_index < _pages.Count)
            {
                int next = _index + 1;
                bool freeExit = IsSkipped(_pages[_index]);
                bool freeEntry = CanBypassEntryGate(_index, next);

                ScenarioSignal gate = next < _pages.Count ? _pages[next].Gate : ScenarioSignal.Advance;
                bool consumePending = false;
                if (gate == ScenarioSignal.Advance)
                {
                    if (!freeExit)
                    {
                        if (!advance) break;
                        advance = false;
                    }
                }
                else
                {
                    if (!_latched[(int)gate] && !freeEntry)
                    {
                        if (!pending || gate != signal) break;
                        consumePending = true;
                    }
                    if (_pages[_index].MustRead && !freeExit)
                    {
                        if (!advance) break;
                        advance = false;
                    }
                }

                if (consumePending) pending = false;
                _index = next;
                moved = true;
                if (_index < _pages.Count) ForgetOnEntry(_pages[_index]);

                if (_index < _pages.Count && _pages[_index].HasText && !IsSkipped(_pages[_index])) break;
            }
            return moved;
        }

        private bool IsSkipped(ScenarioPage page)
        {
            var skipIf = page.SkipIf;
            for (int i = 0; i < skipIf.Count; i++)
            {
                if (_latched[(int)skipIf[i]]) return true;
            }
            return false;
        }

        private bool CanBypassEntryGate(int current, int next)
        {
            if (next >= _pages.Count || !IsSkipped(_pages[next])) return false;
            return _pages[current].Cue != ScenarioCue.ChatScript
                   || _pages[next].Gate != ScenarioSignal.ChatScriptDone;
        }

        private void ForgetOnEntry(ScenarioPage page)
        {
            var forgets = page.ForgetsOnEntry;
            for (int i = 0; i < forgets.Count; i++) _latched[(int)forgets[i]] = false;
        }

        private static bool IsOneShot(ScenarioSignal signal)
            => signal == ScenarioSignal.ActionNag
               || signal == ScenarioSignal.BombOnWrongTarget
               || signal == ScenarioSignal.BombAlreadyOnTarget
               || signal == ScenarioSignal.BombDeniedDistance
               || signal == ScenarioSignal.BombDeniedTargetMoving;
    }
}
