using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ARDraggable3D : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Kamera AR (kosongkan = Camera.main)")]
    [SerializeField] private Camera arCamera;

    [Header("Bidang gerak")]
    [Tooltip("ON: bergerak sejajar layar. OFF: bergerak di bidang datar (horizontal).")]
    [SerializeField] private bool moveParallelToScreen = true;

    [Header("Saat dilepas di atas panel kanan")]
    [SerializeField] private bool destroyOnDrop = true;
    public UnityEvent<GameObject> onDroppedOnPanel;

    private Plane dragPlane;
    private Vector3 grabOffset;

    private Camera Cam => arCamera != null ? arCamera : Camera.main;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Camera cam = Cam;
        if (cam == null) return;

        Vector3 normal = moveParallelToScreen ? -cam.transform.forward : Vector3.up;
        dragPlane = new Plane(normal, transform.position);

        Ray ray = cam.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float enter))
            grabOffset = transform.position - ray.GetPoint(enter);
        else
            grabOffset = Vector3.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Camera cam = Cam;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float enter))
            transform.position = ray.GetPoint(enter) + grabOffset;

        if (ARDropZone.Instance != null)
            ARDropZone.Instance.SetHighlight(ARDropZone.Instance.Contains(eventData.position));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var zone = ARDropZone.Instance;
        if (zone == null) return;

        zone.SetHighlight(false);

        if (zone.Contains(eventData.position))
        {
            onDroppedOnPanel?.Invoke(gameObject);

            if (destroyOnDrop)
                Destroy(gameObject);
        }
    }
}