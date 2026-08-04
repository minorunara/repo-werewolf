using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public readonly struct MapGridCellRange
    {
        public readonly int ColMin;
        public readonly int ColMax;
        public readonly int RowMin;
        public readonly int RowMax;

        public MapGridCellRange(int colMin, int colMax, int rowMin, int rowMax)
        {
            ColMin = colMin;
            ColMax = colMax;
            RowMin = rowMin;
            RowMax = rowMax;
        }

        public int Cols => ColMax - ColMin + 1;
        public int Rows => RowMax - RowMin + 1;
    }

    public sealed class MapGridLayout
    {
        public float RectLeft;

        public float RectRight;

        public float RectBottom;

        public float RectTop;

        public float[] VerticalLineX;

        public float[] HorizontalLineY;

        public string[] ColumnLabels;

        public float[] ColumnLabelX;

        public float ColumnLabelY;

        public string[] RowLabels;

        public float[] RowLabelY;

        public float RowLabelX;
    }

    public static class MapGridMath
    {
        public const float ModuleSizeWorld = 15f;

        public const float BoundsShrinkWorld = 1.0f;

        public const float LabelOffsetPanel = 22f;

        public const float LabelEdgeInsetPanel = 16f;

        public static string ColumnLetter(int index)
        {
            if (index < 0) return "?";
            string result = "";
            int n = index;
            while (true)
            {
                result = (char)('A' + n % 26) + result;
                n = n / 26 - 1;
                if (n < 0) break;
            }
            return result;
        }

        public static int RowNumber(int rowMax, int rowIndex)
        {
            return rowMax - rowIndex + 1;
        }

        public static MapGridCellRange? RangeFromModuleCenters(
            IReadOnlyList<(float X, float Z)> centers, int levelWidth, int levelHeight)
        {
            if (centers == null || levelWidth <= 0 || levelHeight <= 0) return null;
            const float m = ModuleSizeWorld;
            int offsetCols = levelWidth / 2;
            bool has = false;
            int colMin = 0, colMax = 0, rowMin = 0, rowMax = 0;
            for (int i = 0; i < centers.Count; i++)
            {
                int col = (int)Math.Round(centers[i].X / m, MidpointRounding.AwayFromZero) + offsetCols;
                int row = (int)Math.Round((centers[i].Z - m * 0.5f) / m, MidpointRounding.AwayFromZero);
                if (col < 0 || col >= levelWidth || row < 0 || row >= levelHeight) continue;
                if (!has)
                {
                    colMin = colMax = col;
                    rowMin = rowMax = row;
                    has = true;
                }
                else
                {
                    if (col < colMin) colMin = col;
                    if (col > colMax) colMax = col;
                    if (row < rowMin) rowMin = row;
                    if (row > rowMax) rowMax = row;
                }
            }
            if (!has) return null;
            return new MapGridCellRange(colMin, colMax, rowMin, rowMax);
        }

        public static MapGridCellRange? RangeFromBounds(
            int levelWidth, int levelHeight,
            float mapScale, float originMiniX, float originMiniZ,
            float boundsMinMiniX, float boundsMaxMiniX,
            float boundsMinMiniZ, float boundsMaxMiniZ)
        {
            if (levelWidth <= 0 || levelHeight <= 0 || mapScale <= 1e-6f) return null;
            if (boundsMaxMiniX <= boundsMinMiniX || boundsMaxMiniZ <= boundsMinMiniZ) return null;

            int offsetCols = levelWidth / 2;
            float wMinX = (boundsMinMiniX - originMiniX) / mapScale;
            float wMaxX = (boundsMaxMiniX - originMiniX) / mapScale;
            float wMinZ = (boundsMinMiniZ - originMiniZ) / mapScale;
            float wMaxZ = (boundsMaxMiniZ - originMiniZ) / mapScale;
            ShrinkRange(ref wMinX, ref wMaxX, BoundsShrinkWorld);
            ShrinkRange(ref wMinZ, ref wMaxZ, BoundsShrinkWorld);

            int colMin = ClampInt(ColumnIndexOfWorldX(wMinX, offsetCols), 0, levelWidth - 1);
            int colMax = ClampInt(ColumnIndexOfWorldX(wMaxX, offsetCols), 0, levelWidth - 1);
            int rowMin = ClampInt(RowIndexOfWorldZ(wMinZ), 0, levelHeight - 1);
            int rowMax = ClampInt(RowIndexOfWorldZ(wMaxZ), 0, levelHeight - 1);
            if (colMax < colMin || rowMax < rowMin) return null;
            return new MapGridCellRange(colMin, colMax, rowMin, rowMax);
        }

        public static MapGridLayout Compute(
            int levelWidth, MapGridCellRange range,
            float mapScale, float originMiniX, float originMiniZ,
            float camMiniX, float camMiniZ,
            float orthoSize, float aspect,
            float panelWidth, float panelHeight)
        {
            if (levelWidth <= 0) return null;
            if (mapScale <= 1e-6f || orthoSize <= 1e-3f || aspect <= 1e-3f) return null;
            if (panelWidth <= 0f || panelHeight <= 0f) return null;
            if (range.ColMax < range.ColMin || range.RowMax < range.RowMin) return null;

            const float m = ModuleSizeWorld;
            int offsetCols = levelWidth / 2;
            int colMin = range.ColMin, colMax = range.ColMax;
            int rowMin = range.RowMin, rowMax = range.RowMax;

            float pxPerMiniX = panelWidth * 0.5f / (orthoSize * aspect);
            float pxPerMiniZ = panelHeight * 0.5f / orthoSize;
            float PanelX(float worldX) => (worldX * mapScale + originMiniX - camMiniX) * pxPerMiniX;
            float PanelY(float worldZ) => (worldZ * mapScale + originMiniZ - camMiniZ) * pxPerMiniZ;

            float BoundaryWorldX(int i) => (i - offsetCols) * m - m * 0.5f;
            float BoundaryWorldZ(int j) => j * m;

            var layout = new MapGridLayout
            {
                RectLeft = PanelX(BoundaryWorldX(colMin)),
                RectRight = PanelX(BoundaryWorldX(colMax + 1)),
                RectBottom = PanelY(BoundaryWorldZ(rowMin)),
                RectTop = PanelY(BoundaryWorldZ(rowMax + 1)),
            };

            int cols = colMax - colMin + 1;
            int rows = rowMax - rowMin + 1;
            layout.VerticalLineX = new float[cols + 1];
            for (int i = 0; i <= cols; i++)
            {
                layout.VerticalLineX[i] = PanelX(BoundaryWorldX(colMin + i));
            }
            layout.HorizontalLineY = new float[rows + 1];
            for (int j = 0; j <= rows; j++)
            {
                layout.HorizontalLineY[j] = PanelY(BoundaryWorldZ(rowMin + j));
            }

            layout.ColumnLabels = new string[cols];
            layout.ColumnLabelX = new float[cols];
            for (int i = 0; i < cols; i++)
            {
                layout.ColumnLabels[i] = ColumnLetter(i);
                layout.ColumnLabelX[i] = PanelX((colMin + i - offsetCols) * m);
            }
            layout.RowLabels = new string[rows];
            layout.RowLabelY = new float[rows];
            for (int j = 0; j < rows; j++)
            {
                layout.RowLabels[j] = RowNumber(rowMax, rowMin + j).ToString();
                layout.RowLabelY[j] = PanelY((rowMin + j) * m + m * 0.5f);
            }

            layout.ColumnLabelY = Math.Min(
                layout.RectTop + LabelOffsetPanel, panelHeight * 0.5f - LabelEdgeInsetPanel);
            layout.RowLabelX = Math.Max(
                layout.RectLeft - LabelOffsetPanel, -panelWidth * 0.5f + LabelEdgeInsetPanel);
            return layout;
        }

        private static int ColumnIndexOfWorldX(float worldX, int offsetCols)
        {
            return (int)Math.Floor((worldX + offsetCols * ModuleSizeWorld + ModuleSizeWorld * 0.5f)
                / ModuleSizeWorld);
        }

        private static int RowIndexOfWorldZ(float worldZ)
        {
            return (int)Math.Floor(worldZ / ModuleSizeWorld);
        }

        private static void ShrinkRange(ref float min, ref float max, float amount)
        {
            if (max - min > amount * 2f)
            {
                min += amount;
                max -= amount;
            }
            else
            {
                float mid = (min + max) * 0.5f;
                min = mid;
                max = mid;
            }
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
