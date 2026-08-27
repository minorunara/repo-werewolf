using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public readonly struct ScenarioWolfSample
    {
        public ScenarioWolfSample(int valuablesDestroyed, PerkFlags unlocked, bool wolfMode,
                                  bool recordOn, int beaconCharges)
        {
            ValuablesDestroyed = valuablesDestroyed;
            Unlocked = unlocked;
            WolfMode = wolfMode;
            RecordOn = recordOn;
            BeaconCharges = beaconCharges;
        }

        public int ValuablesDestroyed { get; }

        public PerkFlags Unlocked { get; }

        public bool WolfMode { get; }

        public bool RecordOn { get; }

        public int BeaconCharges { get; }
    }

    public sealed class ScenarioWolfWatch
    {
        private static readonly (PerkId Perk, ScenarioSignal Signal)[] PerkSignals =
        {
            (PerkId.InfiniteStamina, ScenarioSignal.PerkStaminaUnlocked),
            (PerkId.InfiniteJump, ScenarioSignal.PerkJumpUnlocked),
            (PerkId.EnemyIgnore, ScenarioSignal.PerkEnemyIgnoreUnlocked),
            (PerkId.NaturalHeal, ScenarioSignal.PerkHealUnlocked),
        };

        private ScenarioWolfSample _prev;
        private bool _primed;

        public bool Primed => _primed;

        public void Observe(ScenarioWolfSample sample, List<ScenarioSignal> signals)
        {
            if (signals == null) return;
            signals.Clear();

            if (!_primed)
            {
                _primed = true;
                _prev = sample;
                return;
            }

            if (sample.ValuablesDestroyed > _prev.ValuablesDestroyed)
            {
                signals.Add(sample.RecordOn
                    ? ScenarioSignal.ValuableDestroyed
                    : ScenarioSignal.StealthDestroyed);
            }

            if (_prev.RecordOn && !sample.RecordOn) signals.Add(ScenarioSignal.RecordTurnedOff);
            if (!_prev.WolfMode && sample.WolfMode) signals.Add(ScenarioSignal.WolfFormTurnedOn);
            if (sample.BeaconCharges < _prev.BeaconCharges) signals.Add(ScenarioSignal.BeaconUsed);

            foreach ((PerkId perk, ScenarioSignal signal) in PerkSignals)
            {
                if (PerkFlagsUtil.Has(_prev.Unlocked, perk)) continue;
                if (!PerkFlagsUtil.Has(sample.Unlocked, perk)) continue;
                signals.Add(signal);
            }

            _prev = sample;
        }
    }
}
