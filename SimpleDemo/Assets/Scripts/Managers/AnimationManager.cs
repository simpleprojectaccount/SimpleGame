using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.Core;

namespace Vertigo.Managers
{
    // Plays animations on hexagon pieces and likewise
    public class AnimationManager : Manager<AnimationManager>
    {
        private float _moveAnimationSpeed = 10f;
        private float _blowAnimationSpeed = 0.75f;
        private float _selectionRotateSpeed = 900f;

        public bool IsAnimating { get { return moveAnimations.Count > 0; } }

        private readonly List<MovePieceAnimation> moveAnimations = new List<MovePieceAnimation>(64);
        private readonly List<BlowPieceAnimation> blowAnimations = new List<BlowPieceAnimation>(8);

        // These animations are pooled
        protected override void Destructor()
        {
            for (int i = moveAnimations.Count - 1; i >= 0; i--)
                PoolManager.Instance.Push(moveAnimations[i]);

            for (int i = blowAnimations.Count - 1; i >= 0; i--)
                PoolManager.Instance.Push(blowAnimations[i]);

            moveAnimations.Clear();
            blowAnimations.Clear();
        }

        // Play the animations
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            for (int i = moveAnimations.Count - 1; i >= 0; i--)
            {
                if (!moveAnimations[i].Execute(deltaTime))
                {
                    PoolManager.Instance.Push(moveAnimations[i]);

                    // This move operation has finished, move the last move operation to this index
                    if (i < moveAnimations.Count - 1)
                        moveAnimations[i] = moveAnimations[moveAnimations.Count - 1];

                    moveAnimations.RemoveAt(moveAnimations.Count - 1);
                }
            }

            for (int i = blowAnimations.Count - 1; i >= 0; i--)
            {
                if (!blowAnimations[i].Execute(deltaTime))
                {
                    PoolManager.Instance.Push(blowAnimations[i]);

                    // This blow operation has finished, move the last blow operation to this index
                    if (i < blowAnimations.Count - 1)
                        blowAnimations[i] = blowAnimations[blowAnimations.Count - 1];

                    blowAnimations.RemoveAt(blowAnimations.Count - 1);
                }
            }
        }

        public void MovePieceToPosition(HexagonPiece piece)
        {
            MovePieceAnimation moveAnimation = PoolManager.Instance.PopMoveAnimation();
            moveAnimation.Initialize(piece, _moveAnimationSpeed);
            moveAnimations.Add(moveAnimation);
        }

        public void BlowPieceAway(HexagonPiece piece)
        {
            BlowPieceAnimation blowAnimation = PoolManager.Instance.PopBlowAnimation();
            blowAnimation.Initialize(piece, _blowAnimationSpeed);
            blowAnimations.Add(blowAnimation);
        }

        // Rotate the selected hexagon pieces on the grid around the center point by degrees
        public IEnumerator RotateSelection(GroupSelection selection, float degrees)
        {
            Quaternion currentAngles = Quaternion.Euler(selection.transform.localEulerAngles);
            Vector3 initialRotation = new Vector3();
            Vector3 targetRotation = new Vector3(0f, 0f, degrees);

            HexagonPiece p1 = selection.Group.Piece1;
            HexagonPiece p2 = selection.Group.Piece2;
            HexagonPiece p3 = selection.Group.Piece3;

            // Rotation happens by rotation the direction vectors
            Vector3 selectionCenter = selection.transform.localPosition;
            Vector3 dir1 = p1.transform.localPosition - selectionCenter;
            Vector3 dir2 = p2.transform.localPosition - selectionCenter;
            Vector3 dir3 = p3.transform.localPosition - selectionCenter;

            float t = 0f;
            float tMultiplier = _selectionRotateSpeed / Mathf.Abs(degrees);
            while ((t = t + Time.deltaTime * tMultiplier) < 1f)
            {
                // Using Quaternion.LerpUnclamped applies no rotation for degrees=360, this is actually smart but not desirable in this case
                Quaternion rotation = Quaternion.Euler(Vector3.LerpUnclamped(initialRotation, targetRotation, t));

                selection.transform.localRotation = currentAngles * rotation;
                p1.transform.localPosition = selectionCenter + rotation * dir1;
                p2.transform.localPosition = selectionCenter + rotation * dir2;
                p3.transform.localPosition = selectionCenter + rotation * dir3;

                yield return null;
            }

            // Can't rely on floating point precision, rotated pieces may deviate from their intended positions after a number of turns,
            // put the rotated pieces at their exact position after the rotation is complete
            selection.transform.localRotation = currentAngles * Quaternion.Euler(targetRotation);
            p1.transform.localPosition = GridManager.Instance[p1.GridPos.x].CalculatePositionAt(p1.GridPos.y);
            p2.transform.localPosition = GridManager.Instance[p2.GridPos.x].CalculatePositionAt(p2.GridPos.y);
            p3.transform.localPosition = GridManager.Instance[p3.GridPos.x].CalculatePositionAt(p3.GridPos.y);
        }

        // Moves the hexagon piece from its current position to its target position on the grid
        public class MovePieceAnimation
        {
            private HexagonPiece _piece;
            private Vector2 _initialPosition;
            private Vector2 _targetPosition;

            private float t;
            private float tMultiplier;

            public void Initialize(HexagonPiece piece, float animationSpeed)
            {
                _piece = piece;
                _initialPosition = piece.transform.localPosition;
                _targetPosition = GridManager.Instance[piece.GridPos.x].CalculatePositionAt(piece.GridPos.y);

                tMultiplier = animationSpeed / Vector2.Distance(_targetPosition, _initialPosition);
                t = 0f;
            }

            public bool Execute(float deltaTime)
            {
                t += deltaTime * tMultiplier;
                if (t < 1f)
                {
                    _piece.transform.localPosition = Vector2.LerpUnclamped(_initialPosition, _targetPosition, t);
                    return true;
                }

                _piece.transform.localPosition = _targetPosition;
                return false;
            }
        }

        // Applies random force to a hexagon piece and shrinks
        public class BlowPieceAnimation
        {
            private HexagonPiece _piece;
            private Vector2 _velocity;

            private float _t;
            private float _timeMultiplier;

            public void Initialize(HexagonPiece piece, float animationSpeed)
            {
                this._piece = piece;
                _velocity = (Random.insideUnitCircle + new Vector2(0f, 0.15f)) * (GridManager.Instance.Width * 1.2f) * GridManager.PIECE_WIDTH;

                _t = 0f;
                _timeMultiplier = animationSpeed;

                piece.SortingOrder = 3; // These pieces should be drawn above the others
            }

            public bool Execute(float deltaTime)
            {
                _t += deltaTime * _timeMultiplier;
                _velocity.y -= deltaTime * 2.5f * GridManager.PIECE_HEIGHT * GridManager.Instance.Height;
                if (_t < 1f)
                {
                    float scale = (1f - _t) * GridManager.PIECE_WIDTH;

                    _piece.transform.Translate(_velocity * deltaTime);
                    _piece.transform.localScale = new Vector3(scale, scale, scale);
                    return true;
                }

                _piece.transform.localScale = new Vector3(GridManager.PIECE_WIDTH, GridManager.PIECE_WIDTH, GridManager.PIECE_WIDTH);
                _piece.SortingOrder = 0;
                PoolManager.Instance.Push(_piece);

                return false;
            }
        }
    }
}