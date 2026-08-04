using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MapGridMathTests
    {
        [Theory]
        [InlineData(0, "A")]
        [InlineData(1, "B")]
        [InlineData(25, "Z")]
        [InlineData(26, "AA")]
        [InlineData(27, "AB")]
        [InlineData(51, "AZ")]
        [InlineData(52, "BA")]
        [InlineData(-1, "?")]
        public void ColumnLetter_ExcelStyle(int index, string expected)
        {
            Assert.Equal(expected, MapGridMath.ColumnLetter(index));
        }

        [Theory]
        [InlineData(5, 5, 1)]
        [InlineData(5, 2, 4)]
        [InlineData(3, 0, 4)]
        public void RowNumber_TopOfRangeIsOne(int rowMax, int rowIndex, int expected)
        {
            Assert.Equal(expected, MapGridMath.RowNumber(rowMax, rowIndex));
        }

        [Fact]
        public void RangeFromModuleCenters_GeneratorFormulaRoundTrips()
        {
            var centers = new List<(float X, float Z)>
            {
                ((1 - 3) * 15f, 2 * 15f + 7.5f),
                ((2 - 3) * 15f, 2 * 15f + 7.5f),
                ((2 - 3) * 15f, 3 * 15f + 7.5f),
            };
            MapGridCellRange? range = MapGridMath.RangeFromModuleCenters(centers, 6, 6);
            Assert.NotNull(range);
            Assert.Equal(1, range.Value.ColMin);
            Assert.Equal(2, range.Value.ColMax);
            Assert.Equal(2, range.Value.RowMin);
            Assert.Equal(3, range.Value.RowMax);
        }

        [Fact]
        public void RangeFromModuleCenters_PositionNoiseIsAbsorbedByRounding()
        {
            var centers = new List<(float X, float Z)>
            {
                ((1 - 3) * 15f + 0.4f, 2 * 15f + 7.5f - 0.4f),
            };
            MapGridCellRange? range = MapGridMath.RangeFromModuleCenters(centers, 6, 6);
            Assert.NotNull(range);
            Assert.Equal(1, range.Value.ColMin);
            Assert.Equal(2, range.Value.RowMin);
        }

        [Fact]
        public void RangeFromModuleCenters_OutOfLatticePointsAreIgnored()
        {
            var centers = new List<(float X, float Z)>
            {
                (500f, 500f),
                ((0 - 3) * 15f, 0f + 7.5f),
            };
            MapGridCellRange? range = MapGridMath.RangeFromModuleCenters(centers, 6, 6);
            Assert.NotNull(range);
            Assert.Equal(0, range.Value.ColMin);
            Assert.Equal(0, range.Value.ColMax);

            Assert.Null(MapGridMath.RangeFromModuleCenters(
                new List<(float X, float Z)> { (500f, 500f) }, 6, 6));
            Assert.Null(MapGridMath.RangeFromModuleCenters(
                new List<(float X, float Z)>(), 6, 6));
            Assert.Null(MapGridMath.RangeFromModuleCenters(null, 6, 6));
        }

        [Fact]
        public void RangeFromBounds_WallSpillDoesNotAddPhantomCells()
        {
            MapGridCellRange? range = MapGridMath.RangeFromBounds(
                6, 6, 0.1f, 0f, 0f,
                -3.80f, -0.70f, 2.95f, 6.05f);
            Assert.NotNull(range);
            Assert.Equal(1, range.Value.ColMin);
            Assert.Equal(2, range.Value.ColMax);
            Assert.Equal(2, range.Value.RowMin);
            Assert.Equal(3, range.Value.RowMax);
        }

        [Fact]
        public void RangeFromBounds_InvalidInputsReturnNull()
        {
            Assert.Null(MapGridMath.RangeFromBounds(0, 4, 0.1f, 0f, 0f, -1f, 1f, 0f, 1f));
            Assert.Null(MapGridMath.RangeFromBounds(4, 0, 0.1f, 0f, 0f, -1f, 1f, 0f, 1f));
            Assert.Null(MapGridMath.RangeFromBounds(4, 4, 0.1f, 0f, 0f, 1f, -1f, 0f, 1f));
        }

        private static MapGridLayout ComputeReference()
        {
            return MapGridMath.Compute(
                levelWidth: 4, new MapGridCellRange(0, 3, 0, 3),
                mapScale: 0.1f, originMiniX: 0f, originMiniZ: 0f,
                camMiniX: -0.75f, camMiniZ: 3f,
                orthoSize: 3f, aspect: 5f / 3f,
                panelWidth: 1200f, panelHeight: 720f);
        }

        [Fact]
        public void Compute_Reference_LinesSnapToModuleLattice()
        {
            MapGridLayout g = ComputeReference();
            Assert.NotNull(g);

            Assert.Equal(new[] { -360f, -180f, 0f, 180f, 360f },
                g.VerticalLineX.Select(v => (float)System.Math.Round(v, 3)).ToArray());
            Assert.Equal(new[] { -360f, -180f, 0f, 180f, 360f },
                g.HorizontalLineY.Select(v => (float)System.Math.Round(v, 3)).ToArray());

            Assert.Equal(-360f, g.RectLeft, 3);
            Assert.Equal(360f, g.RectRight, 3);
            Assert.Equal(-360f, g.RectBottom, 3);
            Assert.Equal(360f, g.RectTop, 3);
        }

        [Fact]
        public void Compute_Reference_LabelsAndPositions()
        {
            MapGridLayout g = ComputeReference();
            Assert.NotNull(g);

            Assert.Equal(new[] { "A", "B", "C", "D" }, g.ColumnLabels);
            Assert.Equal(new[] { -270f, -90f, 90f, 270f },
                g.ColumnLabelX.Select(v => (float)System.Math.Round(v, 3)).ToArray());

            Assert.Equal(new[] { "4", "3", "2", "1" }, g.RowLabels);
            Assert.Equal(new[] { -270f, -90f, 90f, 270f },
                g.RowLabelY.Select(v => (float)System.Math.Round(v, 3)).ToArray());
        }

        [Fact]
        public void Compute_Reference_LabelAnchorsClampIntoPanel()
        {
            MapGridLayout g = ComputeReference();
            Assert.NotNull(g);

            Assert.Equal(720f * 0.5f - MapGridMath.LabelEdgeInsetPanel, g.ColumnLabelY, 3);
            Assert.Equal(-382f, g.RowLabelX, 3);
        }

        [Fact]
        public void Compute_PartialRange_TopLeftCellIsAlwaysA1()
        {
            MapGridLayout g = MapGridMath.Compute(
                levelWidth: 6, new MapGridCellRange(1, 2, 2, 3),
                mapScale: 0.1f, originMiniX: 0f, originMiniZ: 0f,
                camMiniX: -2.25f, camMiniZ: 4.5f,
                orthoSize: 2f, aspect: 5f / 3f,
                panelWidth: 1200f, panelHeight: 720f);
            Assert.NotNull(g);

            Assert.Equal(new[] { "A", "B" }, g.ColumnLabels);
            Assert.Equal(new[] { "2", "1" }, g.RowLabels);
            Assert.Equal(3, g.VerticalLineX.Length);
            Assert.Equal(3, g.HorizontalLineY.Length);

            Assert.Equal(-270f, g.RectLeft, 3);
        }

        [Fact]
        public void Compute_OddWidth_IntegerDivisionOffsetMatchesGenerator()
        {
            MapGridLayout g = MapGridMath.Compute(
                levelWidth: 5, new MapGridCellRange(0, 4, 0, 1),
                mapScale: 0.1f, originMiniX: 0f, originMiniZ: 0f,
                camMiniX: 0f, camMiniZ: 1.5f,
                orthoSize: 3.75f * 0.6f, aspect: 5f / 3f,
                panelWidth: 1200f, panelHeight: 720f);
            Assert.NotNull(g);

            Assert.Equal(6, g.VerticalLineX.Length);
            Assert.Equal(-g.VerticalLineX[5], g.VerticalLineX[0], 3);
            Assert.Equal(new[] { "A", "B", "C", "D", "E" }, g.ColumnLabels);
        }

        [Fact]
        public void Compute_OriginOffset_ShiftsWithMiniature()
        {
            MapGridLayout baseLayout = ComputeReference();
            MapGridLayout shifted = MapGridMath.Compute(
                levelWidth: 4, new MapGridCellRange(0, 3, 0, 3),
                mapScale: 0.1f, originMiniX: 10f, originMiniZ: -5f,
                camMiniX: 10f - 0.75f, camMiniZ: -5f + 3f,
                orthoSize: 3f, aspect: 5f / 3f,
                panelWidth: 1200f, panelHeight: 720f);
            Assert.NotNull(shifted);

            for (int i = 0; i < baseLayout.VerticalLineX.Length; i++)
            {
                Assert.Equal(baseLayout.VerticalLineX[i], shifted.VerticalLineX[i], 2);
            }
            for (int i = 0; i < baseLayout.HorizontalLineY.Length; i++)
            {
                Assert.Equal(baseLayout.HorizontalLineY[i], shifted.HorizontalLineY[i], 2);
            }
        }

        [Fact]
        public void Compute_InvalidInputs_ReturnNull()
        {
            var range = new MapGridCellRange(0, 3, 0, 3);
            Assert.Null(MapGridMath.Compute(0, range, 0.1f, 0f, 0f, 0f, 3f, 3f, 5f / 3f, 1200f, 720f));
            Assert.Null(MapGridMath.Compute(4, new MapGridCellRange(3, 0, 0, 3),
                0.1f, 0f, 0f, 0f, 3f, 3f, 5f / 3f, 1200f, 720f));
            Assert.Null(MapGridMath.Compute(4, range, 0f, 0f, 0f, 0f, 3f, 3f, 5f / 3f, 1200f, 720f));
        }
    }
}
