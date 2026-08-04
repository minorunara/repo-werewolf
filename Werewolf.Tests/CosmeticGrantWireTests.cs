using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CosmeticGrantWireTests
    {

        [Fact]
        public void Roundtrip_SingleActor_RestoresEquivalentGrant()
        {
            var original = CosmeticLottery.BuildGrant(new List<int> { 1 }, new Random(1));

            object[] wire = CosmeticGrantWire.ToWire(original);
            bool ok = CosmeticGrantWire.TryFromWire(wire, out var restored);

            Assert.True(ok);
            Assert.Equal(original.Actors, restored.Actors);
            Assert.Equal(original.Rarities, restored.Rarities);
        }

        [Fact]
        public void Roundtrip_MultipleActors_RestoresEquivalentGrant()
        {
            var original = CosmeticLottery.BuildGrant(new List<int> { 1, 2, 3, 4 }, new Random(2026));

            object[] wire = CosmeticGrantWire.ToWire(original);
            bool ok = CosmeticGrantWire.TryFromWire(wire, out var restored);

            Assert.True(ok);
            Assert.Equal(original.Actors, restored.Actors);
            Assert.Equal(original.Rarities, restored.Rarities);
        }

        [Fact]
        public void Roundtrip_EmptyGrant_RestoresEquivalentEmptyGrant()
        {
            var original = CosmeticLottery.BuildGrant(new List<int> { -101, -102 }, new Random(1));

            object[] wire = CosmeticGrantWire.ToWire(original);
            bool ok = CosmeticGrantWire.TryFromWire(wire, out var restored);

            Assert.True(ok);
            Assert.Empty(restored.Actors);
            Assert.Empty(restored.Rarities);
        }

        [Fact]
        public void Roundtrip_ExplicitRarityValues_PreservesExactByteValues()
        {
            var actors = new[] { 5, 7, 11, 13 };
            var rarities = new byte[actors.Length * CosmeticLottery.CoinsPerPlayer];
            for (int i = 0; i < rarities.Length; i++)
            {
                rarities[i] = (byte)(i % CoinRarity.Count);
            }
            var original = new CosmeticGrant(actors, rarities);

            object[] wire = CosmeticGrantWire.ToWire(original);
            bool ok = CosmeticGrantWire.TryFromWire(wire, out var restored);

            Assert.True(ok);
            Assert.Equal(actors, restored.Actors);
            Assert.Equal(rarities, restored.Rarities);
        }

        [Fact]
        public void ToWire_ProducesTwoElementArrayOfActorsThenRarities()
        {
            var grant = CosmeticLottery.BuildGrant(new List<int> { 9 }, new Random(3));

            object[] wire = CosmeticGrantWire.ToWire(grant);

            Assert.Equal(2, wire.Length);
            Assert.Same(grant.Actors, Assert.IsType<int[]>(wire[0]));
            Assert.Same(grant.Rarities, Assert.IsType<byte[]>(wire[1]));
        }

        [Fact]
        public void ToWire_NullGrant_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CosmeticGrantWire.ToWire(null));
        }

        [Fact]
        public void TryFromWire_NullPayload_ReturnsFalseAndNullGrant()
        {
            bool ok = CosmeticGrantWire.TryFromWire(null, out var grant);

            Assert.False(ok);
            Assert.Null(grant);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void TryFromWire_ElementCountNotTwo_ReturnsFalse(int elementCount)
        {
            var payload = new object[elementCount];
            for (int i = 0; i < elementCount; i++)
            {
                payload[i] = i == 0 ? (object)new int[] { 1 } : new byte[] { 0, 0 };
            }

            bool ok = CosmeticGrantWire.TryFromWire(payload, out var grant);

            Assert.False(ok);
            Assert.Null(grant);
        }

        [Fact]
        public void TryFromWire_ActorsElementWrongType_ReturnsFalse()
        {
            var payload = new object[] { new string[] { "1" }, new byte[] { 0, 0 } };

            bool ok = CosmeticGrantWire.TryFromWire(payload, out var grant);

            Assert.False(ok);
            Assert.Null(grant);
        }

        [Fact]
        public void TryFromWire_RaritiesElementWrongType_ReturnsFalse()
        {
            var payload = new object[] { new[] { 1 }, new[] { 0, 0 } };

            bool ok = CosmeticGrantWire.TryFromWire(payload, out var grant);

            Assert.False(ok);
            Assert.Null(grant);
        }

        [Fact]
        public void TryFromWire_RaritiesLengthMismatch_ReturnsFalse()
        {
            var payload = new object[] { new[] { 1, 2 }, new byte[] { 0, 1, 2 } };

            bool ok = CosmeticGrantWire.TryFromWire(payload, out var grant);

            Assert.False(ok);
            Assert.Null(grant);
        }

        [Fact]
        public void TryFromWire_RaritiesLengthExceedsExpected_ReturnsFalse()
        {
            var payload = new object[]
            {
                new[] { 1 },
                new byte[CosmeticLottery.CoinsPerPlayer + 1],
            };

            bool ok = CosmeticGrantWire.TryFromWire(payload, out var grant);

            Assert.False(ok);
            Assert.Null(grant);
        }

        [Theory]
        [InlineData(CoinRarity.Count)]
        [InlineData(255)]
        public void TryFromWire_RarityOutOfRange_ReturnsFalse(int outOfRangeValue)
        {
            var rarities = new byte[CosmeticLottery.CoinsPerPlayer];
            rarities[0] = (byte)outOfRangeValue;
            var payload = new object[] { new[] { 1 }, rarities };

            bool ok = CosmeticGrantWire.TryFromWire(payload, out var grant);

            Assert.False(ok);
            Assert.Null(grant);
        }

        [Fact]
        public void TryFromWire_OneOutOfRangeAmongOtherwiseValidRarities_ReturnsFalse()
        {
            var actors = new[] { 1, 2, 3, 4 };
            var rarities = new byte[actors.Length * CosmeticLottery.CoinsPerPlayer];
            for (int i = 0; i < rarities.Length; i++)
            {
                rarities[i] = (byte)(i % CoinRarity.Count);
            }
            rarities[rarities.Length - 1] = 200;
            var payload = new object[] { actors, rarities };

            bool ok = CosmeticGrantWire.TryFromWire(payload, out var grant);

            Assert.False(ok);
            Assert.Null(grant);
        }

        [Fact]
        public void TryGetCounts_AfterRoundtrip_AggregatesRarityCountsPerActor()
        {
            int perPlayer = CosmeticLottery.CoinsPerPlayer;
            var rarities = new byte[2 * perPlayer];
            for (int c = 0; c < perPlayer; c++)
            {
                rarities[c] = CoinRarity.Common;
                rarities[perPlayer + c] = CoinRarity.UltraRare;
            }
            var original = new CosmeticGrant(new[] { 1, 2 }, rarities);
            object[] wire = CosmeticGrantWire.ToWire(original);
            Assert.True(CosmeticGrantWire.TryFromWire(wire, out var restored));

            Assert.True(restored.TryGetCounts(1, out var counts1));
            Assert.Equal(new[] { perPlayer, 0, 0, 0 }, counts1);

            Assert.True(restored.TryGetCounts(2, out var counts2));
            Assert.Equal(new[] { 0, 0, 0, perPlayer }, counts2);
        }

        [Fact]
        public void TryGetCounts_AfterRoundtrip_UnknownActor_ReturnsFalseWithNullCounts()
        {
            var original = CosmeticLottery.BuildGrant(new List<int> { 1, 2 }, new Random(42));
            object[] wire = CosmeticGrantWire.ToWire(original);
            Assert.True(CosmeticGrantWire.TryFromWire(wire, out var restored));

            bool ok = restored.TryGetCounts(999, out var counts);

            Assert.False(ok);
            Assert.Null(counts);
        }

        [Fact]
        public void TryGetCounts_CountsSumToCoinsPerPlayerForEachActor()
        {
            var original = CosmeticLottery.BuildGrant(new List<int> { 10, 20, 30 }, new Random(777));
            object[] wire = CosmeticGrantWire.ToWire(original);
            Assert.True(CosmeticGrantWire.TryFromWire(wire, out var restored));

            foreach (int actor in restored.Actors)
            {
                Assert.True(restored.TryGetCounts(actor, out var counts));
                Assert.Equal(CoinRarity.Count, counts.Length);

                int total = 0;
                foreach (int c in counts)
                {
                    total += c;
                }
                Assert.Equal(CosmeticLottery.CoinsPerPlayer, total);
            }
        }
    }
}
