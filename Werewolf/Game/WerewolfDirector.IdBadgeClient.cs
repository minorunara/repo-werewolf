using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private readonly IdBadgePresenter _idBadgePresenter = new IdBadgePresenter();

        private void TickIdBadges()
        {
            EnsurePanelBuilt(_idBadgePresenter);
            bool visible = IdRoster.HasRoster
                && (_clientPhase == GamePhase.Play || _clientPhase == GamePhase.Meeting);
            _idBadgePresenter.Tick(visible, IdRoster.Entries, LocalActor,
                MarkedTeammateRole, ResolveIdBadgeWorldPos);
        }

        private Vector3? ResolveIdBadgeWorldPos(int actor)
        {
            if (IsDeadActorClient(actor)) return null;
            return ResolveBodyWorldPos(actor);
        }
    }
}
