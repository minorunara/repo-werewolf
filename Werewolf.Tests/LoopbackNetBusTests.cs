using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class LoopbackNetBusTests : IDisposable
    {
        private readonly List<(string Line, bool Secret)> _log = new List<(string, bool)>();

        public LoopbackNetBusTests()
        {
            WLog.Sink = (line, secret) => _log.Add((line, secret));
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private static (LoopbackNetBus bus, List<InboundMessage> received) NewBus(int localActor = 1)
        {
            var bus = new LoopbackNetBus(localActor);
            var received = new List<InboundMessage>();
            bus.OnReceived += received.Add;
            return (bus, received);
        }

        [Fact]
        public void LocalActorNumber_DefaultsToOne()
        {
            Assert.Equal(1, new LoopbackNetBus().LocalActorNumber);
        }

        [Fact]
        public void SendToAll_DeliversToSelf()
        {
            var (bus, received) = NewBus();

            bool ok = bus.SendToAll(MessageCodes.PhaseChanged, new object[] { (byte)1, 100L, 200L });

            Assert.True(ok);
            Assert.Single(received);
            Assert.Equal(MessageCodes.PhaseChanged, received[0].Code);
            Assert.Equal(1, received[0].SenderActor);
        }

        [Fact]
        public void SendToMaster_DeliversToSelf()
        {
            var (bus, received) = NewBus();

            bool ok = bus.SendToMaster(MessageCodes.PlayerDied, new object[] { 3, (byte)0 });

            Assert.True(ok);
            Assert.Single(received);
            Assert.Equal(MessageCodes.PlayerDied, received[0].Code);
        }

        [Fact]
        public void SendToActors_DeliversWhenLocalIncluded()
        {
            var (bus, received) = NewBus(localActor: 1);

            bus.SendToActors(MessageCodes.AssignRole, new object[] { (byte)Role.Werewolf }, new[] { 1 });

            Assert.Single(received);
            Assert.Equal(MessageCodes.AssignRole, received[0].Code);
            Assert.Equal((byte)Role.Werewolf, received[0].Payload[0]);
        }

        [Fact]
        public void SendToActors_DoesNotDeliverWhenLocalExcluded()
        {
            var (bus, received) = NewBus(localActor: 1);

            bus.SendToActors(MessageCodes.AssignRole, new object[] { (byte)Role.Villager }, new[] { -1 });

            Assert.Empty(received);
            Assert.Contains(_log, l => l.Line.Contains("dir=send") && l.Line.Contains("code=160") && l.Secret);
        }

        [Fact]
        public void SendToActors_NullTargets_DoesNotDeliver()
        {
            var (bus, received) = NewBus();

            bus.SendToActors(MessageCodes.RevealSelfRole, new object[] { (byte)Role.BlackCat }, null);

            Assert.Empty(received);
        }

        [Fact]
        public void Send_NormalizesBoolInPayloadBeforeDelivery()
        {
            var (bus, received) = NewBus();

            bus.SendToActors(MessageCodes.AssignRole, new object[] { true }, new[] { 1 });

            Assert.Single(received);
            Assert.Equal((byte)1, received[0].Payload[0]);
        }

        [Fact]
        public void SimulatePlayerLeft_RaisesOnPlayerLeftWithActorNumber()
        {
            var bus = new LoopbackNetBus();
            var left = new List<int>();
            bus.OnPlayerLeft += left.Add;

            bus.SimulatePlayerLeft(4);

            Assert.Single(left);
            Assert.Equal(4, left[0]);
        }

        [Fact]
        public void SimulatePlayerLeft_NoSubscriber_DoesNotThrow()
        {
            var bus = new LoopbackNetBus();
            bus.SimulatePlayerLeft(2);
        }

        [Fact]
        public void FivePlayerSolo_OnlySelfRoleNoticeReceived_BotNoticesLoggedOnly()
        {
            var (bus, received) = NewBus(localActor: 1);

            foreach (int actor in new[] { 1, -1, -2, -3, -4 })
            {
                bus.SendToActors(MessageCodes.AssignRole, new object[] { (byte)Role.Villager }, new[] { actor });
            }

            Assert.Single(received);
            Assert.Equal(MessageCodes.AssignRole, received[0].Code);

            int sendLines = _log.FindAll(l => l.Line.Contains("dir=send") && l.Line.Contains("code=160")).Count;
            Assert.Equal(5, sendLines);
        }
    }
}
