using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class BeaconEffect
    {
        private const float InvestigateRadius = 100f;

        private sealed class ActiveSequence
        {
            public BeaconPulseSequence Sequence;
            public Vector3 Position;
            public int Fired;
        }

        private readonly List<ActiveSequence> _active = new List<ActiveSequence>();
        private readonly BeaconSummonGate _summonGate = new BeaconSummonGate();

        public void Trigger(PlayerAvatar requesterAvatar, long nowUnixMs, int summonCooldownSec)
        {
            if (requesterAvatar == null)
            {
                WLog.Line("beacon_effect_skipped", secret: true, ("reason", "no_avatar"));
                return;
            }

            Vector3 position = requesterAvatar.transform.position;
            _active.Add(new ActiveSequence
            {
                Sequence = new BeaconPulseSequence(nowUnixMs),
                Position = position,
                Fired = 0,
            });
            WLog.Line("beacon_effect", secret: true,
                ("x", (int)position.x), ("y", (int)position.y), ("z", (int)position.z));
            TrySummon(nowUnixMs, summonCooldownSec);
            Tick(nowUnixMs);
        }

        private void TrySummon(long nowUnixMs, int cooldownSec)
        {
            var director = EnemyDirector.instance;
            if (director == null || director.enemiesSpawned == null) return;

            var targets = new List<EnemyParent>();
            foreach (EnemyParent ep in director.enemiesSpawned)
            {
                if (ep == null) continue;
                try
                {
                    if (!GameRefs.EnemyParent_Spawned(ep)) targets.Add(ep);
                }
                catch (System.Exception e)
                {
                    WLog.Line("beacon_summon_error", secret: true, ("err", e.Message));
                }
            }
            if (targets.Count == 0)
            {
                WLog.Line("beacon_summon_skipped", secret: true, ("reason", "no_despawned"));
                return;
            }
            if (!_summonGate.TryOpen(nowUnixMs, cooldownSec))
            {
                WLog.Line("beacon_summon_skipped", secret: true, ("reason", "team_cooldown"));
                return;
            }
            for (int i = 0; i < targets.Count; i++)
            {
                try
                {
                    targets[i].DespawnedTimerSet(BeaconSummonPlan.ClampSeconds(i), true);
                }
                catch (System.Exception e)
                {
                    WLog.Line("beacon_summon_error", secret: true, ("err", e.Message));
                }
            }
            WLog.Line("beacon_summon", secret: true, ("count", targets.Count));
        }

        public void ResetSummonGate()
        {
            _summonGate.Reset();
        }

        public void Tick(long nowUnixMs)
        {
            if (_active.Count == 0) return;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveSequence seq = _active[i];
                int due = seq.Sequence.DuePulses(nowUnixMs);
                while (seq.Fired < due)
                {
                    seq.Fired++;
                    FirePulse(seq.Position, seq.Fired);
                }
                if (seq.Fired >= BeaconPulseSequence.PulseCount) _active.RemoveAt(i);
            }
        }

        public void CancelAll(string reason)
        {
            if (_active.Count == 0) return;
            WLog.Line("beacon_pulse_cancel", secret: true,
                ("reason", reason), ("sequences", _active.Count));
            _active.Clear();
        }

        private void FirePulse(Vector3 position, int pulseIndex)
        {
            var director = EnemyDirector.instance;
            if (director == null)
            {
                WLog.Line("beacon_effect_skipped", secret: true, ("reason", "no_enemy_director"));
                return;
            }
            director.SetInvestigate(position, InvestigateRadius);
            WLog.Line("beacon_pulse", secret: true, ("n", pulseIndex));
        }
    }
}
