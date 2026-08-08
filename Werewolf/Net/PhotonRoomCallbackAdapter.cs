using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Werewolf.Core;

namespace Werewolf.Net
{
    public sealed class PhotonRoomCallbackAdapter : IInRoomCallbacks, IConnectionCallbacks
    {
        public event Action<int> OnPlayerLeft;

        public event Action OnLocalDisconnected;

        public event Action<Hashtable> OnRoomPropertiesChanged;

        public void Register() => PhotonNetwork.AddCallbackTarget(this);

        public void Unregister() => PhotonNetwork.RemoveCallbackTarget(this);

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
