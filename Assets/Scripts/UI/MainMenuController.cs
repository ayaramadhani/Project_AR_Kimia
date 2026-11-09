using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("GameMode");
    }

    public void OpenSettings()
    {
        Debug.Log("Tombol Pengaturan ditekan.");
    }

    public void ExitGame()
    {
        Debug.Log("Keluar dari aplikasi.");

        Application.Quit();
    }
}