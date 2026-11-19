
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Exit Confirmation Popup")]
    [SerializeField] private GameObject exitPopupPanel;

    public void PlayGame()
    {
        SceneManager.LoadScene("GameMode");
    }

    public void OpenSettings()
    {
        Debug.Log("Tombol Pengaturan ditekan.");
    }

    // Menampilkan popup konfirmasi keluar.
    public void ShowExitPopup()
    {
        exitPopupPanel.SetActive(true);
    }

    // Menutup popup ketika tombol Tidak ditekan.
    public void CancelExit()
    {
        exitPopupPanel.SetActive(false);
    }

    // Keluar dari aplikasi ketika tombol Ya ditekan.
    public void ExitGame()
    {
        Debug.Log("Keluar dari aplikasi.");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
