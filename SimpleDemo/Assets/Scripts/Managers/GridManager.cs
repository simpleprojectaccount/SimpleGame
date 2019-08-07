using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.Core;

namespace Vertigo.Managers
{
    /// <summary>
    /// Handles grid operations.
    /// </summary>
    public class GridManager : Manager<GridManager>
    {
        // Hexagon pieces' properties in world units, they are all calculated using the width value
        public const float PIECE_WIDTH = 1f;
        public const float PIECE_HEIGHT = PIECE_WIDTH * 0.866025f; // sqrt(3)/2
        public const float PIECE_DELTA_X = PIECE_WIDTH * 0.75f;
        private const float PIECE_DELTA_Y = PIECE_HEIGHT;
        private const float PIECES_INTERSECTION_WIDTH = PIECE_WIDTH * 0.25f;

        [HideInInspector]
        public List<Color> colors;

        [HideInInspector]
        public Vector2Int gridInfo;

        public int Width { get { return gridInfo.x; } }
        public int Height { get { return gridInfo.y; } }

        // Used to find the matching hexagon pieces on the grid
        private readonly List<HexagonMatch> matchesOnGrid = new List<HexagonMatch>(4);
        private readonly HashSet<HexagonPiece> matchesOnGridSet = new HashSet<HexagonPiece>();

        private Column[] grid;
        private Vector2 gridSize; // Total size of the grid in world units

        // Accessing a column by index from outside
        public Column this[int column] { get { return grid[column]; } }

        private void Start()
        {
            if (CreateGrid())
            {
                gridSize = new Vector3(gridInfo.x * PIECE_WIDTH - (gridInfo.x - 1) * PIECES_INTERSECTION_WIDTH, gridInfo.y * PIECE_HEIGHT + PIECE_HEIGHT * 0.5f, 0f);
                CameraManager.Instance.SetGridBounds(new Bounds(gridSize * 0.5f, gridSize));
            }
        }

        protected override void Destructor()
        {
            DestroyGrid();
            grid = null;
        }

        private bool CreateGrid()
        {
            grid = new Column[gridInfo.x];
            for (int x = 0; x < gridInfo.x; x++)
                grid[x] = new Column(x, gridInfo.y);

            // Continuously create the grid until there is no deadlock on the grid (i.e. there is at least one possible match)
            while (true)
            {
                for (int x = 0; x < gridInfo.x; x++)
                {
                    for (int y = 0; y < gridInfo.y; y++)
                    {
                        HexagonPiece piece = PoolManager.Instance.PopPiece();
                        grid[x][y] = piece;

                        piece.transform.localPosition = grid[x].CalculatePositionAt(y);
                        RandomizePieceColor(piece, true); // true: ensures that the picked color for this hexagon piece won't cause a match at the start
                    }
                }

                if (!IsDeadlocked())
                    break;

                // There is deadlock, start the process over
                DestroyGrid();
            }

            return true;
        }

        private void DestroyGrid()
        {
            if (grid == null)
                return;

            for (int x = 0; x < gridInfo.x; x++)
            {
                for (int y = 0; y < gridInfo.y; y++)
                    PoolManager.Instance.Push(grid[x][y]);
            }
        }

        // Pick a random color for the hexagon piece
        private void RandomizePieceColor(HexagonPiece piece, bool ensureNonMatchingColor)
        {
            int colorIndex = UnityEngine.Random.Range(0, colors.Count);
            if (ensureNonMatchingColor)
            {
                // Make sure that assigning this color to the hexagon piece won't cause a match on the grid
                while (CreatedPieceCheckMatch(piece.GridPos.x, piece.GridPos.y, colorIndex))
                {
                    int newColorIndex;
                    do
                    {
                        newColorIndex = UnityEngine.Random.Range(0, colors.Count);
                    } while (newColorIndex == colorIndex);

                    colorIndex = newColorIndex;
                }
            }

            piece.SetColor(colorIndex, colors[colorIndex]);
        }

