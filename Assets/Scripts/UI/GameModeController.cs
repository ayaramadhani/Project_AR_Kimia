
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeController : MonoBehaviour
{
    [Header("Standalone Encyclopedia")]
    [SerializeField] private ARKimia.UI.EncyclopediaBookUI encyclopediaBook;

    public ARKimia.UI.EncyclopediaBookUI EncyclopediaBook => encyclopediaBook;

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

    // Membuka Ensiklopedia (di dalam scene GameMode jika sudah terpasang)
    public void OpenEncyclopedia()
    {
        if (encyclopediaBook != null)
        {
            Debug.Log("Tombol Ensiklopedia ditekan. Membuka buku Ensiklopedia di GameMode.");
            encyclopediaBook.OpenIndex();
            return;
        }

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
