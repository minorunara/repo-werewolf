using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum DisclosureKind
    {
        BlackCatSelfAwareness = 0,

        BlackCatSeesWerewolves = 1,
    }

    public enum DisclosureType : byte
    {
        RoleNotice = 0,

        SelfRoleReveal = 1,

        TeammatesReveal = 2,
    }

    public sealed class Disclosure
    {
        public Disclosure(DisclosureType type, int[] targetActors, Role shownRole,
                          int[] werewolfActors, byte[] werewolfActorRoles = null)
        {
            Type = type;
            TargetActors = targetActors;
            ShownRole = shownRole;
            WerewolfActors = werewolfActors;
            WerewolfActorRoles = werewolfActorRoles;
        }

        public DisclosureType Type { get; }

        public int[] TargetActors { get; }

        public Role ShownRole { get; }

        public int[] WerewolfActors { get; }

        public byte[] WerewolfActorRoles { get; }
    }

    public sealed class DisclosureManager
    {
        private static readonly IReadOnlyList<Disclosure> Empty = Array.Empty<Disclosure>();

        private readonly IReadOnlyList<WPlayer> _players;
        private readonly long _selfAwarenessDueUnixMs;
        private readonly bool _selfAwarenessImmediate;

        private bool _initialIssued;
        private bool _selfAwarenessIssued;
        private bool _werewolvesShownToBlackCat;

        public bool SelfAwarenessIssued => _selfAwarenessIssued;

        public DisclosureManager(IReadOnlyList<WPlayer> players, long gameStartUnixMs, int blackCatRevealDelaySec)
        {
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _selfAwarenessDueUnixMs = gameStartUnixMs + blackCatRevealDelaySec * 1000L;
            _selfAwarenessImmediate = blackCatRevealDelaySec <= 0;
        }

        public IReadOnlyList<Disclosure> IssueInitialDisclosures()
        {
            if (_initialIssued) return Empty;
            _initialIssued = true;

            var result = new List<Disclosure>(_players.Count + 2);

            foreach (var player in _players)
            {
                bool immediateCat = player.Role == Role.BlackCat && _selfAwarenessImmediate;
                var shown = player.Role == Role.BlackCat && !immediateCat ? Role.Villager : player.Role;
                result.Add(new Disclosure(
                    DisclosureType.RoleNotice, new[] { player.ActorNumber }, shown, null));
                if (immediateCat) _selfAwarenessIssued = true;
            }

            CollectTeamRoster(out int[] roster, out byte[] rosterRoles);
            if (roster.Length >= 2)
            {
                foreach (int teammate in roster)
                {
                    result.Add(new Disclosure(
                        DisclosureType.TeammatesReveal, new[] { teammate }, Role.Werewolf,
                        roster, rosterRoles));
                }
            }

            return result;
        }

        public IReadOnlyList<Disclosure> Tick(long nowUnixMs)
        {
            if (nowUnixMs < _selfAwarenessDueUnixMs) return Empty;
            return IssueSelfAwareness();
        }

        public IReadOnlyList<Disclosure> NotifyCondition(DisclosureKind kind)
        {
            switch (kind)
            {
                case DisclosureKind.BlackCatSelfAwareness:
                    return IssueSelfAwareness();

                case DisclosureKind.BlackCatSeesWerewolves:
                    return IssueWerewolvesToBlackCat();

                default:
                    WLog.Line("drop", secret: false, ("reason", "unknown_disclosure_kind"), ("kind", (int)kind));
                    return Empty;
            }
        }

        private IReadOnlyList<Disclosure> IssueSelfAwareness()
        {
            if (_selfAwarenessIssued) return Empty;

            var result = new List<Disclosure>(1);
            foreach (var player in _players)
            {
                if (player.Role != Role.BlackCat) continue;
                result.Add(new Disclosure(
                    DisclosureType.SelfRoleReveal, new[] { player.ActorNumber }, Role.BlackCat, null));
            }

            if (result.Count == 0) return Empty;

            _selfAwarenessIssued = true;
            return result;
        }

        private IReadOnlyList<Disclosure> IssueWerewolvesToBlackCat()
        {
            if (_werewolvesShownToBlackCat) return Empty;

            CollectTeamRoster(out int[] roster, out byte[] rosterRoles);

            var result = new List<Disclosure>(1);
            foreach (var player in _players)
            {
                if (player.Role != Role.BlackCat) continue;
                result.Add(new Disclosure(
                    DisclosureType.TeammatesReveal, new[] { player.ActorNumber }, Role.Werewolf,
                    roster, rosterRoles));
            }

            if (result.Count == 0) return Empty;

            _werewolvesShownToBlackCat = true;
            return result;
        }

        private void CollectTeamRoster(out int[] actors, out byte[] roles)
        {
            var acts = new List<int>();
            var rs = new List<byte>();
            foreach (var player in _players)
            {
                if (player.Role == Role.Werewolf || player.Role == Role.Bomber)
                {
                    acts.Add(player.ActorNumber);
                    rs.Add((byte)player.Role);
                }
            }
            actors = acts.ToArray();
            roles = rs.ToArray();
        }
    }
}
