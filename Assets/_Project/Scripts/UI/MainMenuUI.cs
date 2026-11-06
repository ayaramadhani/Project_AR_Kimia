using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public SceneLoader loader;
    public GameObject helpPanel;

    public void OnStart()     => loader.Load("ARScene");
    public void OnHelp()      => helpPanel.SetActive(true);
    public void OnCloseHelp() => helpPanel.SetActive(false);
    public void OnQuit()      => loader.Quit();
}
