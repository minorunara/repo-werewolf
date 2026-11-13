#nullable enable
using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public sealed class OutboundMessageTests
    {
        [Fact]
        public void ToActors_RequiresTargetsAndCapturesTheirSnapshot()
        {
            int[] actors = { 2, 3 };
            OutboundMessage message = OutboundMessage.ToActors(160, new object[] { (byte)1 }, actors);
            actors[0] = 99;

            var destination = Assert.IsType<MessageDestination.ActorsDestination>(message.Destination);
            Assert.Equal(new[] { 2, 3 }, destination.Actors);
            Assert.Throws<ArgumentNullException>(() =>
                OutboundMessage.ToActors(160, Array.Empty<object>(), null!));
        }

        [Fact]
        public void FactoriesCreateOnlyTheirOwnDestinationShape()
        {
            Assert.IsType<MessageDestination.AllDestination>(
                OutboundMessage.ToAll(168, Array.Empty<object>()).Destination);
            Assert.IsType<MessageDestination.MasterDestination>(
                OutboundMessage.ToMaster(173, Array.Empty<object>()).Destination);
        }

        [Fact]
        public void FactoriesRejectNullPayload()
        {
            Assert.Throws<ArgumentNullException>(() => OutboundMessage.ToAll(168, null!));
        }
    }
}
