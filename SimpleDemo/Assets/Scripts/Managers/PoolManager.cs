using UnityEngine;
using UnityEngine.SceneManagement;
using Vertigo.Core;

namespace Vertigo.Managers
{
    /// <summary>
    /// Handles commonly used objects ingame
    /// </summary>
    public class PoolManager : Manager<PoolManager>
    {
#pragma warning disable 0649
        [SerializeField]
        private HexagonPiece piecePrefab;

        [SerializeField]
        private HexagonBomb bombPrefab;
#pragma warning restore 0649

        private readonly GenericPool<HexagonPiece> _piecePool = new GenericPool<HexagonPiece>();
        private readonly GenericPool<HexagonBomb> _bombPool = new GenericPool<HexagonBomb>();
        private readonly GenericPool<HexagonMatch> _matchPool = new GenericPool<HexagonMatch>();
        private readonly GenericPool<AnimationManager.MovePieceAnimation> _moveAnimPool = new GenericPool<AnimationManager.MovePieceAnimation>();
        private readonly GenericPool<AnimationManager.BlowPieceAnimation> _blowAnimPool = new GenericPool<AnimationManager.BlowPieceAnimation>();

        protected override void Awake()
        {
            base.Awake();

            // Initialize the pools
            if (Instance == this)
            {
                // This manager should persist after level restarts
                transform.SetParent(null, false);
                DontDestroyOnLoad(gameObject);

                _piecePool.CreateFunction = () =>
                {
                    HexagonPiece result = Instantiate(piecePrefab);
                    SceneManager.MoveGameObjectToScene(result.gameObject, gameObject.scene); // So that the piece will be DontDestroyOnLoad'ed
                result.transform.localScale = new Vector3(GridManager.PIECE_WIDTH, GridManager.PIECE_WIDTH, GridManager.PIECE_WIDTH);

                    return result;
                };
                _piecePool.OnPop = (piece) => piece.gameObject.SetActive(true);
                _piecePool.OnPush = (piece) => piece.gameObject.SetActive(false);
                _piecePool.Populate(32);

                _bombPool.CreateFunction = () =>
                {
                    HexagonBomb result = Instantiate(bombPrefab);
                    SceneManager.MoveGameObjectToScene(result.gameObject, gameObject.scene); // So that the bomb will be DontDestroyOnLoad'ed
                result.transform.localScale = new Vector3(GridManager.PIECE_WIDTH, GridManager.PIECE_WIDTH, GridManager.PIECE_WIDTH);

                    return result;
                };
                _bombPool.OnPop = (bomb) => bomb.gameObject.SetActive(true);
                _bombPool.OnPush = (bomb) =>
                {
                    bomb.gameObject.SetActive(false);
                    bomb.transform.SetParent(null, false); // Bombs are attached to their target hexagon pieces, release the bomb
                };
                _bombPool.Populate(2);

                _matchPool.CreateFunction = () => new HexagonMatch();
                _matchPool.OnPush = (match) => match.Clear();
                _matchPool.Populate(8);

                _moveAnimPool.CreateFunction = () => new AnimationManager.MovePieceAnimation();
                _moveAnimPool.Populate(64);

                _blowAnimPool.CreateFunction = () => new AnimationManager.BlowPieceAnimation();
                _blowAnimPool.Populate(8);
            }
        }

        public HexagonPiece PopPiece()
        {
            return _piecePool.Pop();
        }

        public HexagonBomb PopBomb()
        {
            return _bombPool.Pop();
        }

        public HexagonMatch PopMatch()
        {
            return _matchPool.Pop();
        }

        public AnimationManager.MovePieceAnimation PopMoveAnimation()
        {
            return _moveAnimPool.Pop();
        }

        public AnimationManager.BlowPieceAnimation PopBlowAnimation()
        {
            return _blowAnimPool.Pop();
        }

        public void Push(HexagonPiece piece)
        {
            _piecePool.Push(piece);
        }

        public void Push(HexagonBomb bomb)
        {
            bomb.transform.parent.GetComponent<SpriteRenderer>().sprite = piecePrefab.GetComponent<SpriteRenderer>().sprite;
            _bombPool.Push(bomb);
        }

        public void Push(HexagonMatch match)
        {
            _matchPool.Push(match);
        }

        public void Push(AnimationManager.MovePieceAnimation moveAnimation)
        {
            _moveAnimPool.Push(moveAnimation);
        }

        public void Push(AnimationManager.BlowPieceAnimation blowAnimation)
        {
            _blowAnimPool.Push(blowAnimation);
        }
    }
}