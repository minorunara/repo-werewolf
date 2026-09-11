using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class RoundStartPublicData
    {
        public RoundStartPublicData(
            int roundSeconds,
            int werewolfCount,
            bool blackCatPossible,
            int revealDelaySec,
            bool debugMode,
            int[] actorsByIdOrder)
        {
            if (roundSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(roundSeconds));
            if (werewolfCount < 0 || werewolfCount > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(werewolfCount));
            if (revealDelaySec < 0) throw new ArgumentOutOfRangeException(nameof(revealDelaySec));
            if (!IsValidRoster(actorsByIdOrder))
                throw new ArgumentException("参加者名簿は重複しない0以外のactorを1件以上含むこと。", nameof(actorsByIdOrder));
            if (werewolfCount > actorsByIdOrder.Length)
                throw new ArgumentOutOfRangeException(nameof(werewolfCount));

            RoundSeconds = roundSeconds;
            WerewolfCount = werewolfCount;
            BlackCatPossible = blackCatPossible;
            RevealDelaySec = revealDelaySec;
            DebugMode = debugMode;
            ActorsByIdOrder = (int[])actorsByIdOrder.Clone();
        }

        public int RoundSeconds { get; }
        public int WerewolfCount { get; }
        public bool BlackCatPossible { get; }
        public int RevealDelaySec { get; }
        public bool DebugMode { get; }
        public int[] ActorsByIdOrder { get; }

        internal static bool IsValidRoster(int[] actors)
        {
            if (actors == null || actors.Length == 0) return false;
            var seen = new HashSet<int>();
            foreach (int actor in actors)
            {
                if (actor == 0 || !seen.Add(actor)) return false;
            }
            return true;
        }
    }

    public static class RoundStartWire
    {
        public static object[] ToPreplay(
            RoundStartPublicData data,
            long revealStartUnixMs,
            long revealEndUnixMs)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!IsValidRevealWindow(revealStartUnixMs, revealEndUnixMs))
                throw new ArgumentOutOfRangeException(nameof(revealEndUnixMs));

            return new object[]
            {
                revealStartUnixMs,
                revealEndUnixMs,
                data.RoundSeconds,
                (byte)data.WerewolfCount,
                (byte)(data.BlackCatPossible ? 1 : 0),
                data.RevealDelaySec,
                (byte)(data.DebugMode ? 1 : 0),
                data.ActorsByIdOrder,
            };
        }

        public static object[] ToGameStart(RoundStartPublicData data, long roundEndUnixMs)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (roundEndUnixMs <= 0) throw new ArgumentOutOfRangeException(nameof(roundEndUnixMs));

            return new object[]
            {
                roundEndUnixMs,
                data.RoundSeconds,
                (byte)data.WerewolfCount,
                (byte)(data.BlackCatPossible ? 1 : 0),
                data.RevealDelaySec,
                (byte)(data.DebugMode ? 1 : 0),
                data.ActorsByIdOrder,
            };
        }

        public static bool TryReadPreplay(
            object[] payload,
            out long revealStartUnixMs,
            out long revealEndUnixMs,
            out RoundStartPublicData data)
        {
            revealStartUnixMs = 0;
            revealEndUnixMs = 0;
            data = null;
            if (payload == null || payload.Length != 8
                || !(payload[0] is long start)
                || !(payload[1] is long end)
                || !IsValidRevealWindow(start, end)
                || !TryReadPublic(payload, 2, out data))
            {
                return false;
            }

            revealStartUnixMs = start;
            revealEndUnixMs = end;
            return true;
        }

        public static bool TryReadGameStart(
            object[] payload,
            out long roundEndUnixMs,
            out RoundStartPublicData data)
        {
            roundEndUnixMs = 0;
            data = null;
            if (payload == null || payload.Length != 7
                || !(payload[0] is long end)
                || end <= 0
                || !TryReadPublic(payload, 1, out data))
            {
                return false;
            }

            roundEndUnixMs = end;
            return true;
        }

        private static bool TryReadPublic(object[] payload, int offset, out RoundStartPublicData data)
        {
            data = null;
            if (!(payload[offset] is int roundSeconds)
                || !(payload[offset + 1] is byte werewolfCount)
                || !(payload[offset + 2] is byte blackCatPossible)
                || !(payload[offset + 3] is int revealDelaySec)
                || !(payload[offset + 4] is byte debugMode)
                || !(payload[offset + 5] is int[] actors)
                || roundSeconds <= 0
                || revealDelaySec < 0
                || blackCatPossible > 1
                || debugMode > 1
                || werewolfCount > actors.Length
                || !RoundStartPublicData.IsValidRoster(actors))
            {
                return false;
            }

            data = new RoundStartPublicData(
                roundSeconds,
                werewolfCount,
                blackCatPossible != 0,
                revealDelaySec,
                debugMode != 0,
                actors);
            return true;
        }

        private static bool IsValidRevealWindow(long startUnixMs, long endUnixMs)
            => startUnixMs > 0 && endUnixMs - startUnixMs == Preplay.RoleRevealDurationMs;
    }
}
