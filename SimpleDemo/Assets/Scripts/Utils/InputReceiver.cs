using UnityEngine;
using UnityEngine.EventSystems;

namespace Vertigo.Utilities
{
    /// <summary>
    /// Sends pointer inputs via events to the subscribed methods 
    /// </summary>
    public class InputReceiver : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler
    {
#pragma warning disable 0649
        [SerializeField]
        private float _sensitivity = 6f;
        private float _swipeAmountSqr;
#pragma warning restore 0649

        public delegate void PointerDelegate(PointerEventData eventData);

        public event PointerDelegate ClickEvent;
        public event PointerDelegate SwipeEvent;

        private void Awake()
        {
            _swipeAmountSqr = _sensitivity * (Screen.width + Screen.height) * 0.005f;
            _swipeAmountSqr *= _swipeAmountSqr;
        }

        public void OnPointerClick(PointerEventData eventData) => ClickEvent?.Invoke(eventData);

        /// <summary>
        /// Input sanitization; makes sure OnPointerClick is called only once for a swipe
        /// </summary>
        /// <param name="eventData"></param>
        public void OnBeginDrag(PointerEventData eventData) => eventData.pointerPress = null;

        public void OnDrag(PointerEventData eventData)
        {
            if ((eventData.position - eventData.pressPosition).sqrMagnitude >= _swipeAmountSqr)
            {
                eventData.pointerDrag = null;
                eventData.dragging = false;
                SwipeEvent?.Invoke(eventData);
            }
        }
    }
}