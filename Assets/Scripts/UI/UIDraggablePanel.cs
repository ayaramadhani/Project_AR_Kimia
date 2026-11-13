using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ARKimia.UI
{
    /// <summary>
    /// Enables UI panels and floating buttons to be freely dragged across the screen
    /// with safe area / screen boundary clamping and drag thresholding.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIDraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("RectTransform to reposition. Defaults to this GameObject's RectTransform if null.")]
        [SerializeField] private RectTransform targetRect;

        [Tooltip("Boundary RectTransform to clamp inside (e.g. SafeArea). If null, uses parent RectTransform.")]
        [SerializeField] private RectTransform clampBoundary;

        [Tooltip("Optional child handle that initiates dragging. If null, entire object surface can initiate drag.")]
        [SerializeField] private RectTransform dragHandle;

        [Tooltip("Optional counterpart draggable panel whose position should stay synchronized with this panel.")]
        [SerializeField] private UIDraggablePanel linkedDraggable;

        [Tooltip("Minimum pointer movement (screen pixels) required before treating movement as a drag.")]
        [SerializeField] private float dragThreshold = 10f;

        private Canvas rootCanvas;
        private Vector2 pointerDownScreenPos;
        private bool isDragging;
        private bool pointerDownOnHandle;

        public bool IsDragging => isDragging;
        public RectTransform TargetRect => targetRect != null ? targetRect : (targetRect = GetComponent<RectTransform>());
        public RectTransform DragHandle => dragHandle;

        private void Awake()
        {
            if (targetRect == null)
            {
                targetRect = GetComponent<RectTransform>();
            }

            CacheCanvas();

            if (clampBoundary == null && targetRect != null && targetRect.parent != null)
            {
                clampBoundary = targetRect.parent as RectTransform;
            }
        }

        private void OnEnable()
        {
            isDragging = false;
            pointerDownOnHandle = false;
            ClampToBoundary();
        }

        public void Initialize(RectTransform target, RectTransform boundary, RectTransform handle = null, UIDraggablePanel linked = null)
        {
            targetRect = target;
            clampBoundary = boundary;
            dragHandle = handle;
            if (linked != null)
            {
                linkedDraggable = linked;
            }
            CacheCanvas();
            ClampToBoundary();
        }

        public void SetLinkedDraggable(UIDraggablePanel linked)
        {
            linkedDraggable = linked;
        }

        public void SyncPositionToLinked()
        {
            if (linkedDraggable == null || targetRect == null)
                return;

            RectTransform linkedRect = linkedDraggable.TargetRect;
            if (linkedRect != null && linkedRect != targetRect)
            {
                linkedRect.anchoredPosition = targetRect.anchoredPosition;
                linkedDraggable.ClampToBoundary();
            }
        }

        private void CacheCanvas()
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>(true);
            }
        }

        public void NotifyHandlePointerDown(PointerEventData eventData)
        {
            pointerDownScreenPos = eventData.position;
            isDragging = false;
            pointerDownOnHandle = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDownScreenPos = eventData.position;
            isDragging = false;

            if (dragHandle != null)
            {
                pointerDownOnHandle = RectTransformUtility.RectangleContainsScreenPoint(
                    dragHandle, eventData.position, eventData.pressEventCamera);
            }
            else
            {
                pointerDownOnHandle = true;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!pointerDownOnHandle)
                return;

            if (Vector2.Distance(pointerDownScreenPos, eventData.position) >= dragThreshold
                || eventData.delta.sqrMagnitude >= (dragThreshold * dragThreshold))
            {
                isDragging = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!pointerDownOnHandle)
                return;

            if (!isDragging)
            {
                if (Vector2.Distance(pointerDownScreenPos, eventData.position) >= dragThreshold
                    || eventData.delta.sqrMagnitude >= (dragThreshold * dragThreshold))
                {
                    isDragging = true;
                }
            }

            if (!isDragging || targetRect == null)
                return;

            CacheCanvas();
            float scaleFactor = (rootCanvas != null && rootCanvas.scaleFactor > 0.001f)
                ? rootCanvas.scaleFactor
                : 1f;

            targetRect.anchoredPosition += eventData.delta / scaleFactor;
            ClampToBoundary();
            SyncPositionToLinked();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ClampToBoundary();
            SyncPositionToLinked();
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ResetDragFlagNextFrame());
            }
            else
            {
                isDragging = false;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerDownOnHandle = false;
        }

        private IEnumerator ResetDragFlagNextFrame()
        {
            yield return null;
            isDragging = false;
        }

        /// <summary>
        /// Keeps the target RectTransform fully within the bounding RectTransform.
        /// </summary>
        public void ClampToBoundary()
        {
            if (targetRect == null)
                return;

            RectTransform boundary = clampBoundary;
            if (boundary == null && targetRect.parent != null)
            {
                boundary = targetRect.parent as RectTransform;
            }

            if (boundary == null)
                return;

            Vector3[] targetWorldCorners = new Vector3[4];
            targetRect.GetWorldCorners(targetWorldCorners);

            Vector2 minLocal = boundary.InverseTransformPoint(targetWorldCorners[0]);
            Vector2 maxLocal = minLocal;

            for (int i = 1; i < 4; i++)
            {
                Vector2 localPt = boundary.InverseTransformPoint(targetWorldCorners[i]);
                minLocal = Vector2.Min(minLocal, localPt);
                maxLocal = Vector2.Max(maxLocal, localPt);
            }

            Rect boundRect = boundary.rect;
            if (boundRect.width <= 1f || boundRect.height <= 1f)
                return;

            Vector2 shift = Vector2.zero;

            float targetWidth = maxLocal.x - minLocal.x;
            if (targetWidth <= boundRect.width)
            {
                if (minLocal.x < boundRect.xMin)
                    shift.x += (boundRect.xMin - minLocal.x);
                else if (maxLocal.x > boundRect.xMax)
                    shift.x += (boundRect.xMax - maxLocal.x);
            }

            float targetHeight = maxLocal.y - minLocal.y;
            if (targetHeight <= boundRect.height)
            {
                if (minLocal.y < boundRect.yMin)
                    shift.y += (boundRect.yMin - minLocal.y);
                else if (maxLocal.y > boundRect.yMax)
                    shift.y += (boundRect.yMax - maxLocal.y);
            }

            if (shift.sqrMagnitude > 0.0001f)
            {
                targetRect.anchoredPosition += shift;
            }
        }
    }
}
