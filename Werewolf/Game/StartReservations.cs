using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Game
{
    internal sealed class StartReservations
    {
        public List<WPlayer> Bots { get; } = new List<WPlayer>();

        public Dictionary<int, Role> ForcedRoles { get; } = new Dictionary<int, Role>();

        public bool ForcedPeaceful { get; set; }

        public void ClearRoles()
        {
            ForcedRoles.Clear();
            ForcedPeaceful = false;
        }

        public void Clear()
        {
            Bots.Clear();
            ClearRoles();
        }
    }
}
