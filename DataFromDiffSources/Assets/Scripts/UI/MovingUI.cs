using UnityEngine;
using UnityEngine.EventSystems;

namespace DataFromDiffSources.UI
{
    public class MovingUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        private Transform movingTransform;

        private Vector3 _startWorldPosition;
        private Vector3 _startMousePosition;
        private bool _startDragging;

        private void Awake()
        {
            if(movingTransform == null)
            {
                movingTransform = transform;
            }
        }

        private void Update()
        {
            if (!_startDragging)
                return;

            var vOffset = Input.mousePosition - _startMousePosition;
            movingTransform.position = _startWorldPosition + new Vector3(vOffset.x, vOffset.y, _startWorldPosition.z);
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            _startMousePosition = Input.mousePosition;
            _startWorldPosition = movingTransform.position;
            _startDragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _startDragging = false;
        }
    }
}

