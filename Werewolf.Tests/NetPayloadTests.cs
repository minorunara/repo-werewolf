using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class NetPayloadTests : IDisposable
    {
        private readonly List<(string Line, bool Secret)> _captured = new List<(string, bool)>();

        public NetPayloadTests()
        {
            WLog.Sink = (line, secret) => _captured.Add((line, secret));
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Fact]
        public void Build_NormalizesBoolToByte()
        {
            var built = NetPayload.Build(new object[] { true, false });

            Assert.Equal((byte)1, built[0]);
            Assert.Equal((byte)0, built[1]);
        }

        [Fact]
        public void Build_KeepsAllowedTypes()
        {
            var payload = new object[] { 1, 2L, "s", (byte)3, new byte[] { 4 }, new[] { 5 }, new[] { "x" } };
            var built = NetPayload.Build(payload);

            Assert.Equal(payload.Length, built.Length);
            Assert.Equal(1, built[0]);
            Assert.Equal(2L, built[1]);
        }

        [Fact]
        public void Build_NullPayload_YieldsEmptyArray()
        {
            Assert.Empty(NetPayload.Build(null));
        }

        [Fact]
        public void Build_DisallowedType_Throws()
        {
            Assert.Throws<ArgumentException>(() => NetPayload.Build(new object[] { 1.5 }));
        }

        [Fact]
        public void TryDeserialize_ValidPayload_Succeeds()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.PlayerDied, new object[] { 7, (byte)1 }, out var payload, out var reason);

            Assert.True(ok);
            Assert.Null(reason);
            Assert.Equal(7, payload[0]);
            Assert.Equal((byte)1, payload[1]);
        }

        [Fact]
        public void TryDeserialize_NormalizesBoolElementToByte()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.AssignRole, new object[] { true }, out var payload, out _);

            Assert.True(ok);
            Assert.Equal((byte)1, payload[0]);
        }

        [Fact]
        public void TryDeserialize_OutOfRangeCode_DropsWithReasonAndLogs()
        {
            bool ok = NetPayload.TryDeserialize(199, new object[] { (byte)0 }, out var payload, out var reason);

            Assert.False(ok);
            Assert.Null(payload);
            Assert.Equal("badcode", reason);
            Assert.Contains(_captured, c => c.Line.Contains("drop") && c.Line.Contains("badcode"));
        }

        [Fact]
        public void TryDeserialize_RolesCode_ValidAndInvalidPayload()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.BeaconAudit, new object[] { (byte)2 }, out _, out var reason);
            Assert.True(ok);
            Assert.Null(reason);

            ok = NetPayload.TryDeserialize(
                EventCodes.BeaconAudit, new object[] { }, out _, out reason);
            Assert.False(ok);

            ok = NetPayload.TryDeserialize(
                EventCodes.RoleAction, new object[] { (byte)0, 5, (byte)1 }, out var payload, out reason);
            Assert.True(ok);
            Assert.Equal(5, payload[1]);

            ok = NetPayload.TryDeserialize(
                EventCodes.RoleAction, new object[] { "bad", 5, (byte)1 }, out _, out reason);
            Assert.False(ok);
        }

        [Fact]
        public void TryDeserialize_MeetingCode_ValidPayload_Succeeds()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.StartMeeting, new object[] { 3, 1000L, 2000L, (byte)0 }, out var payload, out var reason);

            Assert.True(ok);
            Assert.Null(reason);
            Assert.Equal(3, payload[0]);
            Assert.Equal(2000L, payload[2]);
        }

        [Fact]
        public void TryDeserialize_RequestMeeting_KindPayload_Succeeds()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.RequestMeeting, new object[] { (byte)1 }, out var payload, out var reason);

            Assert.True(ok);
            Assert.Null(reason);
            Assert.Equal((byte)1, payload[0]);

            Assert.False(NetPayload.TryDeserialize(
                EventCodes.RequestMeeting, Array.Empty<object>(), out _, out _));
        }

        [Fact]
        public void TryDeserialize_MeetingCode_WrongArity_Drops()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.VoteProgress, new object[] { new[] { 1, 2 } }, out _, out var reason);

            Assert.False(ok);
            Assert.Equal("arity", reason);
        }

        [Fact]
        public void TryDeserialize_MeetingCode_WrongElementType_Drops()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.MeetingResult, new object[] { -1, "notArray", new[] { 0 } }, out _, out var reason);

            Assert.False(ok);
            Assert.Equal("badtype", reason);
        }

        [Fact]
        public void TryDeserialize_NonArrayContent_Drops()
        {
            bool ok = NetPayload.TryDeserialize(EventCodes.AssignRole, 42, out _, out var reason);

            Assert.False(ok);
            Assert.Equal("notarray", reason);
        }

        [Fact]
        public void TryDeserialize_MissingElements_DropsAsArity()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.PlayerDied, new object[] { 7 }, out _, out var reason);

            Assert.False(ok);
            Assert.Equal("arity", reason);
        }

        [Fact]
        public void TryDeserialize_TypeMismatch_DropsAsBadType()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.AssignRole, new object[] { 5 }, out _, out var reason);

            Assert.False(ok);
            Assert.Equal("badtype", reason);
        }

        [Fact]
        public void TryDeserialize_NullElement_DropsAsBadType()
        {
            bool ok = NetPayload.TryDeserialize(
                EventCodes.PlayerDied, new object[] { 7, null }, out _, out var reason);

            Assert.False(ok);
            Assert.Equal("badtype", reason);
        }
    }
}
