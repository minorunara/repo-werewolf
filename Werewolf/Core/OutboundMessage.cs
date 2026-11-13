#nullable enable
using System;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif

namespace Werewolf.Core
{
    public abstract record MessageDestination
    {
        private MessageDestination() { }

        public sealed record AllDestination : MessageDestination;

        public sealed record MasterDestination : MessageDestination;

        public sealed record ActorsDestination : MessageDestination
        {
            public ActorsDestination(int[] actors)
            {
                Actors = actors != null
                    ? (int[])actors.Clone()
                    : throw new ArgumentNullException(nameof(actors));
            }

            public int[] Actors { get; }
        }
    }

    public sealed record OutboundMessage
    {
        private OutboundMessage(byte code, object[] payload, MessageDestination destination)
        {
            Code = code;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        }

        public byte Code { get; }
        public object[] Payload { get; }
        public MessageDestination Destination { get; }

        public static OutboundMessage ToAll(byte code, object[] payload)
            => new OutboundMessage(code, payload, new MessageDestination.AllDestination());

        public static OutboundMessage ToMaster(byte code, object[] payload)
            => new OutboundMessage(code, payload, new MessageDestination.MasterDestination());

        public static OutboundMessage ToActors(byte code, object[] payload, int[] actors)
            => new OutboundMessage(code, payload, new MessageDestination.ActorsDestination(actors));
    }

    public static class WWEventCodes
    {
        public const byte AssignRole = 160;

        public const byte RevealSelfRole = 161;

        public const byte RevealTeammates = 162;

        public const byte PlayerDied = 168;

        public const byte GameOver = 169;

        public const byte GameStart = 170;

        public const byte PhaseChanged = 172;

        public const byte PreplayStart = 195;

        public const byte PreplayAborted = 196;
    }
}
