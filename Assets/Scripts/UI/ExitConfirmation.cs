
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitConfirmation : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private GameObject popupPanel;

    // Menampilkan popup konfirmasi
    public void ShowPopup()
    {
        popupPanel.SetActive(true);
    }

    // Menutup popup ketika pengguna memilih Tidak
    public void HidePopup()
    {
        popupPanel.SetActive(false);
    }

    // Kembali ke MainMenu ketika pengguna memilih Ya
    public void ConfirmExit()
    {
        Debug.Log("Pengguna memilih kembali ke MainMenu.");

        SceneManager.LoadScene("MainMenu");
    }
}
