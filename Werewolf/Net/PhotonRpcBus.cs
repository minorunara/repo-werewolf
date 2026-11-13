#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
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

        private sealed class EndpointState
        {
            public EndpointState(GameObject root, PhotonView view)
            {
                Root = root;
                View = view;
            }

            public GameObject Root { get; }
            public PhotonView View { get; }
        }

        private sealed class PreparedSend
        {
            public PreparedSend(EndpointState endpoint, object[] args)
            {
                Endpoint = endpoint;
                Args = args;
            }

            public EndpointState Endpoint { get; }
            public object[] Args { get; }
        }

        private readonly PhotonRoomCallbackAdapter _callbacks = new PhotonRoomCallbackAdapter();

        private EndpointState? _endpoint;

        private long _endpointFailLogAtMs;

        private bool _endpointEverCreated;

        public PhotonRpcBus()
        {
            _callbacks.OnPlayerLeft += actor => OnPlayerLeft?.Invoke(actor);
            _callbacks.OnLocalDisconnected += () => OnLocalDisconnected?.Invoke();
            _callbacks.OnRoomPropertiesChanged += props => OnRoomPropertiesChanged?.Invoke(props);
        }

        public event Action<InboundMessage>? OnReceived;

        public event Action<int>? OnPlayerLeft;

        public event Action? OnLocalDisconnected;

        public event Action<Hashtable>? OnRoomPropertiesChanged;

        public int LocalActorNumber
        {
            get
            {
                Player? local = PhotonNetwork.LocalPlayer;
                return PhotonNetwork.InRoom && local != null ? local.ActorNumber : -1;
            }
        }

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
            if (GetLiveEndpointOrNull() != null) return true;

            GameObject? partialRoot = null;
            try
            {
                var go = new GameObject(EndpointObjectName);
                partialRoot = go;
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<RpcEndpoint>();
                var view = go.AddComponent<PhotonView>();
                view.ViewID = RpcProtocol.FixedViewId;
                if (go == null || view == null)
                    throw new InvalidOperationException("RPC endpoint was destroyed during registration");

                _endpoint = new EndpointState(go, view);
                partialRoot = null;
                WLog.Line("rpc_endpoint_created", secret: false,
                    ("viewId", RpcProtocol.FixedViewId), ("revive", _endpointEverCreated));
                _endpointEverCreated = true;
                return true;
            }
            catch (Exception e)
            {
                if (partialRoot != null) UnityEngine.Object.Destroy(partialRoot);
                _endpoint = null;
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
            if (!TryPrepare(code, payload, out PreparedSend? prepared)) return false;
            prepared.Endpoint.View.RPC(RpcProtocol.MethodName, RpcTarget.All, prepared.Args);
            WLog.Event("send", code, "all", (object[])prepared.Args[2],
                secret: MessageCodes.IsSecret(code));
            return true;
        }

        public bool SendToActors(byte code, object[] payload, int[] targetActors)
        {
            if (targetActors == null) throw new ArgumentNullException(nameof(targetActors));
            if (!TryPrepare(code, payload, out PreparedSend? prepared)) return false;
            Room? room = PhotonNetwork.CurrentRoom;
            foreach (int actor in targetActors)
            {
                if (!IsCurrentEndpoint(prepared.Endpoint))
                {
                    WLog.Line("send_fail", secret: false,
                        ("reason", "endpoint_changed"), ("code", (int)code));
                    return false;
                }

                Player? target = room != null ? room.GetPlayer(actor) : null;
                if (target == null) continue;
                prepared.Endpoint.View.RPC(RpcProtocol.MethodName, target, prepared.Args);
            }
            WLog.Event("send", code, "actors", (object[])prepared.Args[2],
                secret: MessageCodes.IsSecret(code), targetActors: targetActors);
            return true;
        }

        public bool SendToMaster(byte code, object[] payload)
        {
            if (!TryPrepare(code, payload, out PreparedSend? prepared)) return false;
            prepared.Endpoint.View.RPC(RpcProtocol.MethodName, RpcTarget.MasterClient, prepared.Args);
            WLog.Event("send", code, "master", (object[])prepared.Args[2],
                secret: MessageCodes.IsSecret(code));
            return true;
        }

        private bool TryPrepare(byte code, object[] payload,
            [NotNullWhen(true)] out PreparedSend? prepared)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            prepared = null;

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

            EndpointState? endpoint = GetLiveEndpointOrNull();
            if (endpoint == null)
            {
                WLog.Line("send_fail", secret: false,
                    ("reason", "no_endpoint"), ("code", (int)code));
                return false;
            }

            prepared = new PreparedSend(endpoint,
                new object[] { RpcProtocol.ProtocolVersion, code, normalized });
            return true;
        }

        private void RelayInbound(InboundMessage message) => OnReceived?.Invoke(message);

        private void DestroyEndpoint()
        {
            EndpointState? endpoint = _endpoint;
            _endpoint = null;
            if (endpoint == null || endpoint.Root == null) return;
            UnityEngine.Object.Destroy(endpoint.Root);
            WLog.Line("rpc_endpoint_destroyed", secret: false,
                ("viewId", RpcProtocol.FixedViewId));
        }

        private EndpointState? GetLiveEndpointOrNull()
        {
            EndpointState? endpoint = _endpoint;
            if (endpoint == null) return null;
            if (endpoint.Root != null && endpoint.View != null) return endpoint;
            _endpoint = null;
            if (endpoint.Root != null) UnityEngine.Object.Destroy(endpoint.Root);
            return null;
        }

        private bool IsCurrentEndpoint(EndpointState endpoint)
            => ReferenceEquals(_endpoint, endpoint)
               && endpoint.Root != null && endpoint.View != null;
    }
}
