using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class CoinRarity
    {
        public const byte Common = 0;
        public const byte Uncommon = 1;
        public const byte Rare = 2;
        public const byte UltraRare = 3;
        public const int Count = 4;
    }

    public static class CosmeticLottery
    {
        public const int CoinsPerPlayer = 1;

        public const int WeightCommon = 280;
        public const int WeightUncommon = 131;
        public const int WeightRare = 105;
        public const int WeightUltraRare = 31;
        public const int TotalWeight = 547;

        public static bool ValidateWeights()
        {
            return WeightCommon + WeightUncommon + WeightRare + WeightUltraRare == TotalWeight;
        }

        public static byte DrawRarity(int roll)
        {
            if (roll < 0 || roll >= TotalWeight)
            {
                throw new ArgumentOutOfRangeException(nameof(roll), roll,
                    "roll は 0〜" + (TotalWeight - 1) + " の範囲でなければなりません。");
            }

            if (roll < WeightCommon)
            {
                return CoinRarity.Common;
            }

            int uncommonEnd = WeightCommon + WeightUncommon;
            if (roll < uncommonEnd)
            {
                return CoinRarity.Uncommon;
            }

            int rareEnd = uncommonEnd + WeightRare;
            if (roll < rareEnd)
            {
                return CoinRarity.Rare;
            }

            return CoinRarity.UltraRare;
        }

        public static byte Draw(Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            return DrawRarity(rng.Next(TotalWeight));
        }

        public static CosmeticGrant BuildGrant(IReadOnlyList<int> rosterActors, Random rng)
        {
            if (rosterActors == null) throw new ArgumentNullException(nameof(rosterActors));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var actors = new List<int>(rosterActors.Count);
            for (int i = 0; i < rosterActors.Count; i++)
            {
                int actor = rosterActors[i];
                if (actor > 0)
                {
                    actors.Add(actor);
                }
            }

            var rarities = new byte[actors.Count * CoinsPerPlayer];
            for (int i = 0; i < actors.Count; i++)
            {
                for (int c = 0; c < CoinsPerPlayer; c++)
                {
                    rarities[i * CoinsPerPlayer + c] = Draw(rng);
                }
            }

            return new CosmeticGrant(actors.ToArray(), rarities);
        }
    }

    public sealed class CosmeticGrant
    {
        public CosmeticGrant(int[] actors, byte[] rarities)
        {
            if (actors == null) throw new ArgumentNullException(nameof(actors));
            if (rarities == null) throw new ArgumentNullException(nameof(rarities));
            if (rarities.Length != actors.Length * CosmeticLottery.CoinsPerPlayer)
            {
                throw new ArgumentException(
                    "rarities の長さは actors.Length * CoinsPerPlayer と一致していなければなりません。",
                    nameof(rarities));
            }

            Actors = actors;
            Rarities = rarities;
        }

        public int[] Actors { get; }

        public byte[] Rarities { get; }

        public bool TryGetCounts(int actor, out int[] countsByRarity)
        {
            for (int i = 0; i < Actors.Length; i++)
            {
                if (Actors[i] != actor)
                {
                    continue;
                }

                var counts = new int[CoinRarity.Count];
                int baseIndex = i * CosmeticLottery.CoinsPerPlayer;
                for (int c = 0; c < CosmeticLottery.CoinsPerPlayer; c++)
                {
                    byte rarity = Rarities[baseIndex + c];
                    counts[rarity]++;
                }

                countsByRarity = counts;
                return true;
            }

            countsByRarity = null;
            return false;
        }
    }
}
