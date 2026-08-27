using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public readonly struct ScenarioShamanSample
    {
        public ScenarioShamanSample(bool senseArmed, bool senseCoolingDown, bool ghostVisible,
                                    ShamanStormTier stormTier)
        {
            SenseArmed = senseArmed;
            SenseCoolingDown = senseCoolingDown;
            GhostVisible = ghostVisible;
            StormTier = stormTier;
        }

        public bool SenseArmed { get; }

        public bool SenseCoolingDown { get; }

        public bool GhostVisible { get; }

        public ShamanStormTier StormTier { get; }
    }

    public sealed class ScenarioShamanWatch
    {
        private ScenarioShamanSample _prev;
        private bool _primed;

        public bool Primed => _primed;

        public void Observe(ScenarioShamanSample sample, List<ScenarioSignal> signals)
        {
            if (signals == null) return;
            signals.Clear();

            if (!_primed)
            {
                _primed = true;
                _prev = sample;
                return;
            }

            if (!_prev.GhostVisible && sample.GhostVisible) signals.Add(ScenarioSignal.ShamanGhostFired);
            if (_prev.StormTier == ShamanStormTier.None && sample.StormTier != ShamanStormTier.None)
            {
                signals.Add(ScenarioSignal.ShamanStormEntered);
            }

            _prev = sample;
        }
    }
}
