
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

    // Membuka Scene Ensiklopedia (diarahkan ke ARScene dengan buku Ensiklopedia langsung terbuka)
    public void OpenEncyclopedia()
    {
        Debug.Log("Tombol Ensiklopedia ditekan. Membuka ARScene -> Ensiklopedia.");
        ARKimia.UI.EncyclopediaBookUI.OpenOnNextSceneLoad = true;
        SceneManager.LoadScene("ARScene");
    }

    // Kembali ke Main Menu
    public void BackToMainMenu()
    {
        Debug.Log("Kembali ke MainMenu.");

        SceneManager.LoadScene("MainMenu");
    }
}
