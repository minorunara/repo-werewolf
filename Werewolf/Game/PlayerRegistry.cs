using System;
using System.Collections.Generic;
using HarmonyLib;
using Werewolf.Core;

namespace Werewolf.Game
{
    public sealed class PlayerRegistry
    {
        private AccessTools.FieldRef<PlayerAvatar, bool> _deadSetRef;

        public bool Available { get; private set; }

        public void Initialize()
        {
            _deadSetRef = GameRefs.PlayerAvatar_deadSet;
            Available = _deadSetRef != null;
            WLog.Line("registry_init", secret: false,
                ("status", Available ? "ok" : "field_resolve_failed"));
        }

        public List<WPlayer> BuildRealPlayers()
        {
            var result = new List<WPlayer>();
            var director = GameDirector.instance;
            if (director == null || director.PlayerList == null)
            {
                WLog.Line("registry_build", secret: false, ("status", "no_gamedirector"));
                return result;
            }

            foreach (PlayerAvatar avatar in director.PlayerList)
            {
                if (avatar == null) continue;
                var player = new WPlayer
                {
                    ActorNumber = ResolveActor(avatar),
                    Name = SemiFunc.PlayerGetName(avatar),
                    SteamId = SemiFunc.PlayerGetSteamID(avatar),
                    IsBot = false,
                };
                result.Add(player);
            }
            return result;
        }

        public int ResolveActor(PlayerAvatar avatar)
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
            if (!Available || _deadSetRef == null || avatar == null) return false;
            return _deadSetRef(avatar);
        }
    }
}
