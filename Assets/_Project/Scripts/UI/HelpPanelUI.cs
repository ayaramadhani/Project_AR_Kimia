using UnityEngine;

public class HelpPanelUI : MonoBehaviour
{
    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
    public void Close()  => gameObject.SetActive(false);
}
