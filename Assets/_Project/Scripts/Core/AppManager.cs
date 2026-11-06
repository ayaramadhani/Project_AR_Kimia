using UnityEngine;

/// Penghubung utama ARScene: tap marker -> inventori -> panel gabung.
/// Catatan: memakai Input lama. Set Player Settings > Active Input Handling = "Both".
public class AppManager : MonoBehaviour
{
    public CombinePanelUI combinePanel;
    public Camera arCamera;
    public SceneLoader loader;

    readonly ElementInventory inventory = new();

    void Start()
    {
        combinePanel.Init(inventory);
        combinePanel.gameObject.SetActive(false);
        MarkerView.OnMarkerTapped += HandleTapped;
    }

    void OnDestroy() => MarkerView.OnMarkerTapped -= HandleTapped;

    void HandleTapped(ElementData e)
    {
        inventory.Add(e);
        combinePanel.gameObject.SetActive(true);
    }

    void Update()
    {
        Vector2 pos;
        bool began;

        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            began = t.phase == TouchPhase.Began;
            pos = t.position;
        }
        else
        {
            began = Input.GetMouseButtonDown(0); // untuk uji di editor
            pos = Input.mousePosition;
        }
        if (!began) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        var ray = arCamera.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out var hit))
        {
            var marker = hit.collider.GetComponentInParent<MarkerView>();
            if (marker != null) marker.Tap();
        }
    }

    // Dipanggil dari tombol bulat kiri atas
    public void OnHome()  => loader.Load("MainMenu");
    public void OnReset() { inventory.Clear(); }
}
