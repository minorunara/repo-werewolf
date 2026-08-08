using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class ConveneDeniedRoundtripTests : IDisposable
    {
        public ConveneDeniedRoundtripTests() { WLog.Sink = (_, __) => { }; }
        public void Dispose() { WLog.Sink = null; }

        private const long Now = MeetingSessionHarness.GameStart;

        private static ToastQueue WireReceiver(LoopbackNetBus bus, long nowUnixMs, int durationSec = 3)
        {
            var queue = new ToastQueue(durationSec);
            bus.OnReceived += msg =>
            {
                if (msg.Code != MessageCodes.ConveneDenied) return;
                var reason = ConveneDeniedWire.FromWire((byte)msg.Payload[0]);
                string text = NoticeCatalog.Format(SessionNotice.ForConveneDenied(reason));
                queue.Push(text, nowUnixMs);
            };
            return queue;
        }

        private static void WireSender(MeetingSession session, LoopbackNetBus bus)
        {
            session.OnSend += msg =>
            {
                switch (msg.Target)
                {
                    case MessageTarget.All: bus.SendToAll(msg.Code, msg.Payload); break;
                    case MessageTarget.Actors: bus.SendToActors(msg.Code, msg.Payload, msg.TargetActors); break;
                    case MessageTarget.Master: bus.SendToMaster(msg.Code, msg.Payload); break;
                }
            };
        }

        [Fact]
        public void TryConvene_NoRight_EmitsConveneDeniedTargetedAtCaller()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.MeetingRightsPerPlayer = 0);

            var reason = h.Session.TryConvene(callerActor: 3, nowUnixMs: Now);

            Assert.Equal(ConveneRejectReason.NoRight, reason);
            var denied = Assert.Single(h.ByCode(MessageCodes.ConveneDenied).ToArray());
            Assert.Equal(MessageTarget.Actors, denied.Target);
            Assert.Equal(new[] { 3 }, denied.TargetActors);
            Assert.Single(denied.Payload);
            Assert.Equal((byte)1, denied.Payload[0]);
        }

        [Fact]
        public void TryConvene_Suppressed_EmitsConveneDeniedWithSuppressedByte()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.ConveneSuppressStartSec = 60);

            var reason = h.Session.TryConvene(3, Now + 10_000);

            Assert.Equal(ConveneRejectReason.Suppressed, reason);
            var denied = Assert.Single(h.ByCode(MessageCodes.ConveneDenied).ToArray());
            Assert.Equal((byte)2, denied.Payload[0]);
        }

        [Fact]
        public void TryConvene_WrongPhase_EmitsConveneDeniedWithWrongPhaseByte()
        {
            var game = new GameSession();
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Name = "P1" },
                new WPlayer { ActorNumber = 2, Name = "P2" },
                new WPlayer { ActorNumber = 3, Name = "P3" },
            };
            var config = new GameConfig { ConveneSuppressStartSec = 0 };
            var session = new MeetingSession(config, game, players, 0);
            var sent = new List<OutboundMessage>();
            session.OnSend += sent.Add;

            Assert.Equal(ConveneRejectReason.WrongPhase, session.TryConvene(1, 100_000));

            var denied = Assert.Single(sent.FindAll(m => m.Code == MessageCodes.ConveneDenied));
            Assert.Equal(MessageTarget.Actors, denied.Target);
            Assert.Equal(new[] { 1 }, denied.TargetActors);
            Assert.Equal((byte)3, denied.Payload[0]);
        }

        [Fact]
        public void TryConvene_OtherReasons_CompressToZeroByte()
        {
            var h = MeetingSessionHarness.Create();
            h.Player(3).Alive = false;

            Assert.Equal(ConveneRejectReason.CallerDead, h.Session.TryConvene(3, Now));

            var denied = Assert.Single(h.ByCode(MessageCodes.ConveneDenied).ToArray());
            Assert.Equal((byte)0, denied.Payload[0]);
        }

        [Fact]
        public void TryConvene_Accepted_DoesNotEmitConveneDenied()
        {
            var h = MeetingSessionHarness.Create();

            Assert.Equal(ConveneRejectReason.None, h.Session.TryConvene(3, Now));

            Assert.Empty(h.ByCode(MessageCodes.ConveneDenied));
        }

        [Fact]
        public void Roundtrip_NoRight_DeliversToRequesterAndPushesLocalizedMessage()
        {
            const int callerActor = 3;
            var h = MeetingSessionHarness.Create(tune: c => c.MeetingRightsPerPlayer = 0);

            var bus = new LoopbackNetBus(localActorNumber: callerActor);
            WireSender(h.Session, bus);
            var queue = WireReceiver(bus, Now);

            h.Session.TryConvene(callerActor, Now);

            var visible = queue.Visible(Now);
            var entry = Assert.Single(visible);
            Assert.Equal("会議を開催できません（開催権がありません）", entry.Message);
        }

        [Fact]
        public void Roundtrip_Suppressed_LocalizedMessage()
        {
            const int callerActor = 3;
            var h = MeetingSessionHarness.Create(tune: c => c.ConveneSuppressStartSec = 60);
            var bus = new LoopbackNetBus(localActorNumber: callerActor);
            WireSender(h.Session, bus);
            var queue = WireReceiver(bus, Now);

            h.Session.TryConvene(callerActor, Now + 10_000);

            var entry = Assert.Single(queue.Visible(Now));
            Assert.Equal("会議を開催できません（現在は抑止時間中です）", entry.Message);
        }

        [Fact]
        public void Roundtrip_NotRequester_DoesNotReceive()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.MeetingRightsPerPlayer = 0);
            var bus = new LoopbackNetBus(localActorNumber: 5);
            WireSender(h.Session, bus);
            var queue = WireReceiver(bus, Now);

            h.Session.TryConvene(callerActor: 3, nowUnixMs: Now);

            Assert.Empty(queue.Visible(Now));
        }
    }
}
