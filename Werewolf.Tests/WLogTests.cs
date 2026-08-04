using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WLogTests : IDisposable
    {
        private readonly List<(string Line, bool Secret)> _captured = new List<(string, bool)>();

        public WLogTests()
        {
            WLog.Sink = (line, secret) => _captured.Add((line, secret));
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Fact]
        public void Event_Send_ToAll_FormatsDesignExampleLine()
        {
            WLog.Event("send", 163, "all", new object[] { 1, 2, "go" });

            Assert.Single(_captured);
            Assert.Equal("[WW] dir=send code=163 target=all payload=[1,2,go]", _captured[0].Line);
            Assert.False(_captured[0].Secret);
        }

        [Fact]
        public void Event_Recv_FormatsDirRecv()
        {
            WLog.Event("recv", 170, "master", new object[] { 5 });

            Assert.Equal("[WW] dir=recv code=170 target=master payload=[5]", _captured[0].Line);
        }

        [Fact]
        public void Event_ToActors_IncludesTargetActors()
        {
            WLog.Event("send", 161, "actors", new object[] { 2, 1 }, secret: true, targetActors: new[] { 3, -1 });

            Assert.Equal(
                "[WW] dir=send code=161 target=actors targetActors=[3,-1] payload=[2,1] secret=1",
                _captured[0].Line);
            Assert.True(_captured[0].Secret);
        }

        [Fact]
        public void Event_Secret_AppendsSecretTagAndFlagsSink()
        {
            WLog.Event("send", 161, "all", new object[] { 0 }, secret: true);

            Assert.EndsWith(" secret=1", _captured[0].Line);
            Assert.True(_captured[0].Secret);
        }

        [Fact]
        public void Event_NonSecret_HasNoSecretTag()
        {
            WLog.Event("send", 163, "all", new object[] { 0 });

            Assert.DoesNotContain("secret", _captured[0].Line);
            Assert.False(_captured[0].Secret);
        }

        [Fact]
        public void Event_NullPayload_RendersEmptyArray()
        {
            WLog.Event("send", 168, "all", null);

            Assert.Equal("[WW] dir=send code=168 target=all payload=[]", _captured[0].Line);
        }

        [Fact]
        public void Event_NestedArrayPayload_RendersRecursively()
        {
            WLog.Event("send", 169, "all", new object[] { new int[] { 1, -2 }, 7 });

            Assert.Equal("[WW] dir=send code=169 target=all payload=[[1,-2],7]", _captured[0].Line);
        }

        [Fact]
        public void Event_BoolAndNullPayloadItems_RenderLowercaseAndNull()
        {
            WLog.Event("send", 165, "all", new object[] { true, false, null });

            Assert.Equal("[WW] dir=send code=165 target=all payload=[true,false,null]", _captured[0].Line);
        }

        [Fact]
        public void Event_StringWithSpaces_IsQuoted()
        {
            WLog.Event("send", 162, "all", new object[] { "Player One" });

            Assert.Equal("[WW] dir=send code=162 target=all payload=[\"Player One\"]", _captured[0].Line);
        }

        [Fact]
        public void Event_EnumPayloadItem_RendersName()
        {
            WLog.Event("send", 161, "actors", new object[] { Role.Werewolf }, secret: true, targetActors: new[] { 2 });

            Assert.Contains("payload=[Werewolf]", _captured[0].Line);
        }

        [Fact]
        public void Phase_FormatsDesignExampleLine()
        {
            WLog.Phase(GamePhase.Lobby, GamePhase.Play, "start");

            Assert.Single(_captured);
            Assert.Equal("[WW] phase Lobby->Play reason=start", _captured[0].Line);
            Assert.False(_captured[0].Secret);
        }

        [Fact]
        public void Phase_MeetingToPlay_UsesEnumNames()
        {
            WLog.Phase(GamePhase.Meeting, GamePhase.Play, "resume");

            Assert.Equal("[WW] phase Meeting->Play reason=resume", _captured[0].Line);
        }

        [Fact]
        public void Line_GenericKeyValues_FormatsKindAndPairs()
        {
            WLog.Line("drop", false, ("reason", "badcode"), ("code", 999));

            Assert.Equal("[WW] drop reason=badcode code=999", _captured[0].Line);
            Assert.False(_captured[0].Secret);
        }

        [Fact]
        public void Line_Secret_AppendsSecretTag()
        {
            WLog.Line("assign", true, ("actor", 3), ("role", Role.BlackCat));

            Assert.Equal("[WW] assign actor=3 role=BlackCat secret=1", _captured[0].Line);
            Assert.True(_captured[0].Secret);
        }

        [Fact]
        public void Line_NoFields_EmitsKindOnly()
        {
            WLog.Line("selftest-start", false);

            Assert.Equal("[WW] selftest-start", _captured[0].Line);
        }

        [Fact]
        public void Sink_Unset_DoesNotThrow()
        {
            WLog.Sink = null;

            var ex1 = Record.Exception(() => WLog.Event("send", 163, "all", new object[] { 1 }));
            var ex2 = Record.Exception(() => WLog.Phase(GamePhase.Play, GamePhase.Meeting, "vote"));
            var ex3 = Record.Exception(() => WLog.Line("drop", false, ("reason", "x")));

            Assert.Null(ex1);
            Assert.Null(ex2);
            Assert.Null(ex3);
        }

        [Fact]
        public void Sink_Throwing_IsSwallowed()
        {
            WLog.Sink = (line, secret) => throw new InvalidOperationException("boom");

            var ex = Record.Exception(() => WLog.Event("send", 163, "all", new object[] { 1 }));

            Assert.Null(ex);
        }

        [Fact]
        public void Sink_CapturesCallsInOrder()
        {
            WLog.Phase(GamePhase.Lobby, GamePhase.Play, "start");
            WLog.Event("send", 163, "all", new object[] { 1 });
            WLog.Line("drop", false, ("reason", "late"));

            Assert.Equal(3, _captured.Count);
            Assert.StartsWith("[WW] phase", _captured[0].Line);
            Assert.StartsWith("[WW] dir=send", _captured[1].Line);
            Assert.StartsWith("[WW] drop", _captured[2].Line);
        }
    }
}
