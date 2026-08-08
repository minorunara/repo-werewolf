using Werewolf.Core;

namespace Werewolf.Net
{
    public static class RpcProtocol
    {
        public const int FixedViewId = 900000001;

        public const byte ProtocolVersion = 1;

        public const string MethodName = "WerewolfRpc";

        public static bool TryOpen(byte protocolVersion, byte messageType, object[] payload,
            int senderActor, out InboundMessage message, out string dropReason)
        {
            message = null;

            if (protocolVersion != ProtocolVersion)
            {
                dropReason = "version";
                WLog.Line("drop", secret: false,
                    ("reason", "version"), ("code", (int)messageType), ("ver", (int)protocolVersion));
                return false;
            }

            if (!NetPayload.TryDeserialize(messageType, payload, out object[] valid, out dropReason))
                return false;

            message = new InboundMessage(messageType, valid, senderActor);
            dropReason = null;
            return true;
        }
    }
}
