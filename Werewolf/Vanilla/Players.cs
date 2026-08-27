using System.Collections.Generic;
using UnityEngine;

namespace Werewolf.Vanilla
{
    internal static class Players
    {
        public static int ActorOf(PlayerAvatar avatar) => PlayerRegistry.ActorOf(avatar);

        public static PlayerHandle Local
        {
            get
            {
                PlayerAvatar local = PlayerAvatar.instance;
                return local != null ? new PlayerHandle(ActorOf(local), local) : PlayerHandle.None;
            }
        }

        public static PlayerHandle Resolve(int actor)
        {
            GameDirector director = GameDirector.instance;
            if (director == null || director.PlayerList == null) return PlayerHandle.None;
            foreach (PlayerAvatar avatar in director.PlayerList)
            {
                if (avatar != null && ActorOf(avatar) == actor) return new PlayerHandle(actor, avatar);
            }
            return PlayerHandle.None;
        }

        public static List<PlayerHandle> All()
        {
            var result = new List<PlayerHandle>();
            GameDirector director = GameDirector.instance;
            if (director == null || director.PlayerList == null) return result;
            foreach (PlayerAvatar avatar in director.PlayerList)
            {
                if (avatar == null) continue;
                result.Add(new PlayerHandle(ActorOf(avatar), avatar));
            }
            return result;
        }

        public static bool TryGetPosition(int actor, out Vector3 position)
            => Resolve(actor).TryGetPosition(out position);

        public static bool TryGetDeathHeadPosition(PlayerHandle player, out Vector3 position)
            => TruckWarper.TryGetDeathHeadPosition(player.Avatar, out position);

        public static bool IsVanillaAlive(int actor)
        {
            PlayerHandle p = Resolve(actor);
            return p.IsValid && !GameRefs.PlayerAvatar_isDisabled(p.Avatar);
        }

        public static void KillByHost(PlayerHandle player)
        {
            if (!player.IsValid) return;
            player.Avatar.PlayerDeath(-1);
        }

        public static bool IsTalking(PlayerHandle player)
        {
            if (!player.IsValid) return false;
            if (GameRefs.PlayerAvatar_voiceChat == null || GameRefs.PlayerAvatar_voiceChatFetched == null
                || GameRefs.PlayerVoiceChat_isTalking == null)
            {
                return false;
            }
            if (!GameRefs.PlayerAvatar_voiceChatFetched(player.Avatar)) return false;
            PlayerVoiceChat vc = GameRefs.PlayerAvatar_voiceChat(player.Avatar);
            return vc != null && GameRefs.PlayerVoiceChat_isTalking(vc);
        }

        public static bool AllLoadAnimationsCompleted()
        {
            List<PlayerAvatar> avatars = SemiFunc.PlayerGetList();
            if (avatars == null || avatars.Count == 0) return false;
            if (SemiFunc.IsMultiplayer())
            {
                Photon.Realtime.Room room = Photon.Pun.PhotonNetwork.CurrentRoom;
                if (room != null && avatars.Count < room.PlayerCount) return false;
            }
            foreach (PlayerAvatar avatar in avatars)
            {
                if (avatar == null) continue;
                if (!GameRefs.PlayerAvatar_levelAnimationCompleted(avatar)) return false;
            }
            return true;
        }
    }
}
