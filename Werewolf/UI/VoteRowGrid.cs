using UnityEngine;

namespace Werewolf.UI
{
    internal static class VoteRowGrid
    {
        public static readonly Vector2 RowSize = new Vector2(572f, 76f);

        public const int RowsPerColumn = 7;

        public const int Columns = 2;

        public const int RowsPerPage = RowsPerColumn * Columns;

        private const float ColumnX = 301f;
        private const float RowStepY = 76f;

        public static Vector2 Position(int index, float topY)
            => Position(index % Columns, index / Columns, topY);

        public static Vector2 Position(int column, int rowInColumn, float topY)
            => new Vector2(column == 0 ? -ColumnX : ColumnX, topY - rowInColumn * RowStepY);
    }
}
