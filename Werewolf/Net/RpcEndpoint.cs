using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Net
{
    public sealed class RpcEndpoint : MonoBehaviour
    {
        public static event Action<InboundMessage> OnMessage;

        private static readonly InboundDropThrottle DropThrottle = new InboundDropThrottle();

        [PunRPC]
        private void WerewolfRpc(byte protocolVersion, byte messageType, object[] payload,
            PhotonMessageInfo info)
        {
            try
            {
                if (info.Sender == null)
                {
                    WLog.Line("drop", secret: false,
                        ("reason", "no_sender"), ("code", (int)messageType));
                    return;
                }

                ClientState state = PhotonNetwork.NetworkClientState;
                if (state != ClientState.Joined)
                {
                    WLog.Line("drop_disconnecting", secret: false,
                        ("code", (int)messageType), ("state", state.ToString()));
                    return;
                }

                int senderActor = info.Sender.ActorNumber;
                int masterActor = PhotonNetwork.MasterClient != null
                    ? PhotonNetwork.MasterClient.ActorNumber : 0;
                if (!InboundAuthority.IsAcceptable(messageType, senderActor, masterActor))
                {
                    long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (DropThrottle.TryTake(nowMs, out int suppressed))
                    {
                        WLog.Line("drop_not_master", secret: false,
                            ("code", (int)messageType), ("sender", senderActor),
                            ("master", masterActor), ("suppressed", suppressed));
                    }
                    return;
                }

                if (!RpcProtocol.TryOpen(protocolVersion, messageType, payload,
                        senderActor, out InboundMessage message, out _))
                    return;

                WLog.Event("recv", messageType, "in", message.Payload,
                    secret: MessageCodes.IsSecret(messageType));
                OnMessage?.Invoke(message);
            }
            catch
            {
            }
        }
    }
}
