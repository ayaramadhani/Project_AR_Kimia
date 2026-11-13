using UnityEngine;
using UnityEngine.EventSystems;

namespace ARKimia.UI
{
    /// <summary>
    /// Forwards drag and pointer events from a dedicated drag handle (such as the top grip bar)
    /// to a parent UIDraggablePanel so the panel can be moved while keeping inner ScrollRect scrolling intact.
    /// </summary>
    public class UIDragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Target UIDraggablePanel to receive forwarded events. If null, searches parents.")]
        [SerializeField] private UIDraggablePanel targetPanel;

        public void SetTarget(UIDraggablePanel target)
        {
            targetPanel = target;
        }

        private UIDraggablePanel Target
        {
            get
            {
                if (targetPanel == null)
                {
                    targetPanel = GetComponentInParent<UIDraggablePanel>(true);
                }
                return targetPanel;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Target?.NotifyHandlePointerDown(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Target?.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Target?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Target?.OnEndDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Target?.OnPointerUp(eventData);
        }
    }
}