        // Returns true if giving a particular color to a hexagon piece results in a match on the grid
        // Checks only the hexagon pieces that were created before this hexagon piece, no need to check the
        // hexagon pieces that are not yet created
        private bool CreatedPieceCheckMatch(int x, int y, int colorIndex)
        {
            if (x == 0)
                return false;

            if (x % 2 == 0)
            {
                if (y > 0)
                {
                    if ((grid[x][y - 1].ColorIndex == colorIndex && grid[x - 1][y - 1].ColorIndex == colorIndex) ||
                        (grid[x - 1][y - 1].ColorIndex == colorIndex && grid[x - 1][y].ColorIndex == colorIndex))
                        return true;
                }
            }
            else
            {
                if (y > 0)
                {
                    if (grid[x][y - 1].ColorIndex == colorIndex && grid[x - 1][y].ColorIndex == colorIndex)
                        return true;
                }

                if (y < gridInfo.y - 1)
                {
                    if (grid[x - 1][y].ColorIndex == colorIndex && grid[x - 1][y + 1].ColorIndex == colorIndex)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If points resides inside the grid, locate the group that are closest to the point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="group">false;null true; sets group to the found one.</param>
        /// <returns>false; point is outside of grid, true; inside.</returns>
        public bool TryGetGroup(Vector2 point, out HexagonGroup group)
        {
            if (point.x <= 0f || point.x >= gridSize.x || point.y <= 0f || point.y >= gridSize.y)
            {
                group = new HexagonGroup();
                return false;
            }

            // Calculate the row and column indices of the hexagon piece that the point resides inside
            GetCoordinatesFrom(point, out int x, out int y);

            // Find the hexagon piece's corner that is closest to the point
            HexagonPiece.Edge corner = grid[x][y].HandleEdge(grid[x][y].GetClosestEdge(point - (Vector2)grid[x][y].transform.localPosition));
            group = GetGroupAtCorner(x, y, corner);

            return true;
        }

        private void GetCoordinatesFrom(Vector2 position, out int x, out int y)
        {
            float _column = (position.x - GridManager.PIECE_WIDTH * 0.5f) / GridManager.PIECE_DELTA_X;
            x = Mathf.Clamp(Mathf.RoundToInt(_column), 0, GridManager.Instance.Width - 1);

            float _row = (x % 2 == 0 ? position.y : position.y - GridManager.PIECE_HEIGHT * 0.5f) / GridManager.PIECE_HEIGHT;
            y = Mathf.Clamp((int)_row, 0, GridManager.Instance.Height - 1);
        }

        private HexagonGroup GetGroupAtCorner(int x, int y, HexagonPiece.Edge corner)
        {
            if (grid[x][y].HandleEdge(corner) != corner)
            {
                return new HexagonGroup();
            }
            int y2 = x % 2 == 0 ? y : y + 1;
                switch (corner)
                {
                    // It is important that the pieces are stored in clockwise order
                    case HexagonPiece.Edge.BottomLeft: return new HexagonGroup(grid[x][y], grid[x][y - 1], grid[x - 1][y2 - 1]);
                    case HexagonPiece.Edge.BottomRight: return new HexagonGroup(grid[x][y], grid[x + 1][y2 - 1], grid[x][y - 1]);
                    case HexagonPiece.Edge.Left: return new HexagonGroup(grid[x][y], grid[x - 1][y2 - 1], grid[x - 1][y2]);
                    case HexagonPiece.Edge.Right: return new HexagonGroup(grid[x][y], grid[x + 1][y2], grid[x + 1][y2 - 1]);
                    case HexagonPiece.Edge.TopLeft: return new HexagonGroup(grid[x][y], grid[x - 1][y2], grid[x][y + 1]);
                    case HexagonPiece.Edge.TopRight: return new HexagonGroup(grid[x][y], grid[x][y + 1], grid[x + 1][y2]);
                    default: return new HexagonGroup();
            }
        }

        /// <summary>
        /// Returns true if there are no possible moves that result in a match on the grid
        /// </summary>
        /// <returns>true; no more move left, false moves possible.</returns>
        public bool IsDeadlocked()
        {
            matchesOnGridSet.Clear();
            // Check only on even coloums; checking all coloums is redundant.
            for (int x = 0; x < gridInfo.x; x += 2)
            {
                for (int y = 0; y < gridInfo.y; y++)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        HexagonGroup group = GetGroupAtCorner(x, y, (HexagonPiece.Edge)i);
                        if (!group.IsEmpty)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                group.RotateClockwise();
                                HexagonMatch match = TryGetMatchingPiecesAt(group);
                                if (match != null)
                                {
                                    PoolManager.Instance.Push(match);
                                    group.RotateClockwise(2 - j);
                                    return false;
                                }
                            }
                            group.RotateClockwise();
                        }
                    }
                }
            }
            return true;
        }

        // Finds all matches on the grid
        public List<HexagonMatch> GetAllMatchingPiecesOnGrid()
        {
            matchesOnGrid.Clear();
            matchesOnGridSet.Clear();

            // We can skip odd columns, if there is a match, it will be found while iterating the even columns
            for (int x = 0; x < gridInfo.x; x += 2)
            {
                for (int y = 0; y < gridInfo.y; y++)
                {
                    HexagonMatch match = TryGetMatchingPiecesAt(grid[x][y]);
                    if (match != null)
                        matchesOnGrid.Add(match);
                }
            }
            return matchesOnGrid;
        }

        // Finds the match that have at least one hexagon piece from this group
        public HexagonMatch TryGetMatchingPiecesAt(HexagonGroup group)
        {
            matchesOnGridSet.Clear();
            HexagonMatch result = TryGetMatchingPiecesAt(group.Piece1);
            if (result == null)
                result = TryGetMatchingPiecesAt(group.Piece2);
            if (result == null)
                result = TryGetMatchingPiecesAt(group.Piece3);
            return result;
        }

