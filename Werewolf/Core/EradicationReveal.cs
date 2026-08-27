using System;

namespace Werewolf.Core
{
    public static class WWEradicationCodes
    {
        public const byte EradicationReveal = 192;
    }

    public static class EradicationCeremony
    {
        public const float GraceSec = 0.5f;

        public const float BackdropFadeSec = 0.3f;

        public const float StampEntranceSec = 0.45f;

        public const float StampHoldSec = 3.0f;

        public const int CeremonyMs = 4500;

        public static TextId TitleId(byte outcome, bool vanished)
        {
            if (outcome == TeamCodes.PeacefulCollapsed)
            {
                return TextId.EradicationPeacefulCollapsed;
            }
            if (outcome == (byte)Team.Villagers)
            {
                return vanished
                    ? TextId.EradicationLastWerewolfVanished
                    : TextId.EradicationLastWerewolfDied;
            }
            return vanished
                ? TextId.EradicationLastVillagerVanished
                : TextId.EradicationLastVillagerDied;
        }
    }

    public sealed class EradicationRevealData
    {
        public EradicationRevealData(int actorNumber, byte outcome, bool vanished, string name)
        {
            ActorNumber = actorNumber;
            Outcome = outcome;
            Vanished = vanished;
            Name = name;
        }

        public int ActorNumber { get; }

        public byte Outcome { get; }

        public bool Vanished { get; }

        public string Name { get; }
    }

    public static class EradicationRevealWire
    {
        private const byte FlagVanished = 1 << 0;

        public static object[] ToWire(int actorNumber, byte outcome, bool vanished, string name)
            => new object[]
            {
                actorNumber,
                outcome,
                (byte)(vanished ? FlagVanished : 0),
                name ?? "",
            };

        public static EradicationRevealData FromWire(object[] payload)
        {
            if (payload == null || payload.Length < 4) return null;
            if (!(payload[0] is int actor) || !(payload[1] is byte outcome)
                || !(payload[2] is byte flags) || !(payload[3] is string name))
            {
                return null;
            }
            if (outcome != (byte)Team.Villagers && outcome != (byte)Team.Werewolves
                && outcome != TeamCodes.PeacefulCollapsed)
            {
                return null;
            }
            return new EradicationRevealData(actor, outcome, (flags & FlagVanished) != 0, name);
        }
    }
}
