using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class CurseResolution
    {
        public const int NoVictim = int.MinValue;

        public CurseResolution(int victimActor, bool wasDesignated)
        {
            VictimActor = victimActor;
            WasDesignated = wasDesignated;
        }

        public int VictimActor { get; }

        public bool HasVictim => VictimActor != NoVictim;

        public bool WasDesignated { get; }
    }

    public sealed class CurseSession
    {
        private int _designatedActor = CurseResolution.NoVictim;
        private bool _resolved;

        private readonly HashSet<int> _voters;

        public CurseSession(int catActor, long resolveAtUnixMs, IEnumerable<int> voterActors = null)
        {
            CatActor = catActor;
            ResolveAtUnixMs = resolveAtUnixMs;
            _voters = voterActors != null ? new HashSet<int>(voterActors) : null;
        }

        public int CatActor { get; }

        public long ResolveAtUnixMs { get; }

        public bool Resolved => _resolved;

        public bool Designate(int senderActor, int targetActor, long nowUnixMs)
        {
            if (_resolved || nowUnixMs >= ResolveAtUnixMs)
            {
                WLog.Line("curse_designate_rejected", secret: true,
                    ("sender", senderActor), ("reason", "expired"));
                return false;
            }
            if (senderActor != CatActor)
            {
                WLog.Line("curse_designate_rejected", secret: true,
                    ("sender", senderActor), ("reason", "not_cat"));
                return false;
            }
            if (_voters != null && !_voters.Contains(targetActor))
            {
                WLog.Line("curse_designate_rejected", secret: true,
                    ("sender", senderActor), ("reason", "not_voter"));
                return false;
            }

            _designatedActor = targetActor;
            WLog.Line("curse_designate", secret: true,
                ("target", targetActor), ("deadline", ResolveAtUnixMs));
            return true;
        }

        public CurseResolution TryResolve(
            long nowUnixMs, IReadOnlyList<WPlayer> players, bool informantEstablished, Random rng)
        {
            if (_resolved || nowUnixMs < ResolveAtUnixMs) return null;
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            _resolved = true;

            if (_designatedActor != CurseResolution.NoVictim)
            {
                foreach (var p in players)
                {
                    if (p.ActorNumber == _designatedActor && p.Alive)
                    {
                        return new CurseResolution(_designatedActor, wasDesignated: true);
                    }
                }
            }

            var candidates = new List<int>();
            var villagerVoters = _voters != null ? new List<int>() : null;
            foreach (var p in players)
            {
                if (!p.Alive || p.ActorNumber == CatActor) continue;
                if (_voters != null)
                {
                    if (!_voters.Contains(p.ActorNumber)) continue;
                    candidates.Add(p.ActorNumber);
                    if (RoleDistribution.TeamOf(p.Role) == Team.Villagers)
                        villagerVoters.Add(p.ActorNumber);
                }
                else if (informantEstablished)
                {
                    if (RoleDistribution.TeamOf(p.Role) == Team.Villagers)
                        candidates.Add(p.ActorNumber);
                }
                else
                {
                    candidates.Add(p.ActorNumber);
                }
            }

            if (_voters != null && informantEstablished && villagerVoters.Count > 0)
            {
                candidates = villagerVoters;
            }

            if (candidates.Count == 0)
            {
                WLog.Line("curse_resolve_no_candidate", secret: true,
                    ("informant", informantEstablished), ("restricted", _voters != null));
                return new CurseResolution(CurseResolution.NoVictim, wasDesignated: false);
            }

            int victim = candidates[rng.Next(candidates.Count)];
            return new CurseResolution(victim, wasDesignated: false);
        }
    }
}
