using System;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif

namespace Werewolf.Core
{
    public enum MessageTarget
    {
        All = 0,

        Actors = 1,

        Master = 2,
    }

    public sealed record OutboundMessage(byte Code, object[] Payload, MessageTarget Target, int[] TargetActors);

    public static class WWEventCodes
    {
        public const byte AssignRole = 160;

        public const byte RevealSelfRole = 161;

        public const byte RevealTeammates = 162;

        public const byte PlayerDied = 168;

        public const byte GameOver = 169;

        public const byte GameStart = 170;

        public const byte PhaseChanged = 172;
    }
}
