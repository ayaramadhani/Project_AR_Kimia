
using UnityEngine;
using UnityEngine.SceneManagement;

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

    // Menutup popup ketika pengguna memilih Tidak
    public void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    // Kembali ke MainMenu ketika pengguna memilih Ya
    public void ConfirmExit()
    {
        Debug.Log("Pengguna memilih kembali ke MainMenu.");

        SceneManager.LoadScene("MainMenu");
    }
}
