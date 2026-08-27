using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ConveneChargeToneTests
    {
        [Fact]
        public void Pitch_RisesMonotonicallyByOneOctave()
        {
            float start = ConveneChargeTone.PitchFor(0f);
            float quarter = ConveneChargeTone.PitchFor(0.25f);
            float half = ConveneChargeTone.PitchFor(0.5f);
            float end = ConveneChargeTone.PitchFor(1f);

            Assert.True(start < quarter);
            Assert.True(quarter < half);
            Assert.True(half < end);
            Assert.Equal(start * 2f, end, 4);
        }

        [Theory]
        [InlineData(-1f, ConveneChargeTone.StartPitch)]
        [InlineData(0f, ConveneChargeTone.StartPitch)]
        [InlineData(1f, ConveneChargeTone.StartPitch * 2f)]
        [InlineData(2f, ConveneChargeTone.StartPitch * 2f)]
        public void Pitch_ClampsRatio(float ratio, float expected)
        {
            Assert.Equal(expected, ConveneChargeTone.PitchFor(ratio), 4);
        }

        [Theory]
        [InlineData(-1f, ConveneChargeTone.StartVolume)]
        [InlineData(0f, ConveneChargeTone.StartVolume)]
        [InlineData(0.5f, 0.50f)]
        [InlineData(1f, ConveneChargeTone.EndVolume)]
        [InlineData(2f, ConveneChargeTone.EndVolume)]
        public void Volume_ClampsAndRises(float ratio, float expected)
        {
            Assert.Equal(expected, ConveneChargeTone.VolumeFor(ratio), 4);
        }
    }
}
