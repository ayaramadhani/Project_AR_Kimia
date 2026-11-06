using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Auto load (untuk Splash)")]
    public bool autoLoad;
    public string autoLoadScene = "MainMenu";
    public float autoLoadDelay = 2f;

    void Start()
    {
        if (autoLoad) Invoke(nameof(AutoLoadNow), autoLoadDelay);
    }

    void AutoLoadNow() => SceneManager.LoadScene(autoLoadScene);
    public void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    public void Quit() => Application.Quit();
}
