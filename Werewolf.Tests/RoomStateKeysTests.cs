using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RoomStateKeysTests
    {

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeDecodeBool_RoundTrips(bool value)
        {
            byte encoded = RoomStateKeys.EncodeBool(value);
            bool decoded = RoomStateKeys.DecodeBool(encoded);

            Assert.Equal(value, decoded);
        }

        [Fact]
        public void EncodeBool_UsesZeroOneForm()
        {
            Assert.Equal((byte)1, RoomStateKeys.EncodeBool(true));
            Assert.Equal((byte)0, RoomStateKeys.EncodeBool(false));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(255)]
        public void EncodeDecodeRights_RoundTrips(int remaining)
        {
            byte encoded = RoomStateKeys.EncodeRights(remaining);
            int decoded = RoomStateKeys.DecodeRights(encoded);

            Assert.Equal(remaining, decoded);
        }

        [Fact]
        public void EncodeRights_ClampsNegativeToZero()
        {
            Assert.Equal((byte)0, RoomStateKeys.EncodeRights(-5));
        }

        [Fact]
        public void EncodeRights_ClampsOverflowToByteMax()
        {
            Assert.Equal(byte.MaxValue, RoomStateKeys.EncodeRights(1000));
        }

        [Fact]
        public void AllKeys_ContainsExactlyExpectedPublicKeys()
        {
            var expected = new[]
            {
                "WW_Phase", "WW_RoundEndTime", "WW_IsAlive", "WW_MeetingCaller", "WW_MeetingEndTime",
                "WW_CfgMinimapHide", "WW_CfgCatPossible", "WW_CfgValuableMapMode", "WW_Rights", "WW_CfgShared",
                "WW_CfgNecroVoiceMode",
                "WW_CfgExtraJump", "WW_CfgConveneSuppressStart", "WW_CfgConveneSuppressAfter",
                "WW_CfgHealInterval",
                "WW_CfgOutfitChange",
                "WW_CfgBomb",
                "WW_CfgShaman",
            };

            Assert.Equal(expected.Length, RoomStateKeys.AllKeys.Length);
            foreach (var key in expected)
            {
                Assert.Contains(key, RoomStateKeys.AllKeys);
            }
        }

        [Theory]
        [InlineData("Role")]
        [InlineData("Team")]
        [InlineData("Werewolves")]
        [InlineData("BlackCatCount")]
        [InlineData("Vote")]
        [InlineData("Secret")]
        public void AllKeys_NeverContainsSecretSoundingSubstring(string forbiddenSubstring)
        {
            foreach (var key in RoomStateKeys.AllKeys)
            {
                Assert.DoesNotContain(forbiddenSubstring, key);
            }
        }

        [Theory]
        [InlineData(RoomStateKeys.CfgMinimapHide)]
        [InlineData(RoomStateKeys.CfgCatPossible)]
        [InlineData(RoomStateKeys.Rights)]
        [InlineData(RoomStateKeys.CfgShared)]
        [InlineData(RoomStateKeys.CfgNecroVoiceMode)]
        [InlineData(RoomStateKeys.CfgExtraJump)]
        [InlineData(RoomStateKeys.CfgConveneSuppressStart)]
        [InlineData(RoomStateKeys.CfgConveneSuppressAfter)]
        [InlineData(RoomStateKeys.CfgOutfitChange)]
        [InlineData(RoomStateKeys.CfgBomb)]
        [InlineData(RoomStateKeys.CfgShaman)]
        public void NewKeys_ArePresentInAllKeys(string key)
        {
            Assert.Contains(key, RoomStateKeys.AllKeys);
        }

        [Fact]
        public void EncodeBomb_UsesFixedLength()
        {
            var packed = RoomStateKeys.EncodeBomb(new GameConfig(), playerCount: 5);
            Assert.Equal(RoomStateKeys.BombIndex.Length, packed.Length);
        }

        [Fact]
        public void EncodeBomb_HeadIsBomberPossibleFlag()
        {
            var withBomber = RoomStateKeys.EncodeBomb(
                new GameConfig { WerewolfCount = 2, BomberChancePercent = 50 }, playerCount: 5);
            Assert.Equal(1, withBomber[RoomStateKeys.BombIndex.BomberPossible]);

            var withoutBomber = RoomStateKeys.EncodeBomb(
                new GameConfig { WerewolfCount = 2, BomberChancePercent = 0 }, playerCount: 5);
            Assert.Equal(0, withoutBomber[RoomStateKeys.BombIndex.BomberPossible]);

            var noSlot = RoomStateKeys.EncodeBomb(
                new GameConfig { WerewolfCount = 1, BomberChancePercent = 50 }, playerCount: 5);
            Assert.Equal(0, noSlot[RoomStateKeys.BombIndex.BomberPossible]);
        }

        [Fact]
        public void EncodeBomb_MetersStoredAsCentimeters()
        {
            var packed = RoomStateKeys.EncodeBomb(new GameConfig
            {
                BomberProximityMeters = 2,
                BomberBlastRadiusMeters = 4.0f,
            }, playerCount: 5);
            Assert.Equal(200, packed[RoomStateKeys.BombIndex.ProximityCm]);
            Assert.Equal(400, packed[RoomStateKeys.BombIndex.BlastRadiusCm]);
        }

        [Fact]
        public void EncodeBomb_SecondsStoredAsMilliseconds()
        {
            var packed = RoomStateKeys.EncodeBomb(new GameConfig
            {
                BomberGaugeFullSec = 20,
                BomberInitialCooldownSec = 60,
                BomberCooldownSec = 30,
            }, playerCount: 5);
            Assert.Equal(20_000, packed[RoomStateKeys.BombIndex.GaugeFullMs]);
            Assert.Equal(30_000, packed[RoomStateKeys.BombIndex.CooldownMs]);
            Assert.Equal(60_000, packed[RoomStateKeys.BombIndex.InitialCooldownMs]);
        }

        [Fact]
        public void EncodeBomb_DamagesStoredAsIntegers()
        {
            var packed = RoomStateKeys.EncodeBomb(new GameConfig
            {
                BomberBlastPlayerDamage = 60,
                BomberBlastEnemyDamage = 75,
            }, playerCount: 5);
            Assert.Equal(60, packed[RoomStateKeys.BombIndex.BlastPlayerDamage]);
            Assert.Equal(75, packed[RoomStateKeys.BombIndex.BlastEnemyDamage]);
        }

        [Fact]
        public void EncodeBomb_NegativeMetersClampToZero()
        {
            var packed = RoomStateKeys.EncodeBomb(new GameConfig
            {
                BomberProximityMeters = -5,
                BomberBlastRadiusMeters = -1f,
            }, playerCount: 5);
            Assert.Equal(0, packed[RoomStateKeys.BombIndex.ProximityCm]);
            Assert.Equal(0, packed[RoomStateKeys.BombIndex.BlastRadiusCm]);
        }

        [Fact]
        public void BombIndex_ValuesAreDistinctAndCoverPack()
        {
            var indexes = new[]
            {
                RoomStateKeys.BombIndex.BomberPossible,
                RoomStateKeys.BombIndex.ProximityCm,
                RoomStateKeys.BombIndex.GaugeFullMs,
                RoomStateKeys.BombIndex.CooldownMs,
                RoomStateKeys.BombIndex.BlastRadiusCm,
                RoomStateKeys.BombIndex.BlastPlayerDamage,
                RoomStateKeys.BombIndex.BlastEnemyDamage,
                RoomStateKeys.BombIndex.InitialCooldownMs,
            };
            Assert.Equal(RoomStateKeys.BombIndex.Length, indexes.Length);
            Assert.Equal(indexes.Length, System.Linq.Enumerable.Count(System.Linq.Enumerable.Distinct(indexes)));
            for (int i = 0; i < indexes.Length; i++)
            {
                Assert.InRange(indexes[i], 0, RoomStateKeys.BombIndex.Length - 1);
            }
        }

        [Theory]
        [InlineData(NecroVoiceMode.Off)]
        [InlineData(NecroVoiceMode.NonWerewolfDead)]
        [InlineData(NecroVoiceMode.AllDead)]
        public void EncodeDecodeNecroVoiceMode_RoundTrips(NecroVoiceMode mode)
        {
            byte encoded = RoomStateKeys.EncodeNecroVoiceMode(mode);
            NecroVoiceMode decoded = RoomStateKeys.DecodeNecroVoiceMode(encoded);

            Assert.Equal(mode, decoded);
        }

        [Fact]
        public void EncodeNecroVoiceMode_UsesEnumByteValue()
        {
            Assert.Equal((byte)0, RoomStateKeys.EncodeNecroVoiceMode(NecroVoiceMode.Off));
            Assert.Equal((byte)1, RoomStateKeys.EncodeNecroVoiceMode(NecroVoiceMode.NonWerewolfDead));
            Assert.Equal((byte)2, RoomStateKeys.EncodeNecroVoiceMode(NecroVoiceMode.AllDead));
        }

        [Theory]
        [InlineData((byte)3)]
        [InlineData((byte)10)]
        [InlineData((byte)255)]
        public void DecodeNecroVoiceMode_UnknownValueFallsBackToOff(byte value)
        {
            Assert.Equal(NecroVoiceMode.Off, RoomStateKeys.DecodeNecroVoiceMode(value));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(20)]
        [InlineData(254)]
        public void EncodeDecodeExtraJump_RoundTrips(int count)
        {
            byte encoded = RoomStateKeys.EncodeExtraJump(count);
            int decoded = RoomStateKeys.DecodeExtraJump(encoded);

            Assert.Equal(count, decoded);
        }

        [Fact]
        public void EncodeExtraJump_MapsInfiniteToZeroByte()
        {
            Assert.Equal((byte)0, RoomStateKeys.EncodeExtraJump(-1));
        }

        [Fact]
        public void EncodeExtraJump_ClampsBelowNegativeOneToNegativeOne()
        {
            Assert.Equal(-1, RoomStateKeys.DecodeExtraJump(RoomStateKeys.EncodeExtraJump(-100)));
        }

        [Fact]
        public void EncodeExtraJump_ClampsOverflowToByteRange()
        {
            Assert.Equal(254, RoomStateKeys.DecodeExtraJump(RoomStateKeys.EncodeExtraJump(10000)));
        }
    }
}
