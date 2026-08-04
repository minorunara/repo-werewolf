using System;
using Werewolf.Core;

namespace Werewolf.Net
{
    public sealed class LoopbackNetBus : INetBus
    {
        public LoopbackNetBus(int localActorNumber = 1)
        {
            LocalActorNumber = localActorNumber;
        }

        public int LocalActorNumber { get; }

        public event Action<InboundMessage> OnReceived;

        public event Action<int> OnPlayerLeft;

        public void SimulatePlayerLeft(int actorNumber)
        {
            WLog.Line("player_left", secret: false, ("actor", actorNumber), ("src", "loopback"));
            OnPlayerLeft?.Invoke(actorNumber);
        }

        public bool SendToAll(byte code, object[] payload)
        {
            object[] normalized = NetPayload.Build(payload);
            LogSend(code, "all", normalized, null);
            Deliver(code, normalized);
            return true;
        }

        public bool SendToMaster(byte code, object[] payload)
        {
            object[] normalized = NetPayload.Build(payload);
            LogSend(code, "master", normalized, null);
            Deliver(code, normalized);
            return true;
        }

        public bool SendToActors(byte code, object[] payload, int[] targetActors)
        {
            object[] normalized = NetPayload.Build(payload);
            LogSend(code, "actors", normalized, targetActors);

            if (Contains(targetActors, LocalActorNumber))
            {
                Deliver(code, normalized);
            }
            return true;
        }

        private void Deliver(byte code, object[] payload)
        {
            if (NetPayload.TryDeserialize(code, payload, out object[] valid, out _))
            {
                OnReceived?.Invoke(new InboundMessage(code, valid, LocalActorNumber));
            }
        }

        private static void LogSend(byte code, string target, object[] payload, int[] targetActors)
        {
            WLog.Event("send", code, target, payload,
                secret: EventCodes.IsSecret(code), targetActors: targetActors);
        }

        private static bool Contains(int[] actors, int value)
        {
            if (actors == null) return false;
            foreach (int actor in actors)
            {
                if (actor == value) return true;
            }
            return false;
        }
    }
}
