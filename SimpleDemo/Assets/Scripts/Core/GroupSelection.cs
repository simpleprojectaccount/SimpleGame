using UnityEngine;
using Vertigo.Managers;

namespace Vertigo.Core
{
    /// <summary>
    /// Controls the selection of a group
    /// </summary>
    public class GroupSelection : MonoBehaviour
    {
        public HexagonGroup Group { get; private set; }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    if (!Group.IsEmpty)
                        Group.SetSelected(value);
                    gameObject.SetActive(value);
                }
            }
        }

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// If the clicked position is located inside the grid;
        /// Gets the group closest to the point and selects it.
        /// </summary>
        /// <param name="position"></param>
        public void SelectGroup(Vector2 position)
        {
            IsVisible = false;
            if (GridManager.Instance.TryGetGroup(position, out HexagonGroup group))
            {
                Vector3 positionAverage = (group.Piece1.transform.localPosition + 
                    group.Piece2.transform.localPosition +
                    group.Piece3.transform.localPosition) / 3f;
                // A selection have two vertical pieces
                // Third piece is either on left or right of them.
                bool isPieceOnRight;
                if (group.Piece1.GridPos.x == group.Piece2.GridPos.x)
                    isPieceOnRight = group.Piece3.GridPos.x > group.Piece1.GridPos.x;
                else if (group.Piece2.GridPos.x == group.Piece3.GridPos.x)
                    isPieceOnRight = group.Piece1.GridPos.x > group.Piece3.GridPos.x;
                else
                    isPieceOnRight = group.Piece2.GridPos.x > group.Piece1.GridPos.x;
                transform.localPosition = positionAverage;
                transform.localEulerAngles = new Vector3(0f, 0f, isPieceOnRight ? 0f : 180f);
                Group = group;
                IsVisible = true;
            }
        }
    }
}