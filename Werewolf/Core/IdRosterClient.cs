using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class IdRosterClient
    {
        private readonly Dictionary<int, int> _idByActor = new Dictionary<int, int>();

        public bool HasRoster => _idByActor.Count > 0;

        public void Apply(int[] actorsByIdOrder)
        {
            if (actorsByIdOrder == null || actorsByIdOrder.Length == 0)
            {
                WLog.Line("id_roster_rejected", secret: false, ("reason", "empty"));
                return;
            }

            var next = new Dictionary<int, int>(actorsByIdOrder.Length);
            for (int i = 0; i < actorsByIdOrder.Length; i++)
            {
                int actor = actorsByIdOrder[i];
                if (next.ContainsKey(actor))
                {
                    WLog.Line("id_roster_rejected", secret: false,
                        ("reason", "duplicate"), ("actor", actor));
                    return;
                }
                next.Add(actor, i + 1);
            }

            _idByActor.Clear();
            foreach (var pair in next)
            {
                _idByActor.Add(pair.Key, pair.Value);
            }
            WLog.Line("id_roster_applied", secret: false, ("count", _idByActor.Count));
        }

        public int IdOf(int actorNumber)
        {
            return _idByActor.TryGetValue(actorNumber, out int id) ? id : 0;
        }

        public IReadOnlyDictionary<int, int> Entries => _idByActor;

        public void Reset()
        {
            _idByActor.Clear();
        }
    }
}
