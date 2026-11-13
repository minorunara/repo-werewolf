#nullable enable
using System.Collections.Generic;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class Players
    {
        public static int ActorOf(PlayerAvatar avatar) => PlayerRegistry.ActorOf(avatar);

        public static PlayerHandle Local
        {
            get
            {
                PlayerAvatar? local = PlayerAvatar.instance;
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
            PlayerAvatar? avatar = p.Avatar;
            return avatar != null && !avatar.isDisabled;
        }

        public static void KillByHost(PlayerHandle player)
        {
            PlayerAvatar? avatar = player.Avatar;
            if (avatar == null) return;
            avatar.PlayerDeath(-1);
        }

        public static bool IsTalking(PlayerHandle player)
        {
            PlayerAvatar? avatar = player.Avatar;
            if (avatar == null) return false;
            if (!avatar.voiceChatFetched) return false;
            PlayerVoiceChat vc = avatar.voiceChat;
            return vc != null && vc.isTalking;
        }

        public static bool AllLoadAnimationsCompleted()
        {
            List<PlayerAvatar>? avatars = SemiFunc.PlayerGetList();
            if (avatars == null || avatars.Count == 0) return false;
            if (SemiFunc.IsMultiplayer())
            {
                Photon.Realtime.Room? room = Photon.Pun.PhotonNetwork.CurrentRoom;
                if (room?.Players == null || room.Players.Count == 0) return false;

                var currentActors = new List<int>(room.Players.Keys);
                var states = new List<ParticipantLoadState>(avatars.Count);
                foreach (PlayerAvatar avatar in avatars)
                {
                    if (avatar == null) continue;
                    states.Add(new ParticipantLoadState(
                        ActorOf(avatar), avatar.levelAnimationCompleted));
                }
                return PreplayRosterGate.AllCurrentParticipantsLoaded(currentActors, states);
            }
            foreach (PlayerAvatar avatar in avatars)
            {
                if (avatar == null) continue;
                if (!avatar.levelAnimationCompleted) return false;
            }
            return true;
        }

        public static Dictionary<int, string> RoomRosterNames()
        {
            var result = new Dictionary<int, string>();
            Photon.Realtime.Room? room = Photon.Pun.PhotonNetwork.CurrentRoom;
            if (room?.Players == null) return result;
            foreach (var pair in room.Players)
            {
                Photon.Realtime.Player player = pair.Value;
                if (player == null || string.IsNullOrEmpty(player.NickName)) continue;
                result[pair.Key] = player.NickName;
            }
            return result;
        }
    }
}
