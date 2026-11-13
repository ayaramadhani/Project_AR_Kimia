
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeController : MonoBehaviour
{
    // Membuka Scene AR
    public void OpenAR()
    {
        Debug.Log("Tombol Mode AR ditekan.");
        
        // Sementara belum memuat Scene.
        // Nama Scene AR akan ditambahkan setelah dipastikan.
    }

    // Membuka Scene Quiz
    public void OpenQuiz()
    {
        Debug.Log("Tombol Mode Quiz ditekan.");
        
        // Sementara belum memuat Scene.
    }

    // Membuka Scene Ensiklopedia
    public void OpenEncyclopedia()
    {
        Debug.Log("Tombol Ensiklopedia ditekan.");
        
        // Sementara belum memuat Scene.
    }

    // Kembali ke Main Menu
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
