using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class RpcProtocolTests
    {
        [Fact]
        public void TryOpen_RejectsVersionMismatch()
        {
            bool ok = RpcProtocol.TryOpen(
                (byte)(RpcProtocol.ProtocolVersion + 1), MessageCodes.AssignRole,
                new object[] { (byte)1 }, senderActor: 2,
                out InboundMessage message, out string dropReason);

            Assert.False(ok);
            Assert.Null(message);
            Assert.Equal("version", dropReason);
        }

        [Fact]
        public void TryOpen_ValidEnvelope_YieldsInboundMessage()
        {
            bool ok = RpcProtocol.TryOpen(
                RpcProtocol.ProtocolVersion, MessageCodes.AssignRole,
                new object[] { (byte)3 }, senderActor: 7,
                out InboundMessage message, out string dropReason);

            Assert.True(ok);
            Assert.Null(dropReason);
            Assert.Equal(MessageCodes.AssignRole, message.Code);
            Assert.Equal(7, message.SenderActor);
            Assert.Equal((byte)3, message.Payload[0]);
        }

        [Fact]
        public void TryOpen_NormalizesBoolPayloadToByte()
        {
            bool ok = RpcProtocol.TryOpen(
                RpcProtocol.ProtocolVersion, MessageCodes.AssignRole,
                new object[] { true }, senderActor: 1,
                out InboundMessage message, out _);

            Assert.True(ok);
            Assert.Equal((byte)1, message.Payload[0]);
        }

        [Fact]
        public void TryOpen_NullPayload_DropsAsNotArray()
        {
            bool ok = RpcProtocol.TryOpen(
                RpcProtocol.ProtocolVersion, MessageCodes.AssignRole,
                null, senderActor: 1, out InboundMessage message, out string dropReason);

            Assert.False(ok);
            Assert.Null(message);
            Assert.Equal("notarray", dropReason);
        }

        [Fact]
        public void TryOpen_ArityMismatch_Drops()
        {
            bool ok = RpcProtocol.TryOpen(
                RpcProtocol.ProtocolVersion, MessageCodes.AssignRole,
                new object[0], senderActor: 1, out _, out string dropReason);

            Assert.False(ok);
            Assert.Equal("arity", dropReason);
        }

        [Fact]
        public void TryOpen_TypeMismatch_Drops()
        {
            bool ok = RpcProtocol.TryOpen(
                RpcProtocol.ProtocolVersion, MessageCodes.AssignRole,
                new object[] { 1 }, senderActor: 1, out _, out string dropReason);

            Assert.False(ok);
            Assert.Equal("badtype", dropReason);
        }

        [Fact]
        public void TryOpen_NullElement_Drops()
        {
            bool ok = RpcProtocol.TryOpen(
                RpcProtocol.ProtocolVersion, MessageCodes.RevealTeammates,
                new object[] { null, new byte[] { 1 } }, senderActor: 1,
                out _, out string dropReason);

            Assert.False(ok);
            Assert.Equal("badtype", dropReason);
        }

        [Theory]
        [InlineData((byte)(MessageCodes.MinCode - 1))]
        [InlineData((byte)(MessageCodes.MaxCode + 1))]
        public void TryOpen_OutOfRangeCode_DropsAsBadCode(byte code)
        {
            bool ok = RpcProtocol.TryOpen(
                RpcProtocol.ProtocolVersion, code,
                new object[0], senderActor: 1, out _, out string dropReason);

            Assert.False(ok);
            Assert.Equal("badcode", dropReason);
        }

        [Fact]
        public void TryOpen_SenderComesFromTransport_NotFromPayload()
        {
            bool ok = RpcProtocol.TryOpen(
                RpcProtocol.ProtocolVersion, MessageCodes.ModIntegrityDetailRequest,
                new object[] { 1, 99 }, senderActor: 2,
                out InboundMessage message, out _);

            Assert.True(ok);
            Assert.Equal(2, message.SenderActor);
            Assert.Equal(99, message.Payload[1]);
        }
    }
}
