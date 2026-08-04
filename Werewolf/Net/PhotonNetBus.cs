using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Werewolf.Core;

namespace Werewolf.Net
{
    public sealed class PhotonNetBus : INetBus, IOnEventCallback, IInRoomCallbacks, IConnectionCallbacks
    {
        public event Action<InboundMessage> OnReceived;

        public event Action<int> OnPlayerLeft;

        public event Action OnLocalDisconnected;

        public event Action<Hashtable> OnRoomPropertiesChanged;

        public int LocalActorNumber =>
            PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : -1;

        public void Register() => PhotonNetwork.AddCallbackTarget(this);

        public void Unregister() => PhotonNetwork.RemoveCallbackTarget(this);

        public bool SendToAll(byte code, object[] payload) =>
            Raise(code, payload, "all",
                new RaiseEventOptions { Receivers = ReceiverGroup.All }, null);

        public bool SendToActors(byte code, object[] payload, int[] targetActors) =>
            Raise(code, payload, "actors",
                new RaiseEventOptions { TargetActors = targetActors }, targetActors);

        public bool SendToMaster(byte code, object[] payload) =>
            Raise(code, payload, "master",
                new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, null);

        private bool Raise(byte code, object[] payload, string target,
            RaiseEventOptions options, int[] targetActors)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                WLog.Line("send_fail", secret: false,
                    ("reason", "not_in_room"), ("code", (int)code));
                return false;
            }

            object[] normalized;
            try
            {
                normalized = NetPayload.Build(payload);
            }
            catch (Exception e)
            {
                WLog.Line("send_fail", secret: false,
                    ("reason", "bad_payload"), ("code", (int)code), ("err", e.Message));
                return false;
            }

            bool sent = PhotonNetwork.RaiseEvent(code, normalized, options, SendOptions.SendReliable);
            WLog.Event("send", code, target, normalized,
                secret: EventCodes.IsSecret(code), targetActors: targetActors);
            return sent;
        }

        public void OnEvent(EventData photonEvent)
        {
            try
            {
                byte code = photonEvent.Code;
                if (!EventCodes.IsInRange(code)) return;

                ClientState state = PhotonNetwork.NetworkClientState;
                if (state != ClientState.Joined)
                {
                    WLog.Line("drop_disconnecting", secret: false,
                        ("code", (int)code), ("state", state.ToString()));
                    return;
                }

                if (NetPayload.TryDeserialize(code, photonEvent.CustomData, out object[] payload, out _))
                {
                    WLog.Event("recv", code, "in", payload, secret: EventCodes.IsSecret(code));
                    OnReceived?.Invoke(new InboundMessage(code, payload, photonEvent.Sender));
                }
            }
            catch
            {
            }
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            try
            {
                if (otherPlayer == null) return;
                int actor = otherPlayer.ActorNumber;
                WLog.Line("player_left", secret: false, ("actor", actor), ("src", "photon"));
                OnPlayerLeft?.Invoke(actor);
            }
            catch
            {
            }
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            try
            {
                if (propertiesThatChanged == null) return;
                OnRoomPropertiesChanged?.Invoke(propertiesThatChanged);
            }
            catch
            {
            }
        }

        public void OnPlayerEnteredRoom(Player newPlayer) { }
        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }
        public void OnMasterClientSwitched(Player newMasterClient) { }

        public void OnDisconnected(DisconnectCause cause)
        {
            try
            {
                WLog.Line("local_disconnected", secret: false, ("cause", cause.ToString()));
                OnLocalDisconnected?.Invoke();
            }
            catch
            {
            }
        }

        public void OnConnected() { }
        public void OnConnectedToMaster() { }
        public void OnRegionListReceived(RegionHandler regionHandler) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }
    }
}
