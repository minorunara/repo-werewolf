using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CosmeticLotteryTests
    {

        [Theory]
        [InlineData(0, CoinRarity.Common)]
        [InlineData(279, CoinRarity.Common)]
        [InlineData(280, CoinRarity.Uncommon)]
        [InlineData(410, CoinRarity.Uncommon)]
        [InlineData(411, CoinRarity.Rare)]
        [InlineData(515, CoinRarity.Rare)]
        [InlineData(516, CoinRarity.UltraRare)]
        [InlineData(546, CoinRarity.UltraRare)]
        public void DrawRarity_BoundaryValues_MapToExpectedRarity(int roll, byte expectedRarity)
        {
            Assert.Equal(expectedRarity, CosmeticLottery.DrawRarity(roll));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(547)]
        public void DrawRarity_OutOfRange_ThrowsArgumentOutOfRangeException(int roll)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CosmeticLottery.DrawRarity(roll));
        }

        [Fact]
        public void ValidateWeights_ReturnsTrue()
        {
            Assert.True(CosmeticLottery.ValidateWeights());
        }

        [Fact]
        public void WeightConstants_SumTo547()
        {
            Assert.Equal(
                547,
                CosmeticLottery.WeightCommon + CosmeticLottery.WeightUncommon
                    + CosmeticLottery.WeightRare + CosmeticLottery.WeightUltraRare);
            Assert.Equal(547, CosmeticLottery.TotalWeight);
        }

        [Fact]
        public void BuildGrant_FixedSeed_ProducesCoinsPerPlayerRaritiesPerActorInCallOrder()
        {
            var actors = new List<int> { 1, 2, 3 };

            var grant = CosmeticLottery.BuildGrant(actors, new Random(12345));

            var expected = new byte[actors.Count * CosmeticLottery.CoinsPerPlayer];
            var replay = new Random(12345);
            for (int i = 0; i < actors.Count; i++)
            {
                for (int c = 0; c < CosmeticLottery.CoinsPerPlayer; c++)
                {
                    expected[i * CosmeticLottery.CoinsPerPlayer + c] = CosmeticLottery.Draw(replay);
                }
            }

            Assert.Equal(expected, grant.Rarities);
            Assert.Equal(actors.Count * CosmeticLottery.CoinsPerPlayer, grant.Rarities.Length);
        }

        [Fact]
        public void BuildGrant_SameSeed_IsReproducibleAcrossIndependentCalls()
        {
            var actors = new List<int> { 1, 2, 3, 4 };

            var grantA = CosmeticLottery.BuildGrant(actors, new Random(999));
            var grantB = CosmeticLottery.BuildGrant(actors, new Random(999));

            Assert.Equal(grantA.Actors, grantB.Actors);
            Assert.Equal(grantA.Rarities, grantB.Rarities);
        }

        [Fact]
        public void BuildGrant_EachActor_ReceivesExactlyCoinsPerPlayerCoins()
        {
            var actors = new List<int> { 1, 2, 3 };

            var grant = CosmeticLottery.BuildGrant(actors, new Random(7));

            foreach (int actor in actors)
            {
                Assert.True(grant.TryGetCounts(actor, out var counts));
                int total = 0;
                foreach (int count in counts)
                {
                    total += count;
                }
                Assert.Equal(CosmeticLottery.CoinsPerPlayer, total);
            }
        }

        [Fact]
        public void BuildGrant_RosterWithBots_ExcludesNonPositiveActorsFromGrant()
        {
            var roster = new List<int> { 1, -101, 3, -102 };

            var grant = CosmeticLottery.BuildGrant(roster, new Random(1));

            Assert.Equal(new[] { 1, 3 }, grant.Actors);
            Assert.Equal(2 * CosmeticLottery.CoinsPerPlayer, grant.Rarities.Length);
            Assert.False(grant.TryGetCounts(-101, out _));
            Assert.False(grant.TryGetCounts(-102, out _));
            Assert.True(grant.TryGetCounts(1, out _));
            Assert.True(grant.TryGetCounts(3, out _));
        }

        [Fact]
        public void BuildGrant_RosterAllBots_ProducesEmptyGrant()
        {
            var roster = new List<int> { -101, -102 };

            var grant = CosmeticLottery.BuildGrant(roster, new Random(1));

            Assert.Empty(grant.Actors);
            Assert.Empty(grant.Rarities);
        }

        [Fact]
        public void BuildGrant_Signature_TakesOnlyActorRosterAndRandom_NoTeamOrOutcomeInput()
        {
            var method = typeof(CosmeticLottery).GetMethod(nameof(CosmeticLottery.BuildGrant));
            Assert.NotNull(method);
            var parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(typeof(IReadOnlyList<int>), parameters[0].ParameterType);
            Assert.Equal(typeof(Random), parameters[1].ParameterType);
        }

        [Fact]
        public void BuildGrant_DeadOrSpectatingActor_IsStillIncluded_NoStatusInputExists()
        {
            var roster = new List<int> { 1, 2, 3 };

            var grant = CosmeticLottery.BuildGrant(roster, new Random(1));

            Assert.Equal(roster, grant.Actors);
        }

        [Fact]
        public void BuildGrant_NullArguments_ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => CosmeticLottery.BuildGrant(null, new Random(1)));
            Assert.Throws<ArgumentNullException>(
                () => CosmeticLottery.BuildGrant(new List<int> { 1 }, null));
        }

        [Fact]
        public void Draw_NullRandom_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CosmeticLottery.Draw(null));
        }

        [Fact]
        public void Draw_ProducesValuesWithinValidRarityRange()
        {
            var rng = new Random(2026);
            for (int i = 0; i < 2000; i++)
            {
                byte rarity = CosmeticLottery.Draw(rng);
                Assert.InRange(rarity, CoinRarity.Common, CoinRarity.UltraRare);
            }
        }

        [Fact]
        public void CosmeticGrant_RaritiesLengthMismatch_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => new CosmeticGrant(new[] { 1, 2 }, new byte[] { 0, 1, 2 }));
        }

        [Fact]
        public void CosmeticGrant_TryGetCounts_UnknownActor_ReturnsFalse()
        {
            var grant = CosmeticLottery.BuildGrant(new List<int> { 1 }, new Random(1));

            Assert.False(grant.TryGetCounts(999, out var counts));
            Assert.Null(counts);
        }
    }
}
