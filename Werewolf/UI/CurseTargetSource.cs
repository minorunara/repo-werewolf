using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class CurseTargetSource : ITargetSelectionSource
    {
        private readonly RolesClientState _roles;
        private readonly MeetingClientState _meeting;
        private readonly IReadOnlyList<WPlayer> _roster;
        private readonly Func<long> _nowUnixMs;
        private readonly Action<int> _sendDesignate;
        private readonly List<int> _targets = new List<int>();

        public CurseTargetSource(RolesClientState roles, MeetingClientState meeting,
                                 IReadOnlyList<WPlayer> roster, Func<long> nowUnixMs,
                                 Action<int> sendDesignate)
        {
            _roles = roles ?? throw new ArgumentNullException(nameof(roles));
            _meeting = meeting ?? throw new ArgumentNullException(nameof(meeting));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _nowUnixMs = nowUnixMs ?? throw new ArgumentNullException(nameof(nowUnixMs));
            _sendDesignate = sendDesignate ?? throw new ArgumentNullException(nameof(sendDesignate));
        }

        public IReadOnlyList<int> TargetActors
        {
            get
            {
                _targets.Clear();
                int catActor = _roles.CurseCatActor;
                int[] candidates = _roles.CurseCandidates;
                foreach (WPlayer player in _roster)
                {
                    if (player == null) continue;
                    if (player.ActorNumber == catActor) continue;
                    if (candidates != null && Array.IndexOf(candidates, player.ActorNumber) < 0) continue;
                    if (_meeting.GetRowStatus(player.ActorNumber) != RowStatus.Alive) continue;
                    _targets.Add(player.ActorNumber);
                }
                return _targets;
            }
        }

        public bool AllowSkip => false;

        public int CurrentSelection { get; private set; } = -1;

        public bool CanConfirm(int localActor)
        {
            if (localActor != _roles.CurseCatActor) return false;
            return _roles.CurseActive(_nowUnixMs());
        }

        public void Confirm(int localActor, int targetActor)
        {
            if (!CanConfirm(localActor)) return;
            if (!IsValidTarget(targetActor))
            {
                WLog.Line("curse_designate_invalid", secret: true, ("target", targetActor));
                return;
            }

            WLog.Line("curse_designate_send", secret: true, ("target", targetActor));
            _sendDesignate(targetActor);
            CurrentSelection = targetActor;
        }

        private bool IsValidTarget(int targetActor)
        {
            if (targetActor == _roles.CurseCatActor) return false;
            int[] candidates = _roles.CurseCandidates;
            if (candidates != null && Array.IndexOf(candidates, targetActor) < 0) return false;
            if (_meeting.GetRowStatus(targetActor) != RowStatus.Alive) return false;
            foreach (WPlayer player in _roster)
            {
                if (player != null && player.ActorNumber == targetActor) return true;
            }
            return false;
        }
    }
}
