using UnityEngine;

public class ARDropZone : MonoBehaviour
{
    public static ARDropZone Instance { get; private set; }

    [Header("Area drop (RectTransform panel kanan)")]
    [SerializeField] private RectTransform zoneRect;

    [Header("Opsional: objek highlight saat objek 3D di atas panel")]
    [SerializeField] private GameObject highlight;

    private Canvas zoneCanvas;

    private void Awake()
    {
        Instance = this;
        if (zoneRect == null) zoneRect = transform as RectTransform;
        zoneCanvas = zoneRect.GetComponentInParent<Canvas>();
        SetHighlight(false);
    }

    public bool Contains(Vector2 screenPos)
    {
        Camera cam = null;
        if (zoneCanvas != null && zoneCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = zoneCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(zoneRect, screenPos, cam);
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }
}