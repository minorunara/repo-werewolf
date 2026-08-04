using System;

namespace Werewolf.Net
{
    public sealed class InboundMessage
    {
        public InboundMessage(byte code, object[] payload, int senderActor)
        {
            Code = code;
            Payload = payload;
            SenderActor = senderActor;
        }

        public byte Code { get; }
        public object[] Payload { get; }
        public int SenderActor { get; }
    }

    public interface INetBus
    {
        bool SendToAll(byte code, object[] payload);

        bool SendToActors(byte code, object[] payload, int[] targetActors);

        bool SendToMaster(byte code, object[] payload);

        event Action<InboundMessage> OnReceived;

        event Action<int> OnPlayerLeft;

        int LocalActorNumber { get; }
    }
}
