using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class NecroVoiceModeTests
    {

        [Fact]
        public void NecroVoiceMode_ByteAssignments_MatchSpec()
        {
            Assert.Equal((byte)0, (byte)NecroVoiceMode.Off);
            Assert.Equal((byte)1, (byte)NecroVoiceMode.NonWerewolfDead);
            Assert.Equal((byte)2, (byte)NecroVoiceMode.AllDead);
        }

        [Fact]
        public void GameConfig_Default_IsNonWerewolfDead()
        {
            var config = new GameConfig();

            Assert.Equal(NecroVoiceMode.NonWerewolfDead, config.NecroVoiceMode);
        }

        [Theory]
        [InlineData((byte)0, NecroVoiceMode.Off)]
        [InlineData((byte)1, NecroVoiceMode.NonWerewolfDead)]
        [InlineData((byte)2, NecroVoiceMode.AllDead)]
        public void FromByte_KnownValue_MapsToCorrespondingMode(byte input, NecroVoiceMode expected)
        {
            Assert.Equal(expected, NecroVoiceModes.FromByte(input));
        }

        [Theory]
        [InlineData((byte)3)]
        [InlineData((byte)10)]
        [InlineData((byte)255)]
        public void FromByte_UnknownValue_FallsBackToOff(byte input)
        {
            Assert.Equal(NecroVoiceMode.Off, NecroVoiceModes.FromByte(input));
        }

    }
}