        // Find the match that this hexagon piece is part of
        private HexagonMatch TryGetMatchingPiecesAt(HexagonPiece piece)
        {
            // Don't search the piece for match if it is already searched before
            if (!matchesOnGridSet.Add(piece))
                return null;

            HexagonMatch result = PoolManager.Instance.PopMatch();
            TryGetMatchingPiecesAt(piece, result);
            if (result.Count > 0)
                return result;

            PoolManager.Instance.Push(result);
            return null;
        }

        // Find the match that this hexagon piece is part of (implementation)
        private void TryGetMatchingPiecesAt(HexagonPiece piece, HexagonMatch match)
        {
            bool isPieceAddedToMatch = false;

            // Iterate over each possible group that is formed with this hexagon piece and see if that group is a match
            for (int i = 0; i < 6; i++)
            {
                HexagonGroup _group = GetGroupAtCorner(piece.GridPos.x, piece.GridPos.y, (HexagonPiece.Edge)i);
                if (_group.IsEmpty || !_group.IsMatching)
                    continue;
                if (!isPieceAddedToMatch)
                {
                    match.Add(piece);
                    isPieceAddedToMatch = true;
                }
                if (matchesOnGridSet.Add(_group.Piece1))
                    TryGetMatchingPiecesAt(_group.Piece1, match);
                if (matchesOnGridSet.Add(_group.Piece2))
                    TryGetMatchingPiecesAt(_group.Piece2, match);
                if (matchesOnGridSet.Add(_group.Piece3))
                    TryGetMatchingPiecesAt(_group.Piece3, match);
            }
        }

        // Fill the blank slots with animation
        public IEnumerator FillBlankSlots()
        {
            for (int x = 0; x < gridInfo.x; x++)
            {
                // Check if there is a blank slot on this column
                int numberOfBlankSlots = 0;
                int firstBlankSlot = -1;
                for (int y = 0; y < gridInfo.y; y++)
                {
                    if (grid[x][y] == null)
                    {
                        if (++numberOfBlankSlots == 1)
                            firstBlankSlot = y;
                    }
                }

                // If there is at least one blank slot on this column
                if (numberOfBlankSlots > 0)
                {
                    // First, fill the blanks slots with the hexagon pieces above them (if any)
                    for (int y = firstBlankSlot; y < gridInfo.y - numberOfBlankSlots; y++)
                    {
                        for (int y2 = y + 1; y2 < gridInfo.y; y2++)
                        {
                            if (grid[x][y2] != null)
                            {
                                grid[x][y] = grid[x][y2];
                                grid[x][y2] = null;

                                break;
                            }
                        }

                        AnimationManager.Instance.MovePieceToPosition(grid[x][y]);
                    }

                    // Fill the remaining blank slots with new hexagon pieces falling from above
                    Vector2 fallingPieceStartPoint = new Vector2(grid[x].XCoord, CameraManager.Instance.Height);
                    if (x % 2 == 0)
                        fallingPieceStartPoint.y += PIECE_HEIGHT * 0.5f;

                    for (int y = gridInfo.y - numberOfBlankSlots; y < gridInfo.y; y++)
                    {
                        HexagonPiece piece = PoolManager.Instance.PopPiece();
                        RandomizePieceColor(piece, false);
                        grid[x][y] = piece;

                        piece.transform.localPosition = fallingPieceStartPoint;
                        fallingPieceStartPoint.y += PIECE_DELTA_Y;

                        AnimationManager.Instance.MovePieceToPosition(piece);
                    }
                }
            }

            // Wait until all pieces sit on their positions
            while (AnimationManager.Instance.IsAnimating)
                yield return null;
        }

        // A column on the grid
        public class Column
        {
            private readonly int column;
            private readonly HexagonPiece[] rows;

            private readonly Vector2 bottomCoords; // Cached data to calculate a hexagon piece's position on the grid as quickly as possible
            public float XCoord { get { return bottomCoords.x; } }

            // Changing the hexagon piece at a given row automatically updates that piece's cached coordinates (SetPosition)
            public HexagonPiece this[int row]
            {
                get { return rows[row]; }
                set
                {
                    rows[row] = value;
                    if (value != null)
                        value.SetPosition(column, row);
                }
            }

            public Column(int column, int size)
            {
                this.column = column;
                rows = new HexagonPiece[size];

                // Offset the starting point by half size of a hexagon piece so that the bottom left point resides at (0,0)
                bottomCoords = new Vector2(PIECE_WIDTH * 0.5f + column * PIECE_DELTA_X, column % 2 == 0 ? PIECE_HEIGHT * 0.5f : PIECE_HEIGHT);
            }

            // Calculates the position of a hexagon piece
            public Vector3 CalculatePositionAt(int row)
            {
                return new Vector3(bottomCoords.x, bottomCoords.y + row * PIECE_DELTA_Y, 0f);
            }
        }
    }
}