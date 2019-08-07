
using Vertigo.Managers;

namespace Vertigo.Core
{
    /// <summary>
    ///  Hexagon group is made up of three neighboring hexagon pieces
    /// </summary>
    public struct HexagonGroup
    {
        public HexagonPiece Piece1 { get; }
        public HexagonPiece Piece2 { get; }
        public HexagonPiece Piece3 { get; }

        public bool IsEmpty => Piece1 == null || Piece2 == null || Piece3 == null;
        public bool IsMatching => Piece1.ColorIndex == Piece2.ColorIndex && Piece2.ColorIndex == Piece3.ColorIndex;

        public HexagonGroup(HexagonPiece piece1, HexagonPiece piece2, HexagonPiece piece3)
        {
            Piece1 = piece1;
            Piece2 = piece2;
            Piece3 = piece3;
        }

        // Selected pieces are drawn above the others so that they don't fall behind the other pieces while being rotated
        public void SetSelected(bool isSelected)
        {
            Piece1.SortingOrder = isSelected ? 1 : 0;
            Piece2.SortingOrder = isSelected ? 1 : 0;
            Piece3.SortingOrder = isSelected ? 1 : 0;
        }

        // Changes the order of the pieces in the tuple, this doesn't actually change their transform.position since it is
        // often desirable to rotate the tuple for certain calculations (e.g. to check if there will be a match at certain rotation)
        // without actually modifying their transform.position values. The transform.position values are usually animated to
        // reach the target position (e.g. while rotation the selection or adding new pieces to the grid from above)
        public void RotateClockwise(int count = 1)
        {
            count = count % 3;
            if (count < 0)
                count += 3;
            else if (count == 0)
                return;

            int x = Piece1.GridPos.x;
            int y = Piece1.GridPos.y;

            if (count == 1)
            {
                GridManager.Instance[Piece2.GridPos.x][Piece2.GridPos.y] = Piece1;
                GridManager.Instance[Piece3.GridPos.x][Piece3.GridPos.y] = Piece2;
                GridManager.Instance[x][y] = Piece3;
            }
            else
            {
                GridManager.Instance[Piece3.GridPos.x][Piece3.GridPos.y] = Piece1;
                GridManager.Instance[Piece2.GridPos.x][Piece2.GridPos.y] = Piece3;
                GridManager.Instance[x][y] = Piece2;
            }
        }

        public void RotateCounterClockwise(int count = 1) => RotateClockwise(-count);
    }
}