using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Vertigo.Core;
using Vertigo.Utilities;

namespace Vertigo.Managers
{
    // Handles the progression of the game and the events that take place during gameplay
    public class GameManager : Manager<GameManager>
    {
        private const int _scoreMultiplier = 5;
        private const int _bombExplosionCounter = 7;
        private const int _bombInterval = 1000;
        
        public InputReceiver inputReceiver;
        public GroupSelection selection;

        // Active bombs on the grid
        private readonly List<HexagonBomb> _bombs = new List<HexagonBomb>(2);

        // Is waiting for the grid to update itself?
        private bool _isBusy = false;

        private int _score = 0;
        private int _nextBombSpawnScore;

        protected override void Awake()
        {
            base.Awake();

            selection = Instantiate(selection);
            selection.transform.localScale = new Vector3(GridManager.PIECE_WIDTH, GridManager.PIECE_WIDTH, GridManager.PIECE_WIDTH);

            _nextBombSpawnScore = _bombInterval;

            inputReceiver.ClickEvent += OnClick;
            inputReceiver.SwipeEvent += OnSwipe;
        }

        protected override void Destructor()
        {
            for (int i = _bombs.Count - 1; i >= 0; i--)
                PoolManager.Instance.Push(_bombs[i]);

            _bombs.Clear();
        }

        private void OnClick(PointerEventData eventData)
        {
            if (_isBusy)
                return;

            selection.SelectGroup(CameraManager.Instance.ScreenToWorldPoint(eventData.position));
        }

        private void OnSwipe(PointerEventData eventData)
        {
            if (_isBusy)
                return;

            if (!selection.IsVisible)
                return;

            // Check if this is a clockwise swipe or a counter-clockwise swipe
            Vector2 center = CameraManager.Instance.WorldToScreenPoint(selection.transform.localPosition);
            bool clockwise = Vector2.SignedAngle(eventData.pressPosition - center, eventData.position - center) < 0f;
            // Check if rotating the selection by a certain amount results in a match on the grid
            int rotationAmount;
            HexagonMatch match = null;
            for (rotationAmount = 1; rotationAmount < 3; rotationAmount++)
            {
                SoundManager.Instance.PlayFx("rotation");
                selection.Group.RotateClockwise(clockwise ? 1 : -1);
                match = GridManager.Instance.TryGetMatchingPiecesAt(selection.Group);
                if (match != null)
                    break;
            }

            if (match == null)
            {
                selection.Group.RotateClockwise(clockwise ? 1 : -1); // So that the selection will rotate 360 degrees
                SoundManager.Instance.PlayFx("rotation");
            }

            StartCoroutine(RotateSelection(clockwise, rotationAmount, match));
        }

        private IEnumerator RotateSelection(bool clockwise, int amount, HexagonMatch match)
        {
            _isBusy = true;
            // Wait for the rotate animation to finish
            yield return StartCoroutine(AnimationManager.Instance.RotateSelection(selection, amount * (clockwise ? -120f : 120f)));

            // The grid will be updated if there is a match
            if (match != null)
            {
                // Don't show the selection while updating the grid
                selection.IsVisible = false;
                UIManager.Instance.ToggleSettingGroup(false);

                // Wait for a short interval so that users can also realize the match
                yield return new WaitForSeconds(0.5f);

                // A column index that is selected randomly from the matching pieces
                int possibleBombColumn = match[Random.Range(0, match.Count)].GridPos.x;

                // Update the score and etc.
                ProcessMatch(match);

                // Start filling in the blank slots (slots previously occupied by matching pieces) but don't wait for it to finish yet
                Coroutine fillBlanksCoroutine = StartCoroutine(GridManager.Instance.FillBlankSlots());
                if (_score >= _nextBombSpawnScore)
                {
                    // Spawn a bomb at a random column if we've reached the target score
                    _nextBombSpawnScore += _bombInterval;

                    HexagonBomb bomb = PoolManager.Instance.PopBomb();
                    bomb.InitBomb(GridManager.Instance[possibleBombColumn][GridManager.Instance.Height - 1], _bombExplosionCounter + 1); // Counter will decrement after this round
                    _bombs.Add(bomb);
                }

                // Wait for the blank slots to be filled
                yield return fillBlanksCoroutine;

                // Check if there are another matches on the grid after the blank slots are filled
                // If so, continue to update the grid until there is no match left
                List<HexagonMatch> matchesOnGrid = GridManager.Instance.GetAllMatchingPiecesOnGrid();
                while (matchesOnGrid != null && matchesOnGrid.Count > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                    for (int i = 0; i < matchesOnGrid.Count; i++)
                        ProcessMatch(matchesOnGrid[i]);

                    yield return StartCoroutine(GridManager.Instance.FillBlankSlots());
                    matchesOnGrid = GridManager.Instance.GetAllMatchingPiecesOnGrid();
                }

                // Decrement the counters of the bombs and end the game if a bomb reaches 0
                for (int i = _bombs.Count - 1; i >= 0; i--)
                {
                    if (!_bombs[i].Pulse())
                    {
                        SoundManager.Instance.PlayFx("bomb_explode");
                        EndGame();
                        yield break;
                    }
                }

                // Update the selection with the new pieces
                selection.SelectGroup(selection.transform.localPosition);

                // Check if there are no more possible matches on the grid (i.e. deadlock)
                if (GridManager.Instance.IsDeadlocked())
                {
                    SoundManager.Instance.PlayFx("oops");
                    EndGame();
                    yield break;
                }
            }

            _isBusy = false;
        }

        private void ProcessMatch(HexagonMatch match)
        {
            _score += _scoreMultiplier * match.Count;
            UIManager.Instance.UpdateScore(_score);
            SoundManager.Instance.PlayFx("cluster");

            for (int i = match.Count - 1; i >= 0; i--)
            {
                // Destroy the matching pieces in a fashionable way
                GridManager.Instance[match[i].GridPos.x][match[i].GridPos.y] = null; // Mark that slot as empty
                AnimationManager.Instance.BlowPieceAway(match[i]);
                // Check if a bomb was attached to the destroyed piece
                for (int j = _bombs.Count - 1; j >= 0; j--)
                {
                    if (_bombs[j].HexagonElement == match[i])
                    {
                        PoolManager.Instance.Push(_bombs[j]);
                        SoundManager.Instance.PlayFx("bomb_remove");
                        // This bomb is now defused, move the last bomb to this index
                        if (j < _bombs.Count - 1)
                            _bombs[j] = _bombs[_bombs.Count - 1];

                        _bombs.RemoveAt(_bombs.Count - 1);
                    }
                }
            }

            PoolManager.Instance.Push(match);
        }

        private void EndGame()
        {
            _isBusy = true;
            SoundManager.Instance.PlayScoreScreenMusic();
            int highscore = PlayerPrefs.GetInt("Highscore", 0);
            if (_score > highscore)
            {
                highscore = _score;

                PlayerPrefs.SetInt("Highscore", _score);
                PlayerPrefs.Save();
            }

            // Show the game over screen
            UIManager.Instance.EndGame(_score, highscore);
        }
    }
}