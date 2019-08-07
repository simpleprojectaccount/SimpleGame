using TMPro;
using UnityEngine;
using Vertigo.Managers;

namespace Vertigo.Core
{
    // Attaches itself to a hexagon piece and ends the game if counter reaches zero
    public class HexagonBomb : MonoBehaviour
    {
        public Sprite bombSprite;
        public HexagonPiece HexagonElement { get; private set; }
#pragma warning disable 0649
        [SerializeField]
        private TextMeshPro _countdownText;
#pragma warning restore 0649

        private int _countdown;

        /// <summary>
        /// Attach bomb logic to a hexagon piece
        /// </summary>
        /// <param name="attachedPiece"></param>
        /// <param name="initialCountdown"></param>
        public void InitBomb(HexagonPiece attachedPiece, int initialCountdown)
        {
            HexagonElement = attachedPiece;
            var sr = HexagonElement.GetComponent<SpriteRenderer>();
            sr.sprite = bombSprite;
            sr.sortingOrder = 2;
            _countdown = initialCountdown;
            transform.SetParent(attachedPiece.transform, false);
            _countdownText.text = initialCountdown.ToString();
        }

        /// <summary>
        /// Process one bomb heartbeat
        /// </summary>
        /// <returns>true if bomb hasn't exploded, false on explosion</returns>
        public bool Pulse()
        {
            _countdown--;
            _countdownText.text = _countdown.ToString();
            SoundManager.Instance.PlayFx("bomb_timer");
            return _countdown > 0;
        }
    }
}