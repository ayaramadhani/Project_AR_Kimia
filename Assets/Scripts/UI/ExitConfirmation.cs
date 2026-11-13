
using UnityEngine;

public class ExitConfirmation : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private GameObject popupPanel;

    public GameObject PopupPanel => popupPanel;

    public void ConfigureReferences(GameObject panel)
    {
        popupPanel = panel;
    }

    // Menampilkan popup konfirmasi
    public void ShowPopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);
    }

    // Menutup popup ketika pengguna memilih Back
    public void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    // Keluar dari aplikasi
    public void ConfirmExit()
    {
        Debug.Log("Pengguna memilih Keluar.");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
