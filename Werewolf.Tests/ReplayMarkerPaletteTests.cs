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
    }
}
