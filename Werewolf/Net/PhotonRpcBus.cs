using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Net
{
    public sealed class PhotonRpcBus : INetBus
    {
        private const string EndpointObjectName = "WerewolfRpcEndpoint";

        private readonly PhotonRoomCallbackAdapter _callbacks = new PhotonRoomCallbackAdapter();

        private PhotonView _view;

        private long _endpointFailLogAtMs;

        private bool _endpointEverCreated;

        public PhotonRpcBus()
        {
            _callbacks.OnPlayerLeft += actor => OnPlayerLeft?.Invoke(actor);
            _callbacks.OnLocalDisconnected += () => OnLocalDisconnected?.Invoke();
            _callbacks.OnRoomPropertiesChanged += props => OnRoomPropertiesChanged?.Invoke(props);
        }

        public event Action<InboundMessage> OnReceived;

        public event Action<int> OnPlayerLeft;

        public event Action OnLocalDisconnected;

        public event Action<Hashtable> OnRoomPropertiesChanged;

        public int LocalActorNumber =>
            PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : -1;

        public void Register()
        {
            _callbacks.Register();
            RpcEndpoint.OnMessage += RelayInbound;
            EnsureEndpoint();
        }

        public void Unregister()
        {
            RpcEndpoint.OnMessage -= RelayInbound;
            _callbacks.Unregister();
            DestroyEndpoint();
        }

        public bool EnsureEndpoint()
        {
            if (_view != null) return true;

            try
            {
                var go = new GameObject(EndpointObjectName);
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<RpcEndpoint>();
                var view = go.AddComponent<PhotonView>();
                view.ViewID = RpcProtocol.FixedViewId;
                _view = view;
                WLog.Line("rpc_endpoint_created", secret: false,
                    ("viewId", RpcProtocol.FixedViewId), ("revive", _endpointEverCreated));
                _endpointEverCreated = true;
                return true;
            }
            catch (Exception e)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (now - _endpointFailLogAtMs >= 10000L)
                {
                    _endpointFailLogAtMs = now;
                    WLog.Line("rpc_endpoint_fail", secret: false, ("err", e.Message));
                }
                return false;
            }
        }

        public bool SendToAll(byte code, object[] payload)
        {
            if (!TryPrepare(code, payload, out object[] args)) return false;
            _view.RPC(RpcProtocol.MethodName, RpcTarget.All, args);
            WLog.Event("send", code, "all", (object[])args[2],
                secret: MessageCodes.IsSecret(code));
            return true;
        }

        public bool SendToActors(byte code, object[] payload, int[] targetActors)
        {
            if (!TryPrepare(code, payload, out object[] args)) return false;
            if (targetActors != null)
            {
                Room room = PhotonNetwork.CurrentRoom;
                foreach (int actor in targetActors)
                {
                    Player target = room != null ? room.GetPlayer(actor) : null;
                    if (target == null) continue;
                    _view.RPC(RpcProtocol.MethodName, target, args);
                }
            }
            WLog.Event("send", code, "actors", (object[])args[2],
                secret: MessageCodes.IsSecret(code), targetActors: targetActors);
            return true;
        }

        public bool SendToMaster(byte code, object[] payload)
        {
            if (!TryPrepare(code, payload, out object[] args)) return false;
            _view.RPC(RpcProtocol.MethodName, RpcTarget.MasterClient, args);
            WLog.Event("send", code, "master", (object[])args[2],
                secret: MessageCodes.IsSecret(code));
            return true;
        }

        private bool TryPrepare(byte code, object[] payload, out object[] args)
        {
            args = null;

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

            if (!EnsureEndpoint())
            {
                WLog.Line("send_fail", secret: false,
                    ("reason", "no_endpoint"), ("code", (int)code));
                return false;
            }

            args = new object[] { RpcProtocol.ProtocolVersion, code, normalized };
            return true;
        }

        private void RelayInbound(InboundMessage message) => OnReceived?.Invoke(message);

        private void DestroyEndpoint()
        {
            if (_view == null) return;
            GameObject go = _view.gameObject;
            _view = null;
            UnityEngine.Object.Destroy(go);
            WLog.Line("rpc_endpoint_destroyed", secret: false,
                ("viewId", RpcProtocol.FixedViewId));
        }
    }
}
