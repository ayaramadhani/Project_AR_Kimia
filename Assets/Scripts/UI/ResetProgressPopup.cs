
using UnityEngine;

public class ResetProgressPopup : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private GameObject popupPanel;

    public void ShowPopup()
    {
        popupPanel.SetActive(true);
    }

    public void HidePopup()
    {
        popupPanel.SetActive(false);
    }

    public void ConfirmResetProgress()
    {
        ProgressSaveManager.ResetAllProgress();

        popupPanel.SetActive(false);

        Debug.Log("Progress berhasil di-reset.");
    }
}
