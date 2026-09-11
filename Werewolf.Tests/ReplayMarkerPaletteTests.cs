using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayMarkerPaletteTests
    {
        [Fact]
        public void ColorFor_KnownValue_Id1()
        {
            ReplayMarkerPalette.ColorFor(1, out float r, out float g, out float b);
            Assert.Equal(0.352f, r, 2);
            Assert.Equal(0.848f, g, 2);
            Assert.Equal(0.497f, b, 2);
        }

        [Fact]
        public void ColorFor_AlwaysInRange_ForManyIds()
        {
            for (int id = 1; id <= 200; id++)
            {
                ReplayMarkerPalette.ColorFor(id, out float r, out float g, out float b);
                Assert.InRange(r, 0f, 1f);
                Assert.InRange(g, 0f, 1f);
                Assert.InRange(b, 0f, 1f);
            }
        }

        [Fact]
        public void ColorFor_NonPositiveId_FallsBackToGray()
        {
            ReplayMarkerPalette.ColorFor(0, out float r, out float g, out float b);
            Assert.Equal(r, g, 3);
            Assert.Equal(g, b, 3);
        }

        [Fact]
        public void AdjacentIds_GetDistinctHues()
        {
            ReplayMarkerPalette.ColorFor(1, out float r1, out float g1, out float b1);
            ReplayMarkerPalette.ColorFor(2, out float r2, out float g2, out float b2);
            float diff = System.Math.Abs(r1 - r2) + System.Math.Abs(g1 - g2) + System.Math.Abs(b1 - b2);
            Assert.True(diff > 0.2f, $"隣接IDの色が近すぎる diff={diff}");
        }

        [Fact]
        public void Hex_RoundTrips_AndParsesCaseInsensitively()
        {
            Assert.Equal("#ff8000", ReplayMarkerPalette.ToHex(1f, 0.502f, 0f));
            Assert.True(ReplayMarkerPalette.TryParseHex("#FF8000", out float r, out float g, out float b));
            Assert.Equal(1f, r, 3);
            Assert.Equal(0.502f, g, 3);
            Assert.Equal(0f, b, 3);
            Assert.Equal("#000000", ReplayMarkerPalette.ToHex(-1f, float.NaN, 0f));
            Assert.Equal("#ffffff", ReplayMarkerPalette.ToHex(2f, 1f, 1f));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ff8000")]
        [InlineData("#ff80")]
        [InlineData("#gg8000")]
        public void TryParseHex_RejectsMalformed(string hex)
        {
            Assert.False(ReplayMarkerPalette.TryParseHex(hex, out _, out _, out _));
        }

        [Fact]
        public void ColorFor_PrefersHeadColor_FallsBackToIdColorWithoutIt()
        {
            ReplayMarkerPalette.ColorFor(1, "#102030", out float r, out float g, out float b);
            Assert.Equal(0x10 / 255f, r, 3);
            Assert.Equal(0x20 / 255f, g, 3);
            Assert.Equal(0x30 / 255f, b, 3);

            ReplayMarkerPalette.ColorFor(1, out float er, out float eg, out float eb);
            ReplayMarkerPalette.ColorFor(1, null, out r, out g, out b);
            Assert.Equal(er, r, 3); Assert.Equal(eg, g, 3); Assert.Equal(eb, b, 3);
            ReplayMarkerPalette.ColorFor(1, "bad", out r, out g, out b);
            Assert.Equal(er, r, 3); Assert.Equal(eg, g, 3); Assert.Equal(eb, b, 3);
        }

        [Fact]
        public void IsDark_SplitsInkByLuminance()
        {
            Assert.True(ReplayMarkerPalette.IsDark(0f, 0f, 0f));
            Assert.True(ReplayMarkerPalette.IsDark(0.1f, 0.1f, 0.4f));
            Assert.True(ReplayMarkerPalette.IsDark(1f, 0f, 0f));
            Assert.False(ReplayMarkerPalette.IsDark(1f, 1f, 1f));
            Assert.False(ReplayMarkerPalette.IsDark(0f, 1f, 0f));
            Assert.False(ReplayMarkerPalette.IsDark(1f, 0.9f, 0.2f));
        }
    }
}
