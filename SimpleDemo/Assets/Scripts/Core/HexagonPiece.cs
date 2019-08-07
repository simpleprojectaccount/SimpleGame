using UnityEngine;
using Vertigo.Managers;


namespace Vertigo.Core
{
    // A single hexagon piece on the grid
    [RequireComponent(typeof(SpriteRenderer))]
    public class HexagonPiece : MonoBehaviour
    {
        public Sprite hexagonSprite;
        private Vector2Int _gridPosition;
        public Vector2Int GridPos => _gridPosition;
        public int SortingOrder { get => _spriteRenderer.sortingOrder; set => _spriteRenderer.sortingOrder = value; }
        public int ColorIndex { get; private set; }
        private SpriteRenderer _spriteRenderer;

        public enum Edge { BottomLeft, Left, TopLeft, TopRight, Right, BottomRight };

        // Ensure hexagon piece is setup.
        private void Awake()
        {
            _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if(_spriteRenderer.sprite == null && hexagonSprite)
                _spriteRenderer.sprite = hexagonSprite;
        }

        public void SetPosition(int x, int y)
        {
            _gridPosition.x = x;
            _gridPosition.y = y;
        }

        public void SetColor(int colorIndex, Color color)
        {
            ColorIndex = colorIndex;
            _spriteRenderer.color = color;
        }

        // Finds the corner that is closest to the local point inside the piece (in range [-0.5,0.5])
        public Edge GetClosestEdge(Vector2 localPoint)
        {
            bool leftSide = localPoint.x < 0f;
            Vector2 p1 = new Vector2(GridManager.PIECE_WIDTH * (leftSide ? -0.25f : 0.25f), GridManager.PIECE_HEIGHT * -0.5f);
            Vector2 p2 = new Vector2(GridManager.PIECE_WIDTH * (leftSide ? -0.5f : 0.5f), 0f);
            Vector2 p3 = new Vector2(GridManager.PIECE_WIDTH * (leftSide ? -0.25f : 0.25f), GridManager.PIECE_HEIGHT * 0.5f);

            if ((p1 - localPoint).sqrMagnitude < (p2 - localPoint).sqrMagnitude)
            {
                if ((p1 - localPoint).sqrMagnitude < (p3 - localPoint).sqrMagnitude)
                    return leftSide ? Edge.BottomLeft : Edge.BottomRight;

                return leftSide ? Edge.TopLeft : Edge.TopRight;
            }

            if ((p2 - localPoint).sqrMagnitude < (p3 - localPoint).sqrMagnitude)
                return leftSide ? Edge.Left : Edge.Right;

            return leftSide ? Edge.TopLeft : Edge.TopRight;
        }

        /// <summary>
        /// Edge case handling for hexagon processing on grid.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns>A sane edge that is inside the grid</returns>
        public Edge HandleEdge(Edge edge)
        {
            // Process for left side
            if (_gridPosition.x == 0)
            {
                switch (edge)
                {
                    case Edge.BottomLeft:
                        edge = Edge.BottomRight;
                        break;
                    case Edge.Left:
                        edge = Edge.Right;
                        break;
                    case Edge.TopLeft:
                        edge = Edge.TopRight;
                        break;
                }
            }
            // Process for right side
            else if (_gridPosition.x == GridManager.Instance.Width - 1)
            {
                switch (edge)
                {
                    case Edge.BottomRight:
                        edge = Edge.BottomLeft;
                        break;
                    case Edge.Right:
                        edge = Edge.Left;
                        break;
                    case Edge.TopRight:
                        edge = Edge.TopLeft;
                        break;
                }
            }
            // Process for bottom side
            if (_gridPosition.y == 0)
            {
                switch (edge)
                {
                    case Edge.BottomLeft:
                        edge = Edge.Left;
                        break;
                    case Edge.BottomRight:
                        edge = Edge.Right;
                        break;
                }

                if (_gridPosition.x % 2 == 0)
                {
                    switch (edge)
                    {
                        case Edge.Left:
                            edge = Edge.TopLeft;
                            break;
                        case Edge.Right:
                            edge = Edge.TopRight;
                            break;
                    }
                }
            }
            // Process for top vertex
            else if (_gridPosition.y == GridManager.Instance.Height - 1)
            {
                switch (edge)
                {
                    case Edge.TopLeft:
                        edge = Edge.Left;
                        break;
                    case Edge.TopRight:
                        edge = Edge.Right;
                        break;
                }

                if (_gridPosition.x % 2 == 1)
                {
                    switch (edge)
                    {
                        case Edge.Left:
                            edge = Edge.BottomLeft;
                            break;
                        case Edge.Right:
                            edge = Edge.BottomRight;
                            break;
                    }
                }
            }
            return edge;
        }
    }
}