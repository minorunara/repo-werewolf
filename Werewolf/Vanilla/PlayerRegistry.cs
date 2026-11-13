#nullable enable
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    public sealed class PlayerRegistry
    {
        public List<WPlayer> BuildRealPlayers()
        {
            var result = new List<WPlayer>();
            var director = GameDirector.instance;
            if (director == null || director.PlayerList == null)
            {
                WLog.Line("registry_build", secret: false, ("status", "no_gamedirector"));
                return result;
            }

            HashSet<int>? currentRoomActors = null;
            if (SemiFunc.IsMultiplayer())
            {
                Photon.Realtime.Room room = Photon.Pun.PhotonNetwork.CurrentRoom;
                if (room?.Players == null) return result;
                currentRoomActors = new HashSet<int>(room.Players.Keys);
            }

            var addedActors = new HashSet<int>();
            foreach (PlayerAvatar avatar in director.PlayerList)
            {
                if (avatar == null) continue;
                int actor = ResolveActor(avatar);
                if (actor <= 0 || (currentRoomActors != null && !currentRoomActors.Contains(actor))
                    || !addedActors.Add(actor)) continue;
                var player = new WPlayer
                {
                    ActorNumber = actor,
                    Name = SemiFunc.PlayerGetName(avatar),
                    SteamId = SemiFunc.PlayerGetSteamID(avatar),
                    IsBot = false,
                };
                result.Add(player);
            }
            return result;
        }

        public int ResolveActor(PlayerAvatar avatar) => ActorOf(avatar);

        public static int ActorOf(PlayerAvatar avatar)
        {
            if (!SemiFunc.IsMultiplayer()) return 1;
            if (avatar != null && avatar.photonView != null && avatar.photonView.Owner != null)
            {
                return avatar.photonView.Owner.ActorNumber;
            }
            return -1;
        }

        public bool IsDeadSet(PlayerAvatar avatar)
        {
            if (avatar == null) return false;
            return avatar.deadSet;
        }
    }
}
