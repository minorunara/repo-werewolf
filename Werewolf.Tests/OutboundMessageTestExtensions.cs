#nullable enable
using Werewolf.Core;

namespace Werewolf.Tests
{
    internal static class OutboundMessageTestExtensions
    {
        internal static bool IsAllDestination(this OutboundMessage message)
            => message.Destination is MessageDestination.AllDestination;

        internal static int[]? ActorTargetsOrNull(this OutboundMessage message)
            => (message.Destination as MessageDestination.ActorsDestination)?.Actors;
    }
}
