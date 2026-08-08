using System;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Core
{
    public enum ResultRowStatus : byte
    {
        Alive = 0,

        Dead = 1,

        Executed = 2,

        Disconnected = 3,
    }

    public static class ResultModel
    {
        public static IReadOnlyList<ResultRow> Build(
            byte winningTeam,
            int[] actors,
            byte[] roles,
            IReadOnlyDictionary<int, DeathCause> deathMirror,
            Func<int, string> resolveName,
            IReadOnlyCollection<int> disconnectedActors = null)
        {
            if (actors == null) throw new ArgumentNullException(nameof(actors));
            if (roles == null) throw new ArgumentNullException(nameof(roles));
            if (resolveName == null) throw new ArgumentNullException(nameof(resolveName));
            if (actors.Length != roles.Length)
            {
                throw new ArgumentException(
                    "actors と roles の長さが一致しません（169 payload破損）。", nameof(roles));
            }

            bool voided = winningTeam == TeamCodes.VoidMatch;
            var team = (Team)winningTeam;
            var rows = new List<ResultRow>(actors.Length);
            for (int i = 0; i < actors.Length; i++)
            {
                int actor = actors[i];
                var role = (Role)roles[i];

                ResultRowStatus status = ResultRowStatus.Alive;
                if (deathMirror != null && deathMirror.TryGetValue(actor, out var cause))
                {
                    status = cause == DeathCause.Vote ? ResultRowStatus.Executed : ResultRowStatus.Dead;
                }
                else if (disconnectedActors != null && disconnectedActors.Contains(actor))
                {
                    status = ResultRowStatus.Disconnected;
                }

                bool isWinningSide = !voided && RoleDistribution.TeamOf(role) == team;

                string name = resolveName(actor);
                if (string.IsNullOrEmpty(name)) name = "Actor" + actor;

                rows.Add(new ResultRow(actor, name, role, status, isWinningSide));
            }
            return rows;
        }
    }

    public sealed class ResultRow
    {
        public ResultRow(int actorNumber, string name, Role role, ResultRowStatus status, bool isWinningSide)
        {
            ActorNumber = actorNumber;
            Name = name;
            Role = role;
            Status = status;
            IsWinningSide = isWinningSide;
        }

        public int ActorNumber { get; }

        public string Name { get; }

        public Role Role { get; }

        public ResultRowStatus Status { get; }

        public bool Alive => Status == ResultRowStatus.Alive;

        public bool Executed => Status == ResultRowStatus.Executed;

        public bool IsWinningSide { get; }
    }
}
