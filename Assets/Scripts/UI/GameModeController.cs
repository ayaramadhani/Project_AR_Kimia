
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeController : MonoBehaviour
{
    // Membuka Scene AR
    public void OpenAR()
    {
        Debug.Log("Tombol Mode AR ditekan. Membuka ARScene.");

        SceneManager.LoadScene("ARScene");
    }

    // Membuka Scene Quiz
    public void OpenQuiz()
    {
        Debug.Log("Tombol Mode Quiz ditekan.");

        // Scene Quiz belum dihubungkan.
    }

    // Membuka Scene Ensiklopedia
    public void OpenEncyclopedia()
    {
        Debug.Log("Tombol Ensiklopedia ditekan.");

        // Scene Ensiklopedia belum dihubungkan.
    }

    // Kembali ke Main Menu
    public void BackToMainMenu()
    {
        Debug.Log("Kembali ke MainMenu.");

        SceneManager.LoadScene("MainMenu");
    }
}
